using PuppeteerSharp;
using RedditScraper.AppSettings;
using RedditScraper.Models;
using RedditScraper.Services.Interfaces;
using RedditScraper.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace RedditScraper.Services
{
    public class ScraperService : IScraperService
    {
        private readonly MainViewModel mainViewModel;

        private IBrowser? browser;

        private Stopwatch stopwatch { get; set; }

        public ScraperService(MainViewModel vm)
        {
            stopwatch = new Stopwatch();
            mainViewModel = vm;
        }

        public async Task Scrape(string url, CancellationToken cToken)
        {
            // Start timer
            stopwatch.Start();

            // Notify user
            mainViewModel.ConsoleContent = "Scraping started";

            await ScrapeContent(url, cToken);

        }

        // Get html with a headless browser
        private async Task ScrapeContent(string url, CancellationToken cToken)
        {
            Settings settings = SettingsManager.ReadSettings();

            // Setup enviroment according to settings
            LaunchOptions launchOptions = new LaunchOptions();

            if (settings.Scraper.Proxy.UseProxy)
            {
                // Setup options for tor
                launchOptions = new LaunchOptions
                {
                    Headless = false,
                    ExecutablePath = settings.Enviroment.TorPath,
                    Browser = SupportedBrowser.Firefox,
                    DefaultViewport = null,
                    Args = new[] { "-wait-for-browser" },
                    UserDataDir = "",
                };
            }
            else
            {
                // Download browser if it doesn't exist in Browsers folder
                var browserFetcher = new BrowserFetcher
                {
                    CacheDir = $"{AppDomain.CurrentDomain.BaseDirectory}Browsers",
                    Platform = Platform.Win64,
                    Browser = SupportedBrowser.Firefox,
                };

                await browserFetcher.DownloadAsync();

                // Setup launchoptions
                launchOptions = new LaunchOptions
                {
                    Headless = false,
                    ExecutablePath = "Browsers\\Firefox\\Win64-129.0a1\\firefox\\firefox.exe",
                    Browser = SupportedBrowser.Firefox,
                    DefaultViewport = null,
                    Args = new[] { "-wait-for-browser" },
                    UserDataDir = "", // Skip profile selection
                };
            }

            try
            {
                // Launch headless browser
                browser = await Puppeteer.LaunchAsync(launchOptions);

                cToken.ThrowIfCancellationRequested();

                // Provide feedback to front end
                mainViewModel.ConsoleContent = $"Opening browser";

                // Set user agent header to mask browser as either mobile or desktop
                string userAgent = "";

                switch (settings.Scraper.BrowserMode)
                {
                    case "Desktop":
                        userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36";
                        break;
                    case "Mobile":
                        userAgent = "Mozilla/5.0 (Linux; Android 14) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.6367.179 Mobile Safari/537.36";
                        break;
                    default:
                        userAgent = "";
                        break;
                }


                // Get first tab
                var pages = await browser.PagesAsync();
                var startPage = pages[0];

                if (settings.Scraper.Proxy.UseProxy)
                {
                    // Click connect button on the first page
                    await startPage.ClickAsync("#connectButton");
                }

                // Create new tab since staying in the first one is bug prone
                IPage page = await browser.NewPageAsync();

                // Setup custom user agent
                await page.SetUserAgentAsync(userAgent);

                // Block content such as pictures & videos to speed up operation
                page = await BlockRequests(page);

                cToken.ThrowIfCancellationRequested();

                // Provide feedback to the front end
                mainViewModel.ConsoleContent = $"Navigating to page";

                // Go to page
                await page.GoToAsync(url, 60000);

                // Evaluate site condition. Return if page has Mature content popup since it requires login
                await EvaluatePage(page, cToken);

                // Scroll down the page to expose all comment trees
                page = await ScrollDownPage(page, cToken);

                // Provide feedback to the front end
                mainViewModel.ConsoleContent = $"Fetching comments";

                // Expand minimized comments
                // ToDo

                // Read conversations from comments and their replies
                page = await GetDialogues(page, cToken);

                // Close page
                await page.CloseAsync();

                // Stop timer
                stopwatch.Stop();
                TimeSpan ts = stopwatch.Elapsed;
                string time = String.Format("{0:00}:{1:00}", ts.Minutes, ts.Seconds);

                mainViewModel.ConsoleContent = $"Scraping concluded [{time}]";
                mainViewModel.ProcessRunning = false;

            }
            catch (Exception ex)
            {
                // Provide feedback to GUI
                // Only provide feedback when errors arent the result of requested cancellation
                if (cToken.IsCancellationRequested == false)
                {
                    mainViewModel.ConsoleContent = $"Error: {ex.Message}";
                }

                if (browser != null)
                {
                    await browser.CloseAsync();
                }

                // Set process running to false in viewmodel
                mainViewModel.ProcessRunning = false;

                return;
            }

            // Close browser after everything is done
            await browser.CloseAsync();

            return;
        }

        // Scroll down to load all first layer comments
        private async Task<IPage> ScrollDownPage(IPage page, CancellationToken cToken)
        {
            cToken.ThrowIfCancellationRequested();

            // Get bounding box of main element
            ElementHandle bodyHandle = (ElementHandle)await page.WaitForSelectorAsync("main");
            BoundingBox main = await bodyHandle.BoundingBoxAsync();

            bool doneScrolling = false;
            decimal oldHeight = 0;
            decimal newHeight = 0;

            do
            {
                oldHeight = newHeight;

                // Scroll down
                await page.EvaluateExpressionAsync("window.scrollBy({top:10000})");

                // Measure height
                main = await bodyHandle.BoundingBoxAsync();
                newHeight = main.Y;

                // Try finding "View more comments" Button 5 times
                for (int i = 0; i <= 5; i++)
                {
                    // Wrap in try-catch since WaitForSelectorAsync throws an error if selector times out
                    try
                    {
                        var options = new WaitForSelectorOptions
                        {
                            Timeout = 1000,
                        };

                        ElementHandle wmcButton = (ElementHandle)await page.WaitForSelectorAsync("#comment-tree > faceplate-partial > div:nth-child(2) > button", options);

                        if (wmcButton != null)
                        {
                            // Click it and reset loop
                            await page.ClickAsync("#comment-tree > faceplate-partial > div:nth-child(2) > button");

                            i = 0;
                        }
                    }
                    catch
                    {
                        if (i == 5)
                        {
                            doneScrolling = true;
                        }
                    }
                }
            }
            while (newHeight != oldHeight && doneScrolling == false);

            return page;
        }

        private async Task<IPage> GetDialogues(IPage page, CancellationToken cToken)
        {
            cToken.ThrowIfCancellationRequested();

            int timeout = 1000;

            string userMessage = "";
            string agentMessage = "";

            for (int i = 2; i <= Int32.MaxValue - 1; i++)
            {
                // Using this selector get the text from the first layer of comments
                string selector = $"#comment-tree > shreddit-comment:nth-child({i})";

                IElementHandle? element = null;

                try
                {
                    element = await page.WaitForSelectorAsync(selector, new WaitForSelectorOptions
                    {
                        Timeout = timeout,
                    });
                }
                catch
                {
                    // Reset messages & element
                    userMessage = "";
                    agentMessage = "";
                    element = null;

                    // Break loop if first comment isn't found between values 2-5
                    if (i >= 5)
                    {
                        break;
                    }
                }

                if (element != null)
                {
                    // Get elements thingid attribute and use it to fetch text inside <p> tag
                    string attributeName = "thingid";
                    string attributeValue = await page.EvaluateFunctionAsync<string>(
                                        @"(selector, attributeName) => {
                                            var element = document.querySelector(selector);
                                            return element ? element.getAttribute(attributeName) : null;}"
                                        , selector, attributeName);

                    // Get the first comment which will be used in the next for loop to create a conversation
                    string textSelector = $"#{attributeValue}-comment-rtjson-content";
                    userMessage = await page.QuerySelectorAsync(textSelector).EvaluateFunctionAsync<string>("_ => _.innerText");

                    // Clear element
                    element = null;
                }

                // Look for replies to comments
                for (int j = 6; j <= 30; j++)
                {
                    selector = $"#comment-tree > shreddit-comment:nth-child({i}) > shreddit-comment:nth-child({j})";

                    try
                    {
                        element = await page.WaitForSelectorAsync(selector, new WaitForSelectorOptions
                        {
                            Timeout = timeout,
                        });
                    }
                    catch
                    {
                        // Allow a few retries because reply indexing is weird at times
                        if (j < 9)
                        {
                            j++;
                            continue;
                        }
                        else
                        {
                            // Reset messages & element
                            userMessage = "";
                            agentMessage = "";
                            element = null;

                            break;
                        }
                    }

                    if (element != null)
                    {
                        // Get elements thingid attribute and use it to fetch text inside <p> tag
                        string attributeName = "thingid";
                        string attributeValue = await page.EvaluateFunctionAsync<string>(
                                            @"(selector, attributeName) => {
                                                var element = document.querySelector(selector);
                                                return element ? element.getAttribute(attributeName) : null;}"
                                            , selector, attributeName);

                        // Get the reply to the first comment
                        string textSelector = $"#{attributeValue}-comment-rtjson-content";
                        agentMessage = await page.QuerySelectorAsync(textSelector).EvaluateFunctionAsync<string>("_ => _.innerText");

                        // Create conversation from comment and its reply
                        await SaveDialogue(userMessage, agentMessage);

                        // Clear element
                        element = null;
                    }

                    // Try finding more replies to replies
                    for (int k = 6; k <= 30; k++)
                    {
                        selector = $"#comment-tree > shreddit-comment:nth-child({i}) > shreddit-comment:nth-child({j}) > shreddit-comment:nth-child({k})";

                        try
                        {
                            element = await page.WaitForSelectorAsync(selector, new WaitForSelectorOptions
                            {
                                Timeout = timeout,
                            });
                        }
                        catch
                        {
                            // Allow a few retries because reply indexing is weird at times
                            if (k < 9)
                            {
                                k++;
                                continue;
                            }
                            else
                            {
                                // Reset messages & element
                                userMessage = "";
                                agentMessage = "";
                                element = null;

                                break;
                            }
                        }

                        if (element != null)
                        {
                            // Get elements thingid attribute and use it to fetch text inside <p> tag
                            string attributeName = "thingid";
                            string attributeValue = await page.EvaluateFunctionAsync<string>(
                                                @"(selector, attributeName) => {
                                                var element = document.querySelector(selector);
                                                return element ? element.getAttribute(attributeName) : null;}"
                                                , selector, attributeName);

                            // New agentMessage is a reply to old agentMessage so store it into userMessage
                            userMessage = agentMessage;

                            // Get the reply to the first comment
                            string textSelector = $"#{attributeValue}-comment-rtjson-content";
                            agentMessage = await page.QuerySelectorAsync(textSelector).EvaluateFunctionAsync<string>("_ => _.innerText");

                            // Create conversation from comment and its reply
                            await SaveDialogue(userMessage, agentMessage);

                            // Clear element
                            element = null;
                        }

                        for (int l = 6; l < 20;)
                        {
                            selector = $"#comment-tree > shreddit-comment:nth-child({i}) > shreddit-comment:nth-child({j}) > shreddit-comment:nth-child({k}) > faceplate-partial:nth-child({l})";

                            try
                            {
                                element = await page.WaitForSelectorAsync(selector, new WaitForSelectorOptions
                                {
                                    Timeout = timeout,
                                });
                            }
                            catch
                            {
                                // Allow a few retries because reply indexing is weird at times
                                if (l < 9)
                                {
                                    l++;
                                    continue;
                                }
                                else
                                {
                                    // Reset messages & element
                                    userMessage = "";
                                    agentMessage = "";
                                    element = null;

                                    break;
                                }
                            }

                            if (element != null)
                            {
                                // Get elements thingid attribute and use it to fetch text inside <p> tag
                                string attributeName = "thingid";
                                string attributeValue = await page.EvaluateFunctionAsync<string>(
                                                    @"(selector, attributeName) => {
                                                var element = document.querySelector(selector);
                                                return element ? element.getAttribute(attributeName) : null;
                                                }"
                                                    , selector, attributeName);

                                // New agentMessage is a reply to old agentMessage so store it into userMessage
                                userMessage = agentMessage;

                                // Get the reply to the first comment
                                string textSelector = $"#{attributeValue}-comment-rtjson-content";
                                agentMessage = await page.QuerySelectorAsync(textSelector).EvaluateFunctionAsync<string>("_ => _.innerText");

                                // Create conversation from comment and its reply
                                await SaveDialogue(userMessage, agentMessage);

                                // Clear element
                                element = null;
                            }
                        }
                    }
                }
            }

            return page;
        }

        private async Task SaveDialogue(string userText, string agentText)
        {
            Conversation user = new Conversation
            {
                Speaker = "user",
                Text = userText,
            };

            Conversation agent = new Conversation
            {
                Speaker = "agent",
                Text = agentText,
            };

            Dialogue dialog = new Dialogue
            {  
                Conversation = new List<Conversation>() { user, agent }
            };

            // Provide feedback to the front end
            mainViewModel.ConsoleContent = $"Dialogue found and saved";

            // Save the dialogue
            DataService dService = new DataService();
            await dService.SaveData(dialog);
        }

        // Check for 18+ pop up which requires login to access and blocked by reddit page
        private async Task EvaluatePage(IPage page, CancellationToken cToken)
        {
            cToken.ThrowIfCancellationRequested();

            // List all css selectors which to look out for
            var tupleList = new List<(string, string)>
            {
                ("shreddit-async-loader.theme-beta > xpromo-nsfw-blocking-modal-desktop:nth-child(1)", "Page contains +18 content restriction"),
                ("body > h1:nth-child(1)", "Request blocked by Reddit"),
            };

            foreach (var tuple in tupleList)
            {
                // WaitForSelectorAsync will throw an error if element isn't found
                try
                {
                    var options = new WaitForSelectorOptions
                    {
                        Timeout = 1000,
                    };

                    ElementHandle wmcButton = (ElementHandle)await page.WaitForSelectorAsync(tuple.Item1, options);
                }
                catch
                {
                    // Continue execution if its not found
                    return;
                }

                // Throw an error if it's found
                throw new ArgumentException(tuple.Item2);
            }
        }

        // Reduce loadtimes by blocking urls from media, advertisement etc servers
        private async Task<IPage> BlockRequests(IPage page)
        {
            // ToDo: Block videos, pictures & other bandwidth consuming urls.

            return page;
        }


    }
}