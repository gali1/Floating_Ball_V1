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
using Avalonia.Animation;
using Avalonia.Styling;

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
        private DispatcherTimer _hoverTimer;
        private bool _isPulsing = false;
        private bool _isHovering = false;
        private readonly TimeSpan _hoverDelay = TimeSpan.FromSeconds(0.2);

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

        // UI elements
        private Button _ballButton;
        private Ellipse _ballEllipse;
        private Ellipse _themeToggle;

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

            // Initialize hover timer for delayed visual feedback
            _hoverTimer = new DispatcherTimer
            {
                Interval = _hoverDelay
            };
            _hoverTimer.Tick += HoverTimer_Tick;
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
            // Get references to UI elements if not already obtained
            if (_ballEllipse == null && _ballButton != null)
            {
                _ballEllipse = _ballButton.FindControl<Ellipse>("Ball");
                _themeToggle = _ballButton.FindControl<Ellipse>("ThemeToggle");
            }

            // Apply theme classes to ball
            if (_ballEllipse != null)
            {
                // Remove all theme classes first
                _ballEllipse.Classes.Remove("Light");
                _ballEllipse.Classes.Remove("Dark");
                _ballEllipse.Classes.Remove("Brown");

                // Add appropriate theme class
                switch (theme)
                {
                    case AppTheme.Light:
                        _ballEllipse.Classes.Add("Light");
                        break;
                    case AppTheme.Dark:
                        _ballEllipse.Classes.Add("Dark");
                        break;
                    case AppTheme.Brown:
                        _ballEllipse.Classes.Add("Brown");
                        break;
                }

                // Add animation to make the transition smooth
                var scaleAnimation = new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(300),
                    FillMode = FillMode.Forward
                };

                scaleAnimation.Children.Add(new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters = { new Setter(ScaleTransform.ScaleXProperty, 0.95), new Setter(ScaleTransform.ScaleYProperty, 0.95) }
                });

                scaleAnimation.Children.Add(new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = { new Setter(ScaleTransform.ScaleXProperty, 1.0), new Setter(ScaleTransform.ScaleYProperty, 1.0) }
                });

                // Ensure we have a scale transform
                if (_ballButton.RenderTransform == null || !(_ballButton.RenderTransform is ScaleTransform))
                {
                    _ballButton.RenderTransform = new ScaleTransform(1, 1);
                }

                scaleAnimation.RunAsync((Animatable)_ballButton.RenderTransform);

                // Update theme toggle appearance
                if (_themeToggle != null)
                {
                    // Theme-specific colors for the toggle
                    switch (theme)
                    {
                        case AppTheme.Light:
                            _themeToggle.Fill = new SolidColorBrush(Color.Parse("#333333"));
                            break;
                        case AppTheme.Dark:
                            _themeToggle.Fill = new SolidColorBrush(Color.Parse("#AAAAAA"));
                            break;
                        case AppTheme.Brown:
                            _themeToggle.Fill = new SolidColorBrush(Color.Parse("#F5DEB3"));
                            break;
                    }
                }
            }
        }

        private void MainWindow_PositionChanged(object sender, PixelPointEventArgs e)
        {
            // Update popup positions when main window moves
            UpdatePopupPositions();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Get references to UI elements
            _ballButton = this.FindControl<Button>("BallButton");
            if (_ballButton != null)
            {
                _ballEllipse = _ballButton.FindControl<Ellipse>("Ball");
                _themeToggle = _ballButton.FindControl<Ellipse>("ThemeToggle");

                // Add theme switching on theme toggle click
                if (_themeToggle != null)
                {
                    _themeToggle.PointerPressed += ThemeToggle_PointerPressed;
                    _themeToggle.PointerEntered += (s, args) =>
                    {
                        _themeToggle.Opacity = 0.8;
                    };
                    _themeToggle.PointerExited += (s, args) =>
                    {
                        _themeToggle.Opacity = 1.0;
                    };
                }

                // Add hover effects
                _ballButton.PointerEntered += (s, args) =>
                {
                    _isHovering = true;
                    _hoverTimer.Start();
                };

                _ballButton.PointerExited += (s, args) =>
                {
                    _isHovering = false;
                    _hoverTimer.Stop();

                    // Reset any hover effects if we were hovering
                    if (_ballEllipse != null && _ballEllipse.Effect is DropShadowEffect hoverEffect)
                    {
                        var fadeAnimation = new Animation
                        {
                            Duration = TimeSpan.FromMilliseconds(200),
                            FillMode = FillMode.Forward
                        };

                        fadeAnimation.Children.Add(new KeyFrame
                        {
                            Cue = new Cue(0.0),
                            Setters = { new Setter(DropShadowEffect.BlurRadiusProperty, hoverEffect.BlurRadius) }
                        });

                        fadeAnimation.Children.Add(new KeyFrame
                        {
                            Cue = new Cue(1.0),
                            Setters = { new Setter(DropShadowEffect.BlurRadiusProperty, 10.0) }
                        });

                        fadeAnimation.RunAsync((Animatable)_ballEllipse.Effect);
                    }
                };
            }

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


        private void ThemeToggle_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // Toggle theme
            ThemeManager.Instance.ToggleTheme();

            // Add a pulse animation for feedback
            if (_themeToggle != null)
            {
                AnimationHelper.AnimateBounce(_themeToggle, 1.0, 1.0, 0.5, TimeSpan.FromMilliseconds(400));
            }

            e.Handled = true;
        }

        private void HoverTimer_Tick(object sender, EventArgs e)
        {
            if (_isHovering && _ballEllipse != null)
            {
                _hoverTimer.Stop();

                // Create a glow effect by adjusting the DropShadow properties directly
                if (_ballEllipse.Effect is DropShadowEffect effect)
                {
                    // Animate the blur radius
                    int steps = 15;
                    double startBlur = effect.BlurRadius;
                    double endBlur = 15.0;
                    int currentStep = 0;

                    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
                    timer.Tick += (s, args) =>
                    {
                        currentStep++;
                        if (currentStep > steps)
                        {
                            effect.BlurRadius = endBlur;
                            timer.Stop();
                            return;
                        }

                        double progress = (double)currentStep / steps;
                        double easedProgress = AnimationHelper.EaseOutCubic(progress);
                        effect.BlurRadius = startBlur + (endBlur - startBlur) * easedProgress;
                    };

                    timer.Start();
                }
                else
                {
                    // Create effect if it doesn't exist
                    var shadowEffect = new DropShadowEffect
                    {
                        BlurRadius = 15,
                        Opacity = 0.3,
                        OffsetX = 0,
                        OffsetY = 0
                    };
                    _ballEllipse.Effect = shadowEffect;
                }
            }
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
            // Animate the minimization with opacity and scale
            AnimationHelper.AnimateScale(this, 1.0, 0.3, TimeSpan.FromMilliseconds(300));
            AnimationHelper.AnimateFade(this, 1.0, 0.0, TimeSpan.FromMilliseconds(300), () =>
            {
                // Reset properties and hide
                this.Hide();
                this.Opacity = 1.0;
                if (this.RenderTransform is ScaleTransform st)
                {
                    st.ScaleX = 1.0;
                    st.ScaleY = 1.0;
                }
            });

            _isInTray = true;
        }

        private void ResetPosition()
        {
            // Calculate target position
            PixelPoint targetPosition = new PixelPoint(
                (int)(Screens.Primary.Bounds.Width / 2 - Width / 2),
                (int)(Screens.Primary.Bounds.Height / 2 - Height / 2)
            );

            // Use a timer to animate the position
            int steps = 30;
            int currentStep = 0;
            var startPosition = Position;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            timer.Tick += (s, e) =>
            {
                currentStep++;
                if (currentStep > steps)
                {
                    Position = targetPosition;
                    timer.Stop();
                    return;
                }

                double progress = (double)currentStep / steps;
                double easedProgress = AnimationHelper.EaseOutCubic(progress);

                int newX = startPosition.X + (int)((targetPosition.X - startPosition.X) * easedProgress);
                int newY = startPosition.Y + (int)((targetPosition.Y - startPosition.Y) * easedProgress);

                Position = new PixelPoint(newX, newY);
            };

            timer.Start();
        }
        private void CloseApplication()
        {
            // Animate closing
            AnimationHelper.AnimateScale(this, 1.0, 0.0, TimeSpan.FromMilliseconds(300));
            AnimationHelper.AnimateFade(this, 1.0, 0.0, TimeSpan.FromMilliseconds(300), () =>
            {
                // Close all popup windows
                if (_inputWindow != null && _inputWindow.IsVisible)
                    _inputWindow.Close();

                if (_responseWindow != null && _responseWindow.IsVisible)
                    _responseWindow.Close();

                if (_settingsWindow != null && _settingsWindow.IsVisible)
                    _settingsWindow.Close();

                // Close main window
                this.Close();

                // Fallback exit
                Environment.Exit(0);
            });
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

            // Enhanced animation using the helper
            ballButton.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            ballButton.RenderTransform = new ScaleTransform(0, 0);

            // Animate from 0 to 1 with a slight bounce
            AnimationHelper.AnimateBounce(ballButton, 0.0, 1.0, 0.15, TimeSpan.FromMilliseconds(800));
        }
        // Ball click handler
        public void Ball_Clicked(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Ball clicked!");

            // Simple non-animation approach to avoid the casting error
            if (_ballButton != null)
            {
                // Ensure the button has a RenderTransformOrigin set
                _ballButton.RenderTransformOrigin = RelativePoint.Center;

                // Create the ScaleTransform if it doesn't exist
                if (_ballButton.RenderTransform == null || !(_ballButton.RenderTransform is ScaleTransform))
                {
                    _ballButton.RenderTransform = new ScaleTransform(1, 1);
                }

                // Use a timer-based animation to achieve the effect
                var scaleTransform = _ballButton.RenderTransform as ScaleTransform;
                if (scaleTransform != null)
                {
                    // Simple feedback without animation
                    scaleTransform.ScaleX = 0.95;
                    scaleTransform.ScaleY = 0.95;

                    // Reset after a short delay
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
                    timer.Tick += (s, args) =>
                    {
                        scaleTransform.ScaleX = 1.0;
                        scaleTransform.ScaleY = 1.0;
                        timer.Stop();
                    };
                    timer.Start();
                }
            }

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

                // Position at bottom right of the ball with adjusted offset
                _inputWindow.Position = new PixelPoint(
                    mainPos.X + 50, // Position to the right of the ball
                    mainPos.Y + 50  // Position at the bottom of the ball
                );
            }

            if (_responseWindow != null && _responseWindow.IsVisible)
            {
                var mainPos = this.Position;
                var responseHeight = _responseWindow.Bounds.Height > 0 ?
                    (int)_responseWindow.Bounds.Height : 100;

                // Position at top right of the ball with adjusted offset
                _responseWindow.Position = new PixelPoint(
                    mainPos.X + 50, // Position to the right of the ball
                    mainPos.Y - responseHeight - 10  // Position above the ball with margin
                );
            }

            if (_settingsWindow != null && _settingsWindow.IsVisible)
            {
                var mainPos = this.Position;
                var settingsHeight = _settingsWindow.Bounds.Height > 0 ?
                    (int)_settingsWindow.Bounds.Height : 150;

                // Position at left of the ball with adjusted offset
                _settingsWindow.Position = new PixelPoint(
                    mainPos.X - (int)_settingsWindow.Bounds.Width - 20, // Position to the left of the ball with margin
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

            // Get reference to the Ball
            var ballButton = this.FindControl<Button>("BallButton");
            if (ballButton == null) return;

            // Use the helper to create a pulsing animation
            _pulseTimer = AnimationHelper.AnimatePulse(ballButton, 1.0, 0.08, 2.0);
        }

        private void StopPulsingAnimation()
        {
            _isPulsing = false;
            if (_pulseTimer != null)
                _pulseTimer.Stop();

            // Reset ball scale with smooth animation
            var ballButton = this.FindControl<Button>("BallButton");
            if (ballButton != null)
            {
                AnimationHelper.AnimateScale(ballButton,
                    ballButton.RenderTransform is ScaleTransform st ? st.ScaleX : 1.0,
                    1.0,
                    TimeSpan.FromMilliseconds(300));
            }
        }

        private void PulseTimer_Tick(object sender, EventArgs e)
        {
            if (!_isPulsing) return;

            // Get reference to the Ball
            var ballButton = this.FindControl<Button>("BallButton");
            if (ballButton == null) return;

            // Create enhanced pulse effect while processing
            double time = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) / 1000.0;
            double scale = 1.0 + 0.08 * Math.Sin(time * Math.PI * 2);

            var scaleTransform = ballButton.RenderTransform as ScaleTransform;
            if (scaleTransform == null)
            {
                scaleTransform = new ScaleTransform(scale, scale);
                ballButton.RenderTransform = scaleTransform;
                ballButton.RenderTransformOrigin = RelativePoint.Center;
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