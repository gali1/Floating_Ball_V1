using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;
using System;
using System.Runtime.InteropServices;
using Avalonia.Threading;
using Avalonia.Animation;
using Avalonia.Styling;

namespace HoveringBallApp
{
    /// <summary>
    /// Base class for popup windows with common functionality like
    /// dragging, minimizing, maximizing, resizing, and theme support.
    /// Enhanced with fluid animations and fully rounded styling.
    /// </summary>
    public class PopupWindow : Window
    {
        protected Border? MainBorder { get; private set; }
        protected Grid? TitleBar { get; private set; }
        protected Panel? ContentArea { get; private set; }
        protected Grid? ResizeGrid { get; private set; }

        private bool _isMinimized = false;
        private Size _normalSize;
        private bool _isDragging = false;
        private Point _dragStartPoint;

        // Resizing constants
        private const int ResizeBorderThickness = 6;
        private ResizeDirection _currentResizeDirection = ResizeDirection.None;
        private bool _isResizing = false;
        private Point _resizeStartPoint;
        private Rect _originalBounds;

        // Animation properties
        private readonly TimeSpan _showDuration = TimeSpan.FromMilliseconds(180);
        private readonly TimeSpan _hideDuration = TimeSpan.FromMilliseconds(120);

        public PopupWindow()
        {
            // Configure window appearance with fully rounded styling
            SystemDecorations = SystemDecorations.None;
            Background = Brushes.Transparent;
            TransparencyLevelHint = new WindowTransparencyLevel[] { WindowTransparencyLevel.Transparent };
            ShowInTaskbar = false;
            UseLayoutRounding = true; // Ensure crisp rounded edges
            CornerRadius = new CornerRadius(24); // Fully rounded window

            // Enable resizing (start with auto-size, but allow manual resizing)
            SizeToContent = SizeToContent.WidthAndHeight;
            CanResize = true;

            // Subscribe to theme changes
            ThemeManager.Instance.ThemeChanged += ThemeManager_ThemeChanged;
            this.Closed += (s, e) => ThemeManager.Instance.ThemeChanged -= ThemeManager_ThemeChanged;

            // Create layout
            InitializeLayout();

            // Add window event handlers
            this.PointerPressed += Window_PointerPressed;
            this.PointerMoved += Window_PointerMoved;
            this.PointerReleased += Window_PointerReleased;

            // Set default size constraints
            this.MinWidth = 250;
            this.MinHeight = 150;

            // Set initial opacity for animation
            this.Opacity = 0;
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);

            // Animate the window appearance using timer-based animation
            this.Opacity = 0;

            if (MainBorder != null)
            {
                MainBorder.RenderTransform = new TranslateTransform(0, -10);
                var translateTransform = MainBorder.RenderTransform as TranslateTransform;

                // Animate opacity with timer
                int steps = 15;
                double startOpacity = 0;
                double endOpacity = 1;
                int currentStep = 0;

                // Create a timer for opacity animation
                var opacityTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; // ~60fps
                opacityTimer.Tick += (s, args) =>
                {
                    currentStep++;
                    if (currentStep > steps)
                    {
                        this.Opacity = endOpacity;
                        opacityTimer.Stop();
                        return;
                    }

                    double progress = (double)currentStep / steps;
                    this.Opacity = startOpacity + (endOpacity - startOpacity) * progress;
                };

                // Create a timer for translation animation
                int translateSteps = 15;
                int translateCurrentStep = 0;
                var translateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
                translateTimer.Tick += (s, args) =>
                {
                    translateCurrentStep++;
                    if (translateCurrentStep > translateSteps)
                    {
                        if (translateTransform != null)
                        {
                            translateTransform.Y = 0;
                        }
                        translateTimer.Stop();
                        return;
                    }

                    double progress = (double)translateCurrentStep / translateSteps;
                    // Add easing
                    double easedProgress = 1 - Math.Pow(1 - progress, 3); // Cubic ease out

                    if (translateTransform != null)
                    {
                        translateTransform.Y = -10 + (10 * easedProgress);
                    }
                };

                // Start both animations
                opacityTimer.Start();
                translateTimer.Start();
            }
            else
            {
                // Fallback if MainBorder is not initialized
                this.Opacity = 1;
            }
        }
        
