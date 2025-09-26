using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;

namespace RenameBooks.ViewModels
{

    public class CopyLocationViewModel : ReactiveObject
    {
        private readonly string _filePath;
        private readonly string _rootFolder;
        private readonly Action<CopyLocationViewModel> _onDeleted;

        public string FilePath => _filePath;
        public string DisplayPath => Path.GetFileName(_filePath);
        public string AuthorAndSeries => GetAuthorAndSeriesFromPath(_filePath);

        public ReactiveCommand<Unit, Unit> DeleteCommand { get; }

        public CopyLocationViewModel(string filePath, string rootFolder, Action<CopyLocationViewModel> onDeleted)
        {
            _filePath = filePath;
            _rootFolder = rootFolder;
            _onDeleted = onDeleted;
            DeleteCommand = ReactiveCommand.Create(DeleteAsync);
        }

        private void DeleteAsync()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    File.Delete(_filePath);
                    Debug.WriteLine($"{_filePath} был удалён");
                }

                TryDeleteEmptyParentFolders(_filePath, _rootFolder);

                // Сообщаем владельцу, что этот элемент нужно удалить из коллекции
                _onDeleted(this);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка удаления: {ex}");
            }
        }

        private static string GetAuthorAndSeriesFromPath(string path)
        {
            var parts = path.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            // Путь: .../organized_books/Автор/Серия/01. Книга.fb2
            // Нам нужны последние 2 папки перед файлом
            if (parts.Length >= 3)
            {
                var series = parts[^2];
                var author = parts[^3];
                return $"{author} → {series}";
            }
            return "Неизвестно";
        }


        private static void TryDeleteEmptyParentFolders(string filePath, string rootFolder)
        {
            // Убедимся, что filePath находится внутри rootFolder
            if (!IsSubPath(filePath, rootFolder))
                return;

            var current = Path.GetDirectoryName(filePath)!;

            // Поднимаемся вверх, пока не дойдём до rootFolder
            while (!string.Equals(current, rootFolder, StringComparison.OrdinalIgnoreCase) &&
                   Directory.Exists(current))
            {
                if (Directory.GetFileSystemEntries(current).Length == 0)
                {
                    try
                    {
                        Directory.Delete(current);
                        current = Path.GetDirectoryName(current)!;
                    }
                    catch (Exception ex)
                    {
                        // Не удалось удалить — выходим, чтобы не зациклиться
                        Debug.WriteLine($"Не удалось удалить папку {current}: {ex}");
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        // Проверка, что path находится внутри root (защита от выхода за пределы)
        private static bool IsSubPath(string path, string root)
        {
            var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
        }
    }
}
