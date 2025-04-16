using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Layout;
using System;
using System.Runtime.InteropServices;

namespace HoveringBallApp
{
    /// <summary>
    /// Base class for popup windows with common functionality like
    /// dragging, minimizing, maximizing, resizing, and theme support
    /// </summary>
    public class PopupWindow : Window
    {
        protected Border MainBorder { get; private set; }
        protected Grid TitleBar { get; private set; }
        protected Panel ContentArea { get; private set; }
        protected Grid ResizeGrid { get; private set; }

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

        public PopupWindow()
        {
            // Configure window appearance
            SystemDecorations = SystemDecorations.None;
            Background = Brushes.Transparent;
            TransparencyLevelHint = new WindowTransparencyLevel[] { WindowTransparencyLevel.Transparent };
            ShowInTaskbar = false;

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
        }

        private void ThemeManager_ThemeChanged(object sender, AppTheme theme)
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

            // Main border with shadow effect
            MainBorder = new Border
            {
                Classes = { "PopupBox" },
                MinWidth = 250
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Title bar with controls
            TitleBar = new Grid
            {
                Background = Brushes.Transparent,
                Height = 30
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
                Margin = new Thickness(0, 0, 0, 0)
            };

            var minimizeButton = new Button
            {
                Classes = { "WindowControl" },
                Content = "─",
                FontWeight = FontWeight.Bold
            };
            minimizeButton.Click += MinimizeButton_Click;

            var maximizeButton = new Button
            {
                Classes = { "WindowControl" },
                Content = "□",
                FontWeight = FontWeight.Bold
            };
            maximizeButton.Click += MaximizeButton_Click;

            var closeButton = new Button
            {
                Classes = { "WindowControl" },
                Content = "✕",
                FontWeight = FontWeight.Bold
            };
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

        private void Window_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            // This handles window-level pointer events that weren't handled by child controls
        }

        private void Window_PointerMoved(object sender, PointerEventArgs e)
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

        private void Window_PointerReleased(object sender, PointerReleasedEventArgs e)
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
            ContentArea.Children.Add(control);
        }

        protected void SetTitle(string title)
        {
            var titleText = new TextBlock
            {
                Text = title,
                Classes = { "PopupTitle" },
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(titleText, 0);
            TitleBar.Children.Add(titleText);
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
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
                if (TitleBar.Bounds.Height > 0)
                {
                    Height = TitleBar.Bounds.Height + 20;
                }
            }
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void TitleBar_PointerPressed(object sender, PointerPressedEventArgs e)
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

        private void TitleBar_PointerMoved(object sender, PointerEventArgs e)
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

        private void TitleBar_PointerReleased(object sender, PointerReleasedEventArgs e)
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