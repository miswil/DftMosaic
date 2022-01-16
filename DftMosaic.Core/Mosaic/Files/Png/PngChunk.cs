using System.Text;

namespace DftMosaic.Core.Mosaic.Files.Png
{
    internal record PngChunk
    {
        public PngChunk(string type, byte[] data)
        {
            this.Type = type ?? throw new ArgumentNullException(nameof(type));
            this.Data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public uint Length => (uint)this.Data.Length;

        public string Type { get; }

        public byte[] Data { get; }

        public uint CRC
        {
            get
            {
                var typeByte = Encoding.ASCII.GetBytes(this.Type);
                var crcByte = new byte[typeByte.Length + this.Data.Length];
                typeByte.CopyTo(crcByte, 0);
                this.Data.CopyTo(crcByte, typeByte.Length);
                return new CRC32().GetCRC32(crcByte);
            }
        }
    }
}
