using ExifLibrary;

namespace DftMosaic.Core.Mosaic.Files.Tiff
{
    internal class TiffMetaDataReader : IReadMetaData
    {
        public MetaData Load(string filePath)
        {
            var ret = new MetaData();
            var metadata = ExifLibrary.ImageFile.FromFile(filePath);
            try
            {
                var comment = metadata.Properties.Get(ExifTag.WindowsComment);
                if (comment != null)
                {
                    ret.Comment = (string)comment.Value;
                }
            }
            catch (Exception ex)
            {
                throw new NotSupportedException("The image does not support exif.", ex);
            }
            return ret;
        }
    }
}
