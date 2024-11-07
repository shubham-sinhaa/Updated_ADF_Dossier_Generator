using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Sahadeva.Dossier.DocumentGenerator.OpenXml;

namespace Sahadeva.Dossier.DocumentGenerator.Processors
{
    internal abstract class UrlProcessorBase : PlaceholderProcessorBase<Text>
    {
        private readonly WordprocessingDocument _document;

        protected string LinkColumnName { get; set; } = string.Empty;

        protected string DisplayColumnName { get; set; } = string.Empty;

        protected UrlProcessorBase(Placeholder<Text> placeholder, WordprocessingDocument document) : base(placeholder)
        {
            _document = document;
        }

        protected void ReplacePlaceholderWithUrl(string link, string displayText)
        {
            // Get the parent run containing the placeholder
            var parentRun = Placeholder.Element.Parent as Run;
            if (parentRun == null) return;

            // Preserve the original formatting by copying the placeholder run properties if any
            var newRunProperties = parentRun.RunProperties != null
                ? (RunProperties)parentRun.RunProperties.CloneNode(true)
                : new RunProperties();

            // Applying some default styles to make it look like a hyperlink
            // This is required if we set the font size, etc as we copy the original styles from the template which may not define the hyperlink style
            if (newRunProperties.GetFirstChild<Underline>() == null)
            {
                newRunProperties.AddChild(new Underline { Val = UnderlineValues.Single });
            }

            if (newRunProperties.GetFirstChild<Color>() == null)
            {
                newRunProperties.AddChild(new Color { ThemeColor = ThemeColorValues.Hyperlink });
            }

            // Create a hyperlink relationship. Pass the relationship id to the hyperlink below.
            var rel = _document.MainDocumentPart!.AddHyperlinkRelationship(new Uri(link), true);

            var hyperlink = new Hyperlink(
                    new Run(
                        newRunProperties,
                        new Text(displayText)
                    ))
            { History = OnOffValue.FromBoolean(true), Id = rel.Id };

            Placeholder.Element.InsertBeforeSelf(hyperlink);
            Placeholder.Element.Remove();
        }
    }
}
