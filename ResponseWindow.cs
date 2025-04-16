using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using System.IO;

namespace HoveringBallApp
{
    public class ResponseWindow : PopupWindow
    {
        private StackPanel _contentPanel;
        private ScrollViewer _scrollViewer;
        private string _content = "";
        private AppTheme _currentTheme;
        private Border _statusBorder;
        private TextBlock _statusText;
        private DispatcherTimer _statusTimer;

        public string ResponseContent
        {
            get => _content;
            set
            {
                _content = value;
                if (_contentPanel != null)
                {
                    ProcessAndDisplayContent(value);
                }
            }
        }

        public ResponseWindow() : base()
        {
            SetTitle("Assistant Response");
            InitializeContent();

            // Subscribe to theme changes
            ThemeManager.Instance.ThemeChanged += (sender, theme) => {
                _currentTheme = theme;
                ProcessAndDisplayContent(_content); // Refresh with new theme
            };
            _currentTheme = ThemeManager.Instance.CurrentTheme;

            // Initialize status notification timer
            _statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _statusTimer.Tick += (s, e) => {
                if (_statusBorder != null)
                {
                    _statusBorder.IsVisible = false;
                }
                _statusTimer.Stop();
            };

            this.Width = 400;
            this.MaxWidth = 800;
            this.MinWidth = 300;

            // Handle resize events
            this.PropertyChanged += (sender, args) => {
                if (args.Property == BoundsProperty)
                {
                    // Refresh content when window is resized
                    ProcessAndDisplayContent(_content);
                }
            };
        }

        private void InitializeContent()
        {
            // Create main layout grid
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Status notification area (initially hidden)
            _statusBorder = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#4CAF50")),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Center,
                IsVisible = false
            };

            _statusText = new TextBlock
            {
                Foreground = Brushes.White,
                FontWeight = FontWeight.Medium,
                TextAlignment = TextAlignment.Center
            };

            _statusBorder.Child = _statusText;
            Grid.SetRow(_statusBorder, 0);
            mainGrid.Children.Add(_statusBorder);

            // Create scrollable response area
            _scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                MaxHeight = 500,
                MinHeight = 200
            };

            _contentPanel = new StackPanel
            {
                Spacing = 8,
                Margin = new Thickness(5)
            };

            _scrollViewer.Content = _contentPanel;
            Grid.SetRow(_scrollViewer, 1);
            mainGrid.Children.Add(_scrollViewer);

