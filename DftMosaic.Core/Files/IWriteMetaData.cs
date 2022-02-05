namespace DftMosaic.Core.Files
{
    internal interface IWriteMetaData
    {
        void Save(string filePath, MetaData metaData);
    }
}
