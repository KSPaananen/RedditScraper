namespace RedditScraper.AppSettings
{
    public class Settings
    {
        public Settings()
        {

        }

        public Application Application { get; set; } = new Application();
        public Enviroment Enviroment { get; set; } = new Enviroment();
        public Scraper Scraper { get; set; } = new Scraper();
    }

    public class Application
    {
        public ClientSize ClientSize { get; set; } = new ClientSize();
        public ClientPosition ClientPosition { get; set; } = new ClientPosition();
    }

    public class ClientSize
    {
        public int X { get; set; } = 1000;
        public int Y { get; set; } = 600;
    }

    public class ClientPosition
    {
        public int X { get; set; } = 50;
        public int Y { get; set; } = 50;
    }

    public class Enviroment
    {
        public string TorPath { get; set; } = string.Empty;
        public string ConnectionString { get; set; } = "";
    }

    public class Scraper
    {
        public string BrowserMode { get; set; } = "Desktop";
        public string Separator { get; set; } = ";";
        public Proxy Proxy { get; set; } = new Proxy();
        public Data Data { get; set; } = new Data();
    }

    public class Proxy
    {
        public bool UseProxy { get; set; }
    }

    public class Data
    {
        public bool UseMongoDB { get; set; } = false;
        public bool UseCSV { get; set; } = false;
    }

}