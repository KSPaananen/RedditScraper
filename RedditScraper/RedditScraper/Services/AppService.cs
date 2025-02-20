using RedditScraper.AppSettings;
using RedditScraper.Services.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace RedditScraper.Services
{
    public class AppService : IAppService
    {
        private IFileService _fileService;

        public AppService()
        {
            _fileService = new FileService();
        }

        public void OnStartUp()
        {
            // Setup/verify files & folders
            _ = Task.Run(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                _fileService.SetupFolders();
                _fileService.SetupFiles();
            });

            // Search for tor and save its filepath to settings
            _ = Task.Run(() =>
            {
                Thread.CurrentThread.IsBackground = true;

                var filepath = _fileService.LocateTor();

                if (filepath != "" && filepath != null)
                {
                    Settings settings = SettingsManager.ReadSettings();
                    settings.Enviroment.TorPath = filepath;
                    SettingsManager.SaveSettings(settings);
                }
            });
        }


    }
}
