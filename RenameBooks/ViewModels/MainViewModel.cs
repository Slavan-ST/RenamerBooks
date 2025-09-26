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
        private readonly IDialogService _dialogService;
        private readonly IBookOrganizationOrchestrator _orchestrator;
        private readonly IReadOnlyCollection<string> _allowedExtensions;

        [Reactive] public bool IsRunning { get; set; }
        [Reactive] public bool IsRecursive { get; set; }
        [Reactive] public ObservableCollection<BookCopyViewModel> OrganizedBooks { get; set; } = new();
        public ObservableCollection<string> Log { get; } = new();

        public ReactiveCommand<Unit, Unit> RenameBooksCommand { get; }

        public MainViewModel(
            IDialogService dialogService,
            IBookOrganizationOrchestrator orchestrator,
            RenamerFactory factory)
        {
            _dialogService = dialogService;
            _orchestrator = orchestrator;
            _allowedExtensions = factory.GetSupportedExtensions().ToList().AsReadOnly();
            RenameBooksCommand = ReactiveCommand.CreateFromTask(RenameBooksAsync);
        }

        private async Task RenameBooksAsync()
        {
            if (IsRunning) return;

            var folder = await _dialogService.ShowFolderPickerAsync();
            if (string.IsNullOrEmpty(folder)) return;

            var message = await BuildConfirmationMessage(folder);
            if (!await _dialogService.ShowConfirmationDialogAsync(message)) return;

            IsRunning = true;
            Log.Clear();
            AppendLog($"Начинаю обработку: {folder}");
            AppendLog($"Режим: {(IsRecursive ? "рекурсивный" : "только текущая папка")}");

            try
            {
                var result = await _orchestrator.OrganizeAsync(folder, IsRecursive);

                OrganizedBooks.Clear();
                foreach (var r in result.OrganizedBooks)
                {
                    var vm = new BookCopyViewModel(r, result.OrganizedRoot, book =>
                        Dispatcher.UIThread.InvokeAsync(() => OrganizedBooks.Remove(book)));
                    OrganizedBooks.Add(vm);
                }

                AppendLog($"✅ {result.LogSummary}");
            }
            catch (Exception ex)
            {
                AppendLog($"❌ Ошибка: {ex.Message}");
                Debug.WriteLine(ex);
            }
            finally
            {
                IsRunning = false;
            }
        }

        private async Task<string> BuildConfirmationMessage(string folder)
        {
            var searchOption = IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var allFiles = Directory.GetFiles(folder, "*.*", searchOption);
            var supported = allFiles.Where(f => _allowedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant())).ToArray();
            var unsupported = allFiles.Except(supported).ToArray();

            if (supported.Length == 0 && unsupported.Length == 0)
            {
                await _dialogService.ShowMessageAsync("Информация", "В папке не найдено файлов.");
                return "Отмена";
            }

            return $"Найдено файлов:\n" +
                   $"— Поддерживаемых: {supported.Length}\n" +
                   $"— Неподдерживаемых: {unsupported.Length}\n\n" +
                   (supported.Length > 0
                       ? $"Форматы: {string.Join(", ", supported.Select(f => Path.GetExtension(f).TrimStart('.')).Distinct())}\n\n"
                       : "") +
                   "Продолжить?";
        }

        private void AppendLog(string message)
        {
            var logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            Dispatcher.UIThread.InvokeAsync(() => Log.Add(logEntry));
        }
    }
}