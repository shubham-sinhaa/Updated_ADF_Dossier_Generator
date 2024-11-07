using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Xml.Linq;

namespace Sahadeva.Dossier.DocumentGenerator.OpenXml
{
    internal class DocumentHelper
    {
        /// <summary>
        /// Word inserts unique ids for paragraphs, text etc which it uses for change tracking
        /// Cloning elements during Dossier generation can lead to duplication of these ids which corrupts the document
        /// This method removes these attributes as they are not required while generating the document
        /// MS Word will insert new ids if the document is edited
        /// </summary>
        /// <param name="document"></param>
        internal void StripTrackingInfo(WordprocessingDocument document)
        {
            var mainPart = document.MainDocumentPart;

            foreach (var element in mainPart!.Document.Descendants())
            {
                element.RemoveAttribute("rsidRPr", "http://schemas.openxmlformats.org/wordprocessingml/2006/main");
                element.RemoveAttribute("rsidR", "http://schemas.openxmlformats.org/wordprocessingml/2006/main");
                element.RemoveAttribute("rsidP", "http://schemas.openxmlformats.org/wordprocessingml/2006/main");
                element.RemoveAttribute("rsidRDefault", "http://schemas.openxmlformats.org/wordprocessingml/2006/main");
                element.RemoveAttribute("rsidTr", "http://schemas.openxmlformats.org/wordprocessingml/2006/main");

                element.RemoveAttribute("paraId", "http://schemas.microsoft.com/office/word/2010/wordml");
                element.RemoveAttribute("textId", "http://schemas.microsoft.com/office/word/2010/wordml");
            }

            mainPart.Document.Save();
        }

        /// <summary>
        /// Removes any grammar error marks in the document.
        /// This does not affect the document layout
        /// </summary>
        /// <param name="document"></param>
        internal void RemoveGrammarErrors(WordprocessingDocument document)
        {
            var mainPart = document.MainDocumentPart!;
            var proofErrors = mainPart.Document.Descendants<ProofError>().ToList();

            foreach (var error in proofErrors)
            {
                error.Remove();
            }

            mainPart.Document.Save();
        }
    }
}
