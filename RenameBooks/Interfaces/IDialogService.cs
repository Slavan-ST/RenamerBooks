using System.Threading.Tasks;

namespace RenameBooks.Interfaces
{
    public interface IDialogService
    {
        Task<string?> ShowFolderPickerAsync(string title = "Выберите папку");
        Task<bool> ShowConfirmationDialogAsync(string message);
        Task ShowMessageAsync(string title, string message);
    }
}
