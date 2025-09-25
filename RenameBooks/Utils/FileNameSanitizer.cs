using RenameBooks.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenameBooks.Utils
{
    public class FileNameSanitizer : IFileNameSanitizer
    {
        private static readonly char[] InvalidChars = Path.GetInvalidFileNameChars()
            .Concat(Path.GetInvalidPathChars())
            .Distinct()
            .ToArray();

        public string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input)) return "без_названия";

            var sanitized = InvalidChars.Aggregate(input, (current, c) => current.Replace(c, '_'));
            // Убираем точки и пробелы в конце
            sanitized = sanitized.Trim('.', ' ', '_');
            return string.IsNullOrEmpty(sanitized) ? "без_названия" : sanitized;
        }
    }
}
