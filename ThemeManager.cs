using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Animation;
using System;
using System.Collections.Generic;
using Avalonia.Styling;

namespace HoveringBallApp
{
    public enum AppTheme
    {
        Light,
        Dark,
        Brown
    }

    public class ThemeManager
    {
        private static ThemeManager _instance;
        public static ThemeManager Instance => _instance ??= new ThemeManager();

        private AppTheme _currentTheme = AppTheme.Brown; // Default to brown theme

        public event EventHandler<AppTheme> ThemeChanged;

        public AppTheme CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (_currentTheme != value)
                {
                    _currentTheme = value;
                    ThemeChanged?.Invoke(this, _currentTheme);
                }
            }
        }

        // Add support for animated theme transitions
        private readonly TimeSpan _themeTransitionDuration = TimeSpan.FromMilliseconds(300);

        private ThemeManager()
        {
            // Private constructor for singleton
        }

        public void ToggleTheme()
        {
            // Toggle between all three themes: Dark -> Light -> Brown -> Dark
            CurrentTheme = CurrentTheme switch
            {
                AppTheme.Dark => AppTheme.Light,
                AppTheme.Light => AppTheme.Brown,
                AppTheme.Brown => AppTheme.Dark,
                _ => AppTheme.Dark
            };
        }

        public void ApplyTheme(Window window)
        {
            if (window == null) return;

            // Update controls based on current theme
            UpdateThemeClasses(window, CurrentTheme);
        }

        private void UpdateThemeClasses(Control control, AppTheme theme)
        {
            // Apply theme classes to this control
            if (control.Classes.Contains("PopupBox") ||
                control.Classes.Contains("PopupTitle") ||
                control.Classes.Contains("PopupInput") ||
                control.Classes.Contains("WindowControl") ||
                control.Classes.Contains("HoveringBall"))
            {
                // Animate transition for theme-specific controls
                AnimateThemeTransition(control);

                control.Classes.Remove("Light");
                control.Classes.Remove("Dark");
                control.Classes.Remove("Brown");

                switch (theme)
                {
                    case AppTheme.Light:
                        control.Classes.Add("Light");
                        break;
                    case AppTheme.Dark:
                        control.Classes.Add("Dark");
                        break;
                    case AppTheme.Brown:
                        control.Classes.Add("Brown");
                        break;
                }
            }

            // Handle standard elements that need theme-specific styles
            if (control is TextBlock textBlock)
            {
                textBlock.Classes.Remove("Light");
                textBlock.Classes.Remove("Dark");
                textBlock.Classes.Remove("Brown");

                switch (theme)
                {
                    case AppTheme.Light:
                        textBlock.Foreground = new SolidColorBrush(Color.Parse("#333333"));
                        textBlock.Classes.Add("Light");
                        break;
                    case AppTheme.Dark:
                        textBlock.Foreground = new SolidColorBrush(Colors.White);
                        textBlock.Classes.Add("Dark");
                        break;
                    case AppTheme.Brown:
                        textBlock.Foreground = new SolidColorBrush(Color.Parse("#F5F5DC")); // Beige/light color on brown
                        textBlock.Classes.Add("Brown");
                        break;
                }
            }
            else if (control is TextBox textBox)
            {
                textBox.Classes.Remove("Light");
                textBox.Classes.Remove("Dark");
                textBox.Classes.Remove("Brown");

                switch (theme)
                {
                    case AppTheme.Light:
                        textBox.Foreground = new SolidColorBrush(Colors.Black); // Explicitly black text for light theme
                        textBox.Background = new SolidColorBrush(Colors.White);
                        textBox.CaretBrush = new SolidColorBrush(Color.Parse("#333333"));
                        textBox.Classes.Add("Light");
                        break;
                    case AppTheme.Dark:
                        textBox.Foreground = new SolidColorBrush(Colors.White);
                        textBox.Background = new SolidColorBrush(Color.Parse("#444444"));
                        textBox.CaretBrush = new SolidColorBrush(Colors.White);
                        textBox.Classes.Add("Dark");
                        break;
                    case AppTheme.Brown:
                        textBox.Foreground = new SolidColorBrush(Color.Parse("#5D4037")); // Dark brown text
                        textBox.Background = new SolidColorBrush(Color.Parse("#F5DEB3")); // Wheat background
                        textBox.CaretBrush = new SolidColorBrush(Color.Parse("#5D4037"));
                        textBox.Classes.Add("Brown");
                        break;
                }
            }
            else if (control is Button button && !button.Classes.Contains("WindowControl"))
            {
                button.Classes.Remove("Light");
                button.Classes.Remove("Dark");
                button.Classes.Remove("Brown");

                switch (theme)
                {
                    case AppTheme.Light:
                        button.Foreground = new SolidColorBrush(Color.Parse("#333333"));
                        button.Background = new SolidColorBrush(Color.Parse("#DDDDDD"));
                        button.Classes.Add("Light");
                        break;
                    case AppTheme.Dark:
                        button.Foreground = new SolidColorBrush(Colors.White);
                        button.Background = new SolidColorBrush(Color.Parse("#444444"));
                        button.Classes.Add("Dark");
                        break;
                    case AppTheme.Brown:
                        button.Foreground = new SolidColorBrush(Colors.White);
                        button.Background = new SolidColorBrush(Color.Parse("#8B5A2B")); // Wood brown
                        button.Classes.Add("Brown");
                        break;
                }
            }
            else if (control is ComboBox comboBox)
            {
                comboBox.Classes.Remove("Light");
                comboBox.Classes.Remove("Dark");
                comboBox.Classes.Remove("Brown");

                switch (theme)
                {
                    case AppTheme.Light:
                        comboBox.Foreground = new SolidColorBrush(Color.Parse("#333333"));
                        comboBox.Background = new SolidColorBrush(Colors.White);
                        comboBox.Classes.Add("Light");
                        break;
                    case AppTheme.Dark:
                        comboBox.Foreground = new SolidColorBrush(Colors.White);
                        comboBox.Background = new SolidColorBrush(Color.Parse("#444444"));
                        comboBox.Classes.Add("Dark");
                        break;
                    case AppTheme.Brown:
                        comboBox.Foreground = new SolidColorBrush(Color.Parse("#5D4037"));
                        comboBox.Background = new SolidColorBrush(Color.Parse("#F5DEB3"));
                        comboBox.Classes.Add("Brown");
                        break;
                }
            }
            else if (control is CheckBox checkBox)
            {
                checkBox.Classes.Remove("Light");
                checkBox.Classes.Remove("Dark");
                checkBox.Classes.Remove("Brown");

                switch (theme)
                {
                    case AppTheme.Light:
                        checkBox.Foreground = new SolidColorBrush(Color.Parse("#333333"));
                        checkBox.Classes.Add("Light");
                        break;
                    case AppTheme.Dark:
                        checkBox.Foreground = new SolidColorBrush(Colors.White);
                        checkBox.Classes.Add("Dark");
                        break;
                    case AppTheme.Brown:
                        checkBox.Foreground = new SolidColorBrush(Colors.White);
                        checkBox.Classes.Add("Brown");
                        break;
                }
            }
            else if (control is RadioButton radioButton)
            {
                radioButton.Classes.Remove("Light");
                radioButton.Classes.Remove("Dark");
                radioButton.Classes.Remove("Brown");

                switch (theme)
                {
                    case AppTheme.Light:
                        radioButton.Foreground = new SolidColorBrush(Color.Parse("#333333"));
                        radioButton.Classes.Add("Light");
                        break;
                    case AppTheme.Dark:
                        radioButton.Foreground = new SolidColorBrush(Colors.White);
                        radioButton.Classes.Add("Dark");
                        break;
                    case AppTheme.Brown:
                        radioButton.Foreground = new SolidColorBrush(Colors.White);
                        radioButton.Classes.Add("Brown");
                        break;
                }
            }
            else if (control is Separator separator)
            {
                separator.Classes.Remove("Light");
                separator.Classes.Remove("Dark");
                separator.Classes.Remove("Brown");

                switch (theme)
                {
                    case AppTheme.Light:
                        separator.Background = new SolidColorBrush(Color.Parse("#22000000"));
                        separator.Classes.Add("Light");
                        break;
                    case AppTheme.Dark:
                        separator.Background = new SolidColorBrush(Color.Parse("#33FFFFFF"));
                        separator.Classes.Add("Dark");
                        break;
                    case AppTheme.Brown:
                        separator.Background = new SolidColorBrush(Color.Parse("#40FFFFFF"));
                        separator.Classes.Add("Brown");
                        break;
                }
            }

            // Recursively apply to children
            if (control is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    if (child is Control childControl)
                    {
                        UpdateThemeClasses(childControl, theme);
                    }
                }
            }
            else if (control is ContentControl contentControl && contentControl.Content is Control contentAsControl)
            {
                UpdateThemeClasses(contentAsControl, theme);
            }
        }

        private void AnimateThemeTransition(Control control)
        {
            // Add a subtle fade transition for theme changes
            var opacityAnimation = new Animation
            {
                Duration = _themeTransitionDuration,
                FillMode = FillMode.Forward
            };

            opacityAnimation.Children.Add(new KeyFrame
            {
                Cue = new Cue(0.0),
                Setters = { new Setter(Control.OpacityProperty, 0.85) }
            });

            opacityAnimation.Children.Add(new KeyFrame
            {
                Cue = new Cue(1.0),
                Setters = { new Setter(Control.OpacityProperty, 1.0) }
            });

            // Only animate if control is visible
            if (control.IsVisible)
            {
                opacityAnimation.RunAsync(control);
            }
        }

        // Get appropriate colors based on current theme
        public Color GetPrimaryColor()
        {
            return CurrentTheme switch
            {
                AppTheme.Light => Color.Parse("#0078D7"),
                AppTheme.Dark => Color.Parse("#3393DF"),
                AppTheme.Brown => Color.Parse("#CD853F"),
                _ => Color.Parse("#3393DF"),
            };
        }

        public Color GetBackgroundColor()
        {
            return CurrentTheme switch
            {
                AppTheme.Light => Color.Parse("#F5F5F5"),
                AppTheme.Dark => Color.Parse("#2D2D2D"),
                AppTheme.Brown => Color.Parse("#8B5A2B"),
                _ => Color.Parse("#2D2D2D"),
            };
        }

        public Color GetTextColor()
        {
            return CurrentTheme switch
            {
                AppTheme.Light => Color.Parse("#333333"),
                AppTheme.Dark => Colors.White,
                AppTheme.Brown => Color.Parse("#F5F5DC"),
                _ => Colors.White,
            };
        }
    }
}