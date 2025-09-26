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
    public class FileRenamerService
    {
        private readonly RenamerFactory _factory;
        private readonly IPathBuilder _pathBuilder;

        public FileRenamerService(RenamerFactory factory, IPathBuilder pathBuilder)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _pathBuilder = pathBuilder ?? throw new ArgumentNullException(nameof(pathBuilder));
        }

        public IReadOnlyList<OrganizationResult> OrganizeBooksToFolder(IEnumerable<string> filePaths, string targetRootFolder)
        {
            if (filePaths == null) throw new ArgumentNullException(nameof(filePaths));
            if (string.IsNullOrWhiteSpace(targetRootFolder)) throw new ArgumentException("...", nameof(targetRootFolder));

            Directory.CreateDirectory(targetRootFolder);
            var results = new List<OrganizationResult>();

            foreach (string filePath in filePaths)
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath)) continue;

                try
                {
                    var result = ProcessSingleFile(filePath, targetRootFolder);
                    if (result != null) results.Add(result);
                }
                catch (Exception ex)
                {
                    // Лучше логировать через ILogger, но пока просто пропускаем
                }
            }

            return results;
        }

        private OrganizationResult? ProcessSingleFile(string sourceFilePath, string targetRootFolder)
        {
            var strategy = _factory.GetStrategy(sourceFilePath);
            var metadata = strategy.ExtractMetadata(sourceFilePath);
            if (metadata == null) return null;

            var authors = metadata.Authors.Any()
                ? metadata.Authors.Select(a => a.ToString()).ToList()
                : new List<string> { "Неизвестный автор" };

            string extension = Path.GetExtension(sourceFilePath);
            var createdPaths = new List<string>();

            foreach (string author in authors)
            {
                string destPath = _pathBuilder.BuildBookPath(
                    targetRootFolder,
                    author,
                    metadata.SeriesName,
                    metadata.SeriesNumber,
                    metadata.Title ?? "Без названия",
                    extension
                );

                File.Copy(sourceFilePath, destPath);
                createdPaths.Add(destPath);
            }

            return new OrganizationResult(sourceFilePath, metadata.Title ?? "Без названия", createdPaths);
        }
    }
}