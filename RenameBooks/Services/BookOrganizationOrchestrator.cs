using RenameBooks.Factories;
using RenameBooks.Interfaces;
using RenameBooks.Records;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenameBooks.Services
{
    public class BookOrganizationOrchestrator : IBookOrganizationOrchestrator
    {
        private readonly FileRenamerService _renamerService;
        private readonly RenamerFactory _renamerFactory;

        public BookOrganizationOrchestrator(
            FileRenamerService renamerService,
            RenamerFactory renamerFactory)
        {
            _renamerService = renamerService;
            _renamerFactory = renamerFactory;
        }

        public async Task<OrganizationSessionResult> OrganizeAsync(string rootFolder, bool isRecursive)
        {
            var allowedExtensions = _renamerFactory.GetSupportedExtensions();
            var searchOption = isRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var allFiles = Directory.GetFiles(rootFolder, "*.*", searchOption);

            var supportedFiles = allFiles
                .Where(f => allowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToArray();

            var unsupportedFiles = allFiles.Except(supportedFiles).ToArray();

            var organizedRoot = Path.Combine(rootFolder, "organized_books");
            var notOrganizedRoot = Path.Combine(rootFolder, "not_organized");

            var organizedResults = new List<OrganizationResult>();
            if (supportedFiles.Length > 0)
            {
                organizedResults = (List<OrganizationResult>)await Task.Run(() =>
                    _renamerService.OrganizeBooksToFolder(supportedFiles, organizedRoot));
            }

            if (unsupportedFiles.Length > 0)
            {
                await Task.Run(() => MoveUnsupportedFiles(unsupportedFiles, notOrganizedRoot, rootFolder));
            }

            var logSummary = $"Организовано: {organizedResults.Count}, неподдерживаемых: {unsupportedFiles.Length}";

            return new OrganizationSessionResult(
                organizedResults,
                unsupportedFiles.Length,
                organizedRoot,
                notOrganizedRoot,
                logSummary
            );
        }

        private void MoveUnsupportedFiles(string[] unsupportedFiles, string notOrganizedRoot, string originalRoot)
        {
            Directory.CreateDirectory(notOrganizedRoot);
            foreach (var filePath in unsupportedFiles)
            {
                try
                {
                    string relativePath = Path.GetRelativePath(originalRoot, filePath);
                    string destinationDir = Path.Combine(notOrganizedRoot, Path.GetDirectoryName(relativePath)!);
                    Directory.CreateDirectory(destinationDir);

                    string fileName = Path.GetFileName(filePath);
                    string destinationPath = GetUniqueFilePath(Path.Combine(destinationDir, fileName));
                    File.Move(filePath, destinationPath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Не удалось переместить '{filePath}': {ex.Message}");
                }
            }
        }

        private string GetUniqueFilePath(string fullPath, int maxAttempts = 1000)
        {
            if (!File.Exists(fullPath)) return fullPath;
            string dir = Path.GetDirectoryName(fullPath)!;
            string name = Path.GetFileNameWithoutExtension(fullPath);
            string ext = Path.GetExtension(fullPath);
            for (int i = 1; i <= maxAttempts; i++)
            {
                string candidate = Path.Combine(dir, $"{name} ({i}){ext}");
                if (!File.Exists(candidate)) return candidate;
            }
            return fullPath;
        }
    }
}
