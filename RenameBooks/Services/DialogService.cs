using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using RenameBooks.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenameBooks.Services
{
    public class DialogService : IDialogService
    {

        public DialogService()
        {

        }

        public async Task<string?> ShowFolderPickerAsync()
        {

            var _topLevel = TopLevel.GetTopLevel(Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null);

            if (_topLevel?.StorageProvider is null)
                return null;

            var result = await _topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                AllowMultiple = false,
                Title = "Выберите папку с книгами"
            });

            return result.FirstOrDefault()?.Path.LocalPath;
        }

        public async Task<bool> ShowConfirmationDialogAsync(string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Подтверждение", message, ButtonEnum.YesNo);

            var result = await box.ShowAsync();

            if (result == ButtonResult.Yes)
            {
                return true;
            }

            if (result == ButtonResult.No)
            {
                return false;
            }
            else
            {
                return false;
            }
        }

        public async Task ShowMessageAsync(string title, string message)
        {
            var box = MessageBoxManager.GetMessageBoxStandard(title, message);
            await box.ShowAsync();
        }
    }
}
