namespace DftMosaic.Core.Files
{
    public record MetaData
    {
        public MetaData(string comment)
        {
            this.Comment = comment;
        }
        public string Comment { get; }
    }
}
