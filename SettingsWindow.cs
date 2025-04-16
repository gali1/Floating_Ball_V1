using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Interactivity;
using Avalonia.Animation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia.Styling;

namespace HoveringBallApp
{
    public class SettingsWindow : PopupWindow
    {
        // Settings properties
        private string _groqApiKey = "";
        private string _ollamaUrl = "";
        private string _ollamaModel = "";
        private string _searxngUrl = "";
        private bool _webSearchEnabled;
        private MainWindow.ApiMode _currentApiMode;

        // UI elements
        private TextBox? _groqApiKeyTextBox;
        private TextBox? _ollamaUrlTextBox;
        private ComboBox? _ollamaModelComboBox;
        private TextBox? _searxngUrlTextBox;
        private CheckBox? _webSearchEnabledCheckBox;
        private RadioButton? _groqRadioButton;
        private RadioButton? _ollamaRadioButton;
        private ProgressBar? _loadingIndicator;
        private Button? _refreshModelsButton;

        // Event when settings change
        public event EventHandler? SettingsChanged;

        // Properties to access settings
        public string GroqApiKey => _groqApiKey;
        public string OllamaUrl => _ollamaUrl;
        public string OllamaModel => _ollamaModel;
        public string SearxngUrl => _searxngUrl;
        public bool WebSearchEnabled => _webSearchEnabled;
        public MainWindow.ApiMode CurrentApiMode => _currentApiMode;

        public SettingsWindow() : base()
        {
            SetTitle("Assistant Settings");

            // Default values
            _groqApiKey = "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";
            _ollamaUrl = "http://localhost:11434";
            _ollamaModel = "llama3";
            _searxngUrl = "http://localhost:8080";
            _webSearchEnabled = true;
            _currentApiMode = MainWindow.ApiMode.Groq;

            InitializeContent();
        }

        private void InitializeContent()
        {
            var mainPanel = new StackPanel
            {
                Spacing = 16,
                Margin = new Thickness(5)
            };

            // Add brief introduction/description
            var introPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 5)
            };

            var introText = new TextBlock
            {
                Text = "Configure how your assistant communicates with AI models",
                Opacity = 0.7,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12),
                FontSize = 13
            };

            introPanel.Children.Add(introText);
            mainPanel.Children.Add(introPanel);

            // API Mode selection with enhanced visuals
            var apiModePanel = new StackPanel
            {
                Spacing = 6
            };

            var apiModeLabel = new TextBlock
            {
                Text = "API Mode",
                FontWeight = FontWeight.Bold,
                FontSize = 14
            };

            var apiModeOptions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 15,
                Margin = new Thickness(0, 8, 0, 0)
            };

            // Create better looking radio buttons with icons
            var groqPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6
            };

            _groqRadioButton = new RadioButton
            {
                Content = "Groq API",
                IsChecked = _currentApiMode == MainWindow.ApiMode.Groq,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(8, 6, 8, 6)
            };

            _groqRadioButton.IsCheckedChanged += (s, e) =>
            {
                if (_groqRadioButton.IsChecked == true)
                {
                    _currentApiMode = MainWindow.ApiMode.Groq;
                    NotifySettingsChanged();
                }
            };

            var groqIcon = new PathIcon
            {
                Data = Geometry.Parse("M20 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V6c0-1.1-.9-2-2-2zm-5 14H4v-4h11v4zm0-5H4V9h11v4zm5 5h-4V9h4v9z"),
                Width = 18,
                Height = 18,
                Margin = new Thickness(0, 0, 5, 0)
            };

            groqPanel.Children.Add(_groqRadioButton);

            var ollamaPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6
            };

            _ollamaRadioButton = new RadioButton
            {
                Content = "Ollama (Local)",
                IsChecked = _currentApiMode == MainWindow.ApiMode.Ollama,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(8, 6, 8, 6)
            };

            _ollamaRadioButton.IsCheckedChanged += (s, e) =>
            {
                if (_ollamaRadioButton.IsChecked == true)
                {
                    _currentApiMode = MainWindow.ApiMode.Ollama;
                    NotifySettingsChanged();
                }
            };

            var ollamaIcon = new PathIcon
            {
                Data = Geometry.Parse("M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z"),
                Width = 18,
                Height = 18,
                Margin = new Thickness(0, 0, 5, 0)
            };

            ollamaPanel.Children.Add(_ollamaRadioButton);

            apiModeOptions.Children.Add(groqPanel);
            apiModeOptions.Children.Add(ollamaPanel);

            apiModePanel.Children.Add(apiModeLabel);
            apiModePanel.Children.Add(apiModeOptions);

            // Groq Settings with enhanced visuals
            var groqPanel2 = new StackPanel
            {
                Spacing = 6,
                Margin = new Thickness(0, 5, 0, 5)
            };

            var groqLabel = new TextBlock
            {
                Text = "Groq API Key",
                FontWeight = FontWeight.Medium,
                FontSize = 13
            };

            _groqApiKeyTextBox = new TextBox
            {
                Text = _groqApiKey,
                PasswordChar = '•',
                Watermark = "Enter your Groq API key",
                MinHeight = 35,
                MaxWidth = 450
            };
            _groqApiKeyTextBox.LostFocus += (s, e) =>
            {
                if (_groqApiKeyTextBox != null)
                {
                    _groqApiKey = _groqApiKeyTextBox.Text;
                    NotifySettingsChanged();
                }
            };

            var groqHint = new TextBlock
            {
                Text = "Get your API key from https://console.groq.com/keys",
                Opacity = 0.7,
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0)
            };

            groqPanel2.Children.Add(groqLabel);
            groqPanel2.Children.Add(_groqApiKeyTextBox);
            groqPanel2.Children.Add(groqHint);

            // Ollama Settings with enhanced visuals
            var ollamaPanel2 = new StackPanel
            {
                Spacing = 6,
                Margin = new Thickness(0, 5, 0, 5)
            };

            var ollamaUrlLabel = new TextBlock
            {
                Text = "Ollama URL",
                FontWeight = FontWeight.Medium,
                FontSize = 13
            };

            _ollamaUrlTextBox = new TextBox
            {
                Text = _ollamaUrl,
                Watermark = "http://localhost:11434",
                MinHeight = 35
            };
            _ollamaUrlTextBox.LostFocus += (s, e) =>
            {
                if (_ollamaUrlTextBox != null)
                {
                    _ollamaUrl = _ollamaUrlTextBox.Text;
                    NotifySettingsChanged();
                }
            };

            var ollamaModelLabel = new TextBlock
            {
                Text = "Ollama Model",
                FontWeight = FontWeight.Medium,
                Margin = new Thickness(0, 10, 0, 0),
                FontSize = 13
            };

            // Create a grid for model selection with refresh button
            var modelSelectionGrid = new Grid();
            modelSelectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            modelSelectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _ollamaModelComboBox = new ComboBox
            {
                Width = 280,
                MinHeight = 35,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // Add some default models
            _ollamaModelComboBox.Items.Add("llama3");
            _ollamaModelComboBox.Items.Add("mistral");
            _ollamaModelComboBox.Items.Add("phi");

            _ollamaModelComboBox.SelectedItem = _ollamaModel;
            _ollamaModelComboBox.SelectionChanged += (s, e) =>
            {
                if (_ollamaModelComboBox != null && _ollamaModelComboBox.SelectedItem != null)
                {
                    _ollamaModel = _ollamaModelComboBox.SelectedItem.ToString() ?? "";
                    NotifySettingsChanged();
                }
            };

            // Refresh models button with better styling
            _refreshModelsButton = new Button
            {
                Content = "↻",
                Width = 35,
                Height = 35,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            ToolTip.SetTip(_refreshModelsButton, "Refresh Models");
            _refreshModelsButton.Click += RefreshModelsButton_Click;

            Grid.SetColumn(_ollamaModelComboBox, 0);
            Grid.SetColumn(_refreshModelsButton, 1);

            modelSelectionGrid.Children.Add(_ollamaModelComboBox);
            modelSelectionGrid.Children.Add(_refreshModelsButton);

            // Add loading indicator
            _loadingIndicator = new ProgressBar
            {
                IsIndeterminate = true,
                Height = 2,
                Margin = new Thickness(0, 6, 0, 6),
                IsVisible = false
            };

            var ollamaHint = new TextBlock
            {
                Text = "Make sure Ollama is running locally to use local models",
                Opacity = 0.7,
                FontSize = 12,
                Margin = new Thickness(0, 8, 0, 0)
            };

            ollamaPanel2.Children.Add(ollamaUrlLabel);
            ollamaPanel2.Children.Add(_ollamaUrlTextBox);
            ollamaPanel2.Children.Add(ollamaModelLabel);
            ollamaPanel2.Children.Add(modelSelectionGrid);
            ollamaPanel2.Children.Add(_loadingIndicator);
            ollamaPanel2.Children.Add(ollamaHint);

            // SearXNG Settings with enhanced visuals
            var searxngPanel = new StackPanel
            {
                Spacing = 6,
                Margin = new Thickness(0, 5, 0, 5)
            };

            var searxngLabel = new TextBlock
            {
                Text = "SearXNG URL",
                FontWeight = FontWeight.Medium,
                FontSize = 13
            };

            _searxngUrlTextBox = new TextBox
            {
                Text = _searxngUrl,
                Watermark = "http://localhost:8080",
                MinHeight = 35
            };
            _searxngUrlTextBox.LostFocus += (s, e) =>
            {
                if (_searxngUrlTextBox != null)
                {
                    _searxngUrl = _searxngUrlTextBox.Text;
                    NotifySettingsChanged();
                }
            };

            _webSearchEnabledCheckBox = new CheckBox
            {
                Content = "Enable Web Search",
                IsChecked = _webSearchEnabled,
                Margin = new Thickness(0, 8, 0, 0)
            };

            // Use PropertyChanged event instead of CheckedChanged for Avalonia's CheckBox
            _webSearchEnabledCheckBox.PropertyChanged += (s, e) =>
            {
                if (e.Property == CheckBox.IsCheckedProperty)
                {
                    _webSearchEnabled = _webSearchEnabledCheckBox.IsChecked == true;
                    NotifySettingsChanged();
                }
            };

            var searxngHint = new TextBlock
            {
                Text = "SearXNG provides web search capabilities to your assistant",
                Opacity = 0.7,
                FontSize = 12,
                Margin = new Thickness(0, 6, 0, 0)
            };

            searxngPanel.Children.Add(searxngLabel);
            searxngPanel.Children.Add(_searxngUrlTextBox);
            searxngPanel.Children.Add(_webSearchEnabledCheckBox);
            searxngPanel.Children.Add(searxngHint);

            // Add all panels to main panel with separators
            mainPanel.Children.Add(CreateSeparator("API Configuration"));
            mainPanel.Children.Add(apiModePanel);
            mainPanel.Children.Add(CreateSeparator("Groq Settings"));
            mainPanel.Children.Add(groqPanel2);
            mainPanel.Children.Add(CreateSeparator("Ollama Settings"));
            mainPanel.Children.Add(ollamaPanel2);
            mainPanel.Children.Add(CreateSeparator("Web Search"));
            mainPanel.Children.Add(searxngPanel);

            // Save button with improved styling
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0),
                Spacing = 10
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                HorizontalAlignment = HorizontalAlignment.Right,
                Padding = new Thickness(15, 8, 15, 8)
            };
            cancelButton.Click += (s, e) => this.Hide();

            var saveButton = new Button
            {
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        new PathIcon
                        {
                            Data = Geometry.Parse("M17 3H5c-1.11 0-2 .9-2 2v14c0 1.1.89 2 2 2h14c1.1 0 2-.9 2-2V7l-4-4zm-5 16c-1.66 0-3-1.34-3-3s1.34-3 3-3 3 1.34 3 3-1.34 3-3 3zm3-10H5V5h10v4z"),
                            Width = 16,
                            Height = 16
                        },
                        new TextBlock { Text = "Save Settings" }
                    }
                },
                HorizontalAlignment = HorizontalAlignment.Right,
                Padding = new Thickness(15, 8, 15, 8)
            };
            saveButton.Click += SaveButton_Click;

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(saveButton);

            mainPanel.Children.Add(buttonPanel);

            // Add to content area
            AddContent(mainPanel);
        }

        private Border CreateSeparator(string title)
        {
            var border = new Border
            {
                Height = 35,
                Margin = new Thickness(0, 5, 0, 5)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var titleBlock = new TextBlock
            {
                Text = title,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.Parse("#888888")),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                FontSize = 12
            };

            var line = new Separator
            {
                VerticalAlignment = VerticalAlignment.Center,
                Height = 1,
                Margin = new Thickness(5, 0, 0, 0)
            };

            Grid.SetColumn(titleBlock, 0);
            Grid.SetColumn(line, 1);

            grid.Children.Add(titleBlock);
            grid.Children.Add(line);

            border.Child = grid;
            return border;
        }

        private async void RefreshModelsButton_Click(object? sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                // Save the original button content
                var originalContent = button.Content;
                button.Content = "...";
                button.IsEnabled = false;

                if (_loadingIndicator != null)
                    _loadingIndicator.IsVisible = true;

                // Add rotation animation to button
                button.RenderTransform = new RotateTransform(0);
                button.RenderTransformOrigin = RelativePoint.Center;

                var rotateAnimation = new Animation
                {
                    Duration = TimeSpan.FromSeconds(0.5),
                    FillMode = Avalonia.Animation.FillMode.Both,
                    IterationCount = new IterationCount(1),
                    PlaybackDirection = PlaybackDirection.Normal
                };

                rotateAnimation.Children.Add(new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters = { new Setter(RotateTransform.AngleProperty, 0.0) }
                });

                rotateAnimation.Children.Add(new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = { new Setter(RotateTransform.AngleProperty, 360.0) }
                });

                rotateAnimation.RunAsync((Animatable)button.RenderTransform);

                try
                {
                    // Get available models
                    string url = _ollamaUrlTextBox?.Text ?? "http://localhost:11434";
                    var ollamaClient = new OllamaClient(url);
                    var models = await ollamaClient.GetAvailableModels();

                    // Update the ComboBox
                    if (_ollamaModelComboBox != null)
                    {
                        _ollamaModelComboBox.Items.Clear();
                        foreach (var model in models)
                        {
                            _ollamaModelComboBox.Items.Add(model);
                        }

                        // If the list is empty, add default models
                        if (models.Count == 0)
                        {
                            _ollamaModelComboBox.Items.Add("llama3");
                            _ollamaModelComboBox.Items.Add("mistral");
                            _ollamaModelComboBox.Items.Add("phi");
                        }

                        // Select the current model if it exists, otherwise select the first one
                        if (_ollamaModelComboBox.Items.Contains(_ollamaModel))
                        {
                            _ollamaModelComboBox.SelectedItem = _ollamaModel;
                        }
                        else if (_ollamaModelComboBox.Items.Count > 0)
                        {
                            _ollamaModelComboBox.SelectedIndex = 0;
                            _ollamaModel = _ollamaModelComboBox.SelectedItem?.ToString() ?? "";
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Show error notification
                    await ShowErrorNotification($"Failed to get models: {ex.Message}");
                }
                finally
                {
                    // Restore the button
                    button.Content = originalContent;
                    button.IsEnabled = true;
                    if (_loadingIndicator != null)
                        _loadingIndicator.IsVisible = false;
                }
            }
        }

        private async Task ShowErrorNotification(string message)
        {
            // Create a modern error notification
            var errorWindow = new Window
            {
                Title = "Error",
                SystemDecorations = SystemDecorations.BorderOnly,
                Width = 350,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                ShowInTaskbar = false,
                Background = new SolidColorBrush(Color.Parse("#22252A"))
            };

            var contentPanel = new StackPanel
            {
                Margin = new Thickness(20)
            };

            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Margin = new Thickness(0, 0, 0, 15)
            };

            var icon = new PathIcon
            {
                Data = Geometry.Parse("M11,15H13V17H11V15M11,7H13V13H11V7M12,2C6.47,2 2,6.5 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20Z"),
                Width = 24,
                Height = 24,
                Foreground = new SolidColorBrush(Color.Parse("#FF5252"))
            };

            var headerText = new TextBlock
            {
                Text = "Connection Error",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };

            headerPanel.Children.Add(icon);
            headerPanel.Children.Add(headerText);

            var messageText = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.Parse("#E0E0E0")),
                Margin = new Thickness(0, 0, 0, 20)
            };

            var okButton = new Button
            {
                Content = "OK",
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 100,
                Padding = new Thickness(0, 8, 0, 8),
                Background = new SolidColorBrush(Color.Parse("#3D4048")),
                Foreground = Brushes.White
            };

            okButton.Click += (s, args) => errorWindow.Close();

            contentPanel.Children.Add(headerPanel);
            contentPanel.Children.Add(messageText);
            contentPanel.Children.Add(okButton);

            errorWindow.Content = contentPanel;
            await errorWindow.ShowDialog(this);
        }

        private void SaveButton_Click(object? sender, RoutedEventArgs e)
        {
            // Apply all changes with animation
            if (_groqApiKeyTextBox != null)
                _groqApiKey = _groqApiKeyTextBox.Text;

            if (_ollamaUrlTextBox != null)
                _ollamaUrl = _ollamaUrlTextBox.Text;

            if (_ollamaModelComboBox?.SelectedItem != null)
                _ollamaModel = _ollamaModelComboBox.SelectedItem.ToString() ?? "llama3";

            if (_searxngUrlTextBox != null)
                _searxngUrl = _searxngUrlTextBox.Text;

            if (_webSearchEnabledCheckBox != null)
                _webSearchEnabled = _webSearchEnabledCheckBox.IsChecked == true;

            // Set API mode
            if (_groqRadioButton?.IsChecked == true)
                _currentApiMode = MainWindow.ApiMode.Groq;
            else if (_ollamaRadioButton?.IsChecked == true)
                _currentApiMode = MainWindow.ApiMode.Ollama;

            // Add subtle save animation
            var saveButton = sender as Button;
            if (saveButton != null)
            {
                saveButton.Content = "Saved!";
                if (saveButton.Background is SolidColorBrush)
                    saveButton.Background = new SolidColorBrush(Color.Parse("#4CAF50"));

                // Create a timer to revert after 1 second
                var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                timer.Tick += (s, args) =>
                {
                    // Hide window
                    this.Hide();
                    timer.Stop();
                };
                timer.Start();
            }

            // Notify that settings have changed
            NotifySettingsChanged();
        }

        private void NotifySettingsChanged()
        {
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}