using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System;
using System.Threading.Tasks;

namespace HoveringBallApp
{
    public class InputWindow : PopupWindow
    {
        private TextBox _inputText;

        public event EventHandler<string> TextSubmitted;

        public TextBox InputText => _inputText;

        public InputWindow() : base()
        {
            SetTitle("Enter Query");
            InitializeContent();
        }

        private void InitializeContent()
        {
            // Create expandable input textbox
            _inputText = new TextBox
            {
                Classes = { "PopupInput" },
                Watermark = "Type your message...",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 60,
                MinWidth = 230,
                Foreground = new SolidColorBrush(Colors.White),
                Background = new SolidColorBrush(Color.Parse("#444444")),
                CaretBrush = new SolidColorBrush(Colors.White)
            };

            _inputText.KeyDown += InputText_KeyDown;

            // Add a submit button
            var submitButton = new Button
            {
                Content = "Submit",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Thickness(0, 5, 0, 0),
                Padding = new Thickness(10, 5, 10, 5),
                Foreground = new SolidColorBrush(Colors.White),
                Background = new SolidColorBrush(Color.Parse("#444444"))
            };
            submitButton.Click += SubmitButton_Click;

            // Add to content area
            AddContent(_inputText);
            AddContent(submitButton);
        }

        protected override void ApplyTheme(AppTheme theme)
        {
            base.ApplyTheme(theme);

            // Explicitly set text colors based on theme
            var isDark = theme == AppTheme.Dark;

            if (_inputText != null)
            {
                if (isDark)
                {
                    _inputText.Foreground = new SolidColorBrush(Colors.White);
                    _inputText.Background = new SolidColorBrush(Color.Parse("#444444"));
                    _inputText.CaretBrush = new SolidColorBrush(Colors.White);
                }
                else
                {
                    _inputText.Foreground = new SolidColorBrush(Color.Parse("#333333"));
                    _inputText.Background = new SolidColorBrush(Colors.White);
                    _inputText.CaretBrush = new SolidColorBrush(Color.Parse("#333333"));
                }
            }
        }

        private void InputText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                SubmitText();
                e.Handled = true;
            }
        }

        private void SubmitButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            SubmitText();
        }

        private void SubmitText()
        {
            if (!string.IsNullOrWhiteSpace(_inputText.Text))
            {
                var text = _inputText.Text;
                _inputText.Text = string.Empty;

                // Notify the main window
                TextSubmitted?.Invoke(this, text);

                // Hide this window
                this.Hide();
            }
        }

        public void FocusInput()
        {
            _inputText.Focus();
        }
    }
}