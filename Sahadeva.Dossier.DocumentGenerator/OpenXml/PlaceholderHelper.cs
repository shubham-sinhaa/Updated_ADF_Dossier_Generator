using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing.Wordprocessing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using OpenXmlPowerTools;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Sahadeva.Dossier.DocumentGenerator.OpenXml
{
    internal interface IPlaceholder<out T> where T : OpenXmlElement
    {
        string Text { get; }
        T Element { get; }
    }

    internal class Placeholder<T> : IPlaceholder<T> where T : OpenXmlElement
    {
        internal Placeholder(string placeholderText, T placeholderElement)
        {
            Text = placeholderText;
            Element = placeholderElement;
        }

        public string Text { get; private set; }

        public T Element { get; private set; }
    }

    internal partial class PlaceholderHelper
    {
        private readonly Regex _placeholder = Placeholder();
        private readonly Regex _placeholderWithDataSource = PlaceholderWithDataSource();

        /// <summary>
        /// Only searches for placeholders that contain a data source i.e TableName.
        /// Children of Tables, Sections etc are ignored
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        internal List<IPlaceholder<OpenXmlElement>> GetPlaceholdersWithDataSource(WordprocessingDocument document)
        {
            var body = document.MainDocumentPart?.Document.Body ?? throw new ApplicationException("Invalid document");

            return ExtractPlaceholders(_placeholderWithDataSource, body);
        }

        /// <summary>
        /// Gets all the placeholders in the document template
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        internal List<IPlaceholder<OpenXmlElement>> GetAllPlaceholders(WordprocessingDocument document)
        {
            var body = document.MainDocumentPart?.Document.Body ?? throw new ApplicationException("Invalid document");

            return ExtractPlaceholders(_placeholder, body);
        }

        internal List<IPlaceholder<OpenXmlElement>> GetPlaceholders(Regex regex, IEnumerable<OpenXmlElement> elements)
        {
            return ExtractPlaceholders(regex, elements.ToArray());
        }

        /// <summary>
        /// In some instances placeholders may be surrounded by text e.g when a placeholder is used in a sentence.
        /// This makes it difficult to replace the entire text node with a value as we would overwrite other text as well.
        /// This method will ensure that each placeholder sits in its own text node so that when we override/replace the placeholder
        /// it should not result in any data loss.
        /// </summary>
        /// <param name="document"></param>
        /// <exception cref="NotImplementedException"></exception>
        internal void IsolatePlaceholders(WordprocessingDocument document)
        {
            XDocument xDoc = document.MainDocumentPart.GetXDocument();

            var textElements = xDoc.Descendants(W.t)
                   .Where(e => _placeholder.IsMatch(e.Value));

            foreach (var textElement in textElements.ToList())
            {
                string text = textElement.Value;
                var matches = _placeholder.Matches(text);

                if (matches.Count > 0)
                {
                    var newElements = new List<XElement>();
                    int currentIndex = 0;

                    foreach (Match match in matches)
                    {
                        int placeholderStartIndex = match.Index;
                        int placeholderLength = match.Length;

                        // Add text before the placeholder
                        if (placeholderStartIndex > currentIndex)
                        {
                            string beforePlaceholder = text.Substring(currentIndex, placeholderStartIndex - currentIndex);
                            if (!string.IsNullOrEmpty(beforePlaceholder))
                            {
                                newElements.Add(new XElement(
                                    W.t,
                                    new XAttribute(XNamespace.Xml + "space", "preserve"),
                                    beforePlaceholder)
                                );
                            }
                        }

                        // Add the placeholder itself
                        string placeholder = match.Value;
                        newElements.Add(new XElement(
                            W.t,
                            new XAttribute(XNamespace.Xml + "space", "preserve"),
                            placeholder));

                        // Update currentIndex to after the placeholder
                        currentIndex = placeholderStartIndex + placeholderLength;
                    }

                    // Add text after the last placeholder
                    if (currentIndex < text.Length)
                    {
                        string afterPlaceholder = text.Substring(currentIndex);
                        if (!string.IsNullOrEmpty(afterPlaceholder))
                        {
                            newElements.Add(new XElement(
                                W.t,
                                new XAttribute(XNamespace.Xml + "space", "preserve"),
                                afterPlaceholder)
                            );
                        }
                    }

                    // Replace the old text element with the new elements
                    textElement.AddBeforeSelf(newElements);

                    // Remove the original text element
                    textElement.Remove();
                }
            }

            // Save changes to the document
            document.MainDocumentPart.PutXDocument();
        }

        /// <summary>
        /// Extracts all placeholders that match the specified pattern
        /// </summary>
        /// <param name="document"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        private List<IPlaceholder<OpenXmlElement>> ExtractPlaceholders(Regex pattern, params OpenXmlElement[] elements)
        {
            var textPlaceholders = elements.SelectMany(e => e.Descendants<Text>())
                .Where(e => pattern.IsMatch(e.Text))
                .Select(e => new Placeholder<Text>(e.Text, e))
                .ToList();

            var imagePlaceholders = elements.SelectMany(e => e.Descendants<Drawing>())
                .Where(d => d.Descendants<DocProperties>()
                             .FirstOrDefault(dp => dp.Description != null && pattern.IsMatch(dp.Description.Value!)) != null
                )
                .Select(d =>
                {
                    var placeholderText = d.Descendants<DocProperties>().First().Description!.Value!;
                    return new Placeholder<Drawing>(placeholderText, d);
                })
                .ToList();

            return [.. textPlaceholders, .. imagePlaceholders];
        }

        /// <summary>
        /// At times, OpenXml may split text across multiple runs. This method flattens such occurrences so that the placeholders
        /// can be identified and replaced more easily.
        /// </summary>
        /// <param name="stream"></param>
        internal void FixPlaceholdersAcrossRuns(WordprocessingDocument document)
        {
            XDocument xDoc = document.MainDocumentPart.GetXDocument();

            var content = xDoc.Descendants(W.p);
            var count = RegexHelper.Replace(
                content,
                _placeholder,
                (match) => match.Value);

            // Save changes to the document
            document.MainDocumentPart.PutXDocument();
        }

        [GeneratedRegex(@"\[AF\.(?:Value|MultilineValue|Table|Url|Section\.Start|Screenshot|Graph):[^\]]+\]", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex PlaceholderWithDataSource();


        /// <summary>
        /// Looks for placeholders matching [AF.*]
        /// </summary>
        // TODO: This should probably check for Row placeholders as well?
        [GeneratedRegex(@"\[AF\.[^\]]+\](?!.*\[\[AF\.[^\]]+\]\])", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        private static partial Regex Placeholder();
    }
}
