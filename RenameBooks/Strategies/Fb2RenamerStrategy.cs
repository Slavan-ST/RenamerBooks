using RenameBooks.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Linq;
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

        private static readonly XNamespace Fb2Namespace = "http://www.gribuser.ru/xml/fictionbook/2.0";

        [return: MaybeNull]
        private XDocument LoadFb2Document(string filePath)
        {
            if (filePath.EndsWith(".fb2.zip", StringComparison.OrdinalIgnoreCase))
            {
                using var archive = ZipFile.OpenRead(filePath);
                var fb2Entry = archive.Entries.FirstOrDefault(e => e.Name.EndsWith(".fb2", StringComparison.OrdinalIgnoreCase));
                if (fb2Entry == null) return null;

                using var stream = fb2Entry.Open();
                return XDocument.Load(stream);
            }
            else if (filePath.EndsWith(".fb2", StringComparison.OrdinalIgnoreCase))
            {
                return XDocument.Load(filePath);
            }
            else
            {
                return null;
            }
        }

        public string? ExtractTitle(string filePath)
        {
            var doc = LoadFb2Document(filePath);
            if (doc == null) return null;

            var ns = Fb2Namespace;

            var bookTitleElement = doc.Descendants(ns + "book-title").FirstOrDefault();
            return bookTitleElement?.Value?.Trim();
        }

        public (string? SeriesName, int? SeriesNumber) ExtractSeriesInfo(string filePath)
        {
            var doc = LoadFb2Document(filePath);
            if (doc == null) return (null, null);

            var ns = Fb2Namespace;

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
            var doc = LoadFb2Document(filePath);
            if (doc == null) return null;

            var ns = Fb2Namespace;

            // Берём первого автора
            var authorElement = doc.Descendants(ns + "author").FirstOrDefault();
            if (authorElement == null) return null;

            string? firstName = authorElement.Element(ns + "first-name")?.Value?.Trim();
            string? lastName = authorElement.Element(ns + "last-name")?.Value?.Trim();
            string? nickname = authorElement.Element(ns + "nickname")?.Value?.Trim();
            string? middleName = authorElement.Element(ns + "middle-name")?.Value?.Trim();

            // Собираем каноническое имя в формате: "Имя Отчество Фамилия" (если есть)
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

            // Если нет фамилии — пробуем ник
            if (!string.IsNullOrEmpty(nickname))
                return nickname;

            // Если есть только имя — возвращаем его
            if (!string.IsNullOrEmpty(firstName))
                return firstName;

            // Ничего нет — null
            return null;
        }
    }
}