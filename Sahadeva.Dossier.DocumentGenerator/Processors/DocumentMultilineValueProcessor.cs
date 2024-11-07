using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Sahadeva.Dossier.DocumentGenerator.OpenXml;
using System.Data;
using System.Text.RegularExpressions;

namespace Sahadeva.Dossier.DocumentGenerator.Processors
{
    /// <summary>
    /// Replaces a placeholder with multiline data. Each line of text is placed within a new paragraph
    /// </summary>
    internal partial class DocumentMultilineValueProcessor : DocumentValueProcessor
    {
        public DocumentMultilineValueProcessor(Placeholder<Text> placeholder) : base(placeholder)
        {
        }

        public override void ReplacePlaceholder(DataTable data)
        {
            var value = GetValueFromSource(data);
            ReplaceWithMultilineText(value);
        }

        protected override Regex GetPlaceholderOptionsRegex()
        {
            return OptionsRegex();
        }

        /// <summary>
        /// Multiline text contains \r\n or \n line breaks which are not understood by Word.
        /// This method converts the line breaks into new paragraphs with text elements.
        /// </summary>
        /// <param name="value"></param>
        private void ReplaceWithMultilineText(string value)
        {
            // Split the multiline string by new line characters
            var lines = value.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            // Get the parent paragraph of the placeholder
            var placeholderParagraph = Placeholder.Element.Ancestors<Paragraph>().FirstOrDefault();

            if (placeholderParagraph == null)
            {
                throw new InvalidOperationException("Placeholder is not within a paragraph.");
            }

            var newParagraph = new Paragraph();
            var newRun = new Run();

            // Insert break in the first paragraph to create space
            newRun.Append(new Break());

            // Iterate through each line and create a new paragraph
            foreach (var line in lines)
            {
                // Preserve the original formatting by copying the placeholder run properties if any
                if (Placeholder.Element.Parent is Run parentRun && parentRun.RunProperties != null)
                {
                    newRun.RunProperties = (RunProperties)parentRun.RunProperties.CloneNode(true);
                }

                // Add the line of text to the run
                newRun.Append(new Text(line) { Space = SpaceProcessingModeValues.Preserve });

                // Append the run to the paragraph
                newParagraph.Append(newRun);

                // Insert the new paragraph after the placeholder paragraph
                placeholderParagraph.InsertBeforeSelf(newParagraph);

                // Reset the paragraph and run for the next line
                newParagraph = new Paragraph();
                newRun = new Run();
            }

            // Remove the placeholder's parent paragraph (including the placeholder itself)
            placeholderParagraph.Remove();
        }

        [GeneratedRegex(@"\[AF\.(MultilineValue):(?<TableName>[^\.\]]+)\.(?<ColumnName>[^\|\]]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        private static partial Regex OptionsRegex();
    }
}
