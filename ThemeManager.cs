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
        System
    }

    public class ThemeManager
    {
        private static ThemeManager _instance;
        public static ThemeManager Instance => _instance ??= new ThemeManager();

        private AppTheme _currentTheme = AppTheme.Dark; // Default to dark theme

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

        // Standardized animation durations for consistency
        private readonly TimeSpan _themeTransitionDuration = TimeSpan.FromMilliseconds(150);
        private readonly TimeSpan _elementTransitionDuration = TimeSpan.FromMilliseconds(150);
        private readonly TimeSpan _contentTransitionDuration = TimeSpan.FromMilliseconds(200);

        private ThemeManager()
        {
            // Private constructor for singleton
        }

        public void ToggleTheme()
        {
            // Toggle between themes: Dark -> Light -> System -> Dark
            CurrentTheme = CurrentTheme switch
            {
                AppTheme.Dark => AppTheme.Light,
                AppTheme.Light => AppTheme.System,
                AppTheme.System => AppTheme.Dark,
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
                control.Classes.Remove("System");

                switch (theme)
                {
                    case AppTheme.Light:
                        control.Classes.Add("Light");
                        break;
                    case AppTheme.Dark:
                        control.Classes.Add("Dark");
                        break;
                    case AppTheme.System:
                        // Detect system theme and apply the appropriate theme
                        var isDarkMode = IsSystemInDarkMode();
                        control.Classes.Add(isDarkMode ? "Dark" : "Light");
                        break;
                }
            }

            // Handle standard elements that need theme-specific styles
            if (control is TextBlock textBlock)
            {
                textBlock.Classes.Remove("Light");
                textBlock.Classes.Remove("Dark");
                textBlock.Classes.Remove("System");

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
                    case AppTheme.System:
                        // Detect system theme and apply the appropriate theme
                        var isDarkMode = IsSystemInDarkMode();
                        textBlock.Foreground = new SolidColorBrush(isDarkMode ? Colors.White : Color.Parse("#333333"));
                        textBlock.Classes.Add(isDarkMode ? "Dark" : "Light");
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
                        textBox.Background = new SolidColorBrush(Color.Parse("#1E1E1E"));
                        textBox.CaretBrush = new SolidColorBrush(Colors.White);
                        textBox.Classes.Add("Dark");
                        break;
                    case AppTheme.System:
                        var isDarkMode = IsSystemInDarkMode();
                        textBox.Foreground = new SolidColorBrush(isDarkMode ? Colors.White : Colors.Black);
                        textBox.Background = new SolidColorBrush(isDarkMode ? Color.Parse("#1E1E1E") : Colors.White);
                        textBox.CaretBrush = new SolidColorBrush(isDarkMode ? Colors.White : Color.Parse("#333333"));
                        textBox.Classes.Add(isDarkMode ? "Dark" : "Light");
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
                        button.Background = new SolidColorBrush(Color.Parse("#2D2D30"));
                        button.Classes.Add("Dark");
                        break;
                    case AppTheme.System:
                        var isDarkMode = IsSystemInDarkMode();
                        button.Foreground = new SolidColorBrush(isDarkMode ? Colors.White : Color.Parse("#333333"));
                        button.Background = new SolidColorBrush(isDarkMode ? Color.Parse("#2D2D30") : Color.Parse("#DDDDDD"));
                        button.Classes.Add(isDarkMode ? "Dark" : "Light");
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
                        comboBox.Background = new SolidColorBrush(Color.Parse("#1E1E1E"));
                        comboBox.Classes.Add("Dark");
                        break;
                    case AppTheme.System:
                        var isDarkMode = IsSystemInDarkMode();
                        comboBox.Foreground = new SolidColorBrush(isDarkMode ? Colors.White : Color.Parse("#333333"));
                        comboBox.Background = new SolidColorBrush(isDarkMode ? Color.Parse("#1E1E1E") : Colors.White);
                        comboBox.Classes.Add(isDarkMode ? "Dark" : "Light");
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
                    case AppTheme.System:
                        var isDarkMode = IsSystemInDarkMode();
                        checkBox.Foreground = new SolidColorBrush(isDarkMode ? Colors.White : Color.Parse("#333333"));
                        checkBox.Classes.Add(isDarkMode ? "Dark" : "Light");
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
                    case AppTheme.System:
                        var isDarkMode = IsSystemInDarkMode();
                        radioButton.Foreground = new SolidColorBrush(isDarkMode ? Colors.White : Color.Parse("#333333"));
                        radioButton.Classes.Add(isDarkMode ? "Dark" : "Light");
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
                    case AppTheme.System:
                        var isDarkMode = IsSystemInDarkMode();
                        separator.Background = new SolidColorBrush(isDarkMode ? Color.Parse("#33FFFFFF") : Color.Parse("#22000000"));
                        separator.Classes.Add(isDarkMode ? "Dark" : "Light");
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
            // Apply GPU-accelerated transitions for theme changes
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
        
        // Detect system light/dark mode
        public bool IsSystemInDarkMode()
        {
            try
            {
                // This is a simplified approach - in a real implementation, we would use platform-specific APIs
                // to properly detect the system theme on Windows, macOS, and Linux
                
                // For now, we'll check if it's after 7PM or before 7AM as a simple approximation
                int hour = DateTime.Now.Hour;
                return hour >= 19 || hour < 7;
                
                // In a real implementation, we would use:
                // Windows: Registry or UWP APIs
                // macOS: NSAppearance APIs
                // Linux: Check desktop environment settings
            }
            catch
            {
                // Default to dark mode if we can't detect
                return true;
            }
        }

        // Get appropriate colors based on current theme
        public Color GetPrimaryColor()
        {
            if (CurrentTheme == AppTheme.System)
            {
                return IsSystemInDarkMode() ? Color.Parse("#4B9EFF") : Color.Parse("#0078D7");
            }
            
            return CurrentTheme switch
            {
                AppTheme.Light => Color.Parse("#0078D7"),
                AppTheme.Dark => Color.Parse("#4B9EFF"),
                _ => Color.Parse("#3393DF"),
            };
        }

        public Color GetBackgroundColor()
        {
            if (CurrentTheme == AppTheme.System)
            {
                return IsSystemInDarkMode() ? Color.Parse("#121212") : Color.Parse("#F5F5F5");
            }
            
            return CurrentTheme switch
            {
                AppTheme.Light => Color.Parse("#F5F5F5"),
                AppTheme.Dark => Color.Parse("#121212"),
                _ => Color.Parse("#2D2D2D"),
            };
        }

        public Color GetTextColor()
        {
            if (CurrentTheme == AppTheme.System)
            {
                return IsSystemInDarkMode() ? Colors.White : Color.Parse("#333333");
            }
            
            return CurrentTheme switch
            {
                AppTheme.Light => Color.Parse("#333333"),
                AppTheme.Dark => Colors.White,
                _ => Colors.White,
            };
        }
    }
}