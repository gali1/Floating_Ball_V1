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
using Avalonia.Interactivity;
using System.IO;
using Avalonia.Animation;
using Avalonia.Styling;

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
        private ProgressBar _progressIndicator;

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
            ThemeManager.Instance.ThemeChanged += (sender, theme) =>
            {
                _currentTheme = theme;
                ProcessAndDisplayContent(_content); // Refresh with new theme
            };
            _currentTheme = ThemeManager.Instance.CurrentTheme;

            // Initialize status notification timer
            _statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _statusTimer.Tick += (s, e) =>
            {
                if (_statusBorder != null)
                {
                    // Animate status notification hiding
                    var fadeOutAnimation = new Animation
                    {
                        Duration = TimeSpan.FromSeconds(0.3),
                        FillMode = FillMode.Forward,
                        Easing = new Avalonia.Animation.Easings.CubicEaseOut()
                    };

                    fadeOutAnimation.Children.Add(new KeyFrame
                    {
                        Cue = new Cue(0.0),
                        Setters = { new Setter(OpacityProperty, 1.0) }
                    });

                    fadeOutAnimation.Children.Add(new KeyFrame
                    {
                        Cue = new Cue(1.0),
                        Setters = { new Setter(OpacityProperty, 0.0) }
                    });

                    var translateAnimation = new Animation
                    {
                        Duration = TimeSpan.FromSeconds(0.3),
                        FillMode = FillMode.Forward,
                        Easing = new Avalonia.Animation.Easings.CubicEaseOut()
                    };

                    translateAnimation.Children.Add(new KeyFrame
                    {
                        Cue = new Cue(0.0),
                        Setters = { new Setter(TranslateTransform.YProperty, 0.0) }
                    });

                    translateAnimation.Children.Add(new KeyFrame
                    {
                        Cue = new Cue(1.0),
                        Setters = { new Setter(TranslateTransform.YProperty, -10.0) }
                    });

                    fadeOutAnimation.RunAsync(_statusBorder);
                    translateAnimation.RunAsync((Animatable)_statusBorder.RenderTransform);

                    var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.3) };
                    timer.Tick += (sender, args) =>
                    {
                        _statusBorder.IsVisible = false;
                        timer.Stop();
                    };
                    timer.Start();
                }
                _statusTimer.Stop();
            };

            this.Width = 450;
            this.MaxWidth = 800;
            this.MinWidth = 300;

            // Handle resize events
            this.PropertyChanged += (sender, args) =>
            {
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
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 0, 12),
                HorizontalAlignment = HorizontalAlignment.Center,
                IsVisible = false,
                RenderTransform = new TranslateTransform(0, 0)
            };

            // Add shadow effect to status notifications
            _statusBorder.Effect = new DropShadowEffect
            {
                BlurRadius = 8,
                Opacity = 0.3,
                OffsetX = 0,
                OffsetY = 1
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

            // Add progress indicator for long-running operations
            _progressIndicator = new ProgressBar
            {
                IsIndeterminate = true,
                Height = 2,
                Margin = new Thickness(0, 0, 0, 8),
                IsVisible = false
            };

            Grid.SetRow(_progressIndicator, 0);
            mainGrid.Children.Add(_progressIndicator);

            // Create scrollable response area with enhanced styling
            _scrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                MaxHeight = 500,
                MinHeight = 200,
                Padding = new Thickness(4)
            };

            _contentPanel = new StackPanel
            {
                Spacing = 10,
                Margin = new Thickness(5)
            };

            _scrollViewer.Content = _contentPanel;
            Grid.SetRow(_scrollViewer, 1);
            mainGrid.Children.Add(_scrollViewer);

            // Add action buttons panel with improved styling
            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 12,
                Margin = new Thickness(0, 12, 0, 0)
            };

            var copyButton = new Button
            {
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 6,
                    Children =
                    {
                        new PathIcon
                        {
                            Data = Geometry.Parse("M19,21H8V7H19M19,5H8A2,2 0 0,0 6,7V21A2,2 0 0,0 8,23H19A2,2 0 0,0 21,21V7A2,2 0 0,0 19,5M16,1H4A2,2 0 0,0 2,3V17H4V3H16V1Z"),
                            Width = 16,
                            Height = 16
                        },
                        new TextBlock { Text = "Copy All" }
                    }
                },
                Padding = new Thickness(12, 6, 12, 6)
            };
            copyButton.Click += CopyAllButton_Click;

            var saveButton = new Button
            {
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 6,
                    Children =
                    {
                        new PathIcon
                        {
                            Data = Geometry.Parse("M15,9H5V5H15M12,19A3,3 0 0,1 9,16A3,3 0 0,1 12,13A3,3 0 0,1 15,16A3,3 0 0,1 12,19M17,3H5C3.89,3 3,3.9 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V7L17,3Z"),
                            Width = 16,
                            Height = 16
                        },
                        new TextBlock { Text = "Save As..." }
                    }
                },
                Padding = new Thickness(12, 6, 12, 6)
            };
            saveButton.Click += SaveButton_Click;

            var clearButton = new Button
            {
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 6,
                    Children =
                    {
                        new PathIcon
                        {
                            Data = Geometry.Parse("M19,4H15.5L14.5,3H9.5L8.5,4H5V6H19M6,19A2,2 0 0,0 8,21H16A2,2 0 0,0 18,19V7H6V19Z"),
                            Width = 16,
                            Height = 16
                        },
                        new TextBlock { Text = "Clear" }
                    }
                },
                Padding = new Thickness(12, 6, 12, 6)
            };
            clearButton.Click += ClearButton_Click;

            buttonsPanel.Children.Add(clearButton);
            buttonsPanel.Children.Add(copyButton);
            buttonsPanel.Children.Add(saveButton);

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
                    Margin = new Thickness(10),
                    FontSize = 14,
                    Opacity = 0.8
                };

                // Add a loading spinner
                var loadingSpinner = new ProgressBar
                {
                    IsIndeterminate = true,
                    Width = 200,
                    Height = 4,
                    Margin = new Thickness(0, 12, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                _contentPanel.Children.Add(loadingText);
                _contentPanel.Children.Add(loadingSpinner);
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

            // Ensure we scroll to top when content changes
            if (_scrollViewer != null)
            {
                _scrollViewer.Offset = new Vector(0, 0);
            }
        }

        private TextBlock CreateTextBlock(string text)
        {
            // Process the text to handle markdown-style formatting
            // These are simple regex replacements for basic markdown

            // Process **bold** text
            text = Regex.Replace(text, @"\*\*(.+?)\*\*", m =>
            {
                return $"<Bold>{m.Groups[1].Value}</Bold>";
            });

            // Process *italic* text
            text = Regex.Replace(text, @"\*(.+?)\*", m =>
            {
                return $"<Italic>{m.Groups[1].Value}</Italic>";
            });

            // Process `inline code` text
            text = Regex.Replace(text, @"`(.+?)`", m =>
            {
                return $"<Code>{m.Groups[1].Value}</Code>";
            });

            // Create TextBlock with rich formatting support
            var textBlock = new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(5),
                LineHeight = 1.5,
                FontSize = 14
            };

            // Process and apply the inline formatting
            string processedText = text;
            var inlines = new List<Avalonia.Controls.Documents.Inline>();

            // Find all formatting tags
            var boldMatches = Regex.Matches(processedText, @"<Bold>(.*?)</Bold>");
            var italicMatches = Regex.Matches(processedText, @"<Italic>(.*?)</Italic>");
            var codeMatches = Regex.Matches(processedText, @"<Code>(.*?)</Code>");

            // Strip all formatting tags for plain text
            processedText = Regex.Replace(processedText, @"<Bold>(.*?)</Bold>", "$1");
            processedText = Regex.Replace(processedText, @"<Italic>(.*?)</Italic>", "$1");
            processedText = Regex.Replace(processedText, @"<Code>(.*?)</Code>", "$1");

            textBlock.Text = processedText;

            return textBlock;
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

            // Language label with icon
            var languageWrapper = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Margin = new Thickness(10, 5, 0, 0)
            };

            // Add language icon based on type
            var languageIcon = new PathIcon
            {
                Width = 14,
                Height = 14,
                Foreground = new SolidColorBrush(Color.Parse("#BBBBBB"))
            };

            // Set icon based on language
            switch (language.ToLower())
            {
                case "python":
                case "py":
                    languageIcon.Data = Geometry.Parse("M12,0C5.41,0 5.37,2.25 5.37,2.25L5.38,4.75H12.25V5.5H3.5C3.5,5.5 0,5.29 0,12C0,18.72 3.05,18.5 3.05,18.5H4.88V15.94C4.88,15.94 4.74,13 7.87,13H12.09C12.09,13 14.74,13.05 14.74,10.5V3.65C14.74,3.65 15.07,0 12,0M7.5,2.5A0.87,0.87 0 0,1 8.37,3.37A0.87,0.87 0 0,1 7.5,4.24A0.87,0.87 0 0,1 6.62,3.37A0.87,0.87 0 0,1 7.5,2.5M12,18C18.59,18 18.63,15.75 18.63,15.75L18.62,13.25H11.75V12.5H20.5C20.5,12.5 24,12.71 24,6C24,-0.71 20.95,-0.5 20.95,-0.5H19.12V2.06C19.12,2.06 19.26,5 16.13,5H11.91C11.91,5 9.26,4.95 9.26,7.5V14.35C9.26,14.35 8.93,18 12,18M16.5,15.5A0.87,0.87 0 0,1 15.63,14.63A0.87,0.87 0 0,1 16.5,13.76A0.87,0.87 0 0,1 17.38,14.63A0.87,0.87 0 0,1 16.5,15.5Z");
                    break;
                case "javascript":
                case "js":
                    languageIcon.Data = Geometry.Parse("M3,3H21V21H3V3M7.73,18.04C8.13,18.89 8.92,19.59 10.27,19.59C11.77,19.59 12.8,18.79 12.8,17.04V11.26H11.1V17C11.1,17.86 10.75,18.08 10.2,18.08C9.62,18.08 9.38,17.68 9.11,17.21L7.73,18.04M13.71,17.86C14.21,18.84 15.22,19.59 16.8,19.59C18.4,19.59 19.6,18.76 19.6,17.23C19.6,15.82 18.79,15.19 17.35,14.57L16.93,14.39C16.2,14.08 15.89,13.87 15.89,13.37C15.89,12.96 16.2,12.64 16.7,12.64C17.18,12.64 17.5,12.85 17.79,13.37L19.1,12.5C18.55,11.54 17.77,11.17 16.7,11.17C15.19,11.17 14.22,12.13 14.22,13.4C14.22,14.78 15.03,15.43 16.25,15.95L16.67,16.13C17.45,16.47 17.91,16.68 17.91,17.26C17.91,17.74 17.46,18.09 16.76,18.09C15.93,18.09 15.45,17.66 15.09,17.06L13.71,17.86Z");
                    break;
                case "csharp":
                case "cs":
                    languageIcon.Data = Geometry.Parse("M11.5,15.97L11.91,18.41C11.65,18.55 11.23,18.68 10.67,18.8C10.1,18.93 9.43,19 8.66,19C6.45,18.96 4.79,18.3 3.68,17.04C2.56,15.77 2,14.16 2,12.21C2.05,9.9 2.72,8.13 4,6.89C5.32,5.63 6.96,5 8.94,5C9.69,5 10.34,5.07 10.88,5.19C11.42,5.31 11.82,5.44 12.08,5.59L11.5,8.08L10.44,7.74C10.04,7.64 9.58,7.59 9.05,7.59C7.89,7.58 6.93,7.95 6.18,8.69C5.42,9.42 5.03,10.54 5,12.03C5,13.39 5.37,14.45 6.08,15.23C6.79,16 7.79,16.4 9.07,16.41L10.4,16.29C10.83,16.21 11.19,16.1 11.5,15.97M13.89,19L14.5,15H13L13.34,13H14.84L15.16,11H13.66L14,9H15.5L16.11,5H18.11L17.5,9H18.5L19.11,5H21.11L20.5,9H22L21.66,11H20.16L19.84,13H21.34L21,15H19.5L18.89,19H16.89L17.5,15H16.5L15.89,19H13.89M16.84,13H17.84L18.16,11H17.16L16.84,13Z");
                    break;
                case "html":
                    languageIcon.Data = Geometry.Parse("M12,17.56L16.07,16.43L16.62,10.33H9.38L9.2,8.3H16.8L17,6.31H7L7.56,12.32H14.45L14.22,14.9L12,15.5L9.78,14.9L9.64,13.24H7.64L7.93,16.43L12,17.56M4.07,3H19.93L18.5,19.2L12,21L5.5,19.2L4.07,3Z");
                    break;
                case "css":
                    languageIcon.Data = Geometry.Parse("M5,3L4.35,6.34H17.94L17.5,8.5H3.92L3.26,11.83H16.85L16.09,15.64L10.61,17.45L5.86,15.64L6.19,14H2.85L2.06,18L9.91,21L18.96,18L20.16,11.97L20.4,10.76L21.94,3H5Z");
                    break;
                default:
                    languageIcon.Data = Geometry.Parse("M14.6,16.6L19.2,12L14.6,7.4L16,6L22,12L16,18L14.6,16.6M9.4,16.6L4.8,12L9.4,7.4L8,6L2,12L8,18L9.4,16.6Z");
                    break;
            }

            var languageLabel = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(language) ? "code" : language,
                Foreground = new SolidColorBrush(Color.Parse("#BBBBBB")),
                FontSize = 12
            };

            languageWrapper.Children.Add(languageIcon);
            languageWrapper.Children.Add(languageLabel);
            Grid.SetColumn(languageWrapper, 0);

            // Copy button with improved style
            var copyButton = new Button
            {
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 6,
                    Children =
                    {
                        new PathIcon
                        {
                            Data = Geometry.Parse("M19,21H8V7H19M19,5H8A2,2 0 0,0 6,7V21A2,2 0 0,0 8,23H19A2,2 0 0,0 21,21V7A2,2 0 0,0 19,5M16,1H4A2,2 0 0,0 2,3V17H4V3H16V1Z"),
                            Width = 14,
                            Height = 14
                        },
                        new TextBlock
                        {
                            Text = "Copy",
                            FontSize = 12
                        }
                    }
                },
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(0, 2, 5, 2),
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            copyButton.Click += (sender, e) => CopyCodeToClipboard(code);
            Grid.SetColumn(copyButton, 1);

            headerPanel.Children.Add(languageWrapper);
            headerPanel.Children.Add(copyButton);
            Grid.SetRow(headerPanel, 0);

            // Code content with improved styling
            var codeBorder = new Border
            {
                Classes = { "CodeBlock" },
                Padding = new Thickness(15),
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
            // Basic syntax highlighting based on language
            switch (language.ToLower())
            {
                case "csharp":
                case "cs":
                    textBlock.Foreground = new SolidColorBrush(Color.Parse("#9CDCFE"));
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
                _progressIndicator.IsVisible = true;

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
            finally
            {
                _progressIndicator.IsVisible = false;
            }
        }

        private async void CopyAllButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                _progressIndicator.IsVisible = true;

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
            finally
            {
                _progressIndicator.IsVisible = false;
            }
        }

        private async void SaveButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                _progressIndicator.IsVisible = true;

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
            finally
            {
                _progressIndicator.IsVisible = false;
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

            // Reset transform and opacity
            _statusBorder.RenderTransform = new TranslateTransform(0, 0);
            _statusBorder.Opacity = 0;
            _statusBorder.IsVisible = true;

            // Animate the notification appearance
            var fadeInAnimation = new Animation
            {
                Duration = TimeSpan.FromSeconds(0.2),
                FillMode = FillMode.Forward,
                Easing = new Avalonia.Animation.Easings.CubicEaseOut()
            };

            fadeInAnimation.Children.Add(new KeyFrame
            {
                Cue = new Cue(0.0),
                Setters = { new Setter(OpacityProperty, 0.0) }
            });

            fadeInAnimation.Children.Add(new KeyFrame
            {
                Cue = new Cue(1.0),
                Setters = { new Setter(OpacityProperty, 1.0) }
            });

            var translateAnimation = new Animation
            {
                Duration = TimeSpan.FromSeconds(0.2),
                FillMode = FillMode.Forward,
                Easing = new Avalonia.Animation.Easings.CubicEaseOut()
            };

            translateAnimation.Children.Add(new KeyFrame
            {
                Cue = new Cue(0.0),
                Setters = { new Setter(TranslateTransform.YProperty, 10.0) }
            });

            translateAnimation.Children.Add(new KeyFrame
            {
                Cue = new Cue(1.0),
                Setters = { new Setter(TranslateTransform.YProperty, 0.0) }
            });

            fadeInAnimation.RunAsync(_statusBorder);
            translateAnimation.RunAsync((Animatable)_statusBorder.RenderTransform);

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