using DftMosaic.Desktop.Messaging;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Win32;
using Microsoft.Xaml.Behaviors;
using System.Windows;

namespace DftMosaic.Desktop.Xaml.Behaviors
{
    internal class GetOpenFileBehavior : Behavior<Window>, IRecipient<GetOpenFileMessage>
    {
        public void Receive(GetOpenFileMessage message)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            if (dialog.ShowDialog() is bool tf && tf)
            {
                message.FileName = dialog.FileName;
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            WeakReferenceMessenger.Default.Register(this);
        }

        protected override void OnDetaching()
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
            base.OnDetaching();
        }
    }
}
