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

        private const int MaxUniqueNameAttempts = 1000;

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

        public IReadOnlyList<OrganizationResult> OrganizeBooksToFolder(IEnumerable<string> filePaths, string targetRootFolder)
        {
            if (filePaths == null)
                throw new ArgumentNullException(nameof(filePaths));
            if (string.IsNullOrWhiteSpace(targetRootFolder))
                throw new ArgumentException("Целевая папка не указана.", nameof(targetRootFolder));

            Directory.CreateDirectory(targetRootFolder);
            var results = new List<OrganizationResult>();

            foreach (string filePath in filePaths)
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                    continue;

                try
                {
                    var result = ProcessSingleFile(filePath, targetRootFolder);
                    if (result != null)
                        results.Add(result);
                }
                catch (Exception ex)
                {

                }
            }

            return results;
        }

        private OrganizationResult? ProcessSingleFile(string sourceFilePath, string targetRootFolder)
        {
            var strategy = _factory.GetStrategy(sourceFilePath);
            var metadata = strategy.ExtractMetadata(sourceFilePath);

            if (metadata == null)
                return null;

            var safeTitle = _sanitizer.Sanitize(metadata.Title ?? UntitledBook);
            var safeSeries = string.IsNullOrEmpty(metadata.SeriesName)
                ? NoSeriesFolder
                : _sanitizer.Sanitize(metadata.SeriesName);

            var authorNames = metadata.Authors.Any()
                ? metadata.Authors.Select(a => _sanitizer.Sanitize(a.ToString())).ToList()
                : new List<string> { _sanitizer.Sanitize(UnknownAuthor) };

            string extension = Path.GetExtension(sourceFilePath);
            var createdPaths = new List<string>();

            foreach (string safeAuthor in authorNames)
            {
                string destinationPath = BuildDestinationPath(
                    targetRootFolder,
                    safeAuthor,
                    safeSeries,
                    metadata.SeriesNumber,
                    safeTitle,
                    extension
                );

                SafeCopyFile(sourceFilePath, destinationPath);
                createdPaths.Add(destinationPath);
            }

            return createdPaths.Count > 0
                ? new OrganizationResult(sourceFilePath, safeTitle, createdPaths)
                : null;
        }

        private string BuildDestinationPath(
            string targetRootFolder,
            string author,
            string series,
            int? seriesNumber,
            string title,
            string extension)
        {
            string seriesFolder = Path.Combine(targetRootFolder, author, series);
            Directory.CreateDirectory(seriesFolder);

            string numberPrefix = seriesNumber.HasValue ? $"{seriesNumber.Value:D2}. " : "";
            string fileName = $"{numberPrefix}{title}{extension}";
            return GetUniqueFilePath(Path.Combine(seriesFolder, fileName));
        }

        private void SafeCopyFile(string sourcePath, string destinationPath)
        {
            File.Copy(sourcePath, destinationPath);
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