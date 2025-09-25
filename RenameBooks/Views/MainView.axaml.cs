using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using RenameBooks.ViewModels;

namespace RenameBooks.Views
{
    public partial class MainView : UserControl
    {
        public MainView()
        {
            InitializeComponent();
            DataContext = (App.Current as App).ServiceProvider.GetRequiredService<MainViewModel>();
        }
    }

}