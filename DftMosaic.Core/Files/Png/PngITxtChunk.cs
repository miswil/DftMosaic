using System.Text;

namespace DftMosaic.Core.Files.Png
{
    internal record PngITxtChunk : PngChunk
    {
        public PngITxtChunk(string keyword, string text)
            : base(PngChunkConstants.ITxtType, ToData(keyword, text))
        {
            this.Keyword = keyword;
        }

        public PngITxtChunk(byte[] data)
            : base(PngChunkConstants.ITxtType, data)
        {
            (this.Keyword, _) = FromData(data);
        }

        public string Keyword { get; }

        public string Text
        {
            get => FromData(this.Data).text;
        }

        private static byte[] ToData(string keyword, string text)
        {
            var keywordByte = Encoding.UTF8.GetBytes(keyword);
            var textByte = Encoding.UTF8.GetBytes(text);
            var dataByte = new byte[keywordByte.Length + 5 + textByte.Length];
            keywordByte.CopyTo(dataByte, 0);
            textByte.CopyTo(dataByte, keywordByte.Length + 5);
            return dataByte;
        }

        private static (string keyword, string text) FromData(byte[] data)
        {
            var keywordLength = Array.IndexOf<byte>(data, 0);
            var keyword = Encoding.UTF8.GetString(data, 0, keywordLength);
            var textLength = data.Length - keywordLength - 5;
            var text = Encoding.UTF8.GetString(data, keywordLength + 5, textLength);
            return (keyword, text);
        }
    }
}
