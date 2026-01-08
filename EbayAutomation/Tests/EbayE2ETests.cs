using EbayAutomation.Pages;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using Microsoft.Playwright;
using System.IO;
using System.Text.Json;
using Allure.NUnit;
using Allure.NUnit.Attributes;
using Allure.Net.Commons;

namespace EbayAutomation.Tests
{
    // Enables parallel execution and Allure
    [AllureNUnit]
    [Parallelizable(ParallelScope.Fixtures)]
    public class EbayE2ETests : PageTest
    {
        private SearchPage _searchPage;
        private ItemPage _itemPage;
        private CartPage _cartPage;
        private TestData _data;

        // Data Model
        public class TestData
        {
            public required string SearchQuery { get; set; }
            public double MaxPrice { get; set; }
            public int ItemsToSelect { get; set; }
        }

        [SetUp]
        public async Task Setup()
        {
            // Load Data-Driven Configuration
            var jsonString = await File.ReadAllTextAsync("test-settings.json");
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _data = JsonSerializer.Deserialize<TestData>(jsonString, jsonOptions) ??
                throw new InvalidOperationException(
                        "Failed to deserialize test-settings.json; ensure the file exists and contains valid TestData JSON.");

            //Add parameters to the report 
            // This ensures the report shows exactly what data was used for this execution.
            AllureApi.AddTestParameter("Search Query", _data.SearchQuery);
            AllureApi.AddTestParameter("Max Price", _data.MaxPrice.ToString());
            AllureApi.AddTestParameter("Items To Select", _data.ItemsToSelect.ToString());

            // Initialize Pages
            _searchPage = new SearchPage(Page);
            _itemPage = new ItemPage(Page);
            _cartPage = new CartPage(Page);
        }

        #region Moon concept
        // ***MOON / CLOUD GRID Ready general consept***
        // This code shows how to connect to a remote Moon instance instead of local browser
        // To activate: Set environment variable REMOTE_BROWSER_URL=wss://your-moon-instance.aerokube.com/playwright/chromium
        // Example URL format for Moon: wss://user:pass@moon.example.com/playwright/chromium/playwright-1.45.0

        // private string? RemoteBrowserUrl => Environment.GetEnvironmentVariable("REMOTE_BROWSER_URL");

        // In a real remote setup, you would use a custom fixture or override the browser creation
        // Example (commented to avoid local conflicts and compilation errors):
        /*
                [OneTimeSetUp]
                public async Task SetupRemoteIfNeeded()
                {
                    if (!string.IsNullOrEmpty(RemoteBrowserUrl))
                    {
                        var playwright = await Playwright.CreateAsync();
                        // Connect to remote Moon browser (Chromium example)
                        Browser = await playwright.Chromium.ConnectAsync(RemoteBrowserUrl);
                        // For Firefox: playwright.Firefox.ConnectAsync(...)
                        // For WebKit: playwright.Webkit.ConnectAsync(...)

                        // Create context and page manually
                        Context = await Browser.NewContextAsync();
                        Page = await Context.NewPageAsync();
                    }
        }*/
        #endregion

        [Test]
        [AllureName("Ebay E2E Shopping Scenario")]
        [AllureSuite("Ebay Shopping Cart")]
        public async Task E2E_Search_Filter_AddToCart_VerifyTotal()
        {
            List<string> urls = [];
            await AllureApi.Step("Step 1: Navigate and Search items", async () =>
            {
                // Step 1: Search and collect URLs
                await _searchPage.GotoAsync();
                urls = await _searchPage.SearchItemsByNameUnderPrice(_data.SearchQuery, _data.MaxPrice, _data.ItemsToSelect);

                AllureApi.Step($"Collected {urls.Count} items under price limit");
                Assert.That(urls, Is.Not.Empty.And.Not.Null, "No items found under the requested price.");
            });

            await AllureApi.Step("Step 2: Add Items to Cart", async () =>
            {
                // Step 2: Add items to cart (handling random variants)
                await _itemPage.AddItemsToCart(urls);
            });

            await AllureApi.Step("Step 3: Verify Cart Total", async () =>
            {
                // Step 3: Verify Cart Total
                await _cartPage.AssertCartTotalNotExceeds(_data.MaxPrice, urls.Count);
            });
        }
    }
}