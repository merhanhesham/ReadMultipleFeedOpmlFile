using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;

namespace OpmlRender.Pages
{
    public class IndexModel : PageModel
    {
        private const int PageSize = 10;
        private readonly HttpClient _httpClient;

        public List<RssItem> RssItems { get; set; } = new List<RssItem>();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;

        public IndexModel(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IActionResult> OnGetAsync(int pageNumber = 1)
        {
            var opmlUrl = "https://blue.feedland.org/opml?screenname=dave";
            var opmlContent = await _httpClient.GetStringAsync(opmlUrl);
            var feedUrls = ParseOpmlContent(opmlContent);

            var tasks = feedUrls.Select(url => FetchAndParseRssFeedAsync(url));
            var rssResponses = await Task.WhenAll(tasks);
            RssItems = rssResponses.SelectMany(r => r).ToList();

            TotalPages = (int)System.Math.Ceiling((double)RssItems.Count / PageSize);
            CurrentPage = pageNumber;

            // Apply paging
            RssItems = RssItems
                .Skip((pageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return Page();
        }

        private List<string> ParseOpmlContent(string opmlContent)
        {
            var feedUrls = new List<string>();

            var doc = XDocument.Parse(opmlContent);
            var outlines = doc.Descendants("outline");

            foreach (var outline in outlines)
            {
                var xmlUrl = outline.Attribute("xmlUrl")?.Value;
                if (!string.IsNullOrEmpty(xmlUrl))
                {
                    feedUrls.Add(xmlUrl);
                }
            }

            return feedUrls;
        }

        private async Task<List<RssItem>> FetchAndParseRssFeedAsync(string url)
        {
            var rssItemList = new List<RssItem>();

            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var xmlContent = await response.Content.ReadAsStringAsync();
                rssItemList = ParseXmlContent(xmlContent);
            }

            return rssItemList;
        }

        private List<RssItem> ParseXmlContent(string xmlContent)
        {
            var rssItemList = new List<RssItem>();
            var doc = XDocument.Parse(xmlContent);
            var items = doc.Descendants("item");

            foreach (var item in items)
            {
                var rssItem = new RssItem
                {
                    Description = item.Element("description")?.Value,
                    PubDate = item.Element("pubDate")?.Value,
                    Link = item.Element("link")?.Value,
                };

                rssItemList.Add(rssItem);
            }

            return rssItemList;
        }
    }

    public class RssItem
    {
        public string Description { get; set; }
        public string PubDate { get; set; }
        public string Link { get; set; }
    }
}