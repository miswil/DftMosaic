using DftMosaic.Desktop.Xaml.Controllers;
using Microsoft.Xaml.Behaviors;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace DftMosaic.Desktop.Xaml.Behaviors
{
    internal class ClippingBehavior : Behavior<UIElement>
    {
        private ClippingAdorner clippingAorner;

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
            var clipper = behavior.clippingAorner;
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
                behavior.clippingAorner.ClippedArea = (Rect)clippedArea;
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
            this.clippingAorner = newClipper;
            newClipper.StartInitialDrag();
        }

        private void HideClippingAdorner()
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(this.AssociatedObject);
            if (adornerLayer is null)
            {
                return;
            }
            var olcClipper = this.clippingAorner;
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
