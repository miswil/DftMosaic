using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace DftMosaic.Desktop.Xaml.Behaviors
{
    internal class SyncScrollBehavior : DependencyObject
    {
        private static Dictionary<string, List<ScrollViewer>> _scrollers = new();

        public static string GetSyncGroup(DependencyObject obj)
        {
            return (string)obj.GetValue(SyncGroupProperty);
        }

        public static void SetSyncGroup(DependencyObject obj, string value)
        {
            obj.SetValue(SyncGroupProperty, value);
        }

        // Using a DependencyProperty as the backing store for SyncGroup.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SyncGroupProperty =
            DependencyProperty.RegisterAttached(
                "SyncGroup",
                typeof(string),
                typeof(SyncScrollBehavior),
                new PropertyMetadata(null, Grouped));

        private static void Grouped(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ScrollViewer scrollViewer || e.NewValue is null)
            {
                return;
            }
            if (_scrollers.TryGetValue((string)e.NewValue, out var scrollers))
            {
                scrollers.Add(scrollViewer);
            }
            else
            {
                _scrollers.Add((string)e.NewValue, new List<ScrollViewer> { scrollViewer });
            }
            
            scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
        }

        private static void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var groupName = GetSyncGroup((ScrollViewer)sender);
            var scrollers = _scrollers[groupName];
            foreach (var scroller in scrollers)
            {
                if (e.VerticalOffset != scroller.VerticalOffset)
                {
                    scroller.ScrollToVerticalOffset(e.VerticalOffset);
                }
                if (e.HorizontalOffset != scroller.HorizontalOffset)
                {
                    scroller.ScrollToHorizontalOffset(e.HorizontalOffset);
                }
            }
        }
    }
}
