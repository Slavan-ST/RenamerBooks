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
    }
}
