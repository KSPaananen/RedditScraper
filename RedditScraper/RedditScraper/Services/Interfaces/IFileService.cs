namespace RedditScraper.Services.Interfaces
{
    public interface IFileService
    {
        void SetupFolders();

        void SetupFiles();

        string LocateTor();

    }
}
