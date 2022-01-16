namespace DftMosaic.Core.Mosaic.Files
{
    internal interface IWriteMetaData
    {
        void Save(string filePath, MetaData metaData);
    }
}
