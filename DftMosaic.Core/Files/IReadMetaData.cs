namespace DftMosaic.Core.Files
{
    internal interface IReadMetaData
    {
        MetaData? Load(string filePath);
    }
}
