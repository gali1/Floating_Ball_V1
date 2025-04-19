using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Styling;
using HoveringBallApp.LLM;
using Avalonia.Threading;

namespace HoveringBallApp
{
    public class ApiSelectionChangedEventArgs : EventArgs
    {
        public string ApiName { get; set; }
        public string ModelName { get; set; }
    }

    public class InputWindow : PopupWindow
    {
        private TextBox _inputText;
        private ComboBox _apiSelector;
        private ComboBox _modelSelector;
        private Button _refreshModelsButton;
        private bool _programmaticChange = false;
        private PathIcon _sendIcon;
        private ProgressBar _loadingIndicator;

        public event EventHandler<string> TextSubmitted;
        public event EventHandler<ApiSelectionChangedEventArgs> ApiSelectionChanged;

        public TextBox InputText => _inputText;

        public string SelectedApiName => _apiSelector?.SelectedItem?.ToString();
        public string SelectedModelName => _modelSelector?.SelectedItem?.ToString();

        public InputWindow() : base()
        {
            // Initialize UI elements
            _inputText = new TextBox
            {
                Classes = { "PopupInput" },
                Watermark = "Type your message...",
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 80,
                MinWidth = 300,
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
                VerticalContentAlignment = VerticalAlignment.Center,
                CornerRadius = new CornerRadius(16), // Perfectly rounded (circle)
                RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative)
            };
            
            // Add interactive hover animation
            _refreshModelsButton.PointerEntered += (s, e) => 
            {
                _refreshModelsButton.RenderTransform = new ScaleTransform(1.1, 1.1);
                _refreshModelsButton.Effect = new DropShadowEffect
                {
                    BlurRadius = 10,
                    Opacity = 0.3,
                    OffsetX = 0,
                    OffsetY = 0,
                    Color = Color.Parse("#60000000") // Subtle glow on hover
                };
            };
            _refreshModelsButton.PointerExited += (s, e) => 
            {
                _refreshModelsButton.RenderTransform = new ScaleTransform(1.0, 1.0);
                _refreshModelsButton.Effect = null;
            };

            _loadingIndicator = new ProgressBar
            {
                IsIndeterminate = true,
                Height = 2,
                Margin = new Thickness(0, 10, 0, 0),
                IsVisible = false
            };

            SetTitle("Ask Assistant");
            InitializeContent();
        }

