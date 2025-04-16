using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;

namespace HoveringBallApp
{
    /// <summary>
    /// Helper class to handle animations in a way that's compatible with Avalonia's implementation
    /// </summary>
    public static class AnimationHelper
    {
        /// <summary>
        /// Creates and runs a scale animation on a control using a timer-based approach
        /// </summary>
        public static void AnimateScale(Control target, double fromScale, double toScale, TimeSpan duration, Action? onComplete = null)
        {
            if (target == null) return;

            // Ensure the control has a proper RenderTransformOrigin
            target.RenderTransformOrigin = RelativePoint.Center;

            // Create or get the ScaleTransform
            ScaleTransform? scaleTransform = null;
            if (target.RenderTransform is ScaleTransform existingTransform)
            {
                scaleTransform = existingTransform;
            }
            else
            {
                scaleTransform = new ScaleTransform(fromScale, fromScale);
                target.RenderTransform = scaleTransform;
            }

            // Calculate animation parameters
            int totalFrames = (int)(duration.TotalMilliseconds / 16); // ~60fps
            int currentFrame = 0;

            // Create and start the animation timer
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            timer.Tick += (s, e) =>
            {
                currentFrame++;
                if (currentFrame > totalFrames)
                {
                    // Animation complete
                    scaleTransform.ScaleX = toScale;
                    scaleTransform.ScaleY = toScale;
                    timer.Stop();
                    onComplete?.Invoke();
                    return;
                }

                // Calculate current scale with easing
                double progress = (double)currentFrame / totalFrames;
                double easedProgress = EaseOutCubic(progress);
                double currentScale = fromScale + (toScale - fromScale) * easedProgress;

                // Apply the scale
                scaleTransform.ScaleX = currentScale;
                scaleTransform.ScaleY = currentScale;
            };

            timer.Start();
        }

        /// <summary>
        /// Creates and runs a bounce animation on a control
        /// </summary>
        public static void AnimateBounce(Control target, double fromScale, double toScale, double overshoot, TimeSpan duration, Action? onComplete = null)
        {
            if (target == null) return;

            // Ensure the control has a proper RenderTransformOrigin
            target.RenderTransformOrigin = RelativePoint.Center;

            // Create or get the ScaleTransform
            ScaleTransform? scaleTransform = null;
            if (target.RenderTransform is ScaleTransform existingTransform)
            {
                scaleTransform = existingTransform;
            }
            else
            {
                scaleTransform = new ScaleTransform(fromScale, fromScale);
                target.RenderTransform = scaleTransform;
            }

            // Calculate animation parameters
            int totalFrames = (int)(duration.TotalMilliseconds / 16); // ~60fps
            int currentFrame = 0;

            // Create and start the animation timer
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            timer.Tick += (s, e) =>
            {
                currentFrame++;
                if (currentFrame > totalFrames)
                {
                    // Animation complete
                    scaleTransform.ScaleX = toScale;
                    scaleTransform.ScaleY = toScale;
                    timer.Stop();
                    onComplete?.Invoke();
                    return;
                }

                // Calculate current scale with bounce effect
                double progress = (double)currentFrame / totalFrames;
                double currentScale;

                if (progress < 0.4) // First 40% - go to peak
                {
                    double adjustedProgress = progress / 0.4;
                    currentScale = fromScale + (toScale + overshoot - fromScale) * EaseOutQuad(adjustedProgress);
                }
                else if (progress < 0.7) // Next 30% - bounce back
                {
                    double adjustedProgress = (progress - 0.4) / 0.3;
                    currentScale = (toScale + overshoot) - overshoot * EaseInOutQuad(adjustedProgress);
                }
                else // Final 30% - settle
                {
                    double adjustedProgress = (progress - 0.7) / 0.3;
                    double bounceValue = Math.Sin(adjustedProgress * Math.PI * 2) * overshoot * 0.3 * (1 - adjustedProgress);
                    currentScale = toScale + bounceValue;
                }

                // Apply the scale
                scaleTransform.ScaleX = currentScale;
                scaleTransform.ScaleY = currentScale;
            };

            timer.Start();
        }

