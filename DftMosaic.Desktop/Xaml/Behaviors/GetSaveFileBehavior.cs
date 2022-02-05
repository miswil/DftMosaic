using DftMosaic.Desktop.Messaging;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Win32;
using Microsoft.Xaml.Behaviors;
using System.Windows;

namespace DftMosaic.Desktop.Xaml.Behaviors
{
    internal class GetSaveFileBehavior : Behavior<Window>, IRecipient<GetSaveFileMessage>
    {
        public void Receive(GetSaveFileMessage message)
        {
            SaveFileDialog dialog = new SaveFileDialog
            {
                Filter = message.Filter,
            };
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
