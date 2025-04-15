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

namespace HoveringBallApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Popup windows
        private InputWindow _inputWindow;
        private ResponseWindow _responseWindow;

        // API client
        private GroqApiClient _apiClient;

        // Shadows the main window z-order
        private bool _isTopmost = false;

        public new event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Configure the window for transparency
            TransparencyLevelHint = new WindowTransparencyLevel[] { WindowTransparencyLevel.Transparent };
            Background = Brushes.Transparent;

            // Initialize API client with key
            _apiClient = new GroqApiClient("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");

            this.Loaded += MainWindow_Loaded;
            this.PositionChanged += MainWindow_PositionChanged;

            // Apply initial theme
            ApplyTheme(ThemeManager.Instance.CurrentTheme);
            ThemeManager.Instance.ThemeChanged += ThemeManager_ThemeChanged;
        }

        private void ThemeManager_ThemeChanged(object sender, AppTheme theme)
        {
            ApplyTheme(theme);
        }

        private void ApplyTheme(AppTheme theme)
        {
            var isDark = theme == AppTheme.Dark;

            // Update ball appearance
            if (isDark)
            {
                Ball.Fill = new SolidColorBrush(Color.Parse("#333333"));
                Ball.Stroke = new SolidColorBrush(Color.Parse("#CCCCCC"));
                ThemeToggle.Fill = new SolidColorBrush(Color.Parse("#CCCCCC"));
            }
            else
            {
                Ball.Fill = Brushes.White;
                Ball.Stroke = Brushes.Black;
                ThemeToggle.Fill = new SolidColorBrush(Color.Parse("#333333"));
            }
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

            _responseWindow = new ResponseWindow();

            // Initial z-order
            _isTopmost = this.Topmost;
            UpdatePopupZOrder();
        }

        private void UpdatePopupZOrder()
        {
            if (_inputWindow != null)
            {
                _inputWindow.Topmost = _isTopmost;
            }

            if (_responseWindow != null)
            {
                _responseWindow.Topmost = _isTopmost;
            }
        }

        private async void InputWindow_TextSubmitted(object sender, string text)
        {
            await SendToGroqApi(text);
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
        }

        private void Ball_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            // Check if this is the theme toggle in the center
            var position = e.GetPosition(Ball);
            double centerX = Ball.Width / 2;
            double centerY = Ball.Height / 2;
            double distance = Math.Sqrt(Math.Pow(position.X - centerX, 2) + Math.Pow(position.Y - centerY, 2));

            // If clicked near the center (theme toggle)
            if (distance < 10)
            {
                ThemeManager.Instance.ToggleTheme();
                e.Handled = true;
                return;
            }

            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                if (_inputWindow.IsVisible)
                {
                    // Hide input window
                    _inputWindow.Hide();
                }
                else
                {
                    // Show input window
                    _inputWindow.Show();
                    UpdatePopupPositions();
                    _inputWindow.FocusInput();
                }

                // Always allow dragging the ball
                BeginMoveDrag(e);
            }
        }

        private void ThemeToggle_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                ThemeManager.Instance.ToggleTheme();
                e.Handled = true;
            }
        }

        private async Task SendToGroqApi(string input)
        {
            try
            {
                // Update response window
                _responseWindow.ResponseContent = "Loading...";
                _responseWindow.Show();
                UpdatePopupPositions();

                // Send to API
                var response = await _apiClient.SendMessage(input);

                // Update response
                _responseWindow.ResponseContent = response;

                // Update position again after content may have changed size
                UpdatePopupPositions();
            }
            catch (Exception ex)
            {
                _responseWindow.ResponseContent = $"Error: {ex.Message}";
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}