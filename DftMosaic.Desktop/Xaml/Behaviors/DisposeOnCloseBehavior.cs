using System;
using System.Windows;

namespace DftMosaic.Desktop.Xaml.Behaviors
{
    internal class DisposeOnCloseBehavior : DependencyObject
    {
        public static bool GetDisposeOnClose(DependencyObject obj)
        {
            return (bool)obj.GetValue(DisposeOnCloseProperty);
        }

        public static void SetDisposeOnClose(DependencyObject obj, bool value)
        {
            obj.SetValue(DisposeOnCloseProperty, value);
        }

        public static readonly DependencyProperty DisposeOnCloseProperty =
            DependencyProperty.RegisterAttached(
                "DisposeOnClose",
                typeof(bool),
                typeof(DisposeOnCloseBehavior),
                new PropertyMetadata(false, (d, e) =>
                {
                    if (d is not Window win)
                    {
                        return;
                    }
                    if ((bool)e.NewValue)
                    {
                        win.Closed += Win_Closed;
                    }
                    else
                    {
                        win.Closed -= Win_Closed;
                    }
                }));

        private static void Win_Closed(object? sender, EventArgs e)
        {
            var win = sender as Window;
            if (win != null)
            {
                (win.DataContext as IDisposable)?.Dispose();
            }
        }
    }
}
