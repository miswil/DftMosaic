using DftMosaic.Desktop.Xaml.Controllers;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;

namespace DftMosaic.Desktop.Xaml.Behaviors
{
    internal class ClippingBehavior : Behavior<UIElement>
    {
        public IList<Rect> ClippedAreas
        {
            get { return (IList<Rect>)this.GetValue(ClippedAreasProperty); }
            set { this.SetValue(ClippedAreasProperty, value); }
        }

        public static readonly DependencyProperty ClippedAreasProperty = DependencyProperty.Register(
                nameof(ClippedAreas),
                typeof(IList<Rect>),
                typeof(ClippingBehavior),
                new PropertyMetadata(null,
                                     ClippedAreasChanged));

        private static void ClippedAreasChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (ClippingBehavior)d;
            var clippedAreas = (ICollection<Rect>)e.NewValue;

            behavior.HideAllClippingAdorner();
            if (clippedAreas is not null)
            {
                foreach (var area in clippedAreas)
                {
                    behavior.ShowClippingAdorner(area);
                }
            }
            if (e.OldValue is INotifyCollectionChanged oldNcc)
            {
                oldNcc.CollectionChanged -= behavior.ClippedAreasCollectionChanged;
            }
            if (e.NewValue is INotifyCollectionChanged newNcc)
            {
                newNcc.CollectionChanged += behavior.ClippedAreasCollectionChanged;
            }
        }

        public ClippingBehavior()
        {
            this.ClippedAreas = new List<Rect>();
        }

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
            var area = new Rect(position, new Size(0, 0));
            var adorner = this.ShowClippingAdorner(area);
            if (adorner != null)
            {
                this.AddClippedAdornerWithoutSelfNotification(area);
                adorner.StartInitialDrag();
            }
        }

        private void AddClippedAdornerWithoutSelfNotification(Rect area)
        {
            var ncc = this.ClippedAreas as INotifyCollectionChanged;
            if (ncc is not null)
            {
                ncc.CollectionChanged -= this.ClippedAreasCollectionChanged;
            }
            this.ClippedAreas.Add(area);
            if (ncc is not null)
            {
                ncc.CollectionChanged += this.ClippedAreasCollectionChanged;
            }
        }

        private void ClippedAreasCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
#pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です。
#pragma warning disable CS8605 // null の可能性がある値をボックス化解除しています。
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    this.ShowClippingAdorner((Rect)e.NewItems[0]);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    this.HideClippingAdorner((Rect)e.OldItems[0]);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    this.ClippingAdorners()[e.NewStartingIndex].ClippedArea = (Rect)e.NewItems[0];
                    break;
                case NotifyCollectionChangedAction.Move:
                    throw new NotImplementedException();
                case NotifyCollectionChangedAction.Reset:
                    this.HideAllClippingAdorner();
                    break;
                default:
                    throw new InvalidOperationException();
#pragma warning restore CS8602 // null 参照の可能性があるものの逆参照です。
#pragma warning restore CS8605 // null の可能性がある値をボックス化解除しています。
            }
        }

        private ClippingAdorner? ShowClippingAdorner(Rect area)
        {
            if (this.AssociatedObject is null)
            {
                return null;
            }
            var adornerLayer = AdornerLayer.GetAdornerLayer(this.AssociatedObject);
            if (adornerLayer is null)
            {
                return null;
            }
            var newClipper = new ClippingAdorner(this.AssociatedObject)
            {
                ClippedArea = area,
            };
            newClipper.ClipedAreaResized += this.ClippedAreaResized;
            adornerLayer.Add(newClipper);
            return newClipper;
        }

        private void HideClippingAdorner(Rect rect)
        {
            if (this.AssociatedObject is null)
            {
                return;
            }
            var adornerLayer = AdornerLayer.GetAdornerLayer(this.AssociatedObject);
            if (adornerLayer is null)
            {
                return;
            }
            var adorner = this.ClippingAdorners()?.FirstOrDefault(a => a.ClippedArea == rect);
            if (adorner is not null)
            {
                adorner.ClipedAreaResized -= this.ClippedAreaResized;
                adornerLayer.Remove(adorner);
            }
        }

        private void HideAllClippingAdorner()
        {
            if (this.AssociatedObject is null)
            {
                return;
            }
            var adornerLayer = AdornerLayer.GetAdornerLayer(this.AssociatedObject);
            if (adornerLayer is null)
            {
                return;
            }
            foreach (var adorner in this.ClippingAdorners() ?? Enumerable.Empty<ClippingAdorner>())
            {
                adorner.ClipedAreaResized -= this.ClippedAreaResized;
                adornerLayer.Remove(adorner);
            }
        }

        private void ClippedAreaResized(object? sender, ClipEventArgs e)
        {
            var index = -1;
            var adorners = this.ClippingAdorners();
#pragma warning disable CS8602 // イベントハンドラに入る時点でnullとならない保証済み
            for (int i = 0; i < adorners.Count; i++)
#pragma warning restore CS8602 // null 参照の可能性があるものの逆参照です。
            {
                if (adorners[i].Equals(sender))
                {
                    index = i;
                    break;
                }
            }
            if (index >= 0)
            {
                this.ClippedAreas[index] = e.ClipedArea;
            }
        }

        private IReadOnlyList<ClippingAdorner>? ClippingAdorners()
        {
            if (this.AssociatedObject is null)
            {
                return null;
            }
            var adornerLayer = AdornerLayer.GetAdornerLayer(this.AssociatedObject);
            if (adornerLayer is null)
            {
                return null;
            }
            return adornerLayer.GetAdorners(this.AssociatedObject).OfType<ClippingAdorner>().ToList();
        }
    }
}
