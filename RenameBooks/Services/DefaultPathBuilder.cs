using RenameBooks.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenameBooks.Services
{
    public class DefaultPathBuilder : IPathBuilder
    {
        private const int MaxUniqueNameAttempts = 1000;

        private readonly IFileNameSanitizer _sanitizer;

        public DefaultPathBuilder(IFileNameSanitizer sanitizer)
        {
            _sanitizer = sanitizer;
        }

        public string BuildBookPath(string targetRoot, string author, string series, int? seriesNumber, string title, string extension)
        {
            var safeAuthor = _sanitizer.Sanitize(author);
            var safeSeries = string.IsNullOrEmpty(series) ? "Без цикла" : _sanitizer.Sanitize(series);
            var safeTitle = _sanitizer.Sanitize(title);

            string seriesFolder = Path.Combine(targetRoot, safeAuthor, safeSeries);
            Directory.CreateDirectory(seriesFolder);

            string numberPrefix = seriesNumber.HasValue ? $"{seriesNumber.Value:D2}. " : "";
            string fileName = $"{numberPrefix}{safeTitle}{extension}";
            return GetUniqueFilePath(Path.Combine(seriesFolder, fileName));
        }


        private string GetUniqueFilePath(string fullPath, int maxAttempts = MaxUniqueNameAttempts)
        {
            if (!File.Exists(fullPath))
                return fullPath;

            string directory = Path.GetDirectoryName(fullPath)!;
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fullPath);
            string extension = Path.GetExtension(fullPath);

            for (int counter = 1; counter <= maxAttempts; counter++)
            {
                string candidate = Path.Combine(directory, $"{fileNameWithoutExt} ({counter}){extension}");
                if (!File.Exists(candidate))
                    return candidate;
            }

            throw new InvalidOperationException($"Не удалось создать уникальное имя для: {fullPath}");
        }
    }
}
