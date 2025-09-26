using RenameBooks.Interfaces;
using RenameBooks.Records;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace RenameBooks.Strategies
{
    public class Fb2RenamerStrategy : IRenamerStrategy
    {
        private static readonly XNamespace Fb2Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0";

        public bool CanHandle(string filePath)
        {
            return IsFb2File(filePath) || IsFb2ZipFile(filePath);
        }
        public IEnumerable<string> GetSupportedExtensions() => new[] { ".fb2", ".fb2.zip" };

        private static bool IsFb2File(string path) =>
            path.EndsWith(".fb2", StringComparison.OrdinalIgnoreCase);

        private static bool IsFb2ZipFile(string path) =>
            path.EndsWith(".fb2.zip", StringComparison.OrdinalIgnoreCase);

        [return: MaybeNull]
        private XDocument? TryLoadFb2Document(string filePath)
        {
            try
            {
                if (IsFb2ZipFile(filePath))
                {
                    using var archive = ZipFile.OpenRead(filePath);
                    var fb2Entry = archive.Entries
                        .FirstOrDefault(e => IsFb2File(e.Name));
                    if (fb2Entry == null)
                        return null;

                    using var entryStream = fb2Entry.Open();
                    using var memoryStream = new MemoryStream();
                    entryStream.CopyTo(memoryStream);
                    memoryStream.Position = 0;
                    return XDocument.Load(memoryStream);
                }
                else if (IsFb2File(filePath))
                {
                    return XDocument.Load(filePath);
                }

                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
        }

        private List<Author> ExtractAuthorsFromDocument(XDocument doc, XNamespace ns)
        {
            var authors = new List<Author>();

            foreach (var authorElement in doc.Descendants(ns + "author"))
            {
                string? firstName = authorElement.Element(ns + "first-name")?.Value?.Trim();
                string? lastName = authorElement.Element(ns + "last-name")?.Value?.Trim();
                string? nickname = authorElement.Element(ns + "nickname")?.Value?.Trim();
                string? middleName = authorElement.Element(ns + "middle-name")?.Value?.Trim();

                authors.Add(new Author(firstName, middleName, lastName, nickname));
            }

            return authors;
        }

        public BookMetadata? ExtractMetadata(string filePath)
        {
            var doc = TryLoadFb2Document(filePath);
            if (doc == null)
                return null;

            var ns = Fb2Namespace;

            string? title = doc.Descendants(ns + "book-title")
                               .FirstOrDefault()?.Value?.Trim();

            var authors = ExtractAuthorsFromDocument(doc, ns);

            var sequenceElement = doc.Descendants(ns + "sequence").FirstOrDefault();
            string? seriesName = null;
            int? seriesNumber = null;

            if (sequenceElement != null)
            {
                seriesName = sequenceElement.Attribute("name")?.Value?.Trim();
                var numberStr = sequenceElement.Attribute("number")?.Value;
                if (!string.IsNullOrEmpty(numberStr) && int.TryParse(numberStr, out int num))
                {
                    seriesNumber = num;
                }

                if (string.IsNullOrEmpty(seriesName))
                {
                    seriesName = null;
                    seriesNumber = null;
                }
            }

            return new BookMetadata(title, authors, seriesName, seriesNumber);
        }
    }
}