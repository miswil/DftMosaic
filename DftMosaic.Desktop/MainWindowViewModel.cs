using DftMosaic.Core.Files;
using DftMosaic.Core.Images;
using DftMosaic.Desktop.Messaging;
using MaterialDesignThemes.Wpf;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace DftMosaic.Desktop
{
    internal class MainWindowViewModel : ObservableObject, IDisposable
    {
        public IRelayCommand MosaicCommand { get; }

        public IRelayCommand SaveCommand { get; }

        public IRelayCommand SelectImageCommand { get; }

        public IRelayCommand ImageSelectingCommand { get; }

        public IRelayCommand ImageSelectedCommand { get; }

        private Image? originalImage;
        public Image? OriginalImage
        {
            get => originalImage;
            set
            {
                this.originalImage?.Dispose();
                this.originalImage = value;
                this.MosaicCommand.NotifyCanExecuteChanged();
            }
        }

        private Image? mosaicedImage;
        public Image? MosaicedImage
        {
            get => this.mosaicedImage;
            set
            {
                this.mosaicedImage?.Dispose();
                this.mosaicedImage = value;
                this.SaveCommand.NotifyCanExecuteChanged();
            }
        }

        private Image? unmosaicedImage;
        public Image? UnmosaicedImage
        {
            get => this.unmosaicedImage;
            set
            {
                this.unmosaicedImage?.Dispose();
                this.unmosaicedImage= value;
                this.SaveCommand.NotifyCanExecuteChanged();
            }
        }

        private int showingImageIndex;
        public int ShowingImageIndex
        {
            get => this.showingImageIndex;
            set => this.SetProperty(ref this.showingImageIndex, value);
        }

        public MosaicType MosaicType { get; set; } = MosaicType.GrayScale;

        private ImageSource? originalImageSource;
        public ImageSource? OriginalImageSource
        {
            get => this.originalImageSource;
            private set => this.SetProperty(ref this.originalImageSource, value);
        }

        private ImageSource? mosaicedImageSource;
        public ImageSource? MosaicedImageSource
        {
            get => this.mosaicedImageSource;
            private set => this.SetProperty(ref this.mosaicedImageSource, value);
        }

        private ImageSource? unmosaicedImageSource;
        public ImageSource? UnmosaicedImageSource
        {
            get => this.unmosaicedImageSource;
            private set => this.SetProperty(ref this.unmosaicedImageSource, value);
        }

        private string? imageFilePath;
        public string? ImageFilePath
        {
            get => this.imageFilePath;
            set => this.SetProperty(ref imageFilePath, value);
        }

        public ObservableCollection<Rect> MosaicAreas { get; set; }

        public SnackbarMessageQueue MessageQueue { get; } = new SnackbarMessageQueue();

        public MainWindowViewModel()
        {
            this.showingImageIndex = 0;
            this.MosaicCommand = new RelayCommand(this.MosaicCommandExecute, this.MosaicCommandCanExecute);
            this.SaveCommand = new RelayCommand(this.SaveCommandExecute, this.SaveCommandCanExecute);
            this.SelectImageCommand = new RelayCommand(this.SelectImageCommandExecute);
            this.ImageSelectingCommand = new RelayCommand<DragEventArgs>(this.ImageSelectingCommandExecute);
            this.ImageSelectedCommand = new RelayCommand<DragEventArgs>(this.ImageSelectedCommandExecute);
            this.MosaicAreas = new ObservableCollection<Rect>();
            this.MosaicAreas.CollectionChanged += (_, _) => this.MosaicCommand.NotifyCanExecuteChanged();
        }

        public void ShowImageFile(string filePath)
        {
            try
            {
                this.OriginalImage = new ImageFileService().Load(filePath);
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
            this.OriginalImageSource = this.OriginalImage.Data.ToBitmapSource();
            this.ImageFilePath = filePath;
            this.MosaicedImage = null;
            this.MosaicedImageSource = null;
            this.UnmosaicedImage = null;
            this.UnmosaicedImageSource = null;
            this.MosaicCommand.NotifyCanExecuteChanged();
            this.SaveCommand.NotifyCanExecuteChanged();
        }

        private void ImageSelectingCommandExecute(DragEventArgs? e)
        {
            if (e is null)
            {
                throw new ArgumentNullException(nameof(e));
            }
            if (this.IsDropable(e))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void ImageSelectedCommandExecute(DragEventArgs? e)
        {
            if (e is null)
            {
                throw new ArgumentNullException(nameof(e));
            }
            if (this.IsDropable(e))
            {
                var imageFilePath = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
                this.ShowImageFile(imageFilePath);
            }
        }
        
        private bool IsDropable(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return false;
            }
            var file = ((string[])e.Data.GetData(DataFormats.FileDrop)).FirstOrDefault();
            if (file == null)
            {
                return false;
            }
            var extension = Path.GetExtension(file);
            return ImageFileFormat.IsReadableFileFormats(extension);
        }

        private void SelectImageCommandExecute()
        {
            var message = new GetOpenFileMessage
            {
                Filter = ImageFileFormat.ReadableFileFormats
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
            if (this.MosaicedImage is null)
            {
                throw new InvalidOperationException();
            }

            var message = new GetSaveFileMessage
            {
                Filter = ImageFileFormat.MosaicWritableFileFormats
                    .Select(format => $"{format.Description}|{string.Join("; ", format.Extensions.Select(ex => $"*{ex}"))}")
                    .Aggregate((fst, scd) => $"{fst}|{scd}"),
            };
            WeakReferenceMessenger.Default.Send(message);
            if (message.FileName is not null)
            {
                try
                {
                    var imageFileService = new ImageFileService();
                    imageFileService.Save(this.MosaicedImage, message.FileName);
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
            if (this.OriginalImage is null)
            {
                return;
            }

            this.MosaicedImage = this.OriginalImage.Mosaic(
                this.MosaicAreas.Select(r => new MosaicRequestArea(new OpenCvSharp.Rect((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height), 0)),
                this.MosaicType);
            this.MosaicedImageSource = this.MosaicedImage.Data.ToBitmapSource();

            this.UnmosaicedImage = this.MosaicedImage.Unmosaic();
            this.UnmosaicedImageSource = this.UnmosaicedImage.Data.ToBitmapSource();
        }

        private bool MosaicCommandCanExecute()
        {
            return this.OriginalImage is not null
                && this.MosaicAreas.Any();
        }

        public void Dispose()
        {
            this.OriginalImage?.Dispose();
            this.mosaicedImage?.Dispose();
            this?.UnmosaicedImage?.Dispose();
        }
    }
}
