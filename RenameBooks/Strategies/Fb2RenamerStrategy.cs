using RenameBooks.Interfaces;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public string? ExtractTitle(string filePath)
        {
            XDocument? doc = null;

            if (filePath.EndsWith(".fb2.zip", StringComparison.OrdinalIgnoreCase))
            {
                // Обработка сжатого FB2
                using (var archive = ZipFile.OpenRead(filePath))
                {
                    var fb2Entry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".fb2", StringComparison.OrdinalIgnoreCase));
                    if (fb2Entry == null) return null;

                    using (var stream = fb2Entry.Open())
                    {
                        doc = XDocument.Load(stream);
                    }
                }
            }
            else if (filePath.EndsWith(".fb2", StringComparison.OrdinalIgnoreCase))
            {
                // Обработка обычного FB2
                doc = XDocument.Load(filePath);
            }
            else
            {
                return null; // Неизвестный формат
            }

            // Пространство имён FB2
            XNamespace ns = "http://www.gribuser.ru/xml/fictionbook/2.0";

            var bookTitleElement = doc.Descendants(ns + "book-title").FirstOrDefault();
            return bookTitleElement?.Value?.Trim();
        }
        public (string? SeriesName, int? SeriesNumber) ExtractSeriesInfo(string filePath)
        {
            XDocument? doc = null;

            if (filePath.EndsWith(".fb2.zip", StringComparison.OrdinalIgnoreCase))
            {
                using var archive = ZipFile.OpenRead(filePath);
                var fb2Entry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".fb2", StringComparison.OrdinalIgnoreCase));
                if (fb2Entry == null) return (null, null);

                using var stream = fb2Entry.Open();
                doc = XDocument.Load(stream);
            }
            else if (filePath.EndsWith(".fb2", StringComparison.OrdinalIgnoreCase))
            {
                doc = XDocument.Load(filePath);
            }
            else
            {
                return (null, null);
            }

            XNamespace ns = "http://www.gribuser.ru/xml/fictionbook/2.0";

            var sequenceElement = doc.Descendants(ns + "sequence").FirstOrDefault();
            if (sequenceElement == null) return (null, null);

            string? name = sequenceElement.Attribute("name")?.Value?.Trim();
            string? numberStr = sequenceElement.Attribute("number")?.Value;

            if (string.IsNullOrEmpty(name)) return (null, null);

            int? number = null;
            if (!string.IsNullOrEmpty(numberStr) && int.TryParse(numberStr, out int num))
            {
                number = num;
            }

            return (name, number);
        }
        public string? ExtractAuthor(string filePath)
        {
            XDocument? doc = null;

            if (filePath.EndsWith(".fb2.zip", StringComparison.OrdinalIgnoreCase))
            {
                using var archive = ZipFile.OpenRead(filePath);
                var fb2Entry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".fb2", StringComparison.OrdinalIgnoreCase));
                if (fb2Entry == null) return null;

                using var stream = fb2Entry.Open();
                doc = XDocument.Load(stream);
            }
            else if (filePath.EndsWith(".fb2", StringComparison.OrdinalIgnoreCase))
            {
                doc = XDocument.Load(filePath);
            }
            else
            {
                return null;
            }

            XNamespace ns = "http://www.gribuser.ru/xml/fictionbook/2.0";

            // Берём первого автора
            var authorElement = doc.Descendants(ns + "author").FirstOrDefault();
            if (authorElement == null) return null;

            string? firstName = authorElement.Element(ns + "first-name")?.Value?.Trim();
            string? lastName = authorElement.Element(ns + "last-name")?.Value?.Trim();
            string? nickname = authorElement.Element(ns + "nickname")?.Value?.Trim();

            // Приоритет: Фамилия + Имя → Фамилия → Ник → Имя
            if (!string.IsNullOrEmpty(lastName))
            {
                return string.IsNullOrEmpty(firstName)
                    ? lastName
                    : $"{lastName} {firstName}";
            }

            if (!string.IsNullOrEmpty(nickname))
                return nickname;

            if (!string.IsNullOrEmpty(firstName))
                return firstName;

            return null;
        }
    }
}
