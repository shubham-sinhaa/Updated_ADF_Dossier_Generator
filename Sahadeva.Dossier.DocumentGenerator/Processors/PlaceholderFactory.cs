using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.DependencyInjection;
using Sahadeva.Dossier.DocumentGenerator.OpenXml;
using Sahadeva.Dossier.DocumentGenerator.Parsers;
using Sahadeva.Dossier.Entities;

namespace Sahadeva.Dossier.DocumentGenerator.Processors
{
    internal class PlaceholderFactory
    {
        private readonly PlaceholderParser _placeholderParser;
        private readonly IServiceProvider _serviceProvider;

        public PlaceholderFactory(PlaceholderParser placeholderParser, IServiceProvider serviceProvider)
        {
            _placeholderParser = placeholderParser;
            _serviceProvider = serviceProvider;
        }

        internal IPlaceholderWithDataSource CreateProcessor<T>(DossierJob job, IPlaceholder<T> placeholder, WordprocessingDocument document) where T : OpenXmlElement
        {
            var placeholderType = _placeholderParser.GetPlaceholderType(placeholder.Text);

            return placeholderType switch
            {
                "Value" => ActivatorUtilities.CreateInstance<DocumentValueProcessor>(_serviceProvider, placeholder),
                "MultilineValue" => ActivatorUtilities.CreateInstance<DocumentMultilineValueProcessor>(_serviceProvider, placeholder),
                "Image" => ActivatorUtilities.CreateInstance<DocumentImageProcessor>(_serviceProvider, placeholder),
                "Url" => ActivatorUtilities.CreateInstance<DocumentUrlProcessor>(_serviceProvider, placeholder, document),
                "Screenshot" => ActivatorUtilities.CreateInstance<DocumentScreenshotProcessor>(_serviceProvider, placeholder),
                "Graph" => ActivatorUtilities.CreateInstance<DocumentGraphProcessor>(_serviceProvider, job, placeholder),
                "Table" => ActivatorUtilities.CreateInstance<TableProcessor>(_serviceProvider, placeholder, document),
                "Section.Start" => ActivatorUtilities.CreateInstance<SectionProcessor>(_serviceProvider, placeholder, document),
                _ => throw new NotSupportedException($"Unsupported placeholder type: {placeholder.Text}"),
            };
        }
    }
}
