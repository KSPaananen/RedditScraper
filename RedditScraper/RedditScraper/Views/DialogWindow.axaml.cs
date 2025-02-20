using Avalonia.Controls;
using RedditScraper.ViewModels;

namespace RedditScraper.Views
{
    public partial class DialogWindow : Window
    {
        private DialogWindowModel dialogWindowModel;
        
        public DialogWindow()
        {
            InitializeComponent();

            this.DataContext = new DialogWindowModel();
            dialogWindowModel = this.DataContext as DialogWindowModel;

            Position = dialogWindowModel!.Position;
            
        }

        // Bit a buggy implementation. Minimizes applications on Win 10
        private void WindowPositionChanged(object? sender, Avalonia.Controls.PixelPointEventArgs e)
        {
            Position = dialogWindowModel.Position;
        }
    }
}
