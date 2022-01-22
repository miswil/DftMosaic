using DftMosaic.Core.Files;
using System.Text;

namespace DftMosaic.Core.Files.Png
{
    internal class PngMetaDataReader : IReadMetaData
    {
        public MetaData? Load(string filePath)
        {
            if (Path.GetExtension(filePath) is not ".png")
            {
                throw new ArgumentException("The image must be png.");
            }
            using var file = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            file.Seek(8, SeekOrigin.Begin); // skip header
            var chunks = this.ReadChunk(file);

            var comment = chunks.ITxts?.FirstOrDefault(t => t.Keyword is "Comment")?.Text;
            if (comment is null)
            {
                return null;
            }
            else
            {
                return new MetaData(comment);
            }
        }

        private PngChunkCollection ReadChunk(Stream stream)
        {
            var ret = new List<PngChunk>();
            for (; ; )
            {
                var chunk = this.ReadOneChunk(stream);
                ret.Add(chunk);
                if (chunk.Type is "IEND")
                {
                    break;
                }
            }
            return new(ret);
        }

        private PngChunk ReadOneChunk(Stream stream)
        {
            byte[] lengthByte = new byte[4];
            stream.Read(lengthByte, 0, lengthByte.Length);
            var length = this.ToUint(lengthByte);
            var typeByte = new byte[4];
            stream.Read(typeByte, 0, typeByte.Length);
            var type = Encoding.ASCII.GetString(typeByte);
            var dataByte = new byte[length];
            stream.Read(dataByte, 0, dataByte.Length);
            var crcByte = new byte[4];
            stream.Read(crcByte, 0, crcByte.Length);
            var crc = this.ToUint(crcByte);

            return CreateChunk(type, dataByte);
        }

        private uint ToUint(byte[] data)
        {
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }
            return BitConverter.ToUInt16(data, 0);
        }
        private PngChunk CreateChunk(string type, byte[] data)
        {
            return type switch
            {
                PngChunkConstants.ITxtType => new PngITxtChunk(data),
                _ => new PngChunk(type, data),
            };
        }
    }
}
