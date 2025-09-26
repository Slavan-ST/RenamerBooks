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
        private readonly RenamerFactory _factory;
        private readonly IFileNameSanitizer _sanitizer;

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="FileRenamerService"/>.
        /// </summary>
        /// <param name="factory">Фабрика для получения стратегий переименования.</param>
        /// <param name="sanitizer">Сервис для очистки недопустимых символов из имен файлов.</param>
        /// <exception cref="ArgumentNullException">Если любой из параметров равен <c>null</c>.</exception>
        public FileRenamerService(RenamerFactory factory, IFileNameSanitizer sanitizer)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _sanitizer = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));
        }

        public IRenamerStrategy GetStrategy(string filePath) =>
            _factory.GetStrategy(filePath);

        public string SanitizeFileName(string title) => _sanitizer.Sanitize(title);

        /// <summary>
        /// Переименовывает файлы на месте, используя извлечённые метаданные.
        /// Формат имени: "[Автор] - [Название].расширение"
        /// </summary>
        public void RenameFiles(IEnumerable<string> filePaths)
        {
            if (filePaths == null)
                throw new ArgumentNullException(nameof(filePaths));

            foreach (string filePath in filePaths)
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                    continue;

                try
                {
                    var strategy = _factory.GetStrategy(filePath);
                    var metadata = strategy.ExtractMetadata(filePath);
                    if (metadata?.Title is null)
                        continue;

                    string author = metadata.Author ?? "Неизвестный автор";
                    string safeAuthor = _sanitizer.Sanitize(author);
                    string safeTitle = _sanitizer.Sanitize(metadata.Title);

                    string extension = Path.GetExtension(filePath);
                    string newFileName = $"{safeAuthor} - {safeTitle}{extension}";
                    string directory = Path.GetDirectoryName(filePath)!;
                    string newFilePath = Path.Combine(directory, newFileName);
                    newFilePath = GetUniqueFilePath(newFilePath);

                    File.Move(filePath, newFilePath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка при переименовании '{filePath}': {ex}");
                }
            }
        }

        /// <summary>
        /// Организует файлы в структуру: [Корень]/[Автор]/[Серия или "Без цикла"]/[№. Название.расш]
        /// </summary>
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

                    string title = metadata?.Title ?? "Без названия";
                    string author = metadata?.Author ?? "Неизвестный автор";
                    string seriesName = metadata?.SeriesName;
                    int? seriesNumber = metadata?.SeriesNumber;

                    string safeTitle = _sanitizer.Sanitize(title);
                    string safeAuthor = _sanitizer.Sanitize(author);
                    string safeSeries = string.IsNullOrEmpty(seriesName)
                        ? "Без цикла"
                        : _sanitizer.Sanitize(seriesName);

                    string extension = Path.GetExtension(filePath);
                    string authorFolder = Path.Combine(targetRootFolder, safeAuthor);
                    string seriesFolder = Path.Combine(authorFolder, safeSeries);
                    Directory.CreateDirectory(seriesFolder);

                    string numberPrefix = seriesNumber.HasValue ? $"{seriesNumber.Value:D2}. " : "";
                    string fileName = $"{numberPrefix}{safeTitle}{extension}";
                    string destinationPath = Path.Combine(seriesFolder, fileName);
                    destinationPath = GetUniqueFilePath(destinationPath);

                    File.Move(filePath, destinationPath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка при организации '{filePath}': {ex}");
                }
            }
        }

        private string GetUniqueFilePath(string fullPath)
        {
            if (!File.Exists(fullPath))
                return fullPath;

            string directory = Path.GetDirectoryName(fullPath)!;
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fullPath);
            string extension = Path.GetExtension(fullPath);

            for (int counter = 1; ; counter++)
            {
                string candidate = Path.Combine(directory, $"{fileNameWithoutExt} ({counter}){extension}");
                if (!File.Exists(candidate))
                    return candidate;
            }
        }
    }
}