using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HoveringBallApp
{
    public class SettingsWindow : PopupWindow
    {
        // Settings properties
        private string _groqApiKey;
        private string _ollamaUrl;
        private string _ollamaModel;
        private string _searxngUrl;
        private bool _webSearchEnabled;
        private MainWindow.ApiMode _currentApiMode;

        // UI elements
        private TextBox _groqApiKeyTextBox;
        private TextBox _ollamaUrlTextBox;
        private ComboBox _ollamaModelComboBox;
        private TextBox _searxngUrlTextBox;
        private CheckBox _webSearchEnabledCheckBox;
        private RadioButton _groqRadioButton;
        private RadioButton _ollamaRadioButton;

        // Event when settings change
        public event EventHandler SettingsChanged;

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
                Spacing = 12,
                Margin = new Thickness(5)
            };

            // API Mode selection
            var apiModePanel = new StackPanel
            {
                Spacing = 4
            };

            var apiModeLabel = new TextBlock
            {
                Text = "API Mode:",
                FontWeight = FontWeight.Bold
            };

            var apiModeOptions = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

            _groqRadioButton = new RadioButton
            {
                Content = "Groq",
                IsChecked = _currentApiMode == MainWindow.ApiMode.Groq
            };
            _groqRadioButton.Checked += (s, e) =>
            {
                _currentApiMode = MainWindow.ApiMode.Groq;
                NotifySettingsChanged();
            };

            _ollamaRadioButton = new RadioButton
            {
                Content = "Ollama (Local)",
                IsChecked = _currentApiMode == MainWindow.ApiMode.Ollama
            };
            _ollamaRadioButton.Checked += (s, e) =>
            {
                _currentApiMode = MainWindow.ApiMode.Ollama;
                NotifySettingsChanged();
            };

            apiModeOptions.Children.Add(_groqRadioButton);
            apiModeOptions.Children.Add(_ollamaRadioButton);

            apiModePanel.Children.Add(apiModeLabel);
            apiModePanel.Children.Add(apiModeOptions);

            // Groq Settings
            var groqPanel = new StackPanel
            {
                Spacing = 4
            };

            var groqLabel = new TextBlock
            {
                Text = "Groq API Key:",
                FontWeight = FontWeight.Bold
            };

            _groqApiKeyTextBox = new TextBox
            {
                Text = _groqApiKey,
                PasswordChar = '•',
                Watermark = "Enter your Groq API key"
            };
            _groqApiKeyTextBox.LostFocus += (s, e) =>
            {
                _groqApiKey = _groqApiKeyTextBox.Text;
                NotifySettingsChanged();
            };

            groqPanel.Children.Add(groqLabel);
            groqPanel.Children.Add(_groqApiKeyTextBox);

            // Ollama Settings
            var ollamaPanel = new StackPanel
            {
                Spacing = 4
            };

            var ollamaUrlLabel = new TextBlock
            {
                Text = "Ollama URL:",
                FontWeight = FontWeight.Bold
            };

            _ollamaUrlTextBox = new TextBox
            {
                Text = _ollamaUrl,
                Watermark = "http://localhost:11434"
            };
            _ollamaUrlTextBox.LostFocus += (s, e) =>
            {
                _ollamaUrl = _ollamaUrlTextBox.Text;
                NotifySettingsChanged();
            };

            var ollamaModelLabel = new TextBlock
            {
                Text = "Ollama Model:",
                FontWeight = FontWeight.Bold
            };

            _ollamaModelComboBox = new ComboBox
            {
                Width = 200
            };

            // Add some default models
            _ollamaModelComboBox.Items.Add("llama3");
            _ollamaModelComboBox.Items.Add("mistral");
            _ollamaModelComboBox.Items.Add("phi");

            _ollamaModelComboBox.SelectedItem = _ollamaModel;
            _ollamaModelComboBox.SelectionChanged += (s, e) =>
            {
                if (_ollamaModelComboBox.SelectedItem != null)
                {
                    _ollamaModel = _ollamaModelComboBox.SelectedItem.ToString();
                    NotifySettingsChanged();
                }
            };

            // Refresh models button
            var refreshModelsButton = new Button
            {
                Content = "Refresh Models",
                Margin = new Thickness(0, 4, 0, 0)
            };
            refreshModelsButton.Click += RefreshModelsButton_Click;

            ollamaPanel.Children.Add(ollamaUrlLabel);
            ollamaPanel.Children.Add(_ollamaUrlTextBox);
            ollamaPanel.Children.Add(ollamaModelLabel);
            ollamaPanel.Children.Add(_ollamaModelComboBox);
            ollamaPanel.Children.Add(refreshModelsButton);

            // SearXNG Settings
            var searxngPanel = new StackPanel
            {
                Spacing = 4
            };

            var searxngLabel = new TextBlock
            {
                Text = "SearXNG URL:",
                FontWeight = FontWeight.Bold
            };

            _searxngUrlTextBox = new TextBox
            {
                Text = _searxngUrl,
                Watermark = "http://localhost:8080"
            };
            _searxngUrlTextBox.LostFocus += (s, e) =>
            {
                _searxngUrl = _searxngUrlTextBox.Text;
                NotifySettingsChanged();
            };

            _webSearchEnabledCheckBox = new CheckBox
            {
                Content = "Enable Web Search",
                IsChecked = _webSearchEnabled
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

            searxngPanel.Children.Add(searxngLabel);
            searxngPanel.Children.Add(_searxngUrlTextBox);
            searxngPanel.Children.Add(_webSearchEnabledCheckBox);

            // Add all panels to main panel
            mainPanel.Children.Add(apiModePanel);
            mainPanel.Children.Add(new Separator { Margin = new Thickness(0, 4, 0, 4) });
            mainPanel.Children.Add(groqPanel);
            mainPanel.Children.Add(new Separator { Margin = new Thickness(0, 4, 0, 4) });
            mainPanel.Children.Add(ollamaPanel);
            mainPanel.Children.Add(new Separator { Margin = new Thickness(0, 4, 0, 4) });
            mainPanel.Children.Add(searxngPanel);

            // Save button
            var saveButton = new Button
            {
                Content = "Save Settings",
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 10, 0, 0)
            };
            saveButton.Click += SaveButton_Click;

            mainPanel.Children.Add(saveButton);

            // Add to content area
            AddContent(mainPanel);
        }

        private async void RefreshModelsButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button != null)
            {
                // Save the original button content
                var originalContent = button.Content;
                button.Content = "Loading...";
                button.IsEnabled = false;

                try
                {
                    // Get available models
                    var ollamaClient = new OllamaClient(_ollamaUrlTextBox.Text);
                    var models = await ollamaClient.GetAvailableModels();

                    // Update the ComboBox
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
                        _ollamaModel = _ollamaModelComboBox.SelectedItem.ToString();
                    }
                }
                catch (Exception ex)
                {
                    // Replace ContentDialog with a simple popup message
                    var errorWindow = new Window
                    {
                        Title = "Error",
                        Content = new TextBlock
                        {
                            Text = $"Failed to get models: {ex.Message}",
                            Margin = new Thickness(15),
                            TextWrapping = TextWrapping.Wrap
                        },
                        SizeToContent = SizeToContent.WidthAndHeight,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        SystemDecorations = SystemDecorations.BorderOnly,
                        Width = 300,
                        Height = 150
                    };

                    var okButton = new Button
                    {
                        Content = "OK",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 10, 0, 0)
                    };

                    okButton.Click += (s, args) => errorWindow.Close();

                    var panel = new StackPanel
                    {
                        Margin = new Thickness(15)
                    };
                    panel.Children.Add(new TextBlock
                    {
                        Text = $"Failed to get models: {ex.Message}",
                        TextWrapping = TextWrapping.Wrap
                    });
                    panel.Children.Add(okButton);

                    errorWindow.Content = panel;
                    await errorWindow.ShowDialog(this);
                }
                finally
                {
                    // Restore the button
                    button.Content = originalContent;
                    button.IsEnabled = true;
                }
            }
        }

        private void SaveButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // Apply all changes
            _groqApiKey = _groqApiKeyTextBox.Text;
            _ollamaUrl = _ollamaUrlTextBox.Text;
            _ollamaModel = _ollamaModelComboBox.SelectedItem?.ToString() ?? "llama3";
            _searxngUrl = _searxngUrlTextBox.Text;
            _webSearchEnabled = _webSearchEnabledCheckBox.IsChecked == true;

            // Set API mode
            if (_groqRadioButton.IsChecked == true)
                _currentApiMode = MainWindow.ApiMode.Groq;
            else if (_ollamaRadioButton.IsChecked == true)
                _currentApiMode = MainWindow.ApiMode.Ollama;

            // Notify that settings have changed
            NotifySettingsChanged();

            // Hide the window
            this.Hide();
        }

        private void NotifySettingsChanged()
        {
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}