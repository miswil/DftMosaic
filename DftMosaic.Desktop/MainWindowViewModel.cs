using DftMosaic.Core.Mosaic;
using DftMosaic.Core.Mosaic.Files;
using DftMosaic.Desktop.Messaging;
using MaterialDesignThemes.Wpf;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using OpenCvSharp.WpfExtensions;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace DftMosaic.Desktop
{
    internal class MainWindowViewModel : ObservableObject
    {
        private Mosaicer? mosaicer;

        public IRelayCommand MosaicCommand { get; }

        public IRelayCommand SaveCommand { get; }

        public IRelayCommand SelectImageCommand { get; }

        public IRelayCommand ImageSelectedCommand { get; }

        private ImageFile? originalImageFile;
        private ImageFile? OriginalImageFile
        {
            get => originalImageFile;
            set
            {
                originalImageFile = value;
                this.MosaicCommand.NotifyCanExecuteChanged();
            }
        }

        private int showingImageIndex;
        public int ShowingImageIndex
        {
            get => this.showingImageIndex;
            set => this.SetProperty(ref this.showingImageIndex, value);
        }

        public MosaicType MosaicType { get; set; } = MosaicType.GrayScale;

        private ImageSource? originalImage;
        public ImageSource? OriginalImage
        {
            get => this.originalImage;
            set => this.SetProperty(ref this.originalImage, value);
        }

        private ImageSource? mosaicedImage;
        public ImageSource? MosaicedImage
        {
            get => this.mosaicedImage;
            set
            {
                this.SetProperty(ref this.mosaicedImage, value);
                this.SaveCommand.NotifyCanExecuteChanged();
            }
        }

        private ImageSource? unmosaicedImage;
        public ImageSource? UnmosaicedImage
        {
            get => this.unmosaicedImage;
            set => this.SetProperty(ref this.unmosaicedImage, value);
        }

        private string? imageFilePath;
        public string? ImageFilePath
        {
            get => this.imageFilePath;
            set => this.SetProperty(ref imageFilePath, value);
        }

        private Rect? mosaicArea;
        public Rect? MosaicArea
        {
            get => this.mosaicArea;
            set
            {
                this.SetProperty(ref this.mosaicArea, value);
                this.MosaicCommand.NotifyCanExecuteChanged();
            }
        }

        public SnackbarMessageQueue MessageQueue { get; } = new SnackbarMessageQueue();

        public MainWindowViewModel()
        {
            this.showingImageIndex = 0;
            this.MosaicCommand = new RelayCommand(this.MosaicCommandExecute, this.MosaicCommandCanExecute);
            this.SaveCommand = new RelayCommand(this.SaveCommandExecute, this.SaveCommandCanExecute);
            this.SelectImageCommand = new RelayCommand(this.SelectImageCommandExecute);
            this.ImageSelectedCommand = new RelayCommand<DragEventArgs>(this.ImageSelectedCommandExecute);
        }

        public void ShowImageFile(string filePath)
        {
            try
            {
                this.OriginalImageFile = new ImageFile(filePath);
            }
            catch (ImageFormatNotSupportedException ex)
            {
                this.MessageQueue.Enqueue(new FormatErrorMessageViewModel
                {
                    SupportedFormats = String.Join("\n", ex.SupportedFormats
                        .Select(f => $"{f.Description}:     {string.Join(", ", f.Extensions)}")),
                });
                return;
            }
            this.OriginalImage = this.originalImageFile.Image.ToBitmapSource();
            this.ImageFilePath = filePath;
            this.mosaicer = null;
            this.MosaicedImage = null;
            this.UnmosaicedImage = null;
            this.MosaicCommand.NotifyCanExecuteChanged();
            this.SaveCommand.NotifyCanExecuteChanged();
        }

        private void ImageSelectedCommandExecute(DragEventArgs? e)
        {
            if (e == null)
            {
                return;
            }
            var imageFilePath = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
            this.ShowImageFile(imageFilePath);
        }

        private void SelectImageCommandExecute()
        {
            var message = new GetOpenFileMessage
            {
                Filter = ImageFile.ReadableFileFormats
                    .Select(format => $"{format.Description}|{string.Join("; ", format.Extensions.Select(ex => $"*{ex}"))}")
                    .Aggregate((fst, scd) => $"{fst}|{scd}"),
            };
            WeakReferenceMessenger.Default.Send(message);
            if (message.FileName is not null)
            {
                this.ShowImageFile(message.FileName);
            }
        }

        private void SaveCommandExecute()
        {
            var message = new GetSaveFileMessage
            {
                Filter = ImageFile.MosaicWritableFileFormats
                    .Select(format => $"{format.Description}|{string.Join("; ", format.Extensions.Select(ex => $"*{ex}"))}")
                    .Aggregate((fst, scd) => $"{fst}|{scd}"),
            };
            WeakReferenceMessenger.Default.Send(message);
            if (message.FileName is not null)
            {
                try
                {
                    new ImageFile(this.mosaicer).Save(message.FileName);
                }
                catch (ImageFormatNotSupportedException ex)
                {
                    this.MessageQueue.Enqueue(new FormatErrorMessageViewModel
                    {
                        SupportedFormats = String.Join("\n", ex.SupportedFormats
                            .Select(f => $"{f.Description}:     {string.Join(", ", f.Extensions)}")),
                    });
                    return;
                }
            }

        }

        private bool SaveCommandCanExecute()
        {
            return this.MosaicedImage is not null;
        }

        private void MosaicCommandExecute()
        {
            if (this.originalImageFile is null)
            {
                return;
            }
            if (this.MosaicArea is not Rect mosaicArea)
            {
                return;
            }
            this.mosaicer = this.originalImageFile.ToMosaicer();
            this.mosaicer.Mosaic(
                new ((int)mosaicArea.X, (int)mosaicArea.Y, (int)mosaicArea.Width, (int)mosaicArea.Height),
                this.MosaicType);
            this.MosaicedImage = this.mosaicer.MosaicedImage.ToBitmapSource();

            var unmosaicer = new ImageFile(this.mosaicer).ToUnmosaicer();
            unmosaicer.Unmosaic();
            this.UnmosaicedImage = unmosaicer.OriginalImage.ToBitmapSource();
        }

        private bool MosaicCommandCanExecute()
        {
            return this.originalImageFile is not null
                && this.MosaicArea is not null;
        }
    }
}
