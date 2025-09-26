using RenameBooks.Factories;
using RenameBooks.Interfaces;
using RenameBooks.Records;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace RenameBooks.Services
{
    /// <summary>
    /// Предоставляет функциональность для переименования и организации файлов книг
    /// на основе метаданных, извлекаемых с помощью стратегий переименования.
    /// </summary>
    public class FileRenamerService
    {
        private const string UnknownAuthor = "Неизвестный автор";
        private const string UntitledBook = "Без названия";
        private const string NoSeriesFolder = "Без цикла";

        private readonly RenamerFactory _factory;
        private readonly IFileNameSanitizer _sanitizer;

        public FileRenamerService(RenamerFactory factory, IFileNameSanitizer sanitizer)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _sanitizer = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));
        }

        public IRenamerStrategy GetStrategy(string filePath) =>
            _factory.GetStrategy(filePath);

        public string SanitizeFileName(string title) => _sanitizer.Sanitize(title);

        public void OrganizeBooksToFolder(IEnumerable<string> filePaths, string targetRootFolder)
        {
            if (filePaths == null)
                throw new ArgumentNullException(nameof(filePaths));
            if (string.IsNullOrWhiteSpace(targetRootFolder))
                throw new ArgumentException("Целевая папка не указана.", nameof(targetRootFolder));

            Directory.CreateDirectory(targetRootFolder);

            foreach (string filePath in filePaths)
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                    continue;

                try
                {
                    var strategy = _factory.GetStrategy(filePath);
                    var metadata = strategy.ExtractMetadata(filePath);

                    if (metadata == null)
                        continue;

                    var (safeTitle, safeSeries, seriesNumber) = (
                        _sanitizer.Sanitize(metadata.Title ?? UntitledBook),
                        string.IsNullOrEmpty(metadata.SeriesName)
                            ? NoSeriesFolder
                            : _sanitizer.Sanitize(metadata.SeriesName),
                        metadata.SeriesNumber
                    );

                    string extension = Path.GetExtension(filePath);
                    string numberPrefix = seriesNumber.HasValue ? $"{seriesNumber.Value:D2}. " : "";

                    // Если авторов нет — кладём в "Неизвестный автор"
                    var authors = metadata.Authors.Any()
                        ? metadata.Authors.Select(a => _sanitizer.Sanitize(a.ToString())).ToList()
                        : new List<string> { _sanitizer.Sanitize(UnknownAuthor) };

                    foreach (string safeAuthor in authors)
                    {
                        string seriesFolder = Path.Combine(targetRootFolder, safeAuthor, safeSeries);
                        Directory.CreateDirectory(seriesFolder);

                        string fileName = $"{numberPrefix}{safeTitle}{extension}";
                        string destinationPath = GetUniqueFilePath(Path.Combine(seriesFolder, fileName));

                        File.Copy(filePath, destinationPath);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка при организации '{filePath}': {ex}");
                }
            }
        }

        private string GetUniqueFilePath(string fullPath, int maxAttempts = 1000)
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