using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenameBooks.Interfaces
{
    public interface IDialogService
    {
        Task<string?> ShowFolderPickerAsync();
        Task<bool> ShowConfirmationDialogAsync(string message);
        Task ShowMessageAsync(string title, string message);
    }
}
