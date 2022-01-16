using System;
using System.Windows;

namespace DftMosaic.Desktop
{
    internal class ClipEventArgs : EventArgs
    {
        public Rect ClipedArea { get; set; }
    }
}