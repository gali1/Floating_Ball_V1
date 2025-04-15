using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;

namespace HoveringBallApp
{
    /// <summary>
    /// Base class for popup windows with common functionality like
    /// dragging, minimizing, maximizing, and theme support
    /// </summary>
    public class PopupWindow : Window
    {
        protected Border MainBorder { get; private set; }
        protected Grid TitleBar { get; private set; }
        protected Panel ContentArea { get; private set; }

        private bool _isMinimized = false;
        private Size _normalSize;
        private bool _isDragging = false;
        private Point _dragStartPoint;

        public PopupWindow()
        {
            // Configure window appearance
            SystemDecorations = SystemDecorations.None;
            Background = Brushes.Transparent;
            TransparencyLevelHint = new WindowTransparencyLevel[] { WindowTransparencyLevel.Transparent };
            ShowInTaskbar = false;
            SizeToContent = SizeToContent.WidthAndHeight;

            // Subscribe to theme changes
            ThemeManager.Instance.ThemeChanged += ThemeManager_ThemeChanged;
            this.Closed += (s, e) => ThemeManager.Instance.ThemeChanged -= ThemeManager_ThemeChanged;

            // Create layout
            InitializeLayout();
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
            MainBorder = new Border
            {
                Classes = { "PopupBox" },
                MinWidth = 250
            };

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Title bar with controls
            TitleBar = new Grid();
            TitleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            TitleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TitleBar.PointerPressed += TitleBar_PointerPressed;
            TitleBar.PointerMoved += TitleBar_PointerMoved;
            TitleBar.PointerReleased += TitleBar_PointerReleased;

            // Window controls
            var controlsPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, 0, 5)
            };

            var minimizeButton = new Button
            {
                Classes = { "WindowControl" },
                Content = "−"
            };
            minimizeButton.Click += MinimizeButton_Click;

            var maximizeButton = new Button
            {
                Classes = { "WindowControl" },
                Content = "□"
            };
            maximizeButton.Click += MaximizeButton_Click;

            controlsPanel.Children.Add(minimizeButton);
            controlsPanel.Children.Add(maximizeButton);

            Grid.SetColumn(controlsPanel, 1);
            TitleBar.Children.Add(controlsPanel);

            Grid.SetRow(TitleBar, 0);
            mainGrid.Children.Add(TitleBar);

            // Content area
            ContentArea = new StackPanel
            {
                Spacing = 10
            };

            Grid.SetRow(ContentArea, 1);
            mainGrid.Children.Add(ContentArea);

            MainBorder.Child = mainGrid;
            this.Content = MainBorder;

            // Apply current theme
            ApplyTheme(ThemeManager.Instance.CurrentTheme);
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
                Margin = new Thickness(5, 0, 0, 5)
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
                    this.Width = _normalSize.Width;
                }
            }
            else
            {
                // Minimize
                _normalSize = this.Bounds.Size;
                ContentArea.IsVisible = false;
                _isMinimized = true;
            }
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle between maximized and normal
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void TitleBar_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _isDragging = true;
                _dragStartPoint = e.GetPosition(this);
                e.Handled = true;
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
}