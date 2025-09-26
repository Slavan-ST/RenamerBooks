using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using RenameBooks.Factories;
using RenameBooks.Interfaces;
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
        public string Status { get; set; } = "Готово";

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


            var files = Directory.GetFiles(folder, "*.*", searchOption)
                .Where(f => _allowedExtensions.Contains(Path.GetExtension(f)))
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

            IsRunning = true;
            Log.Clear();
            AppendLog($"Начинаю обработку папки: {folder}");
            AppendLog($"Режим: {(IsRecursive ? "рекурсивный" : "только текущая папка")}");

            try
            {
                var targetFolder = Path.Combine(folder, "organized_books");
                await Task.Run(() => _renamerService.OrganizeBooksToFolder(files, targetFolder));
                AppendLog("Готово!");
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

        #endregion


        #region Вспомогательные методы

        private void AppendLog(string message)
        {
            var logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            Dispatcher.UIThread.InvokeAsync(() => Log.Add(logEntry));
        }

        #endregion
    }
}