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
using HoveringBallApp.LLM;

namespace HoveringBallApp
{
    public class SettingsChangedEventArgs : EventArgs
    {
        public LLMProvider Provider { get; set; }
        public string Model { get; set; }
        public bool MemorySettingsChanged { get; set; }
    }

    public class SettingsWindow : PopupWindow
    {
        // Settings properties
        private string _groqApiKey = "";
        private string _glhfApiKey = "";
        private string _openRouterApiKey = "";
        private string _cohereApiKey = "";
        private LLMProvider _currentProvider = LLMProvider.Groq;
        private string _currentModel = "";
        private bool _useMemorySystem = true;

        // UI elements
        private TextBox _groqApiKeyTextBox;
        private TextBox _glhfApiKeyTextBox;
        private TextBox _openRouterApiKeyTextBox;
        private TextBox _cohereApiKeyTextBox;
        private RadioButton _groqRadioButton;
        private RadioButton _glhfRadioButton;
        private RadioButton _openRouterRadioButton;
        private RadioButton _cohereRadioButton;
        private ComboBox _modelComboBox;
        private ProgressBar _loadingIndicator;
        private Button _refreshModelsButton;
        private CheckBox _useMemorySystemCheckBox;

        // Configuration manager
        private ConfigurationManager _config;

        // Event when settings change
        public event EventHandler<SettingsChangedEventArgs> SettingsChanged;

        public SettingsWindow(ConfigurationManager config) : base()
        {
            SetTitle("Assistant Settings");

            _config = config;

            // Load settings from configuration
            _groqApiKey = _config.GroqApiKey;
            _glhfApiKey = _config.GLHFApiKey;
            _openRouterApiKey = _config.OpenRouterApiKey;
            _cohereApiKey = _config.CohereApiKey;
            _useMemorySystem = _config.UseMemorySystem;

            InitializeContent();
        }

        private void InitializeContent()
        {
            // Add rounded window style to settings window
            this.CornerRadius = new CornerRadius(20);
            this.UseLayoutRounding = true;
            
            var mainPanel = new StackPanel
            {
                Spacing = 16,
                Margin = new Thickness(10)
            };

            // Add brief introduction
            var introPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 5)
            };

