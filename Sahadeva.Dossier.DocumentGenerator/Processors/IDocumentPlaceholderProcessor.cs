using System.Data;

namespace Sahadeva.Dossier.DocumentGenerator.Processors
{
    /// <summary>
    /// Interface that should be implemented by any placeholder that can be used directly in the document
    /// </summary>
    internal interface IDocumentPlaceholderProcessor
    {
        /// <summary>
        /// Replace the placeholder text with actual content
        /// </summary>
        /// <param name="data"></param>
        void ReplacePlaceholder(DataTable data);
    }
}