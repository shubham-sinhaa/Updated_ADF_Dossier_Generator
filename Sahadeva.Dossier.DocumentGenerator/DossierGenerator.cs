using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Options;
using Sahadeva.Dossier.DAL;
using Sahadeva.Dossier.DocumentGenerator.Configuration;
using Sahadeva.Dossier.DocumentGenerator.Data;
using Sahadeva.Dossier.DocumentGenerator.Imaging;
using Sahadeva.Dossier.DocumentGenerator.IO;
using Sahadeva.Dossier.DocumentGenerator.OpenXml;
using Sahadeva.Dossier.DocumentGenerator.Processors;
using Sahadeva.Dossier.Entities;
using Serilog;
using Serilog.Context;

namespace Sahadeva.Dossier.DocumentGenerator
{
    internal class DossierGenerator
    {
        private readonly DocumentHelper _documentHelper;
        private readonly IStorageProvider _storageProvider;
        private readonly PlaceholderHelper _placeholderHelper;
        private readonly PlaceholderFactory _placeholderFactory;
        private readonly DatasetLoader _datasetLoader;
        private readonly ImageDownloader _imageDownloader;
        private readonly DossierDAL _dal;
        private readonly TemplateStorageOptions _templateStorageOptions;

        public DossierGenerator(
            DocumentHelper documentHelper,
            IStorageProvider storageProvider,
            PlaceholderHelper placeholderHelper,
            PlaceholderFactory placeholderFactory,
            DatasetLoader datasetLoader,
            ImageDownloader imageDownloader,
            DossierDAL dal,
            IOptions<TemplateStorageOptions> options)
        {
            _documentHelper = documentHelper;
            _storageProvider = storageProvider;
            _placeholderHelper = placeholderHelper;
            _placeholderFactory = placeholderFactory;
            _datasetLoader = datasetLoader;
            _imageDownloader = imageDownloader;
            _dal = dal;
            _templateStorageOptions = options.Value;
        }

        internal async Task ExecuteJob(DossierJob job)
        {
            try
            {
                //_dal.UpdateJobStatus(job.CoverageDossierId, DossierStatus.DossierGenerationStart);
                Log.Verbose("Marked dossier generation start");

                using (MemoryStream stream = await ReadFromTemplate(job.TemplateName))
                using (WordprocessingDocument document = WordprocessingDocument.Open(stream, true))
                {
                    _documentHelper.StripTrackingInfo(document);
                    Log.Verbose("Removed tracking info from document");

                    _placeholderHelper.FixPlaceholdersAcrossRuns(document);
                    Log.Verbose("Fixed placeholders that span multiple runs");

                    _placeholderHelper.IsolatePlaceholders(document);
                    Log.Verbose("Placed each placeholder in its own text node");

                    var placeholders = _placeholderHelper.GetPlaceholdersWithDataSource(document);
                    Log.Verbose("Extracted placeholders with data sources");

                    var data = _datasetLoader.LoadDataset(job, placeholders.Select(p => p.Text));
                    Log.Verbose("Loaded the required datasets");

                    foreach (var placeholder in placeholders)
                    {
                        using (LogContext.PushProperty("placeholder", placeholder.Text))
                        {
                            var processor = _placeholderFactory.CreateProcessor(job, placeholder, document);

                            var dataTable = data.Tables[processor.TableName]
                                ?? throw new ApplicationException($"Could not find table for {placeholder.Text} having name {processor.TableName}");

                            processor.ReplacePlaceholder(dataTable);
                        }
                    }
                    Log.Verbose("Finished processing placeholders");

                    await _imageDownloader.DownloadImagesAsync(document);
                    Log.Verbose("Finished Downloading images");

                    CheckForUnProcessedPlaceholders(document);
                    Log.Verbose("Checking for unprocessed placeholders");

                    _documentHelper.RemoveGrammarErrors(document);
                    Log.Verbose("Removed grammar errors");

                    // TODO: Check the template for the errors so we know if the issues are after generation or existing
                    //OpenXmlValidator validator = new OpenXmlValidator();
                    //int errorCount = 0;

                    //foreach (ValidationErrorInfo error in validator.Validate(document))
                    //{
                    //    Console.WriteLine("Error Description: {0}", error.Description);
                    //    Console.WriteLine("Error Path: {0}", error.Path.XPath);
                    //    Console.WriteLine("Error Part: {0}", error.Part.Uri);
                    //    errorCount++;
                    //}

                    // Flush changes from the word doc to the memory stream
                    document.Save();

                    await WriteFile(stream, job.OutputFilePath);
                    Log.Verbose("Saved dossier");

                    //_dal.UpdateJobStatus(job.CoverageDossierId, DossierStatus.DossierGenerationCompleted);
                    Log.Verbose("Updated dossier completed status");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);

                // Failed to generate the dossier, reset to prev state for it to be picked up again
                // TODO: Ideally we should have a new state for dossier generation failure 
                _dal.UpdateJobStatus(job.DID, DossierStatus.SummaryCompleted);
            }
        }

        /// <summary>
        /// Verify that we do not have any unprocessed placdeholders in the document
        /// </summary>
        /// <param name="document"></param>
        /// <exception cref="ApplicationException"></exception>
        private void CheckForUnProcessedPlaceholders(WordprocessingDocument document)
        {
            var leftOvers = _placeholderHelper.GetAllPlaceholders(document);

            if (leftOvers.Any())
            {
                throw new ApplicationException(
                    $"The document contains {leftOvers.Count} unprocessed placeholder(s). " +
                    $"[{string.Join(",", leftOvers.Select(l => l.Text))}]");
            }
        }

        private async Task<MemoryStream> ReadFromTemplate(string fileName)
        {
            var filePath = Path.Combine(_templateStorageOptions.TemplatePath, fileName.TrimStart('/')).Replace("\\", "/");
            var content = await _storageProvider.GetFile(filePath);

            // This creates an expandable memory stream. Do not try to create a new MemoryStream directly from the byte[]
            var stream = new MemoryStream();
            stream.Write(content);

            return stream;
        }

        private async Task WriteFile(MemoryStream stream, string fileName)
        {
            var filePath = Path.Combine(_templateStorageOptions.OutputPath, fileName).Replace("\\", "/");
            await _storageProvider.WriteFile(stream, filePath);
        }
    }
}