            var introText = new TextBlock
            {
                Text = "Configure which AI model provider your assistant uses",
                Opacity = 0.7,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12),
                FontSize = 13
            };

            introPanel.Children.Add(introText);
            mainPanel.Children.Add(introPanel);

            // API Provider selection with enhanced visuals
            var apiProviderPanel = new StackPanel
            {
                Spacing = 6
            };

            var apiProviderLabel = new TextBlock
            {
                Text = "AI Provider",
                FontWeight = FontWeight.Bold,
                FontSize = 14
            };

            var apiProviderOptions = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 8,
                Margin = new Thickness(0, 8, 0, 0)
            };

            // Create radio buttons for each provider
            // Groq
            var groqPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6
            };

            _groqRadioButton = new RadioButton
            {
                Content = "Groq API",
                IsChecked = _currentProvider == LLMProvider.Groq,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(8, 6, 8, 6)
            };

            _groqRadioButton.IsCheckedChanged += (s, e) =>
            {
                if (_groqRadioButton.IsChecked == true)
                {
                    _currentProvider = LLMProvider.Groq;
                    UpdateModelsList();
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

            // GLHF
            var glhfPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6
            };

            _glhfRadioButton = new RadioButton
            {
                Content = "GLHF API",
                IsChecked = _currentProvider == LLMProvider.GLHF,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(8, 6, 8, 6)
            };

            _glhfRadioButton.IsCheckedChanged += (s, e) =>
            {
                if (_glhfRadioButton.IsChecked == true)
                {
                    _currentProvider = LLMProvider.GLHF;
                    UpdateModelsList();
                    NotifySettingsChanged();
                }
            };

            var glhfIcon = new PathIcon
            {
                Data = Geometry.Parse("M21 3H3c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h18c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H3V5h18v14zM8 15c0-1.66 1.34-3 3-3 .35 0 .69.07 1 .18V6h5v2h-3v7.03c-.02 1.64-1.35 2.97-3 2.97-1.66 0-3-1.34-3-3z"),
                Width = 18,
                Height = 18,
                Margin = new Thickness(0, 0, 5, 0)
            };

            glhfPanel.Children.Add(_glhfRadioButton);

            // OpenRouter
            var openRouterPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6
            };

            _openRouterRadioButton = new RadioButton
            {
                Content = "OpenRouter API",
                IsChecked = _currentProvider == LLMProvider.OpenRouter,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(8, 6, 8, 6)
            };

            _openRouterRadioButton.IsCheckedChanged += (s, e) =>
            {
                if (_openRouterRadioButton.IsChecked == true)
                {
                    _currentProvider = LLMProvider.OpenRouter;
                    UpdateModelsList();
                    NotifySettingsChanged();
                }
            };

            var openRouterIcon = new PathIcon
            {
                Data = Geometry.Parse("M4 19h16v2H4v-2zm5-4h11v2H9v-2zm-5-4h16v2H4v-2zm5-4h11v2H9V7zM4 3h16v2H4V3z"),
                Width = 18,
                Height = 18,
                Margin = new Thickness(0, 0, 5, 0)
            };

            openRouterPanel.Children.Add(_openRouterRadioButton);

            // Cohere
            var coherePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6
            };

            _cohereRadioButton = new RadioButton
            {
                Content = "Cohere API",
                IsChecked = _currentProvider == LLMProvider.Cohere,
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(8, 6, 8, 6)
            };

            _cohereRadioButton.IsCheckedChanged += (s, e) =>
            {
                if (_cohereRadioButton.IsChecked == true)
                {
                    _currentProvider = LLMProvider.Cohere;
                    UpdateModelsList();
                    NotifySettingsChanged();
                }
            };

            var cohereIcon = new PathIcon
            {
                Data = Geometry.Parse("M12 3L1 9l11 6l11-6l-11-6zM1 9v6l11 6l11-6V9L12 15L1 9z"),
                Width = 18,
                Height = 18,
                Margin = new Thickness(0, 0, 5, 0)
            };

            coherePanel.Children.Add(_cohereRadioButton);

            apiProviderOptions.Children.Add(groqPanel);
            apiProviderOptions.Children.Add(glhfPanel);
            apiProviderOptions.Children.Add(openRouterPanel);
            apiProviderOptions.Children.Add(coherePanel);

            apiProviderPanel.Children.Add(apiProviderLabel);
            apiProviderPanel.Children.Add(apiProviderOptions);

            // Model Selection with improved layout
            var modelSelectionPanel = new StackPanel
            {
                Spacing = 6,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var modelLabel = new TextBlock
            {
                Text = "Model",
                FontWeight = FontWeight.Bold,
                FontSize = 14
            };

            // Create a grid for model selection with refresh button
            var modelSelectionGrid = new Grid();
            modelSelectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            modelSelectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _modelComboBox = new ComboBox
            {
                Width = 280,
                MinHeight = 35,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            // Add event handler for selection changed
            _modelComboBox.SelectionChanged += (s, e) =>
            {
                if (_modelComboBox.SelectedItem != null)
                {
                    _currentModel = _modelComboBox.SelectedItem.ToString();
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

            Grid.SetColumn(_modelComboBox, 0);
            Grid.SetColumn(_refreshModelsButton, 1);

            modelSelectionGrid.Children.Add(_modelComboBox);
            modelSelectionGrid.Children.Add(_refreshModelsButton);

            // Add loading indicator
            _loadingIndicator = new ProgressBar
            {
                IsIndeterminate = true,
                Height = 2,
                Margin = new Thickness(0, 6, 0, 6),
                IsVisible = false
            };

            modelSelectionPanel.Children.Add(modelLabel);
            modelSelectionPanel.Children.Add(modelSelectionGrid);
            modelSelectionPanel.Children.Add(_loadingIndicator);

            // API Key Settings with enhanced visuals
            // Groq API Key
            var groqApiKeyPanel = new StackPanel
            {
                Spacing = 6,
                Margin = new Thickness(0, 5, 0, 5)
            };

            var groqApiKeyLabel = new TextBlock
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
                    _config.UpdateApiKey("groq", _groqApiKey);
                    NotifySettingsChanged();
                }
            };

            var groqApiKeyHint = new TextBlock
            {
                Text = "Get your API key from https://console.groq.com/keys",
                Opacity = 0.7,
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0)
            };

            groqApiKeyPanel.Children.Add(groqApiKeyLabel);
            groqApiKeyPanel.Children.Add(_groqApiKeyTextBox);
            groqApiKeyPanel.Children.Add(groqApiKeyHint);

            // GLHF API Key
            var glhfApiKeyPanel = new StackPanel
            {
                Spacing = 6,
                Margin = new Thickness(0, 5, 0, 5)
            };

            var glhfApiKeyLabel = new TextBlock
            {
                Text = "GLHF API Key",
                FontWeight = FontWeight.Medium,
                FontSize = 13
            };

            _glhfApiKeyTextBox = new TextBox
            {
                Text = _glhfApiKey,
                PasswordChar = '•',
                Watermark = "Enter your GLHF API key",
                MinHeight = 35,
                MaxWidth = 450
            };
            _glhfApiKeyTextBox.LostFocus += (s, e) =>
            {
                if (_glhfApiKeyTextBox != null)
                {
                    _glhfApiKey = _glhfApiKeyTextBox.Text;
                    _config.UpdateApiKey("glhf", _glhfApiKey);
                    NotifySettingsChanged();
                }
            };

            var glhfApiKeyHint = new TextBlock
            {
                Text = "Get your API key from https://glhf.ai",
                Opacity = 0.7,
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0)
            };

            glhfApiKeyPanel.Children.Add(glhfApiKeyLabel);
            glhfApiKeyPanel.Children.Add(_glhfApiKeyTextBox);
            glhfApiKeyPanel.Children.Add(glhfApiKeyHint);

            // OpenRouter API Key
            var openRouterApiKeyPanel = new StackPanel
            {
                Spacing = 6,
                Margin = new Thickness(0, 5, 0, 5)
            };

            var openRouterApiKeyLabel = new TextBlock
            {
                Text = "OpenRouter API Key",
                FontWeight = FontWeight.Medium,
                FontSize = 13
            };

            _openRouterApiKeyTextBox = new TextBox
            {
                Text = _openRouterApiKey,
                PasswordChar = '•',
                Watermark = "Enter your OpenRouter API key",
                MinHeight = 35,
                MaxWidth = 450
            };
            _openRouterApiKeyTextBox.LostFocus += (s, e) =>
            {
                if (_openRouterApiKeyTextBox != null)
                {
                    _openRouterApiKey = _openRouterApiKeyTextBox.Text;
                    _config.UpdateApiKey("openrouter", _openRouterApiKey);
                    NotifySettingsChanged();
                }
            };

            var openRouterApiKeyHint = new TextBlock
            {
                Text = "Get your API key from https://openrouter.ai/keys",
                Opacity = 0.7,
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0)
            };

            openRouterApiKeyPanel.Children.Add(openRouterApiKeyLabel);
            openRouterApiKeyPanel.Children.Add(_openRouterApiKeyTextBox);
            openRouterApiKeyPanel.Children.Add(openRouterApiKeyHint);

            // Cohere API Key
            var cohereApiKeyPanel = new StackPanel
            {
                Spacing = 6,
                Margin = new Thickness(0, 5, 0, 5)
            };

            var cohereApiKeyLabel = new TextBlock
            {
                Text = "Cohere API Key",
                FontWeight = FontWeight.Medium,
                FontSize = 13
            };

            _cohereApiKeyTextBox = new TextBox
            {
                Text = _cohereApiKey,
                PasswordChar = '•',
                Watermark = "Enter your Cohere API key",
                MinHeight = 35,
                MaxWidth = 450
            };
            _cohereApiKeyTextBox.LostFocus += (s, e) =>
            {
                if (_cohereApiKeyTextBox != null)
                {
                    _cohereApiKey = _cohereApiKeyTextBox.Text;
                    _config.UpdateApiKey("cohere", _cohereApiKey);
                    NotifySettingsChanged();
                }
            };

            var cohereApiKeyHint = new TextBlock
            {
                Text = "Get your API key from https://dashboard.cohere.com/api-keys",
                Opacity = 0.7,
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0)
            };

            cohereApiKeyPanel.Children.Add(cohereApiKeyLabel);
            cohereApiKeyPanel.Children.Add(_cohereApiKeyTextBox);
            cohereApiKeyPanel.Children.Add(cohereApiKeyHint);

            // Memory System Settings
            var memoryPanel = new StackPanel
            {
                Spacing = 6,
                Margin = new Thickness(0, 5, 0, 5)
            };

            var memoryLabel = new TextBlock
            {
                Text = "Memory System",
                FontWeight = FontWeight.Bold,
                FontSize = 14
            };

            _useMemorySystemCheckBox = new CheckBox
            {
                Content = "Enable Memory System",
                IsChecked = _useMemorySystem,
                Margin = new Thickness(0, 8, 0, 0)
            };

            _useMemorySystemCheckBox.PropertyChanged += (s, e) =>
            {
                if (e.Property == CheckBox.IsCheckedProperty)
                {
                    _useMemorySystem = _useMemorySystemCheckBox.IsChecked == true;
                    _config.UseMemorySystem = _useMemorySystem;
                    NotifySettingsChanged(true);
                }
            };

            // Memory system is now fully in-memory

            var memoryHint = new TextBlock
            {
                Text = "The memory system allows Claude to remember previous conversations with remarkable contextual awareness",
                Opacity = 0.7,
                FontSize = 12,
                Margin = new Thickness(0, 6, 0, 0)
            };

            memoryPanel.Children.Add(memoryLabel);
            memoryPanel.Children.Add(_useMemorySystemCheckBox);
            memoryPanel.Children.Add(memoryHint);

            // Add all panels to main panel with separators
            mainPanel.Children.Add(CreateSeparator("Provider Selection"));
            mainPanel.Children.Add(apiProviderPanel);
            mainPanel.Children.Add(modelSelectionPanel);
            mainPanel.Children.Add(CreateSeparator("API Keys"));
            mainPanel.Children.Add(groqApiKeyPanel);
            mainPanel.Children.Add(glhfApiKeyPanel);
            mainPanel.Children.Add(openRouterApiKeyPanel);
            mainPanel.Children.Add(cohereApiKeyPanel);
            mainPanel.Children.Add(CreateSeparator("Memory Settings"));
            mainPanel.Children.Add(memoryPanel);

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
                Content = "Close",
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

            // Initialize model list based on current provider
            InitializeSelectedProvider();
            UpdateModelsList();
        }

        private void InitializeSelectedProvider()
        {
            // Set the correct radio button based on _currentProvider
            switch (_currentProvider)
            {
                case LLMProvider.Groq:
                    _groqRadioButton.IsChecked = true;
                    break;
                case LLMProvider.GLHF:
                    _glhfRadioButton.IsChecked = true;
                    break;
                case LLMProvider.OpenRouter:
                    _openRouterRadioButton.IsChecked = true;
                    break;
                case LLMProvider.Cohere:
                    _cohereRadioButton.IsChecked = true;
                    break;
            }
        }

        private void UpdateModelsList()
        {
            if (_modelComboBox == null) return;

            _modelComboBox.Items.Clear();

            // Add models based on selected provider
            string[] models = LLMClientFactory.GetAvailableModels(_currentProvider);
            foreach (var model in models)
            {
                _modelComboBox.Items.Add(model);
            }

            // Set default model
            string defaultModel = LLMClientFactory.GetDefaultModel(_currentProvider);
            if (string.IsNullOrEmpty(_currentModel) || !_modelComboBox.Items.Contains(_currentModel))
            {
                _currentModel = defaultModel;
            }

            if (_modelComboBox.Items.Contains(_currentModel))
            {
                _modelComboBox.SelectedItem = _currentModel;
            }
            else if (_modelComboBox.Items.Count > 0)
            {
                _modelComboBox.SelectedIndex = 0;
                _currentModel = _modelComboBox.SelectedItem.ToString();
            }
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
                    await Task.Delay(500); // Simulate API request

                    // For this example, we're just refreshing the standard models list
                    UpdateModelsList();
                }
                catch (Exception ex)
                {
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

            if (_glhfApiKeyTextBox != null)
                _glhfApiKey = _glhfApiKeyTextBox.Text;

            if (_openRouterApiKeyTextBox != null)
                _openRouterApiKey = _openRouterApiKeyTextBox.Text;

            if (_cohereApiKeyTextBox != null)
                _cohereApiKey = _cohereApiKeyTextBox.Text;

            // Update config values
            _config.UpdateApiKey("groq", _groqApiKey);
            _config.UpdateApiKey("glhf", _glhfApiKey);
            _config.UpdateApiKey("openrouter", _openRouterApiKey);
            _config.UpdateApiKey("cohere", _cohereApiKey);

            // Update memory settings
            if (_useMemorySystemCheckBox != null)
                _useMemorySystem = _useMemorySystemCheckBox.IsChecked == true;

            _config.UseMemorySystem = _useMemorySystem;

            // Save configuration
            _config.SaveConfiguration();

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

            // Notify that settings have changed (include memory settings change flag)
            NotifySettingsChanged(true);
        }

        private void NotifySettingsChanged(bool memorySettingsChanged = false)
        {
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs
            {
                Provider = _currentProvider,
                Model = _currentModel,
                MemorySettingsChanged = memorySettingsChanged
            });
        }
    }
}