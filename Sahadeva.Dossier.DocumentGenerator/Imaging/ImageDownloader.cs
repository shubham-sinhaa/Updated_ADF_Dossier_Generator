using Amazon.S3;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.Options;
using Sahadeva.Dossier.Common.Configuration;
using Sahadeva.Dossier.Common.Logging;
using Sahadeva.Dossier.DocumentGenerator.Configuration;
using Sahadeva.Dossier.DocumentGenerator.IO;
using Serilog;
using System.Net;

namespace Sahadeva.Dossier.DocumentGenerator.Imaging
{
    internal class ImageDownloader
    {
        private readonly IStorageProvider _storageProvider;
        private readonly TemplateStorageOptions _templateStorageOptions;
        private readonly int _imageMaxDegreeOfParallelism;
        private static readonly HttpClient _httpClient = new();
        private readonly object _documentLock = new();
        private const int DEFAULT_MAX_DEGREE_OF_PARALLELISM = 10;

        public ImageDownloader(IStorageProvider storageProvider, IOptions<TemplateStorageOptions> options)
        {
            _storageProvider = storageProvider;
            _templateStorageOptions = options.Value;
            _imageMaxDegreeOfParallelism = int.Parse(ConfigurationManager.Settings["ImageMaxDegreeOfParallelism"] ?? DEFAULT_MAX_DEGREE_OF_PARALLELISM.ToString());
        }

        /// <summary>
        /// Downloads images in parallel to speed up document processing.
        /// </summary>
        internal async Task DownloadImagesAsync(WordprocessingDocument document)
        {
            var imageRequests = new List<ImageDownloadRequest>();
            var drawings = document.MainDocumentPart!.Document.Descendants<Drawing>().ToList();
            foreach (var drawing in drawings)
            {
                var nonVisualProps = drawing.Descendants<DocProperties>().FirstOrDefault();

                if (nonVisualProps != null && nonVisualProps.Description != null &&
                    nonVisualProps.Description.Value!.StartsWith("AF.Image="))
                {
                    var blip = drawing.Descendants<Blip>().FirstOrDefault();
                    if (blip != null)
                    {
                        imageRequests.Add(new ImageDownloadRequest(nonVisualProps.Description.Value, blip));
                    }

                    // clear out any processing instructions
                    nonVisualProps.Description.Value = string.Empty;
                }
            }

            Log.Verbose($"Found {imageRequests.Count} image(s) in the document");

            using (var timeLog = Log.Logger.TrackTime("ReplaceImages"))
            {
                await ReplaceImagesAsync(document, imageRequests);
            }
        }

        private async Task ReplaceImagesAsync(WordprocessingDocument document, IEnumerable<ImageDownloadRequest> imageRequests)
        {
            var ctr = 1;
            using (var semaphore = new SemaphoreSlim(_imageMaxDegreeOfParallelism))
            {
                var downloadTasks = imageRequests.Select(async request =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var loggerWithContext = Log.ForContext("imageUrl", request.ImageUrl);
                        using (var timeLog = loggerWithContext.TrackTime("DownloadImage"))
                        {
                            byte[]? imageData = request.ShouldCache
                                                ? await ReadImageFromCache(request.CachePath)
                                                : null;

                            if (imageData == null)
                            {
                                imageData = await _httpClient.GetByteArrayAsync(request.ImageUrl);

                                if (request.ShouldCache)
                                {
                                    await WriteImageToCache(request.CachePath, imageData);
                                }
                            }

                            // OpenXml document modifications are not thread safe. Ensure only one thread is modifying the document at any given point
                            // the real bottleneck would be the image downloads which we are running in parallel
                            lock (_documentLock)
                            {
                                ReplaceImageInDocument(document, request.Blip, imageData);
                                Log.Debug($"Finished replacing image {ctr}/{imageRequests.Count()}");
                                ctr++;
                            }
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                // Wait for all downloads to complete
                await Task.WhenAll(downloadTasks);
            }
        }

        private async Task<byte[]?> ReadImageFromCache(string filePath)
        {
            string cachePath = string.Empty;
            try
            {
                cachePath = System.IO.Path.Combine(_templateStorageOptions.CachePath, filePath).Replace("\\", "/");
                var content = await _storageProvider.GetFile(cachePath);

                if (content != null)
                {
                    Log.Verbose($"Cache hit: {cachePath}");
                }

                return content;
            }
            catch (Exception ex) when (
                ex is FileNotFoundException ||
                ex is DirectoryNotFoundException ||
                (ex is AmazonS3Exception s3Ex && s3Ex.StatusCode == HttpStatusCode.NotFound))
            {
                Log.Verbose($"Cache miss: {cachePath}");
                return null;
            }
        }

        private async Task WriteImageToCache(string filePath, byte[] imageData)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                var cachePath = System.IO.Path.Combine(_templateStorageOptions.CachePath, filePath).Replace("\\", "/");
                await _storageProvider.WriteFile(new MemoryStream(imageData), cachePath);
            }
        }

        private void ReplaceImageInDocument(WordprocessingDocument document, Blip blip, byte[] imageData)
        {
            // Retrieve the existing image part
            var oldImagePart = document.MainDocumentPart!.GetPartById(blip.Embed!.Value!) as ImagePart;

            // Add the new image part (Ensure the correct image type is used here)
            ImagePart newImagePart = document.MainDocumentPart.AddImagePart(ImagePartType.Jpeg);

            using (var imageStream = new MemoryStream(imageData))
            {
                newImagePart.FeedData(imageStream);
            }

            // Update the relationship ID to the new image part
            blip.Embed = document.MainDocumentPart.GetIdOfPart(newImagePart);

            // Delete the old image part if it exists
            if (oldImagePart != null)
            {
                document.MainDocumentPart.DeletePart(oldImagePart);
            }
        }
    }
}
