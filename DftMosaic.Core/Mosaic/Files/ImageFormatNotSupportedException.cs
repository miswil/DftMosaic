using System.Runtime.Serialization;

namespace DftMosaic.Core.Mosaic.Files
{
    public class ImageFormatNotSupportedException : NotSupportedException
    {
        public ImageFormatNotSupportedException(IReadOnlyCollection<ImageFileFormat> supportedFormats) : this(null, supportedFormats)
        {
        }

        public ImageFormatNotSupportedException(string? message, IReadOnlyCollection<ImageFileFormat> supportedFormats) : this(message, null, supportedFormats)
        {
        }

        public ImageFormatNotSupportedException(string? message, Exception? innerException, IReadOnlyCollection<ImageFileFormat> supportedFormats) : base(message, innerException)
        {
            this.SupportedFormats = supportedFormats;
        }

        protected ImageFormatNotSupportedException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public IReadOnlyCollection<ImageFileFormat> SupportedFormats { get; }
    }
}
