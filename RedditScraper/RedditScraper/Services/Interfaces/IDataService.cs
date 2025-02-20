using RedditScraper.Models;
using System.Threading.Tasks;

namespace RedditScraper.Services.Interfaces
{
    public interface IDataService
    {
        Task SaveData(Dialogue dialogue);

    }
}
