using MongoDB.Bson;
using MongoDB.Driver;
using RedditScraper.AppSettings;
using RedditScraper.Models;
using RedditScraper.Services.Interfaces;
using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RedditScraper.Services
{
    public class DataService : IDataService
    {
        private Settings settings;

        public DataService()
        {
            settings = SettingsManager.ReadSettings();

        }

        public async Task SaveData(Dialogue dialog)
        {
            if (settings.Scraper.Data.UseCSV)
            {
                WriteToCSV(dialog);
            }

            if (settings.Scraper.Data.UseMongoDB)
            {
                await WriteToDB(dialog);
            }

        }

        // Save dialogue object to db
        private async Task WriteToDB(Dialogue dialog)
        {
            if (settings.Enviroment.ConnectionString != "" && settings.Enviroment.ConnectionString != null)
            {
                var client = new MongoClient(settings.Enviroment.ConnectionString);

                var db = client.GetDatabase("ScraperDB");

                var collection = db.GetCollection<BsonDocument>("Dialogs");

                var bsonDoc = dialog.ToBsonDocument();

                await collection.InsertOneAsync(bsonDoc);
            }
        }

        private void WriteToCSV(Dialogue dialog)
        {
            // Get folder where to create files
            var folder = Path.GetFullPath($"{AppDomain.CurrentDomain.BaseDirectory}\\Data");

            bool doesExist = File.Exists($"{folder}\\content.csv");

            // Check for existing files and create if none exist
            if (!doesExist)
            {
                try
                {
                    File.Create($"{folder}\\content.csv");

                    // File creation can have delays
                    Thread.Sleep(200);
                }
                catch
                {
                    return;
                }
            }

            // Append entry to log
            try
            {
                string separator = settings.Scraper.Separator;

                var entry = JsonSerializer.Serialize(dialog.Conversation);

                File.AppendAllTextAsync($"{folder}\\content.csv", $"{entry}{separator}");
            }
            catch
            {
                return;
            }
        }


    }
}
