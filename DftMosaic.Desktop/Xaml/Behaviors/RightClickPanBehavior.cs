using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DftMosaic.Desktop.Xaml.Behaviors
{
    internal class RightClickPanBehavior : Behavior<ScrollViewer>
    {
        public static Point? GetPreviousPoint(DependencyObject obj)
        {
            return (Point?)obj.GetValue(PreviousPointProperty);
        }

        public static void SetPreviousPoint(DependencyObject obj, Point? value)
        {
            obj.SetValue(PreviousPointProperty, value);
        }

        public static readonly DependencyProperty PreviousPointProperty =
            DependencyProperty.RegisterAttached("PreviousPoint", typeof(Point?), typeof(RightClickPanBehavior), new PropertyMetadata(null));

        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.MouseMove += this.AssociatedObject_MouseMove;
        }

        protected override void OnDetaching()
        {
            this.AssociatedObject.MouseMove -= this.AssociatedObject_MouseMove;
            base.OnDetaching();
        }

        private void AssociatedObject_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.RightButton.HasFlag(MouseButtonState.Pressed))
            {
                var currentPoint = e.GetPosition(this.AssociatedObject);
                var previousPoint = GetPreviousPoint(this.AssociatedObject);
                var scrollViewer = this.AssociatedObject;
                if (previousPoint is not null)
                {
                    var xScroll = currentPoint.X - previousPoint.Value.X;
                    var yScroll = currentPoint.Y - previousPoint.Value.Y;
                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - xScroll);
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - yScroll);
                }
                SetPreviousPoint(this.AssociatedObject, currentPoint);
            }
            else
            {
                SetPreviousPoint(this.AssociatedObject, null);
            }
        }
    }
}
