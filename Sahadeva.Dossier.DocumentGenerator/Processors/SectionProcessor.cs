using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Sahadeva.Dossier.DocumentGenerator.OpenXml;
using Sahadeva.Dossier.DocumentGenerator.Parsers;
using System.Data;
using System.Text.RegularExpressions;

namespace Sahadeva.Dossier.DocumentGenerator.Processors
{
    internal partial class SectionProcessor(
        Placeholder<Text> placeholder,
        WordprocessingDocument document,
        PlaceholderParser placeholderParser,
        PlaceholderHelper placeholderHelper,
        RowPlaceholderFactory rowPlaceholderFactory
            ) : PlaceholderProcessorBase<Text>(placeholder), IPlaceholderWithDataSource
    {
        private readonly WordprocessingDocument _document = document;
        private readonly PlaceholderParser _placeholderParser = placeholderParser;
        private readonly PlaceholderHelper _placeholderHelper = placeholderHelper;
        private readonly RowPlaceholderFactory _rowPlaceholderFactory = rowPlaceholderFactory;

        public string TableName { get; private set; } = string.Empty;

        public void ReplacePlaceholder(DataTable data)
        {
            var parentParagraph = GetParentParagraph();

            if (parentParagraph == null) { throw new ApplicationException($"Section placeholders should be placed in its own paragraph. {Placeholder.Text}"); }

            var sectionTemplate = GetSectionContent();

            var filter = _placeholderParser.GetFilter(Placeholder.Text);
            DataRow[] filteredRows = filter.Length > 0 ? data.Select(filter) : data.Select();

            foreach (var dataRow in filteredRows)
            {
                // Create a clone of all the elements in the section template
                var clones = sectionTemplate.Select(n => n.CloneNode(true)).ToList();

                ProcessClonedImages(clones);

                var clonedSectionPlaceholders = _placeholderHelper.GetPlaceholders(SectionPlaceholderRegex(), clones);
                foreach (var placeholder in clonedSectionPlaceholders)
                {
                    var processor = _rowPlaceholderFactory.CreateProcessor(placeholder, _document);
                    processor.ReplacePlaceholder(dataRow);
                }

                clones.ForEach(n => parentParagraph!.InsertBeforeSelf(n));
            }

            // Remove the original placeholder and placeholder content
            parentParagraph.Remove();
            sectionTemplate.ForEach(e => e.Remove());
        }

        public override void SetPlaceholderOptions()
        {
            var match = OptionsRegex().Match(Placeholder.Text);
            if (match.Success)
            {
                TableName = match.Value;
            }
            else
            {
                throw new ApplicationException($"Could not parse {Placeholder.Text}");
            }
        }

        /// <summary>
        /// Processes cloned elements to create new ImageParts for cloned images
        /// </summary>
        private void ProcessClonedImages(List<OpenXmlElement> clonedElements)
        {
            var drawings = clonedElements.SelectMany(e => e.Descendants<Drawing>()).ToList();
            foreach (var drawing in drawings)
            {
                var blip = drawing.Descendants<DocumentFormat.OpenXml.Drawing.Blip>().FirstOrDefault();
                if (blip != null && blip.Embed != null)
                {
                    // Get the old image part
                    var oldImagePart = (ImagePart)_document.MainDocumentPart!.GetPartById(blip.Embed!.Value!);

                    // Create a new image part for the cloned image
                    var newImagePart = _document.MainDocumentPart.AddImagePart(ImagePartType.Jpeg);

                    // Copy the data from the old image part to the new image part
                    using (var stream = oldImagePart.GetStream())
                    {
                        newImagePart.FeedData(stream);
                    }

                    // Update the Blip embed ID to point to the new image part
                    blip.Embed = _document.MainDocumentPart.GetIdOfPart(newImagePart);
                }
            }
        }

        private List<OpenXmlElement> GetSectionContent()
        {
            var insideSection = true;
            var sectionContent = new List<OpenXmlElement>();

            var parentParagraph = GetParentParagraph();
            var currentElement = parentParagraph?.NextSibling();

            while (currentElement != null && insideSection)
            {
                var end = currentElement
                    .Descendants<Text>()
                    .Where(t => SectionEndRegex().IsMatch(t.Text))
                    .FirstOrDefault();

                if (end != null)
                {
                    insideSection = false;
                    currentElement.Remove();
                    break;
                }

                sectionContent.Add(currentElement);

                currentElement = currentElement.NextSibling();
            }
            return sectionContent;
        }

        /// <summary>
        /// Section placeholders should sit alone in their own paragraphs. This is an important assumption in the processing of these placeholders
        /// </summary>
        /// <returns></returns>
        private Paragraph? GetParentParagraph()
        {
            OpenXmlElement? parentElement = Placeholder.Element.Parent;

            // Traverse up the hierarchy until we find a Paragraph element
            while (parentElement != null && !(parentElement is Paragraph))
            {
                parentElement = parentElement.Parent;
            }

            return parentElement as Paragraph;
        }


        [GeneratedRegex(@"(?<=\[AF\.Section\.Start:)[^\]]+", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        private static partial Regex OptionsRegex();

        [GeneratedRegex(@"\[AF\.Section\.End\]", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        private static partial Regex SectionEndRegex();


        [GeneratedRegex(@"\[AF\.Row\.[^\]]+\]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex SectionPlaceholderRegex();
    }
}
