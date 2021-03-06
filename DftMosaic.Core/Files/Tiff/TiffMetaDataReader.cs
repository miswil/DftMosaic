using ExifLibrary;

namespace DftMosaic.Core.Files.Tiff
{
    internal class TiffMetaDataReader : IReadMetaData
    {
        public MetaData? Load(string filePath)
        {
            var metadata = ImageFile.FromFile(filePath);
            try
            {
                var comment = metadata.Properties.Get(ExifTag.WindowsComment);
                if (comment != null)
                {
                    return new MetaData((string)comment.Value);
                }
            }
            catch (Exception ex)
            {
                throw new NotSupportedException("The image does not support exif.", ex);
            }
            return null;
        }
    }
}
