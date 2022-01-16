using Microsoft.Xaml.Behaviors;
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DftMosaic.Desktop.Xaml.Behaviors
{
    internal class ScrollViewerContentZoomBehavior : Behavior<ScrollViewer>
    {
        public int ZoomScale
        {
            get { return (int)GetValue(ZoomScaleProperty); }
            set { SetValue(ZoomScaleProperty, value); }
        }

        public static readonly DependencyProperty ZoomScaleProperty =
            DependencyProperty.Register("ZoomScale",
                                        typeof(int),
                                        typeof(ScrollViewerContentZoomBehavior),
                                        new PropertyMetadata(100, ZoomScaleChanged));

        private static void ZoomScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (ScrollViewerContentZoomBehavior)d;
            var scrollViewer = behavior.AssociatedObject;
            var scale = ((int)e.NewValue) / 100.0;
            ScaleContent(scrollViewer, scale);
            ScrollToMouseCursorPosition(scrollViewer, scale);
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.PreviewMouseWheel += this.AssociatedObject_PreviewMouseWheel;
        }

        protected override void OnDetaching()
        {
            this.AssociatedObject.PreviewMouseWheel -= this.AssociatedObject_PreviewMouseWheel;
            base.OnDetaching();
        }

        private void AssociatedObject_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.Delta > 0)
                {
                    this.ZoomScale += 10;
                }
                else
                {
                    this.ZoomScale = Math.Max(10, this.ZoomScale - 10);
                }

                e.Handled = true;
            }
        }

        private static void ScaleContent(ScrollViewer scrollViewer, double scale)
        {
            var content = GetScrollViewerChild(scrollViewer);
            if (content is null)
            {
                return;
            }
            if (content.LayoutTransform is not ScaleTransform scaleTransform)
            {
                scaleTransform = new ScaleTransform();
                content.LayoutTransform = scaleTransform;
            }
            scaleTransform.ScaleX = scale;
            scaleTransform.ScaleY = scale;
        }

        private static void ScrollToMouseCursorPosition(ScrollViewer scrollViewer, double scale)
        {
            var content = GetScrollViewerChild(scrollViewer);
            var scrollViewerPostion = Mouse.GetPosition(scrollViewer);
            var contentPosition = Mouse.GetPosition(content);
            var hOffset = contentPosition.X * scale - scrollViewerPostion.X;
            var vOffset = contentPosition.Y * scale - scrollViewerPostion.Y;
            scrollViewer.ScrollToHorizontalOffset(hOffset);
            scrollViewer.ScrollToVerticalOffset(vOffset);
        }

        private static FrameworkElement? GetScrollViewerChild(ScrollViewer scrollViewer)
        {
            var getTemplateChild = typeof(ScrollViewer).GetMethod("GetTemplateChild", BindingFlags.Instance | BindingFlags.NonPublic);
            var getVisualChild = typeof(ScrollContentPresenter).GetMethod("GetVisualChild", BindingFlags.Instance | BindingFlags.NonPublic);
            var contentPresenter = getTemplateChild?.Invoke(scrollViewer, new[] { "PART_ScrollContentPresenter" });
            if (contentPresenter is null)
            {
                return null;
            }
            var scaledElement = getVisualChild?.Invoke(contentPresenter, new[] { (object)0 }) as FrameworkElement;
            return scaledElement;
        }
    }
}
