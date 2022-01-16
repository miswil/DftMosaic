using Newtonsoft.Json;
using OpenCvSharp;

namespace DftMosaic.Core.Mosaic.Files
{
    public class ImageFile
    {
        public static readonly IReadOnlyList<(string Description, string Extensions)> ReadableFileFormats
            = new List<(string Description, string Extensions)>
            {
                ("Windows bitmaps", "*.bmp; *.dib"),
                ("JPEG files", "*.jpeg, *.jpg; *.jpe"),
                ("JPEG 2000 files", "*.jp2"),
                ("Portable Network Graphics", "*.png"),
                ("TIFF files", "*.tiff; *.tif")
            }.AsReadOnly();

        public static bool IsReadableFileFormats(string extension)
            => ExtensionMatch(ReadableFileFormats, extension);

        public static readonly IReadOnlyList<(string Description, string Extensions)> MosaicWritableFileFormats
            = new List<(string Description, string Extensions)>
            {
                ("Portable Network Graphics", "*.png"),
                ("TIFF files", "*.tiff; *.tif")
            }.AsReadOnly();

        public static bool IsMosaicWritableFileFormats(string extension)
            => ExtensionMatch(MosaicWritableFileFormats, extension);

        public static readonly IReadOnlyList<(string Description, string Extensions)> UnmosaicWritableFileFormats
            = new List<(string Description, string Extensions)>
            {
                ("Windows bitmaps", "*.bmp; *.dib"),
                ("JPEG files", "*.jpeg, *.jpg; *.jpe"),
                ("JPEG 2000 files", "*.jp2"),
                ("Portable Network Graphics", "*.png"),
                ("TIFF files", "*.tiff; *.tif")
            }.AsReadOnly();

        public static bool IsUnmosaicWritableFileFormats(string extension)
            => ExtensionMatch(UnmosaicWritableFileFormats, extension);

        public Mat Image { get; private set; }
        public MetaData MetaData { get; private set; }

        public ImageFile(string filePath)
        {
            this.Image = Cv2.ImRead(filePath, ImreadModes.Unchanged | ImreadModes.AnyDepth);
            this.MetaData = new MetaDataOperationFactory().Reader(Path.GetExtension(filePath)).Load(filePath);
        }

        public ImageFile(Mosaicer mosaicer)
        {
            if (mosaicer.MosaicedImage is null)
            {
                throw new ArgumentException("image is not mosaiced.");
            }

            this.Image = mosaicer.MosaicedImage;
            this.MetaData = new MetaData();
            this.MetaData.Comment = JsonConvert.SerializeObject(
                new MosaicInfo
                {
                    Area = mosaicer.MosaicedArea,
                    Type = mosaicer.MosaicType,
                    Scale = mosaicer.Scale
                });
        }

        public ImageFile(Unmosaicer unmosaicer)
        {
            this.Image = unmosaicer.OriginalImage;
            this.MetaData = new MetaData();
        }

        public void Save(string filePath)
        {
            Cv2.ImWrite(filePath, this.Image);
            new MetaDataOperationFactory().Writer(Path.GetExtension(filePath)).Save(filePath, this.MetaData);
        }

        public Mosaicer ToMosaicer()
        {
            return new Mosaicer(this.Image);
        }

        public Unmosaicer ToUnmosaicer()
        {
            if (this.MetaData.Comment is null)
            {
                throw new InvalidOperationException("Image is not mosaiced.");
            }
            var mosaicInfo = JsonConvert.DeserializeObject<MosaicInfo>(this.MetaData.Comment);
            return new Unmosaicer(this.Image, mosaicInfo.Area, mosaicInfo.Type, mosaicInfo.Scale);
        }

        private static bool ExtensionMatch(IReadOnlyList<(string Description, string Extensions)> formats, string extension)
        {
            extension = extension.ToLower();
            return formats
                .Select(f => f.Extensions.Split(';'))
                .SelectMany(ext => ext)
                .Select(ext => ext.Trim().Trim('*'))
                .FirstOrDefault(ext => ext.ToLower() == extension) != null;
        }
    }
}