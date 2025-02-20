using System.Threading;
using System.Threading.Tasks;

namespace RedditScraper.Services.Interfaces
{
    public interface IScraperService
    {
        Task Scrape(string url, CancellationToken cToken);

    }
}
