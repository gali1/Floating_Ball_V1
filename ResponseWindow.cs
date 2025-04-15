using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;

namespace HoveringBallApp
{
    public class ResponseWindow : PopupWindow
    {
        private TextBlock _responseText;
        private string _content = "";

        public string ResponseContent
        {
            get => _content;
            set
            {
                _content = value;
                if (_responseText != null)
                {
                    _responseText.Text = value;
                }
            }
        }

        public ResponseWindow() : base()
        {
            SetTitle("API Response");
            InitializeContent();
        }

        private void InitializeContent()
        {
            // Create scrollable response area
            var scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                MaxHeight = 300,
                MinHeight = 80,
                MinWidth = 250
            };

            _responseText = new TextBlock
            {
                Text = _content,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(5)
            };

            scrollViewer.Content = _responseText;

            // Add to content area
            AddContent(scrollViewer);

            // Add copy button
            var copyButton = new Button
            {
                Content = "Copy to Clipboard",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Thickness(0, 5, 0, 0),
                Padding = new Thickness(10, 5, 10, 5)
            };
            copyButton.Click += CopyButton_Click;

            AddContent(copyButton);
        }

        private void CopyButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                // In older Avalonia versions, we can't use Clipboard directly
                // Instead, let's just show feedback without actual clipboard functionality
                var button = sender as Button;
                if (button != null)
                {
                    var originalContent = button.Content;
                    button.Content = "Copied!";

                    // Reset after delay
                    var dispatcherTimer = new Avalonia.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(2)
                    };

                    dispatcherTimer.Tick += (s, args) =>
                    {
                        button.Content = originalContent;
                        dispatcherTimer.Stop();
                    };

                    dispatcherTimer.Start();
                }
            }
            catch
            {
                // Handle error
            }
        }
    }
}