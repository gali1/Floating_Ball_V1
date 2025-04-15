using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace HoveringBallApp
{
    public enum AppTheme
    {
        Light,
        Dark
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

        private ThemeManager()
        {
            // Private constructor for singleton
        }

        public void ToggleTheme()
        {
            CurrentTheme = CurrentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
        }

        public void ApplyTheme(Window window)
        {
            if (window == null) return;

            // Update controls based on current theme
            var isDark = CurrentTheme == AppTheme.Dark;

            // Find all elements that need theme changes
            UpdateThemeClasses(window, isDark);
        }

        private void UpdateThemeClasses(Control control, bool isDark)
        {
            // Apply theme classes to this control
            if (control.Classes.Contains("PopupBox") ||
                control.Classes.Contains("PopupTitle") ||
                control.Classes.Contains("PopupInput") ||
                control.Classes.Contains("WindowControl") ||
                control.Classes.Contains("HoveringBall"))
            {
                if (isDark)
                {
                    control.Classes.Remove("Light");
                    control.Classes.Add("Dark");
                }
                else
                {
                    control.Classes.Remove("Dark");
                    control.Classes.Add("Light");
                }
            }

            // Handle standard elements that need theme-specific styles
            if (control is TextBlock textBlock)
            {
                if (isDark)
                {
                    textBlock.Foreground = new SolidColorBrush(Colors.White);
                    textBlock.Classes.Remove("Light");
                    textBlock.Classes.Add("Dark");
                }
                else
                {
                    textBlock.Foreground = new SolidColorBrush(Color.Parse("#333333"));
                    textBlock.Classes.Remove("Dark");
                    textBlock.Classes.Add("Light");
                }
            }
            else if (control is TextBox textBox)
            {
                if (isDark)
                {
                    textBox.Foreground = new SolidColorBrush(Colors.White);
                    textBox.Background = new SolidColorBrush(Color.Parse("#444444"));
                    textBox.CaretBrush = new SolidColorBrush(Colors.White);
                    textBox.Classes.Remove("Light");
                    textBox.Classes.Add("Dark");
                }
                else
                {
                    textBox.Foreground = new SolidColorBrush(Color.Parse("#333333"));
                    textBox.Background = new SolidColorBrush(Colors.White);
                    textBox.CaretBrush = new SolidColorBrush(Color.Parse("#333333"));
                    textBox.Classes.Remove("Dark");
                    textBox.Classes.Add("Light");
                }
            }
            else if (control is Button button && !button.Classes.Contains("WindowControl"))
            {
                if (isDark)
                {
                    button.Foreground = new SolidColorBrush(Colors.White);
                    button.Background = new SolidColorBrush(Color.Parse("#444444"));
                    button.Classes.Remove("Light");
                    button.Classes.Add("Dark");
                }
                else
                {
                    button.Foreground = new SolidColorBrush(Color.Parse("#333333"));
                    button.Background = new SolidColorBrush(Color.Parse("#DDDDDD"));
                    button.Classes.Remove("Dark");
                    button.Classes.Add("Light");
                }
            }

            // Recursively apply to children (simplified for older Avalonia versions)
            if (control is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    if (child is Control childControl)
                    {
                        UpdateThemeClasses(childControl, isDark);
                    }
                }
            }
            else if (control is ContentControl contentControl && contentControl.Content is Control contentAsControl)
            {
                UpdateThemeClasses(contentAsControl, isDark);
            }
        }
    }
}