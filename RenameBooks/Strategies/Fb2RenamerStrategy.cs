using RenameBooks.Interfaces;
using RenameBooks.Records;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace RenameBooks.Strategies
{
    public class Fb2RenamerStrategy : IRenamerStrategy
    {
        public bool CanHandle(string filePath)
        {
            return filePath.EndsWith(".fb2", StringComparison.OrdinalIgnoreCase) ||
                   filePath.EndsWith(".fb2.zip", StringComparison.OrdinalIgnoreCase);
        }

        private static readonly XNamespace Fb2Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0";

        [return: MaybeNull]
        private XDocument? TryLoadFb2Document(string filePath)
        {
            try
            {
                if (filePath.EndsWith(".fb2.zip", StringComparison.OrdinalIgnoreCase))
                {
                    using var archive = ZipFile.OpenRead(filePath);
                    var fb2Entry = archive.Entries
                        .FirstOrDefault(e => e.Name.EndsWith(".fb2", StringComparison.OrdinalIgnoreCase));
                    if (fb2Entry == null) return null;

                    using var stream = fb2Entry.Open();
                    return XDocument.Load(stream);
                }
                else if (filePath.EndsWith(".fb2", StringComparison.OrdinalIgnoreCase))
                {
                    return XDocument.Load(filePath);
                }
                return null;
            }
            catch
            {
                // В реальном проекте — логирование
                return null;
            }
        }

        public BookMetadata? ExtractMetadata(string filePath)
        {
            var doc = TryLoadFb2Document(filePath);
            if (doc == null) return null;

            var ns = Fb2Namespace;

            // Извлекаем заголовок
            string? title = doc.Descendants(ns + "book-title")
                               .FirstOrDefault()?.Value?.Trim();

            // Извлекаем автора
            string? author = ExtractAuthorFromDocument(doc, ns);

            // Извлекаем серию
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

                // Если имя серии пустое — игнорируем всю серию
                if (string.IsNullOrEmpty(seriesName))
                {
                    seriesName = null;
                    seriesNumber = null;
                }
            }

            return new BookMetadata(title, author, seriesName, seriesNumber);
        }

        private string? ExtractAuthorFromDocument(XDocument doc, XNamespace ns)
        {
            var authorElement = doc.Descendants(ns + "author").FirstOrDefault();
            if (authorElement == null) return null;

            string? firstName = authorElement.Element(ns + "first-name")?.Value?.Trim();
            string? lastName = authorElement.Element(ns + "last-name")?.Value?.Trim();
            string? nickname = authorElement.Element(ns + "nickname")?.Value?.Trim();
            string? middleName = authorElement.Element(ns + "middle-name")?.Value?.Trim();

            if (!string.IsNullOrEmpty(lastName))
            {
                var givenNames = new List<string>();
                if (!string.IsNullOrEmpty(firstName))
                    givenNames.Add(firstName);
                if (!string.IsNullOrEmpty(middleName))
                    givenNames.Add(middleName);

                string givenNamePart = string.Join(" ", givenNames);
                return string.IsNullOrEmpty(givenNamePart)
                    ? lastName
                    : $"{givenNamePart} {lastName}";
            }

            if (!string.IsNullOrEmpty(nickname))
                return nickname;

            if (!string.IsNullOrEmpty(firstName))
                return firstName;

            return null;
        }
    }
}