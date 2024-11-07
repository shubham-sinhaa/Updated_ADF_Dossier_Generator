using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.DependencyInjection;
using Sahadeva.Dossier.DocumentGenerator.OpenXml;
using Sahadeva.Dossier.DocumentGenerator.Parsers;

namespace Sahadeva.Dossier.DocumentGenerator.Processors
{
    internal class RowPlaceholderFactory
    {
        private readonly PlaceholderParser _placeholderParser;
        private readonly IServiceProvider _serviceProvider;

        public RowPlaceholderFactory(PlaceholderParser placeholderParser, IServiceProvider serviceProvider)
        {
            _placeholderParser = placeholderParser;
            _serviceProvider = serviceProvider;
        }

        internal IRowPlaceholderProcessor CreateProcessor<T>(IPlaceholder<T> placeholder, WordprocessingDocument document) where T : OpenXmlElement
        {
            var placeholderType = _placeholderParser.GetPlaceholderType(placeholder.Text);

            return placeholderType switch
            {
                "Row.Value" => ActivatorUtilities.CreateInstance<RowValueProcessor>(_serviceProvider, placeholder),
                "Row.Url" => ActivatorUtilities.CreateInstance<RowUrlProcessor>(_serviceProvider, placeholder, document),
                "Row.Screenshot" => ActivatorUtilities.CreateInstance<RowScreenshotProcessor>(_serviceProvider, placeholder),
                "Row.Image" => ActivatorUtilities.CreateInstance<RowImageProcessor>(_serviceProvider, placeholder),
                _ => throw new NotSupportedException($"Unsupported placeholder type: {placeholderType} found in {placeholder.Text}"),
            };
        }
    }
}
