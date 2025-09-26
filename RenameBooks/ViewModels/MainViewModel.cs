using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using RenameBooks.Factories;
using RenameBooks.Interfaces;
using RenameBooks.Records;
using RenameBooks.Services;
using RenameBooks.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace RenameBooks.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        #region Поля и зависимости

        private readonly IDialogService _dialogService;
        private readonly FileRenamerService _renamerService;


        private readonly IReadOnlyCollection<string> _allowedExtensions;

        #endregion

        #region Свойства
        [Reactive]
        public bool IsRunning { get; set; } = false;
        [Reactive]
        public bool IsRecursive { get; set; } = false;

        [Reactive]
        public ObservableCollection<BookCopyViewModel> OrganizedBooks { get; set; } = new();

        public ReactiveCommand<Unit, Unit> RenameBooksCommand { get; set; }

        public ObservableCollection<string> Log { get; } = new();

        #endregion

        #region Конструктор


        public MainViewModel(IDialogService dialogService, FileRenamerService renamerService, RenamerFactory factory)
        {
            _allowedExtensions = factory.GetSupportedExtensions().ToList().AsReadOnly();

            _dialogService = dialogService;
            _renamerService = renamerService;

            RenameBooksCommand = ReactiveCommand.CreateFromTask(RenameBooksAsync);
        }

        #endregion

        #region Команды
        private async Task RenameBooksAsync()
        {
            if (IsRunning) return;

            var folder = await _dialogService.ShowFolderPickerAsync();
            if (string.IsNullOrEmpty(folder)) return;

            var searchOption = IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            // Получаем ВСЕ файлы
            var allFiles = Directory.GetFiles(folder, "*.*", searchOption);

            // Фильтруем поддерживаемые
            var supportedFiles = allFiles
                .Where(f => _allowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToArray();

            // Определяем неподдерживаемые
            var unsupportedFiles = allFiles.Except(supportedFiles).ToArray();

            if (supportedFiles.Length == 0 && unsupportedFiles.Length == 0)
            {
                await _dialogService.ShowMessageAsync("Информация", "В папке не найдено файлов.");
                return;
            }

            var message = $"Найдено файлов:\n" +
                          $"— Поддерживаемых: {supportedFiles.Length}\n" +
                          $"— Неподдерживаемых: {unsupportedFiles.Length}\n\n" +
                          (supportedFiles.Length > 0
                              ? $"Форматы: {string.Join(", ", supportedFiles.Select(f => Path.GetExtension(f).TrimStart('.')).Distinct())}\n\n"
                              : "") +
                          "Продолжить?";

            var confirmed = await _dialogService.ShowConfirmationDialogAsync(message);
            if (!confirmed) return;

            IsRunning = true;
            Log.Clear();
            AppendLog($"Начинаю обработку папки: {folder}");
            AppendLog($"Режим: {(IsRecursive ? "рекурсивный" : "только текущая папка")}");

            try
            {
                string organizedRoot = Path.Combine(folder, "organized_books");
                string notOrganizedRoot = Path.Combine(folder, "not_organized");

                // 1. Обрабатываем поддерживаемые файлы
                List<OrganizationResult> results = new();
                if (supportedFiles.Length > 0)
                {
                    results = (List<OrganizationResult>)await Task.Run(() => _renamerService.OrganizeBooksToFolder(supportedFiles, organizedRoot));
                    AppendLog($"Организовано {results.Count} книг.");
                }

                // 2. Перемещаем неподдерживаемые файлы
                if (unsupportedFiles.Length > 0)
                {
                    await Task.Run(() => MoveUnsupportedFiles(unsupportedFiles, notOrganizedRoot, folder));
                    AppendLog($"Перемещено {unsupportedFiles.Length} неподдерживаемых файлов в 'not_organized'.");
                }

                // Обновляем UI
                OrganizedBooks.Clear();
                foreach (var result in results)
                {
                    var bookVm = new BookCopyViewModel(
                        result,
                        organizedRoot,
                        book => Dispatcher.UIThread.InvokeAsync(() => OrganizedBooks.Remove(book))
                    );
                    OrganizedBooks.Add(bookVm);
                }
            }
            catch (Exception ex)
            {
                AppendLog($"КРИТИЧЕСКАЯ ОШИБКА: {ex.Message}");
                Debug.WriteLine(ex);
            }
            finally
            {
                IsRunning = false;
            }
        }

        #endregion


        #region Вспомогательные методы
        private void MoveUnsupportedFiles(string[] unsupportedFiles, string notOrganizedRoot, string originalRoot)
{
    Directory.CreateDirectory(notOrganizedRoot);

    foreach (var filePath in unsupportedFiles)
    {
        try
        {
            // Сохраняем относительную структуру папок (если рекурсивный режим)
            string relativePath = Path.GetRelativePath(originalRoot, filePath);
            string destinationDir = Path.Combine(notOrganizedRoot, Path.GetDirectoryName(relativePath)!);
            Directory.CreateDirectory(destinationDir);

            string fileName = Path.GetFileName(filePath);
            string destinationPath = GetUniqueFilePath(Path.Combine(destinationDir, fileName));

            File.Move(filePath, destinationPath);
        }
        catch (Exception ex)
        {
            // Лучше логировать, но не прерывать всю операцию
            Debug.WriteLine($"Не удалось переместить '{filePath}': {ex.Message}");
        }
    }
}
        private string GetUniqueFilePath(string fullPath, int maxAttempts = 1000)
        {
            if (!File.Exists(fullPath))
                return fullPath;

            string dir = Path.GetDirectoryName(fullPath)!;
            string name = Path.GetFileNameWithoutExtension(fullPath);
            string ext = Path.GetExtension(fullPath);

            for (int i = 1; i <= maxAttempts; i++)
            {
                string candidate = Path.Combine(dir, $"{name} ({i}){ext}");
                if (!File.Exists(candidate))
                    return candidate;
            }

            // Если не получилось — возвращаем оригинальное имя (File.Move сам выбросит исключение)
            return fullPath;
        }
        private void AppendLog(string message)
        {
            var logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            Dispatcher.UIThread.InvokeAsync(() => Log.Add(logEntry));
        }

        #endregion
    }
}