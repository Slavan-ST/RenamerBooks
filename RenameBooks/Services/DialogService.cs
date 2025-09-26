using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using RenameBooks.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace RenameBooks.Services
{
    public class DialogService : IDialogService
    {
        public async Task<string?> ShowFolderPickerAsync(string title = "Выберите папку")
        {
            var topLevel = GetTopLevel();
            if (topLevel?.StorageProvider is null)
                return null;

            var result = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                AllowMultiple = false,
                Title = title
            });

            return result.FirstOrDefault()?.Path.LocalPath;
        }

        public async Task<bool> ShowConfirmationDialogAsync(string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Подтверждение", message, ButtonEnum.YesNo);
            var result = await box.ShowAsync();
            return result == ButtonResult.Yes;
        }

        public async Task ShowMessageAsync(string title, string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(title, message);
            await box.ShowAsync();
        }

        private TopLevel? GetTopLevel()
        {
            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                return TopLevel.GetTopLevel(desktop.MainWindow);
            }
            return null;
        }
    }
}