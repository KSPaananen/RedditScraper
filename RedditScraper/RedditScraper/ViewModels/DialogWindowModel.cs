using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;
using RedditScraper.Views;


namespace RedditScraper.ViewModels
{
    public class DialogWindowModel
    {
        public DialogWindowModel()
        {

        }

        public int Width { get; set; } = 350;
        public int Height { get; set; } = 200;

        private PixelPoint position;
        public PixelPoint Position
        {
            get
            {
                try
                {
                    int x = 0;
                    int y = 0;

                    // Get Owner windows position
                    if (Avalonia.Application.Current!.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow != null)
                    {
                        x = (int)(desktop.MainWindow.Position.X + (desktop.MainWindow.ClientSize.Width / 2) - (Width / 2));
                        y = (int)(desktop.MainWindow.Position.Y + (desktop.MainWindow.ClientSize.Height / 2) - (Height / 2));
                    }

                    position = new Avalonia.PixelPoint(x, y);
                }
                catch
                {

                }

                return position;
            }
            set
            {
                position = value;
            }
        }

    }
}
