using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;

namespace HoveringBallApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Popup windows
        private InputWindow _inputWindow;
        private ResponseWindow _responseWindow;
        private SettingsWindow _settingsWindow;

        // API clients
        private GroqApiClient _groqApiClient;
        private OllamaClient _ollamaClient;
        private SearXNGClient _searxngClient;

        // Current API mode
        private ApiMode _currentApiMode = ApiMode.Groq;
        private string _currentModel = "llama3-8b-8192"; // Default Groq model

        // Settings
        private string _groqApiKey = "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";
        private string _ollamaUrl = "http://localhost:11434";
        private string _ollamaModel = "llama3";
        private string _fallbackOllamaModel = "deepseek-r1:1.5b"; // Fallback model
        private string _searxngUrl = "http://localhost:8080";
        private bool _webSearchEnabled = true;

        // Animation related
        private DispatcherTimer _pulseTimer;
        private bool _isPulsing = false;

        // Ball drag properties
        private bool _isDragging = false;
        private Point _dragStartPoint;

        // Tray functionality
        private bool _isInTray = false;

        // Context menu items
        private MenuItem _settingsMenuItem;
        private MenuItem _minimizeMenuItem;
        private MenuItem _resetPositionMenuItem;
        private MenuItem _exitMenuItem;

        public new event PropertyChangedEventHandler PropertyChanged;

        // Define available API modes
        public enum ApiMode
        {
            Groq,
            Ollama
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Configure the window for transparency
            TransparencyLevelHint = new WindowTransparencyLevel[] { WindowTransparencyLevel.Transparent };
            Background = Brushes.Transparent;

            // Initialize API clients
            InitializeApiClients();

            // Start SearXNG server process
            StartSearXNGServer();

            // Start Ollama server process
            StartOllamaServer();

            this.Loaded += MainWindow_Loaded;
            this.PositionChanged += MainWindow_PositionChanged;

            // Apply initial theme
            ApplyTheme(ThemeManager.Instance.CurrentTheme);
            ThemeManager.Instance.ThemeChanged += ThemeManager_ThemeChanged;

            // Initialize pulse animation timer
            _pulseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _pulseTimer.Tick += PulseTimer_Tick;
        }

        private void InitializeApiClients()
        {
            _groqApiClient = new GroqApiClient(_groqApiKey);
            _ollamaClient = new OllamaClient(_ollamaUrl);
            _searxngClient = new SearXNGClient(_searxngUrl);
        }

        private void StartSearXNGServer()
        {
            try
            {
                // For Windows: Check if SearXNG is installed and start it
                if (OperatingSystem.IsWindows())
                {
                    // This is just a placeholder - in a real implementation, you'd use
                    // Process.Start() to launch SearXNG or check if it's running
                    Console.WriteLine("SearXNG server would be started on Windows...");
                }
                // For Linux: Check if SearXNG is running, if not try to start with systemd or docker
                else if (OperatingSystem.IsLinux())
                {
                    Console.WriteLine("SearXNG server would be started on Linux...");
                }
                // For macOS
                else if (OperatingSystem.IsMacOS())
                {
                    Console.WriteLine("SearXNG server would be started on macOS...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start SearXNG: {ex.Message}");
            }
        }

        private void StartOllamaServer()
        {
            try
            {
                // For Windows: Check if Ollama is installed and start it
                if (OperatingSystem.IsWindows())
                {
                    Console.WriteLine("Ollama server would be started on Windows...");
                }
                // For Linux: Check if Ollama is running, if not try to start with systemd or docker
                else if (OperatingSystem.IsLinux())
                {
                    Console.WriteLine("Ollama server would be started on Linux...");
                }
                // For macOS
                else if (OperatingSystem.IsMacOS())
                {
                    Console.WriteLine("Ollama server would be started on macOS...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start Ollama: {ex.Message}");
            }
        }

        private void ThemeManager_ThemeChanged(object sender, AppTheme theme)
        {
            ApplyTheme(theme);
        }

        private void ApplyTheme(AppTheme theme)
        {
            // Theme is now handled via styles in App.axaml
            // This method is just for additional customization if needed
        }

        private void MainWindow_PositionChanged(object sender, PixelPointEventArgs e)
        {
            // Update popup positions when main window moves
            UpdatePopupPositions();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Create popup windows
            _inputWindow = new InputWindow();
            _inputWindow.TextSubmitted += InputWindow_TextSubmitted;
            _inputWindow.ApiSelectionChanged += InputWindow_ApiSelectionChanged;

            _responseWindow = new ResponseWindow();

            _settingsWindow = new SettingsWindow();
            _settingsWindow.SettingsChanged += SettingsWindow_SettingsChanged;

            // Apply initial animation
            AnimateBallAppearance();

            // Make sure the window can be dragged
            this.PointerPressed += (s, args) =>
            {
                if (args.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                {
                    _isDragging = true;
                    _dragStartPoint = args.GetPosition(this);
                    args.Handled = true;
                }
            };

            // Connect to context menu items
            InitializeContextMenuEvents();

            Console.WriteLine("MainWindow loaded successfully");
        }

        private void InitializeContextMenuEvents()
        {
            // Find the context menu items
            _settingsMenuItem = this.FindControl<MenuItem>("SettingsMenuItem");
            _minimizeMenuItem = this.FindControl<MenuItem>("MinimizeMenuItem");
            _resetPositionMenuItem = this.FindControl<MenuItem>("ResetPositionMenuItem");
            _exitMenuItem = this.FindControl<MenuItem>("ExitMenuItem");

            // Add event handlers
            if (_settingsMenuItem != null)
                _settingsMenuItem.Click += (s, e) => ShowSettingsWindow();

            if (_minimizeMenuItem != null)
                _minimizeMenuItem.Click += (s, e) => MinimizeToTray();

            if (_resetPositionMenuItem != null)
                _resetPositionMenuItem.Click += (s, e) => ResetPosition();

            if (_exitMenuItem != null)
                _exitMenuItem.Click += (s, e) => CloseApplication();
        }

        private void ShowSettingsWindow()
        {
            if (!_settingsWindow.IsVisible)
            {
                _settingsWindow.Show();
                UpdatePopupPositions();
            }
            else
            {
                _settingsWindow.Hide();
            }
        }

        private void MinimizeToTray()
        {
            // In a real implementation, this would minimize to the system tray
            // For this example, we'll just hide the window
            this.Hide();
            _isInTray = true;

            // In a real implementation, you'd create a system tray icon here
        }

        private void ResetPosition()
        {
            // Center the window on the screen
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Position = new PixelPoint(
                (int)(Screens.Primary.Bounds.Width / 2 - Width / 2),
                (int)(Screens.Primary.Bounds.Height / 2 - Height / 2)
            );
        }

        private void CloseApplication()
        {
            // Close all popup windows first
            if (_inputWindow != null && _inputWindow.IsVisible)
                _inputWindow.Close();

            if (_responseWindow != null && _responseWindow.IsVisible)
                _responseWindow.Close();

            if (_settingsWindow != null && _settingsWindow.IsVisible)
                _settingsWindow.Close();

            // Close the main window - this should exit the application
            // in most Avalonia applications
            this.Close();

            // If for some reason the application is still running after
            // closing all windows, use Environment.Exit as a fallback
            Environment.Exit(0);
        }

        // New API selection changed handler
        private void InputWindow_ApiSelectionChanged(object sender, ApiSelectionChangedEventArgs e)
        {
            if (e.ApiName == "Groq")
            {
                _currentApiMode = ApiMode.Groq;
                _currentModel = e.ModelName ?? "llama3-8b-8192";
            }
            else if (e.ApiName == "Ollama")
            {
                _currentApiMode = ApiMode.Ollama;
                _currentModel = e.ModelName ?? _ollamaModel;
            }
        }

        private void AnimateBallAppearance()
        {
            // Find the Ball button
            var ballButton = this.FindControl<Button>("BallButton");
            if (ballButton == null) return;

            // Simplified animation
            ballButton.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

            // Create a scale transform with initial scale of 0
            var scaleTransform = new ScaleTransform(0, 0);
            ballButton.RenderTransform = scaleTransform;

            // Create a timer to animate the scale
            var animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60fps
            };

            double progress = 0;
            animationTimer.Tick += (s, e) =>
            {
                progress += 0.02; // Adjust speed

                if (progress >= 1.0)
                {
                    scaleTransform.ScaleX = 1.0;
                    scaleTransform.ScaleY = 1.0;
                    animationTimer.Stop();
                }
                else
                {
                    // Easing function (ease-out)
                    double easedProgress = 1 - Math.Pow(1 - progress, 3);
                    scaleTransform.ScaleX = easedProgress;
                    scaleTransform.ScaleY = easedProgress;
                }
            };

            animationTimer.Start();
        }

        // Ball click handler
        public void Ball_Clicked(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Ball clicked!");

            // Show input window on click
            if (!_inputWindow.IsVisible)
            {
                _inputWindow.Show();

                // Set the current API mode and model in the input window
                _inputWindow.SetApiSelection(
                    _currentApiMode == ApiMode.Groq ? "Groq" : "Ollama",
                    _currentModel
                );

                UpdatePopupPositions();
                _inputWindow.FocusInput();
            }
            else
            {
                _inputWindow.Hide();
            }

            e.Handled = true;
        }

        private async void InputWindow_TextSubmitted(object sender, string text)
        {
            await SendToApi(text);
        }

        private void SettingsWindow_SettingsChanged(object sender, EventArgs e)
        {
            // Update settings based on SettingsWindow changes
            if (_settingsWindow.GroqApiKey != null)
            {
                _groqApiKey = _settingsWindow.GroqApiKey;
                if (_currentApiMode == ApiMode.Groq)
                {
                    _groqApiClient = new GroqApiClient(_groqApiKey);
                }
            }

            if (_settingsWindow.OllamaUrl != null)
            {
                _ollamaUrl = _settingsWindow.OllamaUrl;
                if (_currentApiMode == ApiMode.Ollama)
                {
                    _ollamaClient = new OllamaClient(_ollamaUrl);
                }
            }

            if (_settingsWindow.OllamaModel != null)
            {
                _ollamaModel = _settingsWindow.OllamaModel;
            }

            if (_settingsWindow.SearxngUrl != null)
            {
                _searxngUrl = _settingsWindow.SearxngUrl;
                _searxngClient = new SearXNGClient(_searxngUrl);
            }

            _webSearchEnabled = _settingsWindow.WebSearchEnabled;
            _currentApiMode = _settingsWindow.CurrentApiMode;
        }

        private void UpdatePopupPositions()
        {
            if (_inputWindow != null && _inputWindow.IsVisible)
            {
                var mainPos = this.Position;

                // Position at bottom right of the ball
                _inputWindow.Position = new PixelPoint(
                    mainPos.X + 40, // Position to the right of the ball
                    mainPos.Y + 40  // Position at the bottom of the ball
                );
            }

            if (_responseWindow != null && _responseWindow.IsVisible)
            {
                var mainPos = this.Position;
                var responseHeight = _responseWindow.Bounds.Height > 0 ?
                    (int)_responseWindow.Bounds.Height : 100;

                // Position at top right of the ball
                _responseWindow.Position = new PixelPoint(
                    mainPos.X + 40, // Position to the right of the ball
                    mainPos.Y - responseHeight  // Position above the ball
                );
            }

            if (_settingsWindow != null && _settingsWindow.IsVisible)
            {
                var mainPos = this.Position;
                var settingsHeight = _settingsWindow.Bounds.Height > 0 ?
                    (int)_settingsWindow.Bounds.Height : 150;

                // Position at top left of the ball
                _settingsWindow.Position = new PixelPoint(
                    mainPos.X - (int)_settingsWindow.Bounds.Width - 10, // Position to the left of the ball
                    mainPos.Y - settingsHeight / 2 + 40  // Position centered relative to the ball
                );
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            if (_isDragging)
            {
                var currentPoint = e.GetPosition(this);
                var delta = currentPoint - _dragStartPoint;

                Position = new PixelPoint(
                    Position.X + (int)delta.X,
                    Position.Y + (int)delta.Y
                );

                UpdatePopupPositions();
                e.Handled = true;
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            _isDragging = false;
            e.Handled = true;
        }

        private async Task SendToApi(string input)
        {
            try
            {
                // Start pulsing animation to indicate processing
                StartPulsingAnimation();

                // Update response window
                _responseWindow.ResponseContent = "Loading...";
                _responseWindow.Show();
                UpdatePopupPositions();

                string response = string.Empty;

                // Use the current API mode and model
                try
                {
                    if (_currentApiMode == ApiMode.Groq)
                    {
                        // Get the currently selected model from the input window
                        response = await _groqApiClient.SendMessage(input, _currentModel);
                    }
                    else
                    {
                        // Use Ollama with selected model
                        response = await _ollamaClient.SendMessage(input, _currentModel);
                    }
                }
                catch (HttpRequestException ex)
                {
                    // If there's a connection issue with Groq, fall back to Ollama
                    if (_currentApiMode == ApiMode.Groq)
                    {
                        Console.WriteLine($"Groq API connection error: {ex.Message}. Falling back to Ollama.");

                        // Set up a notification in the response
                        response = $"**Note: Groq API connection failed. Falling back to local Ollama model.**\n\n";

                        // Use the fallback model
                        response += await _ollamaClient.SendMessage(input, _fallbackOllamaModel);
                    }
                    else
                    {
                        // If Ollama was the primary choice and failed, rethrow
                        throw;
                    }
                }

                // Stop pulsing animation
                StopPulsingAnimation();

                // Update response
                _responseWindow.ResponseContent = response;

                // Update position again after content may have changed size
                UpdatePopupPositions();
            }
            catch (Exception ex)
            {
                StopPulsingAnimation();
                _responseWindow.ResponseContent = $"Error: {ex.Message}";
            }
        }

        private void StartPulsingAnimation()
        {
            _isPulsing = true;
            _pulseTimer.Start();
        }

        private void StopPulsingAnimation()
        {
            _isPulsing = false;
            _pulseTimer.Stop();

            // Reset ball scale
            var ballButton = this.FindControl<Button>("BallButton");
            if (ballButton != null)
            {
                var scaleTransform = ballButton.RenderTransform as ScaleTransform;
                if (scaleTransform != null)
                {
                    scaleTransform.ScaleX = 1.0;
                    scaleTransform.ScaleY = 1.0;
                }
            }
        }

        private void PulseTimer_Tick(object sender, EventArgs e)
        {
            if (!_isPulsing) return;

            // Get reference to the Ball
            var ballButton = this.FindControl<Button>("BallButton");
            if (ballButton == null) return;

            // Create pulse effect while processing
            double time = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) / 1000.0;
            double scale = 1.0 + 0.05 * Math.Sin(time * Math.PI * 2);

            var scaleTransform = ballButton.RenderTransform as ScaleTransform;
            if (scaleTransform == null)
            {
                scaleTransform = new ScaleTransform(scale, scale);
                ballButton.RenderTransform = scaleTransform;
            }
            else
            {
                scaleTransform.ScaleX = scale;
                scaleTransform.ScaleY = scale;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}