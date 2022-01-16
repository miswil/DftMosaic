using DftMosaic.Desktop.Xaml.Controllers;
using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;

namespace DftMosaic.Desktop.Xaml.Behaviors
{
    internal class ClippingBehavior : Behavior<UIElement>
    {
        public Rect? ClippedArea
        {
            get { return (Rect?)this.GetValue(ClippedAreaProperty); }
            set { this.SetValue(ClippedAreaProperty, value); }
        }

        public static readonly DependencyProperty ClippedAreaProperty = DependencyProperty.Register(
                nameof(ClippedArea),
                typeof(Rect?),
                typeof(ClippingBehavior),
                new PropertyMetadata(null,
                                     ClippedAreaChanged));

        public static ClippingAdorner GetClippingAdorner(DependencyObject obj)
        {
            return (ClippingAdorner)obj.GetValue(ClippingAdornerProperty);
        }

        public static void SetClippingAdorner(DependencyObject obj, ClippingAdorner value)
        {
            obj.SetValue(ClippingAdornerProperty, value);
        }

        public static readonly DependencyProperty ClippingAdornerProperty =
            DependencyProperty.RegisterAttached("ClippingAdorner", typeof(ClippingAdorner), typeof(ClippingBehavior), new PropertyMetadata(null));

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
            if (!e.LeftButton.HasFlag(MouseButtonState.Pressed))
            {
                return;
            }
            this.BeginClip(e.GetPosition(this.AssociatedObject));
        }

        private void BeginClip(Point position)
        {
            this.HideClippingAdorner();
            this.ShowClippingAdorner(position);
        }

        private static void ClippedAreaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (ClippingBehavior)d;
            var clipper = GetClippingAdorner(behavior);
            var clippedArea = (Rect?)e.NewValue;
            if (clipper is null)
            {
                return;
            }
            if (clippedArea is null)
            {
                behavior.HideClippingAdorner();
            }
            else
            {
                GetClippingAdorner(behavior).ClippedArea = (Rect)clippedArea;
            }
        }

        private void ShowClippingAdorner(Point position)
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(this.AssociatedObject);
            if (adornerLayer is null)
            {
                return;
            }
            var newClipper = new ClippingAdorner(this.AssociatedObject)
            {
                ClippedArea = new Rect(position, new Size(0, 0)),
            };
            newClipper.ClipedAreaResized += this.ClippedAreaResized;
            adornerLayer.Add(newClipper);
            SetClippingAdorner(this.AssociatedObject, newClipper);
        }

        private void HideClippingAdorner()
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(this.AssociatedObject);
            if (adornerLayer is null)
            {
                return;
            }
            var olcClipper = GetClippingAdorner(this.AssociatedObject);
            if (olcClipper is not null)
            {
                olcClipper.ClipedAreaResized -= this.ClippedAreaResized;
                adornerLayer.Remove(olcClipper);
            }
        }

        private void ClippedAreaResized(object? sender, ClipEventArgs e)
        {
            this.ClippedArea = e.ClipedArea;
        }
    }
}