        private void InitializeContent()
        {
            // Create main content panel with spacing
            var mainPanel = new StackPanel
            {
                Spacing = 12
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

            // Create styled API selection panel
            var apiSelectionGrid = new Grid
            {
                Margin = new Thickness(0, 0, 0, 4)
            };
            apiSelectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            apiSelectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            var apiLabel = new TextBlock
            {
                Text = "AI Provider:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                FontSize = 13
            };

            // Configure API selector with fully rounded styling
            _apiSelector.Items.Add("Groq");
            _apiSelector.Items.Add("GLHF");
            _apiSelector.Items.Add("OpenRouter");
            _apiSelector.Items.Add("Cohere");
            _apiSelector.SelectedIndex = 0; // Default to Groq
            _apiSelector.SelectionChanged += ApiSelector_SelectionChanged;
            _apiSelector.MinHeight = 32;
            _apiSelector.VerticalContentAlignment = VerticalAlignment.Center;
            _apiSelector.CornerRadius = new CornerRadius(14); // Fully rounded corners
            
            // Add interactive hover effect
            _apiSelector.PointerEntered += (s, e) => 
            {
                _apiSelector.Effect = new DropShadowEffect
                {
                    BlurRadius = 10,
                    Opacity = 0.2,
                    OffsetX = 0,
                    OffsetY = 2,
                    Color = Color.Parse("#60000000") // Subtle shadow on hover
                };
            };
            _apiSelector.PointerExited += (s, e) => 
            {
                _apiSelector.Effect = null;
            };

            Grid.SetColumn(apiLabel, 0);
            Grid.SetColumn(_apiSelector, 1);

            apiSelectionGrid.Children.Add(apiLabel);
            apiSelectionGrid.Children.Add(_apiSelector);

            // Create model selection panel with improved layout
            var modelSelectionGrid = new Grid
            {
                Margin = new Thickness(0, 0, 0, 10)
            };
            modelSelectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            modelSelectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            modelSelectionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var modelLabel = new TextBlock
            {
                Text = "Model:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                FontSize = 13
            };

            // Configure model selector with enhanced and fully rounded styling
            _modelSelector.SelectionChanged += ModelSelector_SelectionChanged;
            _modelSelector.MinHeight = 32;
            _modelSelector.VerticalContentAlignment = VerticalAlignment.Center;
            _modelSelector.CornerRadius = new CornerRadius(14); // Fully rounded corners
            
            // Add interactive hover effect
            _modelSelector.PointerEntered += (s, e) => 
            {
                _modelSelector.Effect = new DropShadowEffect
                {
                    BlurRadius = 10,
                    Opacity = 0.2,
                    OffsetX = 0,
                    OffsetY = 2,
                    Color = Color.Parse("#60000000") // Subtle shadow on hover
                };
            };
            _modelSelector.PointerExited += (s, e) => 
            {
                _modelSelector.Effect = null;
            };

            // Set tooltip using Avalonia's tooltip API
            ToolTip.SetTip(_refreshModelsButton, "Refresh available models");
            _refreshModelsButton.Click += RefreshModelsButton_Click;

            Grid.SetColumn(modelLabel, 0);
            Grid.SetColumn(_modelSelector, 1);
            Grid.SetColumn(_refreshModelsButton, 2);

            modelSelectionGrid.Children.Add(modelLabel);
            modelSelectionGrid.Children.Add(_modelSelector);
            modelSelectionGrid.Children.Add(_refreshModelsButton);

            // Initialize with Groq models
            PopulateGroqModels();

            // Input area with improved layout
            var inputPanel = new Panel();

            // Wrap TextBox in a border for better visual appearance with rounded edges
            var inputBorder = new Border
            {
                Child = _inputText,
                Margin = new Thickness(0, 6, 0, 0),
                CornerRadius = new CornerRadius(14), // Fully rounded corners
                BoxShadow = new BoxShadows(new BoxShadow
                {
                    OffsetX = 0,
                    OffsetY = 2,
                    Blur = 8,
                    Spread = 0,
                    Color = Color.Parse("#20000000") // Subtle shadow for depth
                })
            };

            inputPanel.Children.Add(inputBorder);

            // Create buttons panel with improved layout
            var buttonsPanel = new Grid
            {
                Margin = new Thickness(0, 12, 0, 0)
            };

            buttonsPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            buttonsPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            buttonsPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var clearButton = new Button
            {
                Content = "Clear",
                Padding = new Thickness(12, 8, 12, 8),
                VerticalAlignment = VerticalAlignment.Center,
                CornerRadius = new CornerRadius(14), // Fully rounded corners
                // Add subtle hover transform
                RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative)
            };
            clearButton.Click += ClearButton_Click;
            
            // Add interactive hover animation to clear button
            clearButton.PointerEntered += (s, e) => 
            {
                clearButton.RenderTransform = new ScaleTransform(1.05, 1.05);
            };
            clearButton.PointerExited += (s, e) => 
            {
                clearButton.RenderTransform = new ScaleTransform(1.0, 1.0);
            };

            // Create path-based icon for the send button
            _sendIcon = new PathIcon
            {
                Data = Geometry.Parse("M2.01 21L23 12 2.01 3 2 10l15 2-15 2z")
            };

            var submitButton = new Button
            {
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children =
                    {
                        new TextBlock { Text = "Submit" },
                        _sendIcon
                    }
                },
                Padding = new Thickness(15, 8, 15, 8),
                IsDefault = true,
                HorizontalAlignment = HorizontalAlignment.Right,
                CornerRadius = new CornerRadius(14), // Fully rounded corners
                // Add transform origin for hover effect
                RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
                Classes = { "PrimaryButton" } // Add class for styling
            };
            
            // Add interactive hover animation to submit button
            submitButton.PointerEntered += (s, e) => 
            {
                submitButton.RenderTransform = new ScaleTransform(1.05, 1.05);
                
                // Add a subtle glow effect on hover
                submitButton.Effect = new DropShadowEffect
                {
                    BlurRadius = 12,
                    Opacity = 0.4,
                    OffsetX = 0,
                    OffsetY = 0,
                    Color = Color.Parse("#4080FFFF") // Subtle blue glow
                };
            };
            submitButton.PointerExited += (s, e) => 
            {
                submitButton.RenderTransform = new ScaleTransform(1.0, 1.0);
                submitButton.Effect = null;
            };
            submitButton.Click += SubmitButton_Click;

