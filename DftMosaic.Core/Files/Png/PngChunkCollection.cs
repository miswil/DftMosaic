using System.Diagnostics.CodeAnalysis;

namespace DftMosaic.Core.Files.Png
{
    internal class PngChunkCollection
    {
        private Dictionary<string, IList<PngChunk>> chunks;

        public int Count => this.chunks.Count;

        public ICollection<string> Types => this.chunks.Keys;

        public IEnumerable<PngChunk> Chunks => this.chunks.Values.SelectMany(c => c);

        public IEnumerable<PngITxtChunk>? ITxts
        {
            get
            {
                if (this.TryGetChunks(PngChunkConstants.ITxtType, out var chunks))
                {
                    return chunks.Cast<PngITxtChunk>();
                }
                return null;
            }
        }

        public IList<PngChunk> this[string key]
        {
            get => this.chunks[key];
            set => this.chunks[key] = value;
        }

        public PngChunkCollection()
        {
            this.chunks = new();
        }

        public PngChunkCollection(List<PngChunk> chunks)
        {
            this.chunks =
                chunks
                .GroupBy(c => c.Type)
                .ToDictionary(c => c.Key, c => (IList<PngChunk>)c.ToList());
        }

        public void Add(PngChunk chunk)
        {
            if (this.chunks.TryGetValue(chunk.Type, out var cc))
            {
                cc.Add(chunk);
            }
            else
            {
                this.chunks.Add(chunk.Type, new List<PngChunk>() { chunk });
            }
        }

        public bool ContainsType(string type)
        {
            return this.chunks.ContainsKey(type);
        }

        public void Remove(PngChunk chunk)
        {
            if (this.chunks.TryGetValue(chunk.Type, out var cc))
            {
                cc.Remove(chunk);
            }
        }

        public bool TryGetChunks(string type, [MaybeNullWhen(false)] out IList<PngChunk> value)
        {
            return this.chunks.TryGetValue(type, out value);
        }

        public void Clear()
        {
            this.chunks.Clear();
        }

        public void AddITxt(string keyword, string text)
        {
            this.Add(new PngITxtChunk(keyword, text));
        }
    }
}
