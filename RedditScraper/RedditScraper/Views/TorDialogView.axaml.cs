using Avalonia.Controls;
using RedditScraper.ViewModels;

namespace RedditScraper.Views
{
    public partial class TorDialogView : UserControl
    {
        public TorDialogView()
        {
            InitializeComponent();

            this.DataContext = new TorDialogViewModel();

        }
    }
}
