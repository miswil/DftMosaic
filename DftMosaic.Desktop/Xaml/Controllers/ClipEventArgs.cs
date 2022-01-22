using System;
using System.Windows;

namespace DftMosaic.Desktop.Xaml.Controllers
{
    internal class ClipEventArgs : EventArgs
    {
        public Rect ClipedArea { get; set; }
    }
}