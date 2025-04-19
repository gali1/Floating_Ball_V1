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
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using Avalonia.Animation;
using Avalonia.Styling;
using HoveringBallApp.Memory;
using HoveringBallApp.LLM;

namespace HoveringBallApp
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Popup windows
        private InputWindow _inputWindow;
        private ResponseWindow _responseWindow;
        private SettingsWindow _settingsWindow;

        // LLM Client Factory and current client
        private LLMClientFactory _llmClientFactory;
        private ILLMClient _currentLlmClient;
        private LLMProvider _currentProvider = LLMProvider.Groq;
        private string _currentModel = "llama3-70b-8192"; // Default Groq model

        // Memory system
        private IMemoryManager _memoryManager;
        private Guid _sessionId = Guid.NewGuid();

        // Configuration
        private ConfigurationManager _config;

        // Animation related
        private DispatcherTimer _pulseTimer;
        private DispatcherTimer _hoverTimer;
        private bool _isPulsing = false;
        private bool _isHovering = false;
        private readonly TimeSpan _hoverDelay = TimeSpan.FromSeconds(0.2);

        // Ball drag properties
        private bool _isDragging = false;
        private Point _dragStartPoint;

        // Tray functionality
        private bool _isInTray = false;

        // Context menu items
        private MenuItem _settingsMenuItem;
        private MenuItem _minimizeMenuItem;
        private MenuItem _resetPositionMenuItem;
        private MenuItem _exitMenuItem;

        // UI elements
        private Button _ballButton;
        private Ellipse _ballEllipse;
        private Ellipse _themeToggle;

        public new event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            // Configure the window for transparency with a fully rounded appearance
            TransparencyLevelHint = new WindowTransparencyLevel[] { WindowTransparencyLevel.Transparent };
            Background = Brushes.Transparent;
            this.CornerRadius = new CornerRadius(45); // Ensure perfect roundness
            UseLayoutRounding = true; // Ensure crisp edges

            // Initialize configuration
            _config = new ConfigurationManager();

            // Initialize memory manager
            InitializeMemorySystem();

            // Initialize LLM client factory
            _llmClientFactory = new LLMClientFactory(_memoryManager, _config);

            // Set the current LLM client
            _currentLlmClient = _llmClientFactory.CreateClient(_currentProvider);

            this.Loaded += MainWindow_Loaded;
            this.PositionChanged += MainWindow_PositionChanged;

            // Apply initial theme
            ApplyTheme(ThemeManager.Instance.CurrentTheme);
            ThemeManager.Instance.ThemeChanged += ThemeManager_ThemeChanged;

            // Initialize pulse animation timer
            _pulseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50)
            };
            _pulseTimer.Tick += PulseTimer_Tick;

            // Initialize hover timer for delayed visual feedback
            _hoverTimer = new DispatcherTimer
            {
                Interval = _hoverDelay
            };
            _hoverTimer.Tick += HoverTimer_Tick;
        }

        private void InitializeMemorySystem()
        {
            // Use only in-memory storage to avoid database connection issues
            _memoryManager = new MemoryOnlyManager();
            Console.WriteLine("Using in-memory storage exclusively (no database connections)");
        }
        private void ThemeManager_ThemeChanged(object sender, AppTheme theme)
        {
            ApplyTheme(theme);
        }

        private void ApplyTheme(AppTheme theme)
        {
            // Get references to UI elements if not already obtained
            if (_ballEllipse == null && _ballButton != null)
            {
                _ballEllipse = _ballButton.FindControl<Ellipse>("Ball");
                _themeToggle = _ballButton.FindControl<Ellipse>("ThemeToggle");
            }

            // Apply theme classes to ball
            if (_ballEllipse != null)
            {
                // Remove all theme classes first
                _ballEllipse.Classes.Remove("Light");
                _ballEllipse.Classes.Remove("Dark");
                _ballEllipse.Classes.Remove("System");

                // Add appropriate theme class
                switch (theme)
                {
                    case AppTheme.Light:
                        _ballEllipse.Classes.Add("Light");
                        break;
                    case AppTheme.Dark:
                        _ballEllipse.Classes.Add("Dark");
                        break;
                    case AppTheme.System:
                        bool isDarkMode = ThemeManager.Instance.IsSystemInDarkMode();
                        _ballEllipse.Classes.Add(isDarkMode ? "Dark" : "Light");
                        break;
                }

                // Add animation to make the transition smooth
                var scaleAnimation = new Animation
                {
                    Duration = TimeSpan.FromMilliseconds(300),
                    FillMode = FillMode.Forward
                };

                scaleAnimation.Children.Add(new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters = { new Setter(ScaleTransform.ScaleXProperty, 0.95), new Setter(ScaleTransform.ScaleYProperty, 0.95) }
                });

                scaleAnimation.Children.Add(new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = { new Setter(ScaleTransform.ScaleXProperty, 1.0), new Setter(ScaleTransform.ScaleYProperty, 1.0) }
                });

                // Ensure we have a scale transform
                if (_ballButton.RenderTransform == null || !(_ballButton.RenderTransform is ScaleTransform))
                {
                    _ballButton.RenderTransform = new ScaleTransform(1, 1);
                }

                scaleAnimation.RunAsync((Animatable)_ballButton.RenderTransform);

                // Update theme toggle appearance
                if (_themeToggle != null)
                {
                    // Theme-specific colors for the toggle
                    switch (theme)
                    {
                        case AppTheme.Light:
                            _themeToggle.Fill = new SolidColorBrush(Color.Parse("#333333"));
                            break;
                        case AppTheme.Dark:
                            _themeToggle.Fill = new SolidColorBrush(Color.Parse("#AAAAAA"));
                            break;
                        case AppTheme.System:
                            bool isDarkMode = ThemeManager.Instance.IsSystemInDarkMode();
                            _themeToggle.Fill = new SolidColorBrush(isDarkMode ? Color.Parse("#AAAAAA") : Color.Parse("#333333"));
                            break;
                    }
                }
            }
        }

        private void MainWindow_PositionChanged(object sender, PixelPointEventArgs e)
        {
            // Update popup positions when main window moves
            UpdatePopupPositions();
        }

        // Reference to UI elements for fluid animations
        private Ellipse _glossHighlight;
        private Ellipse _rimHighlight;
        private Ellipse _spotHighlight;
        private Ellipse _ambientGlow;
        private Ellipse _outerGlow;
        private Ellipse _innerCircle;

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Get references to UI elements
            _ballButton = this.FindControl<Button>("BallButton");
            if (_ballButton != null)
            {
                _ballEllipse = _ballButton.FindControl<Ellipse>("Ball");
                _themeToggle = _ballButton.FindControl<Ellipse>("ThemeToggle");
                
                // Get references to fluid animation elements
                _glossHighlight = _ballButton.FindControl<Ellipse>("GlossHighlight");
                _rimHighlight = _ballButton.FindControl<Ellipse>("RimHighlight");
                _spotHighlight = _ballButton.FindControl<Ellipse>("SpotHighlight");
                _ambientGlow = _ballButton.FindControl<Ellipse>("AmbientGlow");
                _outerGlow = _ballButton.FindControl<Ellipse>("OuterGlow");
                _innerCircle = _ballButton.FindControl<Ellipse>("InnerCircle");

                // Start ambient glow animation
                StartAmbientAnimation();

                // Add theme switching on theme toggle click
                if (_themeToggle != null)
                {
                    _themeToggle.PointerPressed += ThemeToggle_PointerPressed;
                    _themeToggle.PointerEntered += (s, args) =>
                    {
                        _themeToggle.Opacity = 0.9;
                        AnimationHelper.AnimateScale(_themeToggle, 1.0, 1.2, TimeSpan.FromMilliseconds(200));
                    };
                    _themeToggle.PointerExited += (s, args) =>
                    {
                        _themeToggle.Opacity = 1.0;
                        AnimationHelper.AnimateScale(_themeToggle, 1.2, 1.0, TimeSpan.FromMilliseconds(150));
                    };
                }

                // Add enhanced hover effects
                _ballButton.PointerEntered += (s, args) =>
                {
                    _isHovering = true;
                    _hoverTimer.Start();
                    
                    // Enhanced 3D hover effect - create a more dramatic light source effect
                    if (_rimHighlight != null)
                    {
                        _rimHighlight.Opacity = 0.9;
                        _rimHighlight.StrokeThickness = 2.5;
                        
                        // Add smooth scale animation for rim
                        if (_rimHighlight.RenderTransform is ScaleTransform rimScale)
                        {
                            AnimationHelper.AnimateScale(rimScale, rimScale.ScaleX, 1.03, TimeSpan.FromMilliseconds(350));
                        }
                    }
                    
                    // Move the spot highlight to create depth illusion
                    if (_spotHighlight != null)
                    {
                        // Create position animation via transform for more realistic light movement
                        if (_spotHighlight.RenderTransform is TransformGroup spotTransforms)
                        {
                            foreach (var transform in spotTransforms.Children)
                            {
                                if (transform is TranslateTransform translateTransform)
                                {
                                    AnimationHelper.AnimateDouble(
                                        obj: translateTransform,
                                        property: TranslateTransform.XProperty,
                                        from: translateTransform.X,
                                        to: -8,
                                        duration: TimeSpan.FromMilliseconds(300),
                                        easing: new Avalonia.Animation.Easings.CircularEaseOut()
                                    );
                                    
                                    AnimationHelper.AnimateDouble(
                                        obj: translateTransform,
                                        property: TranslateTransform.YProperty,
                                        from: translateTransform.Y,
                                        to: -10,
                                        duration: TimeSpan.FromMilliseconds(300),
                                        easing: new Avalonia.Animation.Easings.CircularEaseOut()
                                    );
                                }
                                else if (transform is ScaleTransform scaleTransform)
                                {
                                    AnimationHelper.AnimateScale(scaleTransform, scaleTransform.ScaleX, 1.2, TimeSpan.FromMilliseconds(350));
                                }
                            }
                        }
                        
                        // Enhance the spot glow
                        _spotHighlight.Width = 30;
                        _spotHighlight.Height = 30;
                        _spotHighlight.Opacity = 0.8;
                        
                        // Add blur effect animation to increase glow
                        if (_spotHighlight.Effect is BlurEffect spotBlur)
                        {
                            AnimationHelper.AnimateDouble(
                                obj: spotBlur,
                                property: BlurEffect.RadiusProperty,
                                from: spotBlur.Radius,
                                to: 3.5,
                                duration: TimeSpan.FromMilliseconds(300)
                            );
                        }
                    }
                    
                    // Move glossy highlight for enhanced 3D lighting effect
                    if (_glossHighlight != null)
                    {
                        if (_glossHighlight.RenderTransform is TranslateTransform glossTranslate)
                        {
                            AnimationHelper.AnimateDouble(
                                obj: glossTranslate,
                                property: TranslateTransform.XProperty,
                                from: glossTranslate.X,
                                to: -6,
                                duration: TimeSpan.FromMilliseconds(350)
                            );
                            
                            AnimationHelper.AnimateDouble(
                                obj: glossTranslate,
                                property: TranslateTransform.YProperty,
                                from: glossTranslate.Y,
                                to: -12,
                                duration: TimeSpan.FromMilliseconds(350)
                            );
                        }
                        
                        // Increase the glossy highlight opacity
                        AnimationHelper.AnimateDouble(
                            obj: _glossHighlight,
                            property: OpacityProperty,
                            from: _glossHighlight.Opacity,
                            to: 0.9,
                            duration: TimeSpan.FromMilliseconds(350)
                        );
                    }
                    
                    // Enhance inner circle depth effect
                    if (_innerCircle != null)
                    {
                        if (_innerCircle.RenderTransform is TransformGroup innerTransforms)
                        {
                            foreach (var transform in innerTransforms.Children)
                            {
                                if (transform is TranslateTransform translateTransform)
                                {
                                    AnimationHelper.AnimateDouble(
                                        obj: translateTransform,
                                        property: TranslateTransform.XProperty,
                                        from: translateTransform.X,
                                        to: 6,
                                        duration: TimeSpan.FromMilliseconds(400)
                                    );
                                    
                                    AnimationHelper.AnimateDouble(
                                        obj: translateTransform,
                                        property: TranslateTransform.YProperty,
                                        from: translateTransform.Y,
                                        to: 4,
                                        duration: TimeSpan.FromMilliseconds(400)
                                    );
                                }
                                else if (transform is ScaleTransform scaleTransform)
                                {
                                    AnimationHelper.AnimateScale(scaleTransform, scaleTransform.ScaleX, 1.15, TimeSpan.FromMilliseconds(400));
                                }
                            }
                        }
                        
                        // Increase inner glow opacity
                        AnimationHelper.AnimateDouble(
                            obj: _innerCircle,
                            property: OpacityProperty,
                            from: _innerCircle.Opacity,
                            to: 0.45,
                            duration: TimeSpan.FromMilliseconds(300)
                        );
                    }
                    
                    // Enhance the ambient and outer glows
                    if (_ambientGlow != null && _ambientGlow.RenderTransform is ScaleTransform ambientScale)
                    {
                        AnimationHelper.AnimateScale(ambientScale, ambientScale.ScaleX, 1.08, TimeSpan.FromMilliseconds(450));
                        AnimationHelper.AnimateDouble(
                            obj: _ambientGlow,
                            property: OpacityProperty,
                            from: _ambientGlow.Opacity,
                            to: _ambientGlow.Opacity * 1.4,
                            duration: TimeSpan.FromMilliseconds(450)
                        );
                    }
                    
                    if (_outerGlow != null && _outerGlow.RenderTransform is ScaleTransform outerScale)
                    {
                        AnimationHelper.AnimateScale(outerScale, outerScale.ScaleX, 1.1, TimeSpan.FromMilliseconds(500));
                        AnimationHelper.AnimateDouble(
                            obj: _outerGlow,
                            property: OpacityProperty,
                            from: _outerGlow.Opacity,
                            to: _outerGlow.Opacity * 1.5,
                            duration: TimeSpan.FromMilliseconds(500)
                        );
                    }
                };

                _ballButton.PointerExited += (s, args) =>
                {
                    _isHovering = false;
                    _hoverTimer.Stop();

                    // Reset hover effects with smooth animations for main ball drop shadow
                    if (_ballEllipse != null && _ballEllipse.Effect is DropShadowEffect hoverEffect)
                    {
                        AnimationHelper.AnimateDouble(
                            obj: hoverEffect,
                            property: DropShadowEffect.BlurRadiusProperty,
                            from: hoverEffect.BlurRadius,
                            to: 10.0,
                            duration: TimeSpan.FromMilliseconds(450)
                        );
                        
                        AnimationHelper.AnimateDouble(
                            obj: hoverEffect,
                            property: DropShadowEffect.OpacityProperty,
                            from: hoverEffect.Opacity,
                            to: 0.2,
                            duration: TimeSpan.FromMilliseconds(450)
                        );
                    }
                    
                    // Smoothly reset rim highlight
                    if (_rimHighlight != null)
                    {
                        AnimationHelper.AnimateDouble(
                            obj: _rimHighlight,
                            property: OpacityProperty,
                            from: _rimHighlight.Opacity,
                            to: 0.4,
                            duration: TimeSpan.FromMilliseconds(400)
                        );
                        
                        AnimationHelper.AnimateDouble(
                            obj: _rimHighlight,
                            property: Avalonia.Controls.Shapes.Ellipse.StrokeThicknessProperty,
                            from: _rimHighlight.StrokeThickness,
                            to: 1.5,
                            duration: TimeSpan.FromMilliseconds(400)
                        );
                        
                        if (_rimHighlight.RenderTransform is ScaleTransform rimScale)
                        {
                            AnimationHelper.AnimateScale(rimScale, rimScale.ScaleX, 1.0, TimeSpan.FromMilliseconds(400));
                        }
                    }
                    
                    // Reset spot highlight with fluid animation
                    if (_spotHighlight != null)
                    {
                        AnimationHelper.AnimateDouble(
                            obj: _spotHighlight,
                            property: WidthProperty,
                            from: _spotHighlight.Width,
                            to: 25,
                            duration: TimeSpan.FromMilliseconds(450)
                        );
                        
                        AnimationHelper.AnimateDouble(
                            obj: _spotHighlight,
                            property: HeightProperty,
                            from: _spotHighlight.Height,
                            to: 25,
                            duration: TimeSpan.FromMilliseconds(450)
                        );
                        
                        AnimationHelper.AnimateDouble(
                            obj: _spotHighlight,
                            property: OpacityProperty,
                            from: _spotHighlight.Opacity,
                            to: 0.5,
                            duration: TimeSpan.FromMilliseconds(450)
                        );
                        
                        if (_spotHighlight.RenderTransform is TransformGroup spotTransforms)
                        {
                            foreach (var transform in spotTransforms.Children)
                            {
                                if (transform is TranslateTransform translateTransform)
                                {
                                    AnimationHelper.AnimateDouble(
                                        obj: translateTransform,
                                        property: TranslateTransform.XProperty,
                                        from: translateTransform.X,
                                        to: 0,
                                        duration: TimeSpan.FromMilliseconds(450)
                                    );
                                    
                                    AnimationHelper.AnimateDouble(
                                        obj: translateTransform,
                                        property: TranslateTransform.YProperty,
                                        from: translateTransform.Y,
                                        to: 0,
                                        duration: TimeSpan.FromMilliseconds(450)
                                    );
                                }
                                else if (transform is ScaleTransform scaleTransform)
                                {
                                    AnimationHelper.AnimateScale(scaleTransform, scaleTransform.ScaleX, 1.0, TimeSpan.FromMilliseconds(450));
                                }
                            }
                        }
                        
                        // Reset blur effect
                        if (_spotHighlight.Effect is BlurEffect spotBlur)
                        {
                            AnimationHelper.AnimateDouble(
                                obj: spotBlur,
                                property: BlurEffect.RadiusProperty,
                                from: spotBlur.Radius,
                                to: 2.0,
                                duration: TimeSpan.FromMilliseconds(450)
                            );
                        }
                    }
                    
                    // Reset glossy highlight with smooth animation
                    if (_glossHighlight != null)
                    {
                        if (_glossHighlight.RenderTransform is TranslateTransform glossTranslate)
                        {
                            AnimationHelper.AnimateDouble(
                                obj: glossTranslate,
                                property: TranslateTransform.XProperty,
                                from: glossTranslate.X,
                                to: 0,
                                duration: TimeSpan.FromMilliseconds(450)
                            );
                            
                            AnimationHelper.AnimateDouble(
                                obj: glossTranslate,
                                property: TranslateTransform.YProperty,
                                from: glossTranslate.Y,
                                to: 0,
                                duration: TimeSpan.FromMilliseconds(450)
                            );
                        }
                        
                        AnimationHelper.AnimateDouble(
                            obj: _glossHighlight,
                            property: OpacityProperty,
                            from: _glossHighlight.Opacity,
                            to: 0.6,
                            duration: TimeSpan.FromMilliseconds(450)
                        );
                    }
                    
                    // Reset inner circle with smooth animation
                    if (_innerCircle != null)
                    {
                        AnimationHelper.AnimateDouble(
                            obj: _innerCircle,
                            property: OpacityProperty,
                            from: _innerCircle.Opacity,
                            to: 0.25,
                            duration: TimeSpan.FromMilliseconds(450)
                        );
                        
                        if (_innerCircle.RenderTransform is TransformGroup innerTransforms)
                        {
                            foreach (var transform in innerTransforms.Children)
                            {
                                if (transform is TranslateTransform translateTransform)
                                {
                                    AnimationHelper.AnimateDouble(
                                        obj: translateTransform,
                                        property: TranslateTransform.XProperty,
                                        from: translateTransform.X,
                                        to: 0,
                                        duration: TimeSpan.FromMilliseconds(450)
                                    );
                                    
                                    AnimationHelper.AnimateDouble(
                                        obj: translateTransform,
                                        property: TranslateTransform.YProperty,
                                        from: translateTransform.Y,
                                        to: 0,
                                        duration: TimeSpan.FromMilliseconds(450)
                                    );
                                }
                                else if (transform is ScaleTransform scaleTransform)
                                {
                                    AnimationHelper.AnimateScale(scaleTransform, scaleTransform.ScaleX, 1.0, TimeSpan.FromMilliseconds(450));
                                }
                            }
                        }
                    }
                    
                    // Reset ambient and outer glows
                    if (_ambientGlow != null && _ambientGlow.RenderTransform is ScaleTransform ambientScale)
                    {
                        AnimationHelper.AnimateScale(ambientScale, ambientScale.ScaleX, 1.0, TimeSpan.FromMilliseconds(500));
                        AnimationHelper.AnimateDouble(
                            obj: _ambientGlow,
                            property: OpacityProperty,
                            from: _ambientGlow.Opacity,
                            to: 0.25,
                            duration: TimeSpan.FromMilliseconds(500)
                        );
                    }
                    
                    if (_outerGlow != null && _outerGlow.RenderTransform is ScaleTransform outerScale)
                    {
                        AnimationHelper.AnimateScale(outerScale, outerScale.ScaleX, 1.0, TimeSpan.FromMilliseconds(550));
                        AnimationHelper.AnimateDouble(
                            obj: _outerGlow,
                            property: OpacityProperty,
                            from: _outerGlow.Opacity,
                            to: 0.15,
                            duration: TimeSpan.FromMilliseconds(550)
                        );
                    }
                };
            }

            // Create popup windows
            _inputWindow = new InputWindow();
            _inputWindow.TextSubmitted += InputWindow_TextSubmitted;
            _inputWindow.ApiSelectionChanged += InputWindow_ApiSelectionChanged;

            _responseWindow = new ResponseWindow();

            _settingsWindow = new SettingsWindow(_config);
            _settingsWindow.SettingsChanged += SettingsWindow_SettingsChanged;

            // Apply initial animation
            AnimateBallAppearance();

            // Make sure the window can be dragged
            this.PointerPressed += (s, args) =>
            {
                if (args.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                {
                    _isDragging = true;
                    _dragStartPoint = args.GetPosition(this);
                    args.Handled = true;
                }
            };

            // Connect to context menu items
            InitializeContextMenuEvents();

            Console.WriteLine("MainWindow loaded successfully");
        }

        private void ThemeToggle_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // Toggle theme
            ThemeManager.Instance.ToggleTheme();

            // Add a pulse animation for feedback
            if (_themeToggle != null)
            {
                AnimationHelper.AnimateBounce(_themeToggle, 1.0, 1.0, 0.5, TimeSpan.FromMilliseconds(400));
            }

            e.Handled = true;
        }

        private void StartAmbientAnimation()
        {
            // Create enhanced fluid ambient animations for the ball
            if (_ambientGlow != null && _outerGlow != null)
            {
                var ambientTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; // ~60fps for smoother motion
                double time = 0;
                ambientTimer.Tick += (s, e) =>
                {
                    time += 0.016;
                    
                    // Create complex motion with multiple sine waves for more organic, fluid 3D feel
                    double primaryPulse = Math.Sin(time) * 0.5 + 0.5; // 0 to 1 oscillation
                    double secondaryPulse = Math.Sin(time * 0.6) * 0.5 + 0.5; // Different frequency
                    double tertiaryPulse = Math.Sin(time * 0.3) * 0.3 + 0.5; // Slower frequency
                    double quaternaryPulse = Math.Sin(time * 0.15) * 0.7 + 0.3; // Very slow undulation
                    
                    // Add scale transform animations for inner elements
                    if (_ambientGlow.RenderTransform is ScaleTransform ambientScaleTransform)
                    {
                        ambientScaleTransform.ScaleX = 1.0 + primaryPulse * 0.1;
                        ambientScaleTransform.ScaleY = 1.0 + primaryPulse * 0.1;
                    }
                    
                    if (_outerGlow.RenderTransform is ScaleTransform outerScaleTransform)
                    {
                        outerScaleTransform.ScaleX = 1.0 + secondaryPulse * 0.15;
                        outerScaleTransform.ScaleY = 1.0 + secondaryPulse * 0.15;
                    }
                    
                    // Smoothly vary the opacity of the ambient glows for depth effect
                    _ambientGlow.Opacity = 0.15 + primaryPulse * 0.2;
                    _outerGlow.Opacity = 0.12 + secondaryPulse * 0.1;
                    
                    // Add 3D parallax movement to inner circle for enhanced depth feel
                    if (_innerCircle != null && !_isHovering)
                    {
                        // More complex motion by combining multiple frequencies
                        double moveX = (Math.Sin(time * 0.7) * 3.0) + (Math.Sin(time * 0.4) * 1.5);
                        double moveY = (Math.Cos(time * 0.5) * 2.5) + (Math.Cos(time * 0.3) * 1.2);
                        
                        // Access the TransformGroup for more complex movement
                        if (_innerCircle.RenderTransform is TransformGroup innerTransformGroup)
                        {
                            // Find the TranslateTransform within the group
                            foreach (var transform in innerTransformGroup.Children)
                            {
                                if (transform is TranslateTransform translateTransform)
                                {
                                    translateTransform.X = moveX;
                                    translateTransform.Y = moveY;
                                }
                                else if (transform is ScaleTransform scaleTransform)
                                {
                                    // Add slight pulsing to make it seem more alive
                                    scaleTransform.ScaleX = 1.0 + quaternaryPulse * 0.12;
                                    scaleTransform.ScaleY = 1.0 + quaternaryPulse * 0.12;
                                }
                            }
                        }
                        
                        _innerCircle.Opacity = 0.25 + tertiaryPulse * 0.15;
                    }
                    
                    // Enhance spot highlight movement with 3D-like parallax
                    if (_spotHighlight != null && !_isHovering)
                    {
                        // Create organic movement that feels slightly independent
                        double moveX = (Math.Sin(time * 0.5) * 3.5) + (Math.Sin(time * 0.3) * 1.2);
                        double moveY = (Math.Cos(time * 0.4) * 2.8) + (Math.Cos(time * 0.25) * 1.5);
                        
                        // Update transform properties for fluid motion
                        if (_spotHighlight.RenderTransform is TransformGroup spotTransformGroup)
                        {
                            foreach (var transform in spotTransformGroup.Children)
                            {
                                if (transform is TranslateTransform translateTransform)
                                {
                                    translateTransform.X = moveX;
                                    translateTransform.Y = moveY;
                                }
                                else if (transform is ScaleTransform scaleTransform)
                                {
                                    // Very subtle scale oscillation
                                    scaleTransform.ScaleX = 1.0 + quaternaryPulse * 0.08;
                                    scaleTransform.ScaleY = 1.0 + quaternaryPulse * 0.08;
                                }
                            }
                        }
                        
                        _spotHighlight.Opacity = 0.4 + tertiaryPulse * 0.25;
                    }
                    
                    // Add enhanced 3D-like movement to glossy highlight
                    if (_glossHighlight != null && !_isHovering)
                    {
                        // Create slow, fluid movement that follows the light source
                        double moveX = Math.Sin(time * 0.2) * 2.2;
                        double moveY = Math.Sin(time * 0.15) * 1.8;
                        
                        // Update transform for parallax effect
                        if (_glossHighlight.RenderTransform is TranslateTransform glossTranslate)
                        {
                            glossTranslate.X = moveX;
                            glossTranslate.Y = moveY - 1.0; // Offset to maintain highlight position
                        }
                    }
                    
                    // Enhance rim highlight for 3D appearance
                    if (_rimHighlight != null && !_isHovering)
                    {
                        // Create scale breathing effect for the rim
                        if (_rimHighlight.RenderTransform is ScaleTransform rimScale)
                        {
                            rimScale.ScaleX = 1.0 + quaternaryPulse * 0.02; // Very subtle scale
                            rimScale.ScaleY = 1.0 + quaternaryPulse * 0.02;
                        }
                        
                        _rimHighlight.Opacity = 0.35 + tertiaryPulse * 0.15;
                    }
                };
                ambientTimer.Start();
            }
        }
        
        private void HoverTimer_Tick(object sender, EventArgs e)
        {
            if (_isHovering && _ballEllipse != null)
            {
                _hoverTimer.Stop();

                // Enhanced glow effect with smoother animation
                if (_ballEllipse.Effect is DropShadowEffect effect)
                {
                    // Animate the drop shadow with more subtle, fluid effect
                    int steps = 20; // More steps for smoother animation
                    double startBlur = effect.BlurRadius;
                    double startOpacity = effect.Opacity;
                    double endBlur = 18.0;
                    double endOpacity = 0.4;
                    int currentStep = 0;

                    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
                    timer.Tick += (s, args) =>
                    {
                        currentStep++;
                        if (currentStep > steps)
                        {
                            effect.BlurRadius = endBlur;
                            effect.Opacity = endOpacity;
                            timer.Stop();
                            return;
                        }

                        double progress = (double)currentStep / steps;
                        double easedProgress = AnimationHelper.EaseOutCubic(progress);
                        effect.BlurRadius = startBlur + (endBlur - startBlur) * easedProgress;
                        effect.Opacity = startOpacity + (endOpacity - startOpacity) * easedProgress;
                    };

                    timer.Start();
                }
                else
                {
                    // Create effect if it doesn't exist
                    var shadowEffect = new DropShadowEffect
                    {
                        BlurRadius = 18,
                        Opacity = 0.4,
                        OffsetX = 0,
                        OffsetY = 0
                    };
                    _ballEllipse.Effect = shadowEffect;
                }
            }
        }

        private void InitializeContextMenuEvents()
        {
            // Find the context menu items
            _settingsMenuItem = this.FindControl<MenuItem>("SettingsMenuItem");
            _minimizeMenuItem = this.FindControl<MenuItem>("MinimizeMenuItem");
            _resetPositionMenuItem = this.FindControl<MenuItem>("ResetPositionMenuItem");
            _exitMenuItem = this.FindControl<MenuItem>("ExitMenuItem");

            // Add event handlers
            if (_settingsMenuItem != null)
                _settingsMenuItem.Click += (s, e) => ShowSettingsWindow();

            if (_minimizeMenuItem != null)
                _minimizeMenuItem.Click += (s, e) => MinimizeToTray();

            if (_resetPositionMenuItem != null)
                _resetPositionMenuItem.Click += (s, e) => ResetPosition();

            if (_exitMenuItem != null)
                _exitMenuItem.Click += (s, e) => CloseApplication();
        }

        private void ShowSettingsWindow()
        {
            if (!_settingsWindow.IsVisible)
            {
                _settingsWindow.Show();
                UpdatePopupPositions();
            }
            else
            {
                _settingsWindow.Hide();
            }
        }

        private void MinimizeToTray()
        {
            // Animate the minimization with opacity and scale
            AnimationHelper.AnimateScale(this, 1.0, 0.3, TimeSpan.FromMilliseconds(300));
            AnimationHelper.AnimateFade(this, 1.0, 0.0, TimeSpan.FromMilliseconds(300), () =>
            {
                // Reset properties and hide
                this.Hide();
                this.Opacity = 1.0;
                if (this.RenderTransform is ScaleTransform st)
                {
                    st.ScaleX = 1.0;
                    st.ScaleY = 1.0;
                }
            });

            _isInTray = true;
        }

        private void ResetPosition()
        {
            // Calculate target position
            PixelPoint targetPosition = new PixelPoint(
                (int)(Screens.Primary.Bounds.Width / 2 - Width / 2),
                (int)(Screens.Primary.Bounds.Height / 2 - Height / 2)
            );

            // Use a timer to animate the position
            int steps = 30;
            int currentStep = 0;
            var startPosition = Position;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            timer.Tick += (s, e) =>
            {
                currentStep++;
                if (currentStep > steps)
                {
                    Position = targetPosition;
                    timer.Stop();
                    return;
                }

                double progress = (double)currentStep / steps;
                double easedProgress = AnimationHelper.EaseOutCubic(progress);

                int newX = startPosition.X + (int)((targetPosition.X - startPosition.X) * easedProgress);
                int newY = startPosition.Y + (int)((targetPosition.Y - startPosition.Y) * easedProgress);

                Position = new PixelPoint(newX, newY);
            };

            timer.Start();
        }

        private void CloseApplication()
        {
            // Animate closing
            AnimationHelper.AnimateScale(this, 1.0, 0.0, TimeSpan.FromMilliseconds(300));
            AnimationHelper.AnimateFade(this, 1.0, 0.0, TimeSpan.FromMilliseconds(300), () =>
            {
                // Close all popup windows
                if (_inputWindow != null && _inputWindow.IsVisible)
                    _inputWindow.Close();

                if (_responseWindow != null && _responseWindow.IsVisible)
                    _responseWindow.Close();

                if (_settingsWindow != null && _settingsWindow.IsVisible)
                    _settingsWindow.Close();

                // Close main window
                this.Close();

                // Fallback exit
                Environment.Exit(0);
            });
        }

        // New API selection changed handler
        private void InputWindow_ApiSelectionChanged(object sender, ApiSelectionChangedEventArgs e)
        {
            if (Enum.TryParse<LLMProvider>(e.ApiName, out var provider))
            {
                _currentProvider = provider;
                _currentModel = e.ModelName ?? LLMClientFactory.GetDefaultModel(provider);

                // Update the current LLM client
                _currentLlmClient = _llmClientFactory.CreateClient(provider);
            }
            else
            {
                // Fallback to Groq if the provider is not recognized
                _currentProvider = LLMProvider.Groq;
                _currentModel = LLMClientFactory.GetDefaultModel(LLMProvider.Groq);
                _currentLlmClient = _llmClientFactory.CreateClient(LLMProvider.Groq);
            }
        }

        private void AnimateBallAppearance()
        {
            // Find the Ball button
            var ballButton = this.FindControl<Button>("BallButton");
            if (ballButton == null) return;

            // Enhanced animation using the helper
            ballButton.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
            ballButton.RenderTransform = new ScaleTransform(0, 0);

            // Animate from 0 to 1 with a slight bounce
            AnimationHelper.AnimateBounce(ballButton, 0.0, 1.0, 0.15, TimeSpan.FromMilliseconds(800));
        }

        // Ball click handler
        public void Ball_Clicked(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Ball clicked!");

            // Simple non-animation approach to avoid the casting error
            if (_ballButton != null)
            {
                // Ensure the button has a RenderTransformOrigin set
                _ballButton.RenderTransformOrigin = RelativePoint.Center;

                // Create the ScaleTransform if it doesn't exist
                if (_ballButton.RenderTransform == null || !(_ballButton.RenderTransform is ScaleTransform))
                {
                    _ballButton.RenderTransform = new ScaleTransform(1, 1);
                }

                // Use a timer-based animation to achieve the effect
                var scaleTransform = _ballButton.RenderTransform as ScaleTransform;
                if (scaleTransform != null)
                {
                    // Simple feedback without animation
                    scaleTransform.ScaleX = 0.95;
                    scaleTransform.ScaleY = 0.95;

                    // Reset after a short delay
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
                    timer.Tick += (s, args) =>
                    {
                        scaleTransform.ScaleX = 1.0;
                        scaleTransform.ScaleY = 1.0;
                        timer.Stop();
                    };
                    timer.Start();
                }
            }

            // Show input window on click
            if (!_inputWindow.IsVisible)
            {
                _inputWindow.Show();

                // Set the current API mode and model in the input window
                _inputWindow.SetApiSelection(
                    _currentProvider.ToString(),
                    _currentModel
                );

                UpdatePopupPositions();
                _inputWindow.FocusInput();
            }
            else
            {
                _inputWindow.Hide();
            }

            e.Handled = true;
        }

        private async void InputWindow_TextSubmitted(object sender, string text)
        {
            await SendToLLM(text);
        }

        private void SettingsWindow_SettingsChanged(object sender, SettingsChangedEventArgs e)
        {
            // Update the current provider if it has changed
            if (e.Provider != _currentProvider)
            {
                _currentProvider = e.Provider;
                _currentLlmClient = _llmClientFactory.CreateClient(e.Provider);
            }

            // Update the current model if it has changed
            if (e.Model != _currentModel)
            {
                _currentModel = e.Model;
            }

            // Reinitialize memory system if memory settings have changed
            if (e.MemorySettingsChanged)
            {
                InitializeMemorySystem();

                // Re-create LLM client factory with new memory manager
                _llmClientFactory = new LLMClientFactory(_memoryManager, _config);
                _currentLlmClient = _llmClientFactory.CreateClient(_currentProvider);
            }
        }

        private void UpdatePopupPositions()
        {
            if (_inputWindow != null && _inputWindow.IsVisible)
            {
                var mainPos = this.Position;

                // Position at bottom right of the ball with adjusted offset
                _inputWindow.Position = new PixelPoint(
                    mainPos.X + 50, // Position to the right of the ball
                    mainPos.Y + 50  // Position at the bottom of the ball
                );
            }

            if (_responseWindow != null && _responseWindow.IsVisible)
            {
                var mainPos = this.Position;
                var responseHeight = _responseWindow.Bounds.Height > 0 ?
                    (int)_responseWindow.Bounds.Height : 100;

                // Position at top right of the ball with adjusted offset
                _responseWindow.Position = new PixelPoint(
                    mainPos.X + 50, // Position to the right of the ball
                    mainPos.Y - responseHeight - 10  // Position above the ball with margin
                );
            }

            if (_settingsWindow != null && _settingsWindow.IsVisible)
            {
                var mainPos = this.Position;
                var settingsHeight = _settingsWindow.Bounds.Height > 0 ?
                    (int)_settingsWindow.Bounds.Height : 150;

                // Position at left of the ball with adjusted offset
                _settingsWindow.Position = new PixelPoint(
                    mainPos.X - (int)_settingsWindow.Bounds.Width - 20, // Position to the left of the ball with margin
                    mainPos.Y - settingsHeight / 2 + 40  // Position centered relative to the ball
                );
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            if (_isDragging)
            {
                var currentPoint = e.GetPosition(this);
                var delta = currentPoint - _dragStartPoint;

                Position = new PixelPoint(
                    Position.X + (int)delta.X,
                    Position.Y + (int)delta.Y
                );

                UpdatePopupPositions();
                e.Handled = true;
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            _isDragging = false;
            e.Handled = true;
        }

        private async Task SendToLLM(string input)
        {
            try
            {
                // Start pulsing animation to indicate processing
                StartPulsingAnimation();

                // Update response window
                _responseWindow.ResponseContent = "Loading...";
                _responseWindow.Show();
                UpdatePopupPositions();

                string response = string.Empty;

                try
                {
                    // Send message to the current LLM client
                    response = await _currentLlmClient.SendMessageAsync(input, _sessionId, _currentModel);
                }
                catch (HttpRequestException ex)
                {
                    // If there's a connection issue, try falling back to a different provider
                    Console.WriteLine($"{_currentProvider} API connection error: {ex.Message}. Falling back to alternate provider.");

                    // Set up a notification in the response
                    response = $"**Note: {_currentProvider} API connection failed. Falling back to alternate provider.**\n\n";

                    // Try another provider as fallback
                    var fallbackProvider = _currentProvider == LLMProvider.Groq ? LLMProvider.OpenRouter : LLMProvider.Groq;
                    var fallbackClient = _llmClientFactory.CreateClient(fallbackProvider);
                    var fallbackModel = LLMClientFactory.GetDefaultModel(fallbackProvider);

                    response += await fallbackClient.SendMessageAsync(input, _sessionId, fallbackModel);
                }

                // Stop pulsing animation
                StopPulsingAnimation();

                // Update response
                _responseWindow.ResponseContent = response;

                // Update position again after content may have changed size
                UpdatePopupPositions();
            }
            catch (Exception ex)
            {
                StopPulsingAnimation();
                _responseWindow.ResponseContent = $"Error: {ex.Message}";
            }
        }

        private void StartPulsingAnimation()
        {
            _isPulsing = true;

            // Get reference to the Ball
            var ballButton = this.FindControl<Button>("BallButton");
            if (ballButton == null) return;

            // Use the helper to create a pulsing animation
            _pulseTimer = AnimationHelper.AnimatePulse(ballButton, 1.0, 0.08, 2.0);
        }

        private void StopPulsingAnimation()
        {
            _isPulsing = false;
            if (_pulseTimer != null)
                _pulseTimer.Stop();

            // Reset ball scale with smooth animation
            var ballButton = this.FindControl<Button>("BallButton");
            if (ballButton != null)
            {
                AnimationHelper.AnimateScale(ballButton,
                    ballButton.RenderTransform is ScaleTransform st ? st.ScaleX : 1.0,
                    1.0,
                    TimeSpan.FromMilliseconds(300));
            }
        }

        private void PulseTimer_Tick(object sender, EventArgs e)
        {
            if (!_isPulsing) return;

            // Get reference to the Ball
            var ballButton = this.FindControl<Button>("BallButton");
            if (ballButton == null) return;

            // Create enhanced pulse effect while processing
            double time = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) / 1000.0;
            double scale = 1.0 + 0.08 * Math.Sin(time * Math.PI * 2);

            var scaleTransform = ballButton.RenderTransform as ScaleTransform;
            if (scaleTransform == null)
            {
                scaleTransform = new ScaleTransform(scale, scale);
                ballButton.RenderTransform = scaleTransform;
                ballButton.RenderTransformOrigin = RelativePoint.Center;
            }
            else
            {
                scaleTransform.ScaleX = scale;
                scaleTransform.ScaleY = scale;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}