            Grid.SetColumn(clearButton, 0);
            Grid.SetColumn(submitButton, 2);

            buttonsPanel.Children.Add(clearButton);
            buttonsPanel.Children.Add(submitButton);

            // Add loading indicator
            Grid.SetColumnSpan(_loadingIndicator, 3);
            buttonsPanel.Children.Add(_loadingIndicator);

            // Add all elements to main panel
            mainPanel.Children.Add(apiSelectionGrid);
            mainPanel.Children.Add(modelSelectionGrid);
            mainPanel.Children.Add(inputPanel);
            mainPanel.Children.Add(buttonsPanel);

            // Add to content area
            AddContent(mainPanel);
        }

        private void ApiSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_programmaticChange) return;

            var selectedApi = _apiSelector.SelectedItem?.ToString();

            switch (selectedApi)
            {
                case "Groq":
                    PopulateGroqModels();
                    break;
                case "GLHF":
                    PopulateGLHFModels();
                    break;
                case "OpenRouter":
                    PopulateOpenRouterModels();
                    break;
                case "Cohere":
                    PopulateCohereModels();
                    break;
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
            var models = LLMClientFactory.GetAvailableModels(LLMProvider.Groq);
            foreach (var model in models)
            {
                _modelSelector.Items.Add(model);
            }

            _modelSelector.SelectedIndex = 0;
            _refreshModelsButton.IsVisible = false;

            _programmaticChange = false;
        }

        private void PopulateGLHFModels()
        {
            if (_modelSelector == null || _refreshModelsButton == null) return;

            _programmaticChange = true;

            _modelSelector.Items.Clear();

            // Add GLHF models
            var models = LLMClientFactory.GetAvailableModels(LLMProvider.GLHF);
            foreach (var model in models)
            {
                _modelSelector.Items.Add(model);
            }

            _modelSelector.SelectedIndex = 0;
            _refreshModelsButton.IsVisible = false;

            _programmaticChange = false;
        }

        private void PopulateOpenRouterModels()
        {
            if (_modelSelector == null || _refreshModelsButton == null) return;

            _programmaticChange = true;

            _modelSelector.Items.Clear();

            // Add OpenRouter models
            var models = LLMClientFactory.GetAvailableModels(LLMProvider.OpenRouter);
            foreach (var model in models)
            {
                _modelSelector.Items.Add(model);
            }

            _modelSelector.SelectedIndex = 0;
            _refreshModelsButton.IsVisible = false;

            _programmaticChange = false;
        }

        private void PopulateCohereModels()
        {
            if (_modelSelector == null || _refreshModelsButton == null) return;

            _programmaticChange = true;

            _modelSelector.Items.Clear();

            // Add Cohere models
            var models = LLMClientFactory.GetAvailableModels(LLMProvider.Cohere);
            foreach (var model in models)
            {
                _modelSelector.Items.Add(model);
            }

            _modelSelector.SelectedIndex = 0;
            _refreshModelsButton.IsVisible = false;

            _programmaticChange = false;
        }

        private async void RefreshModelsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_refreshModelsButton == null) return;

            _refreshModelsButton.IsEnabled = false;
            _loadingIndicator.IsVisible = true;

            try
            {
                // Store current selection if any
                string currentSelection = _modelSelector.SelectedItem?.ToString();

                // Enhanced visual feedback for refresh with more fluid animation
                _refreshModelsButton.RenderTransform = new RotateTransform(0);
                _refreshModelsButton.RenderTransformOrigin = RelativePoint.Center;
                
                // Use timer-based animation for more control over easing
                var rotateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; // ~60fps
                double startTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                double duration = 800; // Longer duration (800ms) for more fluid rotation
                double startAngle = 0;
                
                rotateTimer.Tick += (s, args) =>
                {
                    double elapsed = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - startTime;
                    double progress = Math.Min(1.0, elapsed / duration);
                    
                    // Custom easing function for rotation - starts fast, slows at end
                    double easedProgress = 1 - Math.Pow(1 - progress, 2.5);
                    
                    // Apply rotation with slight overshoot and bounce at the end
                    double targetAngle = 360;
                    double overshootFactor = 0;
                    
                    if (progress > 0.8)
                    {
                        // Add slight bounce/elastic effect at the end of rotation
                        double bounceProgress = (progress - 0.8) / 0.2; // Normalize to 0-1 for last 20%
                        overshootFactor = Math.Sin(bounceProgress * Math.PI) * 15; // Max 15 degree overshoot
                    }
                    
                    double currentAngle = startAngle + (easedProgress * targetAngle) + overshootFactor;
                    
                    if (_refreshModelsButton.RenderTransform is RotateTransform transform)
                    {
                        transform.Angle = currentAngle;
                    }
                    
                    if (progress >= 1.0)
                    {
                        rotateTimer.Stop();
                    }
                };
                
                rotateTimer.Start();
                
                // Add subtle pulsing glow during refresh
                var glowTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
                double glowStartTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                double glowDuration = duration; // Match rotation duration
                
                glowTimer.Tick += (s, args) =>
                {
                    double elapsed = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - glowStartTime;
                    double progress = Math.Min(1.0, elapsed / glowDuration);
                    
                    // Create pulsing glow effect
                    double pulseIntensity = Math.Sin(progress * Math.PI * 2) * 0.5 + 0.5; // Oscillate between 0-1
                    
                    _refreshModelsButton.Effect = new DropShadowEffect
                    {
                        BlurRadius = 10 + (pulseIntensity * 8),
                        Opacity = 0.3 + (pulseIntensity * 0.2),
                        OffsetX = 0,
                        OffsetY = 0,
                        Color = Color.Parse("#60A0A0FF") // Subtle blue glow
                    };
                    
                    if (progress >= 1.0)
                    {
                        _refreshModelsButton.Effect = null;
                        glowTimer.Stop();
                    }
                };
                
                glowTimer.Start();

                // Simulate fetching models (in a real app, you'd call the appropriate API)
                await Task.Delay(500);

                // Refresh models based on current provider
                var selectedApi = _apiSelector.SelectedItem?.ToString();
                switch (selectedApi)
                {
                    case "Groq":
                        PopulateGroqModels();
                        break;
                    case "GLHF":
                        PopulateGLHFModels();
                        break;
                    case "OpenRouter":
                        PopulateOpenRouterModels();
                        break;
                    case "Cohere":
                        PopulateCohereModels();
                        break;
                }

                // Try to restore selection
                if (!string.IsNullOrEmpty(currentSelection) && _modelSelector.Items.Contains(currentSelection))
                {
                    _modelSelector.SelectedItem = currentSelection;
                }
            }
            finally
            {
                _refreshModelsButton.IsEnabled = true;
                _loadingIndicator.IsVisible = false;
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

                // Create more advanced animations for submit
                if (_sendIcon != null && MainBorder != null)
                {
                    // Prepare the send icon for animation
                    _sendIcon.RenderTransform = new TranslateTransform(0, 0);
                    _sendIcon.RenderTransformOrigin = RelativePoint.Center;
                    
                    // Create a fluid trajectory animation for the send icon
                    var translateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; // ~60fps
                    double startTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    double duration = 250; // 250ms duration - faster for more responsive feel
                    
                    translateTimer.Tick += (s, e) =>
                    {
                        double elapsed = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - startTime;
                        double progress = Math.Min(1.0, elapsed / duration);
                        
                        // Cubic ease out for more natural motion
                        double easedProgress = 1 - Math.Pow(1 - progress, 3);
                        
                        // Create an arc trajectory (combination of X and Y movement)
                        if (_sendIcon.RenderTransform is TranslateTransform transform)
                        {
                            // Move horizontally and slightly upward in an arc
                            transform.X = 30 * easedProgress;
                            
                            // Create a parabolic arc - first up, then down
                            double arcHeight = 15;
                            double arcProgress = 4 * easedProgress * (1 - easedProgress); // Peaks at 0.5
                            transform.Y = -arcHeight * arcProgress;
                        }
                        
                        // Gradually fade out
                        _sendIcon.Opacity = 1 - easedProgress;
                        
                        if (progress >= 1.0)
                        {
                            translateTimer.Stop();
                        }
                    };
                    
                    translateTimer.Start();
                    
                    // Create a pulse effect on the parent container
                    var container = _sendIcon.Parent as Control;
                    if (container != null)
                    {
                        // Add a quick scale pulse to the container
                        var scaleTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
                        double scaleStartTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                        double scaleDuration = 200; // 200ms duration
                        
                        // Apply a scale transform to the container
                        container.RenderTransformOrigin = RelativePoint.Center;
                        container.RenderTransform = new ScaleTransform(1, 1);
                        
                        scaleTimer.Tick += (s, e) =>
                        {
                            double elapsed = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - scaleStartTime;
                            double progress = Math.Min(1.0, elapsed / scaleDuration);
                            
                            if (container.RenderTransform is ScaleTransform scaleTransform)
                            {
                                // First expand slightly, then contract
                                double scaleProgress = Math.Sin(progress * Math.PI);
                                double scaleFactor = 1 + (0.1 * scaleProgress);
                                
                                scaleTransform.ScaleX = scaleFactor;
                                scaleTransform.ScaleY = scaleFactor;
                            }
                            
                            if (progress >= 1.0)
                            {
                                scaleTimer.Stop();
                            }
                        };
                        
                        scaleTimer.Start();
                    }
                    
                    // Create a smooth transition out for the whole window
                    var transformGroup = new TransformGroup();
                    var scaleTransform = new ScaleTransform(1, 1);
                    var translateTransform = new TranslateTransform(0, 0);
                    transformGroup.Children.Add(scaleTransform);
                    transformGroup.Children.Add(translateTransform);
                    MainBorder.RenderTransform = transformGroup;
                    
                    // Animate the main border shrinking and moving up with timer
                    var windowAnimTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
                    double windowStartTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                    double windowDuration = 300; // 300ms for window close
                    
                    windowAnimTimer.Tick += (s, e) =>
                    {
                        double elapsed = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) - windowStartTime;
                        double progress = Math.Min(1.0, elapsed / windowDuration);
                        
                        // Add easing
                        double easedProgress = 1 - Math.Pow(1 - progress, 2); // Quadratic ease out
                        
                        // Gradually fade out
                        this.Opacity = 1 - easedProgress;
                        
                        // Shrink and move upward
                        scaleTransform.ScaleX = 1 - (0.05 * easedProgress);
                        scaleTransform.ScaleY = 1 - (0.05 * easedProgress);
                        translateTransform.Y = -10 * easedProgress;
                        
                        if (progress >= 1.0)
                        {
                            windowAnimTimer.Stop();
                            
                            // Notify the main window of submitted text
                            TextSubmitted?.Invoke(this, text);
                            
                            // Hide after animation completes
                            Dispatcher.UIThread.Post(() => base.Hide(), DispatcherPriority.Background);
                        }
                    };
                    
                    // Start window animation after a short delay
                    var delayTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                    delayTimer.Tick += (s, e) =>
                    {
                        windowAnimTimer.Start();
                        delayTimer.Stop();
                    };
                    delayTimer.Start();
                }
                else
                {
                    // Fallback if animation can't be applied
                    TextSubmitted?.Invoke(this, text);
                    this.Hide();
                }
            }
        }

        private void UpdateTextWrapping()
        {
            if (_inputText == null) return;

            // Ensure text wrapping works properly by adjusting width constraints
            if (_inputText.Bounds.Width > 0)
            {
                double desiredWidth = Math.Min(500, Math.Max(300, _inputText.Bounds.Width));
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

            // Set the API selector
            int apiIndex = -1;
            switch (apiName)
            {
                case "Groq":
                    apiIndex = 0;
                    break;
                case "GLHF":
                    apiIndex = 1;
                    break;
                case "OpenRouter":
                    apiIndex = 2;
                    break;
                case "Cohere":
                    apiIndex = 3;
                    break;
            }

            if (apiIndex >= 0 && apiIndex < _apiSelector.Items.Count)
            {
                _apiSelector.SelectedIndex = apiIndex;
            }
            else
            {
                _apiSelector.SelectedIndex = 0; // Default to Groq
            }

            // Load models for the selected API
            switch (apiName)
            {
                case "Groq":
                    PopulateGroqModels();
                    break;
                case "GLHF":
                    PopulateGLHFModels();
                    break;
                case "OpenRouter":
                    PopulateOpenRouterModels();
                    break;
                case "Cohere":
                    PopulateCohereModels();
                    break;
                default:
                    PopulateGroqModels(); // Default to Groq
                    break;
            }

            // Set the selected model if available
            if (!string.IsNullOrEmpty(modelName))
            {
                // Wait a moment for models to load then set selection
                var timer = new Avalonia.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(100)
                };

                timer.Tick += (s, e) =>
                {
                    if (_modelSelector.Items.Contains(modelName))
                    {
                        _modelSelector.SelectedItem = modelName;
                    }
                    timer.Stop();
                };

                timer.Start();
            }

            _programmaticChange = false;
        }
    }
}