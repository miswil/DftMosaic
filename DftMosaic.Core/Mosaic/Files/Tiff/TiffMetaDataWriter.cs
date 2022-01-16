using ExifLibrary;
using System.Text;

namespace DftMosaic.Core.Mosaic.Files.Tiff
{
    internal class TiffMetaDataWriter : IWriteMetaData
    {
        public void Save(string filePath, MetaData metaData)
        {
            var tmpFile = Path.GetTempFileName();
            try
            {
                var fileMetaData = ExifLibrary.ImageFile.FromFile(filePath);
                MapToExifProperty(fileMetaData.Properties, metaData);
                fileMetaData.Save(tmpFile);
            }
            catch (Exception ex)
            {
                throw new NotSupportedException("The image does not support exif.", ex);
            }
            File.Move(tmpFile, filePath, true);
        }

        private void MapToExifProperty(ExifPropertyCollection<ExifProperty> properties, MetaData metaData)
        {
            if (metaData.Comment is not null)
            {
                properties.Add(ExifTag.WindowsComment, metaData.Comment);
            }
        }
    }
}
