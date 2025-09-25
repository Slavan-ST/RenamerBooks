using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using RenameBooks.Interfaces;
using RenameBooks.Services;
using RenameBooks.ViewModels;
using System;
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
        private readonly IDialogService _dialogService;
        private readonly FileRenamerService _renamerService;

        [Reactive]
        public bool IsRunning { get; set; }

        [Reactive]
        public string Status { get; set; } = "Готово";
        public ReactiveCommand<Unit, Unit> RenameBooksCommand { get; set; }

        public ObservableCollection<string> Log { get; } = new();

        public MainViewModel(IDialogService dialogService, FileRenamerService renamerService)
        {
            _dialogService = dialogService;
            _renamerService = renamerService;

            RenameBooksCommand = ReactiveCommand.CreateFromTask(RenameBooksAsync);
        }

        private async Task RenameBooksAsync()
        {
            if (IsRunning) return;

            var folder = await _dialogService.ShowFolderPickerAsync();
            if (string.IsNullOrEmpty(folder)) return;

            // Анализ: сколько файлов и каких типов
            var files = Directory.GetFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f => f.EndsWith(".fb2", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".fb2.zip", StringComparison.OrdinalIgnoreCase)
                            )
                .ToArray();

            if (files.Length == 0)
            {
                await _dialogService.ShowMessageAsync("Информация", "В папке не найдено поддерживаемых файлов.");
                return;
            }

            var message = $"Найдено {files.Length} файлов для переименования.\n\n" +
                          $"Форматы: {string.Join(", ", files.Select(f => Path.GetExtension(f).TrimStart('.')).Distinct())}\n\n" +
                          "Продолжить?";

            var confirmed = await _dialogService.ShowConfirmationDialogAsync(message);
            if (!confirmed) return;

            // Запуск переименования
            await Task.Run(() => RenameBooksInFolder(folder));
        }

        private void RenameBooksInFolder(string folderPath)
        {
            IsRunning = true;
            Log.Clear();
            AppendLog($"Начинаю обработку папки: {folderPath}");

            try
            {
                var originalFiles = Directory.GetFiles(folderPath, "*.*")
                    .Where(f => f.EndsWith(".fb2", StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".fb2.zip", StringComparison.OrdinalIgnoreCase)
                                )
                    .ToArray();

                int success = 0, failed = 0;

                foreach (string filePath in originalFiles)
                {
                    if (IsRunning == false) break; 

                    try
                    {
                        var strategy = _renamerService.GetStrategy(filePath); 
                        string? title = strategy.ExtractTitle(filePath);

                        if (string.IsNullOrWhiteSpace(title))
                        {
                            AppendLog($"⚠ Пропущен (нет заголовка): {Path.GetFileName(filePath)}");
                            continue;
                        }

                        string safeName = _renamerService.SanitizeFileName(title); 
                        string extension = Path.GetExtension(filePath);
                        string newFilePath = GetUniqueFilePath(folderPath, safeName, extension);

                        File.Move(filePath, newFilePath);
                        AppendLog($"✅ {Path.GetFileName(filePath)} → {Path.GetFileName(newFilePath)}");
                        success++;
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"❌ Ошибка: {Path.GetFileName(filePath)} — {ex.Message}");
                        failed++;
                    }
                }

                AppendLog($"Готово! Успешно: {success}, Ошибок: {failed}");
            }
            catch (Exception ex)
            {
                AppendLog($"КРИТИЧЕСКАЯ ОШИБКА: {ex.Message}");
            }
            finally
            {
                IsRunning = false;
            }
        }

        private void AppendLog(string message)
        {
            // Обновление UI из фонового потока
            Dispatcher.UIThread.Post(() => Log.Add($"[{DateTime.Now:HH:mm:ss}] {message}"));
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