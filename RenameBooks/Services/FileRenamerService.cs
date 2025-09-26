using RenameBooks.Factories;
using RenameBooks.Interfaces;
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
        private readonly RenamerStrategyFactory _factory;
        private readonly IFileNameSanitizer _sanitizer;
        private readonly INameNormalizer _normalizer; // ← новое поле

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="FileRenamerService"/>.
        /// </summary>
        /// <param name="factory">Фабрика для получения стратегий переименования.</param>
        /// <param name="sanitizer">Сервис для очистки недопустимых символов из имен файлов.</param>
        /// <exception cref="ArgumentNullException">Если любой из параметров равен <c>null</c>.</exception>
        public FileRenamerService(RenamerStrategyFactory factory, IFileNameSanitizer sanitizer, INameNormalizer normalizer)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _sanitizer = sanitizer ?? throw new ArgumentNullException(nameof(sanitizer));
            _normalizer = normalizer ?? throw new ArgumentNullException(nameof(normalizer));
        }

        /// <summary>
        /// Получает стратегию переименования, подходящую для указанного файла.
        /// </summary>
        /// <param name="filePath">Путь к файлу.</param>
        /// <returns>Экземпляр стратегии переименования.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="filePath"/> равен <c>null</c> или пуст.</exception>
        public IRenamerStrategy GetStrategy(string filePath) => _factory.GetStrategy(filePath);

        /// <summary>
        /// Очищает переданное название от недопустимых символов для использования в имени файла.
        /// </summary>
        /// <param name="title">Исходное название.</param>
        /// <returns>Очищенное и безопасное для файловой системы название.</returns>
        public string SanitizeFileName(string title) => _sanitizer.Sanitize(title);

        /// <summary>
        /// Переименовывает указанные файлы, извлекая заголовки с помощью соответствующих стратегий.
        /// Файлы, для которых не удалось извлечь заголовок, пропускаются.
        /// </summary>
        /// <param name="filePaths">Коллекция путей к файлам для переименования.</param>
        /// <exception cref="ArgumentNullException"><paramref name="filePaths"/> равен <c>null</c>.</exception>
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
                    string? title = strategy.ExtractTitle(filePath);

                    if (string.IsNullOrWhiteSpace(title))
                        continue;

                    string newFilePath = GetUniqueFilePath(filePath);
                    File.Move(filePath, newFilePath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка при обработке файла '{filePath}': {ex}");
                }
            }
        }

        /// <summary>
        /// Организует указанные файлы книг в иерархическую структуру папок:
        /// [Целевая папка] / [Автор] / [Цикл или "Без цикла"].
        /// Имена файлов формируются как "[Номер]. Название.расширение".
        /// </summary>
        /// <param name="filePaths">Коллекция путей к файлам для организации.</param>
        /// <param name="targetRootFolder">Корневая папка, в которую будут перемещены файлы.</param>
        /// <exception cref="ArgumentNullException"><paramref name="filePaths"/> равен <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="targetRootFolder"/> не указан или пуст.</exception>
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
                    string title = strategy.ExtractTitle(filePath) ?? "Без названия";
                    var (seriesName, seriesNumber) = strategy.ExtractSeriesInfo(filePath);
                    string author = strategy.ExtractAuthor(filePath) ?? "Неизвестный автор";

                    string safeTitle = _sanitizer.Sanitize(title);
                    string safeAuthor = _sanitizer.Sanitize(author);
                    string extension = Path.GetExtension(filePath);

                    string authorFolder = Path.Combine(targetRootFolder, safeAuthor);
                    string seriesFolder = string.IsNullOrEmpty(seriesName)
                        ? Path.Combine(authorFolder, "Без цикла")
                        : Path.Combine(authorFolder, _sanitizer.Sanitize(seriesName));

                    Directory.CreateDirectory(seriesFolder);

                    string numberPrefix = seriesNumber.HasValue ? $"{seriesNumber.Value:D2}. " : "";
                    string fileName = $"{numberPrefix}{safeTitle}{extension}";
                    string destinationPath = Path.Combine(seriesFolder, fileName);

                    destinationPath = GetUniqueFilePath(destinationPath);
                    File.Copy(filePath, destinationPath, overwrite: false);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Ошибка при обработке '{filePath}': {ex}");
                }
            }
        }

        /// <summary>
        /// Генерирует уникальный путь к файлу, добавляя числовой суффикс при необходимости,
        /// чтобы избежать конфликта имён.
        /// </summary>
        /// <param name="fullPath">Исходный путь к файлу.</param>
        /// <returns>Уникальный путь к файлу, не конфликтующий с существующими файлами.</returns>
        private string GetUniqueFilePath(string fullPath)
        {
            if (!File.Exists(fullPath))
                return fullPath;

            string directory = Path.GetDirectoryName(fullPath)!;
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(fullPath);
            string extension = Path.GetExtension(fullPath);

            int counter = 1;
            while (true)
            {
                string candidate = Path.Combine(directory, $"{fileNameWithoutExt} ({counter}){extension}");
                if (!File.Exists(candidate))
                    return candidate;
                counter++;
            }
        }
    }
}