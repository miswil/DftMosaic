using DftMosaic.Core.Images;
using OpenCvSharp;
using System.Text.Json;

namespace DftMosaic.Core.Files
{
    public class ImageFileService
    {
        public Image Load(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            if (!ImageFileFormat.IsReadableFileFormats(extension))
            {
                throw new ImageFormatNotSupportedException(
                    $"{extension} is not a supported image format.",
                    ImageFileFormat.ReadableFileFormats);
            }

            var data = Cv2.ImRead(filePath, ImreadModes.Unchanged | ImreadModes.AnyDepth);
            var metaData = new MetaDataOperationFactory().Reader(Path.GetExtension(filePath)).Load(filePath);

            return new(data, this.ToMosaicInfo(metaData));
        }

        public void Save(Image image, string filePath)
        {
            var extension = Path.GetExtension(filePath);
            if (image.IsMosaiced && !ImageFileFormat.IsMosaicWritableFileFormats(extension))
            {
                throw new ImageFormatNotSupportedException(
                    $"{extension} is not a supported image format.",
                    ImageFileFormat.MosaicWritableFileFormats);
            }
            if (!image.IsMosaiced && !ImageFileFormat.IsUnmosaicWritableFileFormats(extension))
            {
                throw new ImageFormatNotSupportedException(
                    $"{extension} is not a supported image format.",
                    ImageFileFormat.UnmosaicWritableFileFormats);
            }

            Cv2.ImWrite(filePath, image.Data);
            var metaData = this.ToMetaData(image.MosaicInfo);
            if (metaData is not null)
            {
                new MetaDataOperationFactory().Writer(Path.GetExtension(filePath)).Save(filePath, metaData);
            }
        }

        private MetaData? ToMetaData(MosaicInfo? mosaicInfo)
        {
            if (mosaicInfo is null)
            {
                return null;
            }
            return new(JsonSerializer.Serialize(mosaicInfo, new JsonSerializerOptions
            {
                IncludeFields = true,
            }));
        }

        private MosaicInfo? ToMosaicInfo(MetaData? metaData)
        {
            if (metaData is null)
            {
                return null;
            }
            return JsonSerializer.Deserialize<MosaicInfo>(metaData.Comment, new JsonSerializerOptions
            {
                IncludeFields = true,
            });
        }
    }
}
