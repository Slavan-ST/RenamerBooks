using RenameBooks.Factories;
using RenameBooks.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenameBooks.Services
{
    public class FileRenamerService
    {
        private readonly RenamerStrategyFactory _factory;
        private readonly IFileNameSanitizer _sanitizer;


        public FileRenamerService(RenamerStrategyFactory factory, IFileNameSanitizer sanitizer)
        {
            _factory = factory;
            _sanitizer = sanitizer;
        }


        public IRenamerStrategy GetStrategy(string filePath) => _factory.GetStrategy(filePath);
        public string SanitizeFileName(string title) => _sanitizer.Sanitize(title);

        public void RenameFilesInDirectory(string directoryPath, string searchPattern = "*.*")
        {
            foreach (string filePath in Directory.GetFiles(directoryPath, searchPattern))
            {
                try
                {
                    var strategy = _factory.GetStrategy(filePath);
                    string title = strategy.ExtractTitle(filePath);

                    if (string.IsNullOrWhiteSpace(title))
                    {
                        Console.WriteLine($"Пропущен (нет заголовка): {filePath}");
                        continue;
                    }

                    string safeName = _sanitizer.Sanitize(title);
                    string extension = Path.GetExtension(filePath);
                    string newFilePath = GetUniqueFilePath(directoryPath, safeName, extension);

                    File.Move(filePath, newFilePath);
                    Console.WriteLine($"✅ {Path.GetFileName(filePath)} → {Path.GetFileName(newFilePath)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка: {filePath} — {ex.Message}");
                }
            }
        }

        private string GetUniqueFilePath(string directory, string baseName, string extension)
        {
            string candidate = Path.Combine(directory, baseName + extension);
            int counter = 1;
            while (File.Exists(candidate))
            {
                candidate = Path.Combine(directory, $"{baseName} ({counter}){extension}");
                counter++;
            }
            return candidate;
        }
    }
}
