namespace Sahadeva.Dossier.DocumentGenerator.IO
{
    internal class FilesystemStorageProvider : IStorageProvider
    {
        public Task<byte[]> GetFile(string filePath)
        {
            return File.ReadAllBytesAsync(filePath);
        }

        public async Task WriteFile(MemoryStream stream, string filePath)
        {
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true))
            {
                stream.Position = 0; // Ensure the stream's position is at the start
                await stream.CopyToAsync(fileStream);
            }
        }
    }
}
