namespace Sahadeva.Dossier.DocumentGenerator.IO
{
    internal interface IStorageProvider
    {
        Task<byte[]> GetFile(string filePath);

        Task WriteFile(MemoryStream stream, string filePath);
    }
}
