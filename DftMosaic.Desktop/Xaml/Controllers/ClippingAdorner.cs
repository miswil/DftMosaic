using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace DftMosaic.Desktop.Xaml.Controllers
{
    internal class ClippingAdorner : Adorner
    {
        public const int ResizeCornerThumbSize = 12;

        // initial arrangement
        // c1 h1 c2
        // v1    v2
        // c4 h2 c3
        private readonly Thumb corner1;
        private readonly Thumb corner2;
        private readonly Thumb corner3;
        private readonly Thumb corner4;
        private readonly Thumb hSide1;
        private readonly Thumb hSide2;
        private readonly Thumb vSide1;
        private readonly Thumb vSide2;
        private readonly Thumb plane;

        private readonly VisualCollection visualChildren;

        public Rect ClippedArea
        {
            get { return (Rect)GetValue(ClippedAreaProperty); }
            set { SetValue(ClippedAreaProperty, value); }
        }

        public static readonly DependencyProperty ClippedAreaProperty =
            DependencyProperty.Register(
                nameof(ClippedArea),
                typeof(Rect),
                typeof(ClippingAdorner),
                new FrameworkPropertyMetadata(default(Rect),
                                              FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender,
                                              ClippedAreaChanged));

        public bool IsHorizontallyTurnedOver
        {
            get { return (bool)GetValue(IsHorizontallyTurnedOverProperty); }
            private set { SetValue(IsHorizontallyTurnedOverPropertyKey, value); }
        }

        // Using a DependencyProperty as the backing store for IsHorizontallyTurnedOver.  This enables animation, styling, binding, etc...
        public static readonly DependencyPropertyKey IsHorizontallyTurnedOverPropertyKey =
            DependencyProperty.RegisterReadOnly("IsHorizontallyTurnedOver", typeof(bool), typeof(ClippingAdorner), new PropertyMetadata(false));
        public static readonly DependencyProperty IsHorizontallyTurnedOverProperty =
            IsHorizontallyTurnedOverPropertyKey.DependencyProperty;

        public bool IsVerticallyTurnedOver
        {
            get { return (bool)GetValue(IsVerticallyTurnedOverProperty); }
            private set { SetValue(IsVerticallyTurnedOverPropertyKey, value); }
        }

        // Using a DependencyProperty as the backing store for IsVerticallyTurnedOver.  This enables animation, styling, binding, etc...
        public static readonly DependencyPropertyKey IsVerticallyTurnedOverPropertyKey =
            DependencyProperty.RegisterReadOnly("IsVerticallyTurnedOver", typeof(bool), typeof(ClippingAdorner), new PropertyMetadata(false));
        public static readonly DependencyProperty IsVerticallyTurnedOverProperty =
            IsVerticallyTurnedOverPropertyKey.DependencyProperty;

        public event EventHandler<ClipEventArgs>? ClipedAreaResized;

        public event EventHandler<ClipEventArgs>? Clipped;

        private static void ClippedAreaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var adorner = (ClippingAdorner)d;
            var adorned = adorner.AdornedElement;
            if (adorned is null)
            {
                return;
            }

            var desiredArea = (Rect)e.NewValue;
            var maxWidth = adorned.RenderSize.Width;
            var maxHeight = adorned.RenderSize.Height;
            var calcLeft = Math.Max(0, Math.Min(maxWidth, desiredArea.X));
            var calcTop = Math.Max(0, Math.Min(maxHeight, desiredArea.Y));
            var calcWidth = Math.Max(0, Math.Min(desiredArea.X + desiredArea.Width, maxWidth)) - calcLeft;
            var calcHeight = Math.Max(0, Math.Min(desiredArea.Y + desiredArea.Height, maxHeight)) - calcTop;

            adorner.ClippedArea = new Rect(calcLeft, calcTop, calcWidth, calcHeight);
            adorner.ClipedAreaResized?.Invoke(adorner, new ClipEventArgs
            {
                ClipedArea = adorner.ClippedArea,
            });
        }

        public ClippingAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
            this.visualChildren = new VisualCollection(this);

            this.corner1 = this.CreateChildThumbForResize();
            this.hSide1 = this.CreateChildThumbForResize();
            this.corner2 = this.CreateChildThumbForResize();
            this.vSide2 = this.CreateChildThumbForResize();
            this.corner4 = this.CreateChildThumbForResize();
            this.hSide2 = this.CreateChildThumbForResize();
            this.corner3 = this.CreateChildThumbForResize();
            this.vSide1 = this.CreateChildThumbForResize();
            this.plane = this.CreateChildThumbForMove();

            this.corner1.DragDelta += this.Resized;
            this.hSide1.DragDelta += this.Resized;
            this.corner2.DragDelta += this.Resized;
            this.vSide2.DragDelta += this.Resized;
            this.corner3.DragDelta += this.Resized;
            this.hSide2.DragDelta += this.Resized;
            this.corner4.DragDelta += this.Resized;
            this.vSide1.DragDelta += this.Resized;
            this.plane.DragDelta += this.Moved;

            this.corner1.Drop += this.Dropped;
            this.hSide1.Drop += this.Dropped;
            this.corner2.Drop += this.Dropped;
            this.vSide2.Drop += this.Dropped;
            this.corner3.Drop += this.Dropped;
            this.hSide2.Drop += this.Dropped;
            this.corner4.Drop += this.Dropped;
            this.vSide1.Drop += this.Dropped;
            this.plane.Drop += this.Dropped;
        }

        public void StartInitialDrag()
        {
            if (this.IsLoaded)
            {
                this.StartInitialDragCore();
            }
            else
            {
                this.Loaded += this.This_Loaded;
            }
        }

        protected override int VisualChildrenCount => visualChildren.Count;

        protected override Visual GetVisualChild(int index) => visualChildren[index];

        protected override Size ArrangeOverride(Size finalSize)
        {
            var topLeft = this.corner1;
            var top = this.hSide1;
            var topRight = this.corner2;
            var right = this.vSide2;
            var bottomRight = this.corner3;
            var bottom = this.hSide2;
            var bottomLeft = this.corner4;
            var left = this.vSide1;

            if (this.IsHorizontallyTurnedOver)
            {
                Swap(ref topLeft, ref topRight);
                Swap(ref left, ref right);
                Swap(ref bottomLeft, ref bottomRight);
            }
            if (this.IsVerticallyTurnedOver)
            {
                Swap(ref topLeft, ref bottomLeft);
                Swap(ref top, ref bottom);
                Swap(ref topRight, ref bottomRight);
            }
            top.Width = bottom.Width = Math.Max(0, this.ClippedArea.Width - ResizeCornerThumbSize);
            left.Height = right.Height = Math.Max(0, this.ClippedArea.Height - ResizeCornerThumbSize);

            topLeft.Arrange(new Rect(this.ClippedArea.X - ResizeCornerThumbSize / 2, this.ClippedArea.Y - ResizeCornerThumbSize / 2, topLeft.Width, topLeft.Height));
            top.Arrange(new Rect(this.ClippedArea.X + ResizeCornerThumbSize / 2, this.ClippedArea.Y - ResizeCornerThumbSize / 2, top.Width, top.Height));
            topRight.Arrange(new Rect(this.ClippedArea.X + this.ClippedArea.Width - ResizeCornerThumbSize / 2, this.ClippedArea.Y - ResizeCornerThumbSize / 2, topRight.Width, topRight.Height));
            right.Arrange(new Rect(this.ClippedArea.X + this.ClippedArea.Width - ResizeCornerThumbSize / 2, this.ClippedArea.Y + ResizeCornerThumbSize / 2, right.Width, right.Height));
            bottomRight.Arrange(new Rect(this.ClippedArea.X + this.ClippedArea.Width - ResizeCornerThumbSize / 2, this.ClippedArea.Y + this.ClippedArea.Height - ResizeCornerThumbSize / 2, bottomRight.Width, bottomRight.Height));
            bottom.Arrange(new Rect(this.ClippedArea.X + ResizeCornerThumbSize / 2, this.ClippedArea.Y + this.ClippedArea.Height - ResizeCornerThumbSize / 2, bottom.Width, bottom.Height));
            bottomLeft.Arrange(new Rect(this.ClippedArea.X - ResizeCornerThumbSize / 2, this.ClippedArea.Y + this.ClippedArea.Height - ResizeCornerThumbSize / 2, bottomLeft.Width, bottomLeft.Height));
            left.Arrange(new Rect(this.ClippedArea.X - ResizeCornerThumbSize / 2, this.ClippedArea.Y + ResizeCornerThumbSize / 2, left.Width, left.Height));

            this.plane.Width = Math.Max(0, this.ClippedArea.Size.Width - ResizeCornerThumbSize);
            this.plane.Height = Math.Max(0, this.ClippedArea.Height - ResizeCornerThumbSize);
            this.plane.Arrange(new Rect(this.ClippedArea.X + ResizeCornerThumbSize / 2, this.ClippedArea.Y + ResizeCornerThumbSize / 2, this.plane.Width, this.plane.Height)); ;

            topLeft.Cursor = Cursors.SizeNWSE;
            top.Cursor = Cursors.SizeNS;
            topRight.Cursor = Cursors.SizeNESW;
            right.Cursor = Cursors.SizeWE;
            bottomRight.Cursor = Cursors.SizeNWSE;
            bottom.Cursor = Cursors.SizeNS;
            bottomLeft.Cursor = Cursors.SizeNESW;
            left.Cursor = Cursors.SizeWE;

            return finalSize;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var brush = new SolidColorBrush(Color.FromArgb(80, 255, 255, 255));
            var pen = new Pen(new SolidColorBrush(Colors.Black), 1)
            {
                DashStyle = new DashStyle(new[] { 9.0, 9.0 }, 0.0),
            };
            drawingContext.DrawRectangle(brush, pen, this.ClippedArea);
        }

        private Thumb CreateChildThumbForResize()
        {
            var thumb = new Thumb();

            thumb.Height = ResizeCornerThumbSize;
            thumb.Width = ResizeCornerThumbSize;
            thumb.Opacity = 0.0;
            thumb.Background = new SolidColorBrush(Colors.Transparent);
            thumb.Background.Freeze();

            this.visualChildren.Add(thumb);

            return thumb;
        }

        private Thumb CreateChildThumbForMove()
        {
            var thumb = new Thumb
            {
                Cursor = Cursors.ScrollAll,
            };

            thumb.Height = ResizeCornerThumbSize;
            thumb.Width = ResizeCornerThumbSize;
            thumb.Opacity = 0.0;
            thumb.Background = new SolidColorBrush(Colors.Transparent);
            thumb.Background.Freeze();

            this.visualChildren.Add(thumb);

            return thumb;
        }

        private void Resized(object sender, DragDeltaEventArgs e)
        {
            var thumb = (Thumb)sender;
            var horizontalPosition = this.HorizontalPosition(thumb);
            var verticalPosition = this.VerticalPosition(thumb);

            var rectNow = this.ClippedArea;
            var newX = rectNow.X +
                horizontalPosition switch
                {
                    -1 => Math.Min(e.HorizontalChange, rectNow.Width),
                    0 => 0,
                    1 => Math.Min(0, rectNow.Width + e.HorizontalChange),
                    _ => throw new InvalidOperationException()
                };
            var newY = rectNow.Y +
                verticalPosition switch
                {
                    -1 => Math.Min(e.VerticalChange, rectNow.Height),
                    0 => 0,
                    1 => Math.Min(0, rectNow.Height + e.VerticalChange),
                    _ => throw new InvalidOperationException()
                };
            var newW = rectNow.Width + horizontalPosition * e.HorizontalChange;
            var newH = rectNow.Height + verticalPosition * e.VerticalChange;

            if (newW < 0)
            {
                this.IsHorizontallyTurnedOver = !this.IsHorizontallyTurnedOver;
                newW = -newW;
            }
            if (newH < 0)
            {
                this.IsVerticallyTurnedOver = !this.IsVerticallyTurnedOver;
                newH = -newH;
            }
            this.ClippedArea = new Rect(newX, newY, newW, newH);
        }

        private void Moved(object sender, DragDeltaEventArgs e)
        {
            var newX = Math.Max(0, Math.Min(this.AdornedElement.RenderSize.Width - this.ClippedArea.Width, this.ClippedArea.X + e.HorizontalChange));
            var newY = Math.Max(0, Math.Min(this.AdornedElement.RenderSize.Height - this.ClippedArea.Height, this.ClippedArea.Y + e.VerticalChange));
            this.ClippedArea = new Rect(newX, newY, this.ClippedArea.Width, this.ClippedArea.Height);
        }

        private void Dropped(object sender, DragEventArgs e)
        {
            this.Clipped?.Invoke(this, new ClipEventArgs
            {
                ClipedArea = this.ClippedArea,
            });
        }

        private void This_Loaded(object sender, RoutedEventArgs e)
        {
            this.StartInitialDragCore();
            this.Loaded -= this.This_Loaded;
        }

        private void StartInitialDragCore()
        {
            var mouseDown = new MouseButtonEventArgs(Mouse.PrimaryDevice, new TimeSpan(DateTime.Now.Ticks).Milliseconds, MouseButton.Left)
            {
                RoutedEvent = MouseLeftButtonDownEvent,
                Source = this.corner3,
            };
            this.corner3.RaiseEvent(mouseDown);
        }

        private static void Swap(ref Thumb lhs, ref Thumb rhs)
        {
            var tmp = lhs;
            lhs = rhs;
            rhs = tmp;
        }

        private int HorizontalPosition(Thumb thumb)
        {
            if (thumb == this.corner1 || thumb == this.vSide1 || thumb == this.corner4)
            {
                return this.IsHorizontallyTurnedOver ? 1 : -1;
            }
            if (thumb == this.corner2 || thumb == this.vSide2 || thumb == this.corner3)
            {
                return this.IsHorizontallyTurnedOver ? -1 : 1;
            }
            return 0;
        }

        private int VerticalPosition(Thumb thumb)
        {
            if (thumb == this.corner1 || thumb == this.hSide1 || thumb == this.corner2)
            {
                return this.IsVerticallyTurnedOver ? 1 : -1;
            }
            if (thumb == this.corner4 || thumb == this.hSide2 || thumb == this.corner3)
            {
                return this.IsVerticallyTurnedOver ? -1 : 1;
            }
            return 0;
        }
    }
}
