using DftMosaic.Core.Mosaic.Files.Png;
using DftMosaic.Core.Mosaic.Files.Tiff;

namespace DftMosaic.Core.Mosaic.Files
{
    internal class MetaDataOperationFactory
    {
        public IReadMetaData Reader(string extension)
        {
            return extension.ToLower() switch
            {
                ".png" => new PngMetaDataReader(),
                ".jpg" or ".jpeg" or ".tiff" or ".tif" => new TiffMetaDataReader(),
                _ => throw new NotSupportedException(@$"The image format ""{extension}"" is not supported.")
            };
        }

        public IWriteMetaData Writer(string extension)
        {
            return extension.ToLower() switch
            {
                ".png" => new PngMetaDataWriter(),
                ".jpg" or ".jpeg" or ".tiff" or ".tif" => new TiffMetaDataWriter(),
                _ => throw new NotSupportedException("The image format is not supported.")
            };
        }
    }
}
