namespace DftMosaic.Core.Files
{
    public record ImageFileFormat(string Description, string[] Extensions)
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