            // Add action buttons panel
            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 10,
                Margin = new Thickness(0, 8, 0, 0)
            };

            var copyButton = new Button
            {
                Content = "Copy All",
                Padding = new Thickness(10, 5, 10, 5)
            };
            copyButton.Click += CopyAllButton_Click;

            var saveButton = new Button
            {
                Content = "Save As...",
                Padding = new Thickness(10, 5, 10, 5)
            };
            saveButton.Click += SaveButton_Click;

            var clearButton = new Button
            {
                Content = "Clear",
                Padding = new Thickness(10, 5, 10, 5)
            };
            clearButton.Click += ClearButton_Click;

            buttonsPanel.Children.Add(copyButton);
            buttonsPanel.Children.Add(saveButton);
            buttonsPanel.Children.Add(clearButton);

            Grid.SetRow(buttonsPanel, 2);
            mainGrid.Children.Add(buttonsPanel);

            // Add to content area
            AddContent(mainGrid);
        }

        private void ProcessAndDisplayContent(string content)
        {
            _contentPanel.Children.Clear();

            if (string.IsNullOrEmpty(content))
            {
                return;
            }

            // Check if content is "Loading..."
            if (content == "Loading...")
            {
                var loadingText = new TextBlock
                {
                    Text = "Loading...",
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(10)
                };

                _contentPanel.Children.Add(loadingText);
                return;
            }

            // Process for code blocks - detect markdown style ```language ... ``` blocks
            var codePattern = new Regex(@"```([a-zA-Z]*)\s*\n([\s\S]*?)\n```", RegexOptions.Multiline);
            var matches = codePattern.Matches(content);

            if (matches.Count > 0)
            {
                int lastIndex = 0;
                foreach (Match match in matches)
                {
                    // Add text before code block
                    string textBefore = content.Substring(lastIndex, match.Index - lastIndex);
                    if (!string.IsNullOrWhiteSpace(textBefore))
                    {
                        _contentPanel.Children.Add(CreateTextBlock(textBefore));
                    }

                    // Add code block
                    string language = match.Groups[1].Value;
                    string code = match.Groups[2].Value;
                    _contentPanel.Children.Add(CreateCodeBlock(code, language));

                    lastIndex = match.Index + match.Length;
                }

                // Add any remaining text
                if (lastIndex < content.Length)
                {
                    string textAfter = content.Substring(lastIndex);
                    if (!string.IsNullOrWhiteSpace(textAfter))
                    {
                        _contentPanel.Children.Add(CreateTextBlock(textAfter));
                    }
                }
            }
            else
            {
                // No code blocks, just add the text
                _contentPanel.Children.Add(CreateTextBlock(content));
            }
        }

        private TextBlock CreateTextBlock(string text)
        {
            return new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(5)
            };
        }

        private Panel CreateCodeBlock(string code, string language)
        {
            // Create container with code and copy button
            var container = new Grid();
            container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            container.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Language indicator and copy button
            var headerPanel = new Grid();
            headerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            headerPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Language label
            var languageLabel = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(language) ? "code" : language,
                Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")),
                Margin = new Thickness(8, 4, 0, 0),
                FontSize = 12
            };
            Grid.SetColumn(languageLabel, 0);

            // Copy button
            var copyButton = new Button
            {
                Content = "Copy",
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(0, 2, 5, 2),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            copyButton.Click += (sender, e) => CopyCodeToClipboard(code);
            Grid.SetColumn(copyButton, 1);

            headerPanel.Children.Add(languageLabel);
            headerPanel.Children.Add(copyButton);
            Grid.SetRow(headerPanel, 0);

            // Code content
            var codeBorder = new Border
            {
                Classes = { "CodeBlock" },
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 5)
            };

            var codeText = new TextBlock
            {
                Classes = { "CodeText" },
                Text = code,
                TextWrapping = TextWrapping.Wrap,
                FontFamily = new FontFamily("Consolas, Menlo, Monaco, 'Courier New', monospace")
            };

            // Apply syntax highlighting based on language
            if (!string.IsNullOrWhiteSpace(language))
            {
                ApplySyntaxHighlighting(codeText, code, language);
            }

            codeBorder.Child = codeText;
            Grid.SetRow(codeBorder, 1);

            container.Children.Add(headerPanel);
            container.Children.Add(codeBorder);

            return container;
        }

        private void ApplySyntaxHighlighting(TextBlock textBlock, string code, string language)
        {
            // Basic syntax highlighting would typically be handled by a proper library
            // This is a simplified version just to show the concept

            // For now, just set a color based on language
            switch (language.ToLower())
            {
                case "csharp":
                case "cs":
                    textBlock.Foreground = new SolidColorBrush(Color.Parse("#86C1B9"));
                    break;
                case "javascript":
                case "js":
                    textBlock.Foreground = new SolidColorBrush(Color.Parse("#F8DC75"));
                    break;
                case "html":
                    textBlock.Foreground = new SolidColorBrush(Color.Parse("#E44D26"));
                    break;
                case "css":
                    textBlock.Foreground = new SolidColorBrush(Color.Parse("#563D7C"));
                    break;
                case "python":
                case "py":
                    textBlock.Foreground = new SolidColorBrush(Color.Parse("#3572A5"));
                    break;
                default:
                    textBlock.Foreground = new SolidColorBrush(Color.Parse("#D4D4D4"));
                    break;
            }
        }

        private async void CopyCodeToClipboard(string code)
        {
            try
            {
                // Get the top level window to access clipboard
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel != null)
                {
                    await topLevel.Clipboard.SetTextAsync(code);
                    ShowStatusNotification("Code copied to clipboard!", StatusType.Success);
                }
                else
                {
                    ShowStatusNotification("Clipboard not available", StatusType.Error);
                }
            }
            catch (Exception ex)
            {
                ShowStatusNotification($"Failed to copy: {ex.Message}", StatusType.Error);
            }
        }

        private async void CopyAllButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                // Get the top level window to access clipboard
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel != null)
                {
                    await topLevel.Clipboard.SetTextAsync(_content);
                    ShowStatusNotification("Content copied to clipboard!", StatusType.Success);
                }
                else
                {
                    ShowStatusNotification("Clipboard not available", StatusType.Error);
                }
            }
            catch (Exception ex)
            {
                ShowStatusNotification($"Failed to copy: {ex.Message}", StatusType.Error);
            }
        }

        private async void SaveButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                var file = await SaveFileDialog();
                if (file != null)
                {
                    await File.WriteAllTextAsync(file.Path.LocalPath, _content);
                    ShowStatusNotification("Content saved successfully!", StatusType.Success);
                }
            }
            catch (Exception ex)
            {
                ShowStatusNotification($"Failed to save: {ex.Message}", StatusType.Error);
            }
        }

        private async Task<IStorageFile> SaveFileDialog()
        {
            // Use Avalonia's file picker API
            var storageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
            if (storageProvider == null)
                return null;

            var options = new FilePickerSaveOptions
            {
                Title = "Save Response",
                DefaultExtension = ".txt",
                FileTypeChoices = new List<FilePickerFileType>
                {
                    new FilePickerFileType("Text Files")
                    {
                        Patterns = new[] { "*.txt" },
                        MimeTypes = new[] { "text/plain" }
                    },
                    new FilePickerFileType("Markdown Files")
                    {
                        Patterns = new[] { "*.md" },
                        MimeTypes = new[] { "text/markdown" }
                    },
                    new FilePickerFileType("All Files")
                    {
                        Patterns = new[] { "*.*" }
                    }
                },
                SuggestedFileName = $"Response_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            var file = await storageProvider.SaveFilePickerAsync(options);
            return file;
        }

        private void ClearButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            _content = "";
            _contentPanel.Children.Clear();
            ShowStatusNotification("Content cleared", StatusType.Info);
        }

        private void ShowStatusNotification(string message, StatusType type)
        {
            _statusText.Text = message;

            // Set color based on status type
            switch (type)
            {
                case StatusType.Success:
                    _statusBorder.Background = new SolidColorBrush(Color.Parse("#4CAF50")); // Green
                    break;
                case StatusType.Error:
                    _statusBorder.Background = new SolidColorBrush(Color.Parse("#F44336")); // Red
                    break;
                case StatusType.Info:
                    _statusBorder.Background = new SolidColorBrush(Color.Parse("#2196F3")); // Blue
                    break;
                case StatusType.Warning:
                    _statusBorder.Background = new SolidColorBrush(Color.Parse("#FF9800")); // Orange
                    break;
            }

            _statusBorder.IsVisible = true;

            // Reset the timer to hide the status after delay
            _statusTimer.Stop();
            _statusTimer.Start();
        }

        protected override void OnResized(WindowResizedEventArgs e)
        {
            base.OnResized(e);

            // Update content layout when window is resized
            if (_content != null)
            {
                ProcessAndDisplayContent(_content);
            }
        }
    }

    public enum StatusType
    {
        Success,
        Error,
        Info,
        Warning
    }
}