        /// <summary>
        /// Creates and runs a pulsing animation on a control
        /// </summary>
        public static DispatcherTimer AnimatePulse(Control target, double baseScale, double pulseAmount, double frequency, Action? onComplete = null)
        {
            if (target == null)
                return new DispatcherTimer(); // Return dummy timer if target is null

            // Ensure the control has a proper RenderTransformOrigin
            target.RenderTransformOrigin = RelativePoint.Center;

            // Create or get the ScaleTransform
            ScaleTransform? scaleTransform = null;
            if (target.RenderTransform is ScaleTransform existingTransform)
            {
                scaleTransform = existingTransform;
            }
            else
            {
                scaleTransform = new ScaleTransform(baseScale, baseScale);
                target.RenderTransform = scaleTransform;
            }

            // Create and start the animation timer
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; // ~60fps
            double startTime = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;

            timer.Tick += (s, e) =>
            {
                double currentTime = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
                double elapsed = currentTime - startTime;

                // Calculate pulse using sine wave
                double pulse = baseScale + pulseAmount * Math.Sin(elapsed * frequency * Math.PI);

                // Apply the scale
                if (scaleTransform != null)
                {
                    scaleTransform.ScaleX = pulse;
                    scaleTransform.ScaleY = pulse;
                }
            };

            timer.Start();
            return timer;
        }

        /// <summary>
        /// Creates and runs a fade animation on a control
        /// </summary>
        public static void AnimateFade(Control target, double fromOpacity, double toOpacity, TimeSpan duration, Action? onComplete = null)
        {
            if (target == null) return;

            // Calculate animation parameters
            int totalFrames = (int)(duration.TotalMilliseconds / 16); // ~60fps
            int currentFrame = 0;

            // Create and start the animation timer
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            timer.Tick += (s, e) =>
            {
                currentFrame++;
                if (currentFrame > totalFrames)
                {
                    // Animation complete
                    target.Opacity = toOpacity;
                    timer.Stop();
                    onComplete?.Invoke();
                    return;
                }

                // Calculate current opacity with easing
                double progress = (double)currentFrame / totalFrames;
                double easedProgress = EaseOutCubic(progress);
                double currentOpacity = fromOpacity + (toOpacity - fromOpacity) * easedProgress;

                // Apply the opacity
                target.Opacity = currentOpacity;
            };

            timer.Start();
        }

        /// <summary>
        /// Creates and runs a move animation on a control using TranslateTransform
        /// </summary>
        public static void AnimateMove(Control target, Point from, Point to, TimeSpan duration, Action? onComplete = null)
        {
            if (target == null) return;

            // Create or get the TranslateTransform
            TranslateTransform? translateTransform = null;
            if (target.RenderTransform is TranslateTransform existingTransform)
            {
                translateTransform = existingTransform;
                from = new Point(existingTransform.X, existingTransform.Y);
            }
            else
            {
                translateTransform = new TranslateTransform(from.X, from.Y);
                target.RenderTransform = translateTransform;
            }

            // Calculate animation parameters
            int totalFrames = (int)(duration.TotalMilliseconds / 16); // ~60fps
            int currentFrame = 0;

            // Create and start the animation timer
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            timer.Tick += (s, e) =>
            {
                currentFrame++;
                if (currentFrame > totalFrames)
                {
                    // Animation complete
                    translateTransform.X = to.X;
                    translateTransform.Y = to.Y;
                    timer.Stop();
                    onComplete?.Invoke();
                    return;
                }

                // Calculate current position with easing
                double progress = (double)currentFrame / totalFrames;
                double easedProgress = EaseOutCubic(progress);
                double currentX = from.X + (to.X - from.X) * easedProgress;
                double currentY = from.Y + (to.Y - from.Y) * easedProgress;

                // Apply the translation
                translateTransform.X = currentX;
                translateTransform.Y = currentY;
            };

            timer.Start();
        }

        #region Easing Functions

        public static double EaseOutCubic(double t)
        {
            return 1 - Math.Pow(1 - t, 3);
        }

        public static double EaseOutQuad(double t)
        {
            return t * (2 - t);
        }

        public static double EaseInOutQuad(double t)
        {
            return t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2;
        }

        public static double EaseOutElastic(double t)
        {
            const double c4 = (2 * Math.PI) / 3;
            return t == 0 ? 0 : t == 1 ? 1 : Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * c4) + 1;
        }

        #endregion
    }
}