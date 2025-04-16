using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HoveringBallApp
{
    public class InputWindow : PopupWindow
    {
        private TextBox _inputText;
        private ComboBox _apiSelector;
        private ComboBox _modelSelector;
        private Button _refreshModelsButton;
        private bool _programmaticChange = false;

        public event EventHandler<string> TextSubmitted;
        public event EventHandler<ApiSelectionChangedEventArgs> ApiSelectionChanged;

        public TextBox InputText => _inputText;

        public string SelectedApiName => _apiSelector?.SelectedItem?.ToString();
        public string SelectedModelName => _modelSelector?.SelectedItem?.ToString();

        private OllamaClient _ollamaClient;

        public InputWindow(string ollamaUrl = "http://localhost:11434") : base()
        {
            // Initialize Ollama client for model fetching
            _ollamaClient = new OllamaClient(ollamaUrl);

            // Initialize UI elements
            _inputText = new TextBox
            {
                Classes = { "PopupInput" },
                Watermark = "Type your message...",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 80,
                MinWidth = 280,
                MaxWidth = 500
            };

            _apiSelector = new ComboBox
            {
                MinWidth = 120,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            _modelSelector = new ComboBox
            {
                MinWidth = 180,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            _refreshModelsButton = new Button
            {
                Content = "↻",
                Width = 32,
                Height = 32,
                Padding = new Thickness(0),
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };

            SetTitle("Enter Query");
            InitializeContent();
        }

        private void InitializeContent()
        {
            // Create main content panel with spacing
            var mainPanel = new StackPanel
            {
                Spacing = 8
            };

            // Set up TextBox events
            _inputText.KeyDown += InputText_KeyDown;
            _inputText.PropertyChanged += (s, e) =>
            {
                if (e.Property == TextBox.BoundsProperty)
                {
                    // Adjust the width based on content when bounds change
                    UpdateTextWrapping();
                }
            };

            // Create API selection panel
            var apiSelectionPanel = new Grid();
            apiSelectionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            apiSelectionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var apiLabel = new TextBlock
            {
                Text = "API:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };

            // Configure API selector
            _apiSelector.Items.Add("Groq");
            _apiSelector.Items.Add("Ollama");
            _apiSelector.SelectedIndex = 0; // Default to Groq
            _apiSelector.SelectionChanged += ApiSelector_SelectionChanged;

            Grid.SetColumn(apiLabel, 0);
            Grid.SetColumn(_apiSelector, 1);

            apiSelectionPanel.Children.Add(apiLabel);
            apiSelectionPanel.Children.Add(_apiSelector);

            // Create model selection panel
            var modelSelectionPanel = new Grid();
            modelSelectionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            modelSelectionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            modelSelectionPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var modelLabel = new TextBlock
            {
                Text = "Model:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };

            // Configure model selector and refresh button
            _modelSelector.SelectionChanged += ModelSelector_SelectionChanged;

            // Set tooltip using Avalonia's tooltip API
            ToolTip.SetTip(_refreshModelsButton, "Refresh available models");
            _refreshModelsButton.Click += RefreshModelsButton_Click;

            Grid.SetColumn(modelLabel, 0);
            Grid.SetColumn(_modelSelector, 1);
            Grid.SetColumn(_refreshModelsButton, 2);

            modelSelectionPanel.Children.Add(modelLabel);
            modelSelectionPanel.Children.Add(_modelSelector);
            modelSelectionPanel.Children.Add(_refreshModelsButton);

            // Initialize with Groq models
            PopulateGroqModels();

            // Create buttons panel
            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8,
                Margin = new Thickness(0, 5, 0, 0)
            };

            var clearButton = new Button
            {
                Content = "Clear",
                Padding = new Thickness(10, 5, 10, 5)
            };
            clearButton.Click += ClearButton_Click;

            var submitButton = new Button
            {
                Content = "Submit",
                Padding = new Thickness(15, 5, 15, 5),
                IsDefault = true
            };
            submitButton.Click += SubmitButton_Click;

            buttonsPanel.Children.Add(clearButton);
            buttonsPanel.Children.Add(submitButton);

            // Add all elements to main panel
            mainPanel.Children.Add(_inputText);
            mainPanel.Children.Add(apiSelectionPanel);
            mainPanel.Children.Add(modelSelectionPanel);
            mainPanel.Children.Add(buttonsPanel);

            // Add to content area
            AddContent(mainPanel);
        }

        private void ApiSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_programmaticChange) return;

            var selectedApi = _apiSelector.SelectedItem?.ToString();

            if (selectedApi == "Groq")
            {
                PopulateGroqModels();
            }
            else if (selectedApi == "Ollama")
            {
                PopulateOllamaModels();
            }

            // Notify of API selection change
            ApiSelectionChanged?.Invoke(this, new ApiSelectionChangedEventArgs
            {
                ApiName = selectedApi,
                ModelName = _modelSelector.SelectedItem?.ToString()
            });
        }

        private void ModelSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_programmaticChange || _modelSelector == null) return;

            // Notify of model selection change
            ApiSelectionChanged?.Invoke(this, new ApiSelectionChangedEventArgs
            {
                ApiName = _apiSelector?.SelectedItem?.ToString(),
                ModelName = _modelSelector.SelectedItem?.ToString()
            });
        }

        private void PopulateGroqModels()
        {
            if (_modelSelector == null || _refreshModelsButton == null) return;

            _programmaticChange = true;

            _modelSelector.Items.Clear();

            // Add Groq models
            _modelSelector.Items.Add("llama3-8b-8192");
            _modelSelector.Items.Add("llama3-70b-8192");
            _modelSelector.Items.Add("mixtral-8x7b-32768");
            _modelSelector.Items.Add("gemma-7b-it");

            _modelSelector.SelectedIndex = 0;
            _refreshModelsButton.IsVisible = false;

            _programmaticChange = false;
        }

        private async void PopulateOllamaModels()
        {
            if (_modelSelector == null || _refreshModelsButton == null) return;

            _programmaticChange = true;

            _modelSelector.Items.Clear();
            _modelSelector.Items.Add("Loading models...");
            _modelSelector.SelectedIndex = 0;
            _modelSelector.IsEnabled = false;
            _refreshModelsButton.IsVisible = true;

            try
            {
                var models = await _ollamaClient.GetAvailableModels();

                _modelSelector.Items.Clear();

                if (models.Count > 0)
                {
                    foreach (var model in models)
                    {
                        _modelSelector.Items.Add(model);
                    }
                    _modelSelector.SelectedIndex = 0;
                }
                else
                {
                    // Add default models if none available
                    _modelSelector.Items.Add("llama3");
                    _modelSelector.Items.Add("deepseek-r1:1.5b");
                    _modelSelector.Items.Add("mistral");
                    _modelSelector.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                _modelSelector.Items.Clear();
                _modelSelector.Items.Add("deepseek-r1:1.5b");
                _modelSelector.Items.Add("llama3");
                _modelSelector.Items.Add("mistral");
                _modelSelector.SelectedIndex = 0;

                Console.WriteLine($"Error fetching Ollama models: {ex.Message}");
            }
            finally
            {
                _modelSelector.IsEnabled = true;
                _programmaticChange = false;
            }
        }

        private async void RefreshModelsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_refreshModelsButton == null) return;

            _refreshModelsButton.IsEnabled = false;

            try
            {
                // Store current selection if any
                string currentSelection = _modelSelector.SelectedItem?.ToString();

                // Load models again
                await Task.Run(() => PopulateOllamaModels());

                // Try to restore selection
                if (!string.IsNullOrEmpty(currentSelection) && _modelSelector.Items.Contains(currentSelection))
                {
                    _modelSelector.SelectedItem = currentSelection;
                }
            }
            finally
            {
                _refreshModelsButton.IsEnabled = true;
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (_inputText == null) return;

            _inputText.Text = string.Empty;
            _inputText.Focus();
        }

        private void InputText_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                SubmitText();
                e.Handled = true;
            }
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            SubmitText();
        }

        private void SubmitText()
        {
            if (_inputText == null) return;

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

        private void UpdateTextWrapping()
        {
            if (_inputText == null) return;

            // Ensure text wrapping works properly by adjusting width constraints
            if (_inputText.Bounds.Width > 0)
            {
                double desiredWidth = Math.Min(500, Math.Max(280, _inputText.Bounds.Width));
                _inputText.MaxWidth = desiredWidth;
            }
        }

        protected override void OnResized(WindowResizedEventArgs e)
        {
            base.OnResized(e);

            // Update text wrapping on window resize
            UpdateTextWrapping();
        }

        public void FocusInput()
        {
            _inputText?.Focus();
        }

        public void SetApiSelection(string apiName, string modelName)
        {
            if (_apiSelector == null || _modelSelector == null) return;

            _programmaticChange = true;

            if (apiName == "Groq")
            {
                _apiSelector.SelectedIndex = 0;
                PopulateGroqModels();

                if (!string.IsNullOrEmpty(modelName) && _modelSelector.Items.Contains(modelName))
                {
                    _modelSelector.SelectedItem = modelName;
                }
            }
            else if (apiName == "Ollama")
            {
                _apiSelector.SelectedIndex = 1;

                // We'll set the model after loading the list
                string desiredModel = modelName;

                _programmaticChange = false;
                PopulateOllamaModels();

                // The model will be set after loading if available
                if (!string.IsNullOrEmpty(desiredModel))
                {
                    // Wait a moment for models to load then set selection
                    var timer = new Avalonia.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(500)
                    };

                    timer.Tick += (s, e) =>
                    {
                        if (_modelSelector.Items.Contains(desiredModel))
                        {
                            _programmaticChange = true;
                            _modelSelector.SelectedItem = desiredModel;
                            _programmaticChange = false;
                        }
                        timer.Stop();
                    };

                    timer.Start();
                }

                return; // Return early since we're setting the model asynchronously
            }

            _programmaticChange = false;
        }
    }

    public class ApiSelectionChangedEventArgs : EventArgs
    {
        public string ApiName { get; set; }
        public string ModelName { get; set; }
    }
}