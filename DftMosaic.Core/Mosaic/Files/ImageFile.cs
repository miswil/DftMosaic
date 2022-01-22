using Newtonsoft.Json;
using OpenCvSharp;

namespace DftMosaic.Core.Mosaic.Files
{
    public class ImageFile
    {
        public static readonly IReadOnlyList<ImageFileFormat> ReadableFileFormats
            = new List<ImageFileFormat>
            {
                new("Windows bitmaps", new[]{".bmp",".dib" }),
                new("JPEG files", new[]{".jpeg",".jpg",".jpe" }),
                new("JPEG 2000 files", new[]{".jp2" }),
                new("Portable Network Graphics", new[]{".png" }),
                new("TIFF files", new[]{".tiff",".tif" })
            }.AsReadOnly();

        public static bool IsReadableFileFormats(string extension)
            => ExtensionMatch(ReadableFileFormats, extension);

        public static readonly IReadOnlyList<ImageFileFormat> MosaicWritableFileFormats
            = new List<ImageFileFormat>
            {
                new("Portable Network Graphics", new[]{".png" }),
                new("TIFF files", new[]{".tiff","*.tif" }),
            }.AsReadOnly();

        public static bool IsMosaicWritableFileFormats(string extension)
            => ExtensionMatch(MosaicWritableFileFormats, extension);

        public static readonly IReadOnlyList<ImageFileFormat> UnmosaicWritableFileFormats
            = new List<ImageFileFormat>
            {
                new("Windows bitmaps", new[]{".bmp",".dib" }),
                new("JPEG files", new[]{".jpeg",".jpg",".jpe" }),
                new("JPEG 2000 files", new[]{".jp2" }),
                new("Portable Network Graphics", new[]{".png" }),
                new("TIFF files", new[]{".tiff",".tif" })
            }.AsReadOnly();

        public static bool IsUnmosaicWritableFileFormats(string extension)
            => ExtensionMatch(UnmosaicWritableFileFormats, extension);

        public Mat Image { get; private set; }
        public MetaData? MetaData { get; private set; }

        public bool IsMosaiced => this.MetaData is not null;

        public ImageFile(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            if (!IsReadableFileFormats(extension))
            {
                throw new NotSupportedException($"{extension} is not a supported image format.");
            }

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
            this.MetaData = new MetaData(comment: JsonConvert.SerializeObject(
                    new MosaicInfo
                    {
                        Area = mosaicer.MosaicedArea,
                        Type = mosaicer.MosaicType,
                        Scale = mosaicer.Scale
                    })
            );
        }

        public ImageFile(Unmosaicer unmosaicer)
        {
            if (unmosaicer.OriginalImage is null)
            {
                throw new ArgumentException("image is not unmosaiced.");
            }

            this.Image = unmosaicer.OriginalImage;
            this.MetaData = null;
        }

        public void Save(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            if (this.IsMosaiced && !IsMosaicWritableFileFormats(extension))
            {
                throw new NotSupportedException($"{extension} is not a supported image format.");
            }
            if (!this.IsMosaiced && !IsUnmosaicWritableFileFormats(extension))
            {
                throw new NotSupportedException($"{extension} is not a supported image format.");
            }

            Cv2.ImWrite(filePath, this.Image);
            new MetaDataOperationFactory().Writer(Path.GetExtension(filePath)).Save(filePath, this.MetaData);
        }

        public Mosaicer ToMosaicer()
        {
            return new Mosaicer(this.Image);
        }

        public Unmosaicer ToUnmosaicer()
        {
            if (this.MetaData is null)
            {
                throw new InvalidOperationException("Image is not mosaiced.");
            }
            var mosaicInfo = JsonConvert.DeserializeObject<MosaicInfo>(this.MetaData.Comment);
            if (mosaicInfo is null)
            {
                throw new InvalidOperationException("Metadata is invalid.");
            }
            return new Unmosaicer(this.Image, mosaicInfo.Area, mosaicInfo.Type, mosaicInfo.Scale);
        }

        private static bool ExtensionMatch(IReadOnlyList<ImageFileFormat> formats, string extension)
        {
            extension = extension.ToLower();
            return formats
                .Select(f => f.Extensions)
                .SelectMany(ext => ext)
                .FirstOrDefault(ext => ext.ToLower() == extension) != null;
        }
    }
}