        public new void Hide()
        {
            // Animate hiding using timer-based animation
            if (MainBorder != null && MainBorder.RenderTransform is TranslateTransform translateTransform)
            {
                // Create opacity animation timer
                int steps = 10;
                double startOpacity = this.Opacity;
                double endOpacity = 0;
                int currentStep = 0;

                var opacityTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; // ~60fps
                opacityTimer.Tick += (s, args) =>
                {
                    currentStep++;
                    if (currentStep > steps)
                    {
                        this.Opacity = endOpacity;
                        opacityTimer.Stop();

                        // Call base.Hide() after opacity animation completes
                        base.Hide();
                        return;
                    }

                    double progress = (double)currentStep / steps;
                    // Add easing
                    double easedProgress = progress; // Linear for fade out

                    this.Opacity = startOpacity - (startOpacity * easedProgress);
                };

                // Create translation animation timer
                int translateSteps = 10;
                int translateCurrentStep = 0;
                var translateTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
                translateTimer.Tick += (s, args) =>
                {
                    translateCurrentStep++;
                    if (translateCurrentStep > translateSteps)
                    {
                        translateTimer.Stop();
                        return;
                    }

                    double progress = (double)translateCurrentStep / translateSteps;
                    // Add easing
                    double easedProgress = progress; // Linear for move out

                    translateTransform.Y = 10 * easedProgress;
                };

                // Start both animations
                opacityTimer.Start();
                translateTimer.Start();
            }
            else
            {
                // Fallback if animation can't be applied
                base.Hide();
            }
        }

        private void ThemeManager_ThemeChanged(object? sender, AppTheme theme)
        {
            ApplyTheme(theme);
        }

        protected virtual void ApplyTheme(AppTheme theme)
        {
            ThemeManager.Instance.ApplyTheme(this);
        }

        private void InitializeLayout()
        {
            // Main window content
            var windowContent = new Grid();

            // Main border with enhanced shadow and round styling
            MainBorder = new Border
            {
                Classes = { "PopupBox" },
                MinWidth = 250,
                CornerRadius = new CornerRadius(24), // Fully rounded corners
                ClipToBounds = true, // Respect rounded corners for content
                RenderTransform = new TranslateTransform(0, 0),
                Effect = new DropShadowEffect
                {
                    BlurRadius = 20,
                    Opacity = 0.4,
                    OffsetX = 0,
                    OffsetY = 5,
                    Color = Color.Parse("#80000000") // Semi-transparent shadow for depth
                }
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Title bar with controls
            TitleBar = new Grid
            {
                Background = Brushes.Transparent,
                Height = 40
            };
            TitleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            TitleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TitleBar.PointerPressed += TitleBar_PointerPressed;
            TitleBar.PointerMoved += TitleBar_PointerMoved;
            TitleBar.PointerReleased += TitleBar_PointerReleased;

            // Window controls
            var controlsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            };

            var minimizeButton = new Button
            {
                Classes = { "WindowControl" },
                Content = "─",
                FontWeight = FontWeight.Bold,
                CornerRadius = new CornerRadius(14), // Fully rounded button
                Width = 28,
                Height = 28
            };
            ToolTip.SetTip(minimizeButton, "Minimize");
            minimizeButton.Click += MinimizeButton_Click;

            var maximizeButton = new Button
            {
                Classes = { "WindowControl" },
                Content = "□",
                FontWeight = FontWeight.Bold,
                CornerRadius = new CornerRadius(14), // Fully rounded button
                Width = 28,
                Height = 28
            };
            ToolTip.SetTip(maximizeButton, "Maximize");
            maximizeButton.Click += MaximizeButton_Click;

            var closeButton = new Button
            {
                Classes = { "WindowControl" },
                Content = "✕",
                FontWeight = FontWeight.Bold,
                CornerRadius = new CornerRadius(14), // Fully rounded button
                Width = 28,
                Height = 28
            };
            ToolTip.SetTip(closeButton, "Close");
            closeButton.Click += CloseButton_Click;

            controlsPanel.Children.Add(minimizeButton);
            controlsPanel.Children.Add(maximizeButton);
            controlsPanel.Children.Add(closeButton);

            Grid.SetColumn(controlsPanel, 1);
            TitleBar.Children.Add(controlsPanel);

            Grid.SetRow(TitleBar, 0);
            mainGrid.Children.Add(TitleBar);

            // Content area
            ContentArea = new StackPanel
            {
                Spacing = 10,
                Margin = new Thickness(5)
            };

            Grid.SetRow(ContentArea, 1);
            mainGrid.Children.Add(ContentArea);

            MainBorder.Child = mainGrid;

            // Resize grips - Create these ONCE and add to the window content
            ResizeGrid = new Grid();

            // Add resize borders
            ResizeGrid.Children.Add(new Border
            {
                Height = ResizeBorderThickness,
                VerticalAlignment = VerticalAlignment.Top,
                Cursor = new Cursor(StandardCursorType.TopSide),
                Background = Brushes.Transparent
            });
            ((Border)ResizeGrid.Children[ResizeGrid.Children.Count - 1]).PointerPressed += (s, e) => StartResize(e, ResizeDirection.Top);

            ResizeGrid.Children.Add(new Border
            {
                Height = ResizeBorderThickness,
                VerticalAlignment = VerticalAlignment.Bottom,
                Cursor = new Cursor(StandardCursorType.BottomSide),
                Background = Brushes.Transparent
            });
            ((Border)ResizeGrid.Children[ResizeGrid.Children.Count - 1]).PointerPressed += (s, e) => StartResize(e, ResizeDirection.Bottom);

            ResizeGrid.Children.Add(new Border
            {
                Width = ResizeBorderThickness,
                HorizontalAlignment = HorizontalAlignment.Left,
                Cursor = new Cursor(StandardCursorType.LeftSide),
                Background = Brushes.Transparent
            });
            ((Border)ResizeGrid.Children[ResizeGrid.Children.Count - 1]).PointerPressed += (s, e) => StartResize(e, ResizeDirection.Left);

            ResizeGrid.Children.Add(new Border
            {
                Width = ResizeBorderThickness,
                HorizontalAlignment = HorizontalAlignment.Right,
                Cursor = new Cursor(StandardCursorType.RightSide),
                Background = Brushes.Transparent
            });
            ((Border)ResizeGrid.Children[ResizeGrid.Children.Count - 1]).PointerPressed += (s, e) => StartResize(e, ResizeDirection.Right);

            ResizeGrid.Children.Add(new Border
            {
                Width = ResizeBorderThickness,
                Height = ResizeBorderThickness,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Cursor = new Cursor(StandardCursorType.TopLeftCorner),
                Background = Brushes.Transparent
            });
            ((Border)ResizeGrid.Children[ResizeGrid.Children.Count - 1]).PointerPressed += (s, e) => StartResize(e, ResizeDirection.TopLeft);

            ResizeGrid.Children.Add(new Border
            {
                Width = ResizeBorderThickness,
                Height = ResizeBorderThickness,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Cursor = new Cursor(StandardCursorType.TopRightCorner),
                Background = Brushes.Transparent
            });
            ((Border)ResizeGrid.Children[ResizeGrid.Children.Count - 1]).PointerPressed += (s, e) => StartResize(e, ResizeDirection.TopRight);

            ResizeGrid.Children.Add(new Border
            {
                Width = ResizeBorderThickness,
                Height = ResizeBorderThickness,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Cursor = new Cursor(StandardCursorType.BottomLeftCorner),
                Background = Brushes.Transparent
            });
            ((Border)ResizeGrid.Children[ResizeGrid.Children.Count - 1]).PointerPressed += (s, e) => StartResize(e, ResizeDirection.BottomLeft);

            ResizeGrid.Children.Add(new Border
            {
                Width = ResizeBorderThickness,
                Height = ResizeBorderThickness,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Cursor = new Cursor(StandardCursorType.BottomRightCorner),
                Background = Brushes.Transparent
            });
            ((Border)ResizeGrid.Children[ResizeGrid.Children.Count - 1]).PointerPressed += (s, e) => StartResize(e, ResizeDirection.BottomRight);

            // Add main content and resize grid to the window content
            windowContent.Children.Add(ResizeGrid);
            windowContent.Children.Add(MainBorder);

            // Set the window content
            this.Content = windowContent;

            // Apply current theme
            ApplyTheme(ThemeManager.Instance.CurrentTheme);
        }

