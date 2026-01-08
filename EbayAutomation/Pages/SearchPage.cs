using Allure.Net.Commons;
using EbayAutomation.Core;
using Microsoft.Playwright;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EbayAutomation.Pages
{
    public class SearchPage : BasePage
    {
        public SearchPage(IPage page) : base(page) { }

        public async Task GotoAsync() => await _page.GotoAsync("https://www.ebay.com", new() { WaitUntil = WaitUntilState.DOMContentLoaded });

        public async Task<List<string>> SearchItemsByNameUnderPrice(string query, double maxPrice, int limit = 5)
        {
            var itemUrls = new List<string>();
            await AllureApi.Step("Searching for items", async () =>
            {
                AllureApi.Step("Perform Search using Smart Locators (CSS & XPath fallback)");
                await SmartFillAsync(new[] { "input[id='gh-ac']", "//input[@name='_nkw']" }, query, "Search Input");
                await SmartClickAsync(new[] { "#gh-search-btn", "//*[@id='gh-search-btn']" }, "Search Button");

                #region optional
                // 2. Apply Price Filter (Optional attempt)
                // eBay DOM is dynamic; we try to fill the filter if it exists.
                AllureApi.Step("Checking if there is a price limiter");
                await RetryWithBackoffAsync(async () =>
                 {
                     var priceInput = _page.Locator("input[aria-label='Maximum Value']").First;
                     if (await priceInput.IsVisibleAsync())
                     {
                         await priceInput.FillAsync(maxPrice.ToString());
                         await _page.Keyboard.PressAsync("Enter");
                         await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                     }
                 }, maxRetries: 2); // Optional, ignore if filter not found
                 AllureApi.Step("No price limiter was found. moving on...");
                #endregion

                AllureApi.Step("Collect in stock Items (Paging Logic)");
                var unwantedPhrases = new[] { "out of stock", "sold out", "unavailable" };
                while (itemUrls.Count < limit)
                {
                    // Get all item containers
                    var locateItemsList = _page.Locator("ul.srp-results > li.s-card");
                    await locateItemsList.First.WaitForAsync(/* OPTIONAL: new() {State = WaitForSelectorState.Visible}*/);

                    var fullItemsList = await locateItemsList.AllAsync();
                    foreach (var item in fullItemsList)
                    {
                        if (itemUrls.Count >= limit) break;

                        //use of standard Playwright for internal item data as these aren't "Action" buttons
                        var title = await item.Locator(".s-card__title .su-styled-text").InnerTextAsync();
                        if (unwantedPhrases.Any(p => title.Contains(p, StringComparison.OrdinalIgnoreCase))) continue; // Skip out of stock

                        var linkLocator = item.Locator("a.s-card__link").First;
                        var priceLocator = item.Locator(".s-card__price").Last; //getting from main search where it says 'ILS' - taking highest price range

                        if (await linkLocator.CountAsync() == 0 || await priceLocator.CountAsync() == 0) continue;

                        var priceText = await priceLocator.InnerTextAsync();
                        var url = await linkLocator.GetAttributeAsync("href");

                        // Check price condition and ensure url is not null
                        if (url != null && ParsePrice(priceText) <= maxPrice)
                        {
                            AllureApi.Step($"Found item: '{title}, Price: {priceText}");
                            itemUrls.Add(url);
                        }
                    }

                    if (itemUrls.Count >= limit) break;

                    // Handle Paging: If we still need items, click "Next"
                    try
                    {
                         AllureApi.Step($"Missing items in the cart ({itemUrls.Count}), Checking next page..");
                        var nextBtnSelectors = new[] { "a[type='next']", "a.pagination__next", "a[aria-label='Next Page']" };
                        await SmartClickAsync(nextBtnSelectors, "Next Page");
                    }

                    catch
                    {
                         AllureApi.Step("No more pages found");
                        //ignore error if no other page is found
                        break;
                    }
                }
            });

            return itemUrls;
        }
    }
}