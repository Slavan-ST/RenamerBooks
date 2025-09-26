using Avalonia.Threading;
using ReactiveUI;
using RenameBooks.Records;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;

namespace RenameBooks.ViewModels
{
    public class BookCopyViewModel : ReactiveObject
    {
        private readonly string _originalPath;
        private readonly Action<BookCopyViewModel> _onAllCopiesDeleted;

        public string Title { get; }
        public string OriginalFilePath => _originalPath;
        public ObservableCollection<CopyLocationViewModel> Copies { get; }

        public BookCopyViewModel(OrganizationResult result, string rootFolder, Action<BookCopyViewModel> onAllCopiesDeleted)
        {
            _originalPath = result.OriginalFilePath;
            Title = result.Title;
            _onAllCopiesDeleted = onAllCopiesDeleted;

            Copies = new ObservableCollection<CopyLocationViewModel>(
                result.CreatedFilePaths.Select(path => new CopyLocationViewModel(path, rootFolder, RemoveCopy))
            );
        }

        private void RemoveCopy(CopyLocationViewModel copy)
        {
            // Удаляем из коллекции (в UI-потоке!)
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                Copies.Remove(copy);

                // Если осталась только одна книга — уведомляем
                if (Copies.Count == 1)
                {
                    _onAllCopiesDeleted(this);
                }
            });
        }
    }

}