        private void StartResize(PointerPressedEventArgs e, ResizeDirection direction)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _isResizing = true;
                _currentResizeDirection = direction;
                _resizeStartPoint = e.GetPosition(this);
                _originalBounds = new Rect(Position.X, Position.Y, Width, Height);

                // Capture pointer
                e.Pointer.Capture(this);
                e.Handled = true;

                // Switch to manual sizing
                SizeToContent = SizeToContent.Manual;
            }
        }

        private void Window_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // This handles window-level pointer events that weren't handled by child controls
        }

        private void Window_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (_isResizing)
            {
                var currentPoint = e.GetPosition(this);
                var deltaX = currentPoint.X - _resizeStartPoint.X;
                var deltaY = currentPoint.Y - _resizeStartPoint.Y;

                double newX = Position.X;
                double newY = Position.Y;
                double newWidth = Width;
                double newHeight = Height;

                // Apply resize according to direction
                switch (_currentResizeDirection)
                {
                    case ResizeDirection.Top:
                        newY = _originalBounds.Y + deltaY;
                        newHeight = Math.Max(_originalBounds.Height - deltaY, MinHeight);
                        break;
                    case ResizeDirection.Bottom:
                        newHeight = Math.Max(_originalBounds.Height + deltaY, MinHeight);
                        break;
                    case ResizeDirection.Left:
                        newX = _originalBounds.X + deltaX;
                        newWidth = Math.Max(_originalBounds.Width - deltaX, MinWidth);
                        break;
                    case ResizeDirection.Right:
                        newWidth = Math.Max(_originalBounds.Width + deltaX, MinWidth);
                        break;
                    case ResizeDirection.TopLeft:
                        newX = _originalBounds.X + deltaX;
                        newY = _originalBounds.Y + deltaY;
                        newWidth = Math.Max(_originalBounds.Width - deltaX, MinWidth);
                        newHeight = Math.Max(_originalBounds.Height - deltaY, MinHeight);
                        break;
                    case ResizeDirection.TopRight:
                        newY = _originalBounds.Y + deltaY;
                        newWidth = Math.Max(_originalBounds.Width + deltaX, MinWidth);
                        newHeight = Math.Max(_originalBounds.Height - deltaY, MinHeight);
                        break;
                    case ResizeDirection.BottomLeft:
                        newX = _originalBounds.X + deltaX;
                        newWidth = Math.Max(_originalBounds.Width - deltaX, MinWidth);
                        newHeight = Math.Max(_originalBounds.Height + deltaY, MinHeight);
                        break;
                    case ResizeDirection.BottomRight:
                        newWidth = Math.Max(_originalBounds.Width + deltaX, MinWidth);
                        newHeight = Math.Max(_originalBounds.Height + deltaY, MinHeight);
                        break;
                }

                // Apply new size and position
                Position = new PixelPoint((int)newX, (int)newY);
                Width = newWidth;
                Height = newHeight;

                e.Handled = true;
            }
        }

        private void Window_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_isResizing)
            {
                _isResizing = false;
                _currentResizeDirection = ResizeDirection.None;
                e.Pointer.Capture(null);
                e.Handled = true;
            }
        }

        protected void AddContent(Control control)
        {
            ContentArea?.Children.Add(control);
        }

        protected void SetTitle(string title)
        {
            if (TitleBar == null) return;

            var titleText = new TextBlock
            {
                Text = title,
                Classes = { "PopupTitle" },
                Margin = new Thickness(12, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(titleText, 0);
            TitleBar.Children.Add(titleText);
        }

        private void MinimizeButton_Click(object? sender, RoutedEventArgs e)
        {
            if (ContentArea == null) return;

            if (_isMinimized)
            {
                // Restore
                ContentArea.IsVisible = true;
                _isMinimized = false;
                if (_normalSize.Width > 0)
                {
                    Width = _normalSize.Width;
                    Height = _normalSize.Height;
                }
            }
            else
            {
                // Minimize
                _normalSize = new Size(Width, Height);
                ContentArea.IsVisible = false;
                _isMinimized = true;

                // Collapse to just the titlebar
                if (TitleBar != null && TitleBar.Bounds.Height > 0)
                {
                    Height = TitleBar.Bounds.Height + 20;
                }
            }
        }

        private void MaximizeButton_Click(object? sender, RoutedEventArgs e)
        {
            // Toggle between maximized and normal
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                WindowState = WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object? sender, RoutedEventArgs e)
        {
            // Animate closing with a subtle scale and fade effect
            if (MainBorder != null)
            {
                // Create a combined transform group for more complex animation
                var transformGroup = new TransformGroup();
                var scaleTransform = new ScaleTransform(1, 1);
                var translateTransform = new TranslateTransform(0, 0);
                transformGroup.Children.Add(scaleTransform);
                transformGroup.Children.Add(translateTransform);
                MainBorder.RenderTransform = transformGroup;

                // Number of animation steps
                int steps = 12;
                int currentStep = 0;
                
                // Create timer for the closing animation
                var animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; // ~60fps
                animationTimer.Tick += (s, args) =>
                {
                    currentStep++;
                    if (currentStep > steps)
                    {
                        animationTimer.Stop();
                        
                        // Ensure we call base.Hide() after animation completes
                        Dispatcher.UIThread.Post(() => base.Hide(), DispatcherPriority.Background);
                        return;
                    }
                    
                    double progress = (double)currentStep / steps;
                    
                    // Cubic ease out for more natural motion
                    double easedProgress = 1 - Math.Pow(1 - progress, 3);
                    
                    // Animate opacity
                    this.Opacity = 1 - easedProgress;
                    
                    // Animate scale (subtle shrink effect)
                    scaleTransform.ScaleX = 1 - (0.05 * easedProgress);
                    scaleTransform.ScaleY = 1 - (0.05 * easedProgress);
                    
                    // Animate position (subtle upward movement)
                    translateTransform.Y = -5 * easedProgress;
                };
                
                // Start the animation
                animationTimer.Start();
            }
            else
            {
                // Fallback if animation can't be applied
                base.Hide();
            }
        }

        private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // Check this is really the titlebar and not a child control
            if (e.Source == TitleBar)
            {
                if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                {
                    _isDragging = true;
                    _dragStartPoint = e.GetPosition(this);
                    e.Handled = true;
                }
            }
        }

        private void TitleBar_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (_isDragging)
            {
                Point currentPoint = e.GetPosition(this);
                Vector delta = currentPoint - _dragStartPoint;

                Position = new PixelPoint(
                    Position.X + (int)delta.X,
                    Position.Y + (int)delta.Y
                );

                e.Handled = true;
            }
        }

        private void TitleBar_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _isDragging = false;
            e.Handled = true;
        }
    }

    public enum ResizeDirection
    {
        None,
        Top,
        Bottom,
        Left,
        Right,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }
}