using System.Text;

namespace DftMosaic.Core.Files.Png
{
    internal class PngMetaDataWriter : IWriteMetaData
    {
        public void Save(string filePath, MetaData metaData)
        {
            if (Path.GetExtension(filePath) is not ".png")
            {
                throw new ArgumentException("The image must be png.");
            }

            var tmpFile = Path.GetTempFileName();
            using (var image = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var file = new FileStream(tmpFile, FileMode.Create, FileAccess.Write))
            {
                this.CopyPngHeaderAndIHDR(image, file);
                PngChunkCollection chunks = this.FromMetaData(metaData);
                this.WriteChunk(file, chunks);
                image.CopyTo(file);
            }
            File.Move(tmpFile, filePath, true);
        }

        private PngChunkCollection FromMetaData(MetaData metaData)
        {
            var chunks = new PngChunkCollection();
            if (metaData.Comment is not null)
            {
                chunks.AddITxt("Comment", metaData.Comment);
            }
            return chunks;
        }

        private void CopyPngHeaderAndIHDR(Stream src, Stream dst)
        {
            var headerAndIHDR = new byte[8 + 25];
            src.Read(headerAndIHDR, 0, headerAndIHDR.Length);
            dst.Write(headerAndIHDR, 0, headerAndIHDR.Length);
        }

        private void WriteChunk(Stream stream, PngChunkCollection chunks)
        {
            foreach (var chunk in chunks.Chunks)
            {
                this.WriteOneChunk(stream, chunk);
            }
        }

        private void WriteOneChunk(Stream stream, PngChunk chunk)
        {
            byte[] lengthByte = this.ToBigEndian(chunk.Length);
            stream.Write(lengthByte, 0, lengthByte.Length);
            var typeByte = Encoding.ASCII.GetBytes(chunk.Type);
            stream.Write(typeByte, 0, typeByte.Length);
            var dataByte = chunk.Data;
            stream.Write(dataByte, 0, dataByte.Length);
            var crcByte = ToBigEndian(chunk.CRC);
            stream.Write(crcByte, 0, crcByte.Length);
        }

        private byte[] ToBigEndian(uint u)
        {
            var data = BitConverter.GetBytes(u);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }
            return data;
        }

    }
}
