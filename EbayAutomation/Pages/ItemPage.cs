using System.Text.RegularExpressions;
using Allure.Net.Commons;
using EbayAutomation.Core;
using Microsoft.Playwright;
using Microsoft.VisualBasic;

namespace EbayAutomation.Pages
{
    public class ItemPage : BasePage
    {
        public ItemPage(IPage page) : base(page) { }

        public async Task AddItemsToCart(List<string> urls)
        {
            await AllureApi.Step("Starting - add items to cart", async () =>
            {
                var urlCount = 1;
                var unwantedPhrases = new[] { "(out of stock)", "sold out", "unavailable", "not available", "Select" };

                foreach (var url in urls)
                {
                    AllureApi.Step($"Trying to adding items [Item {urlCount} out of {urls.Count}]");
                    await _page.GotoAsync(url);
                    await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                    try
                    {
                        await LookForItemOptionsAndSelect(unwantedPhrases);
                    }

                    catch { /*ignore error if no options*/}

                    AllureApi.Step("clearing any pop-up that might stay on screen before adding to cart");
                    await _page.Keyboard.PressAsync("Escape");

                    //Click Add to Cart using Smart Locators
                    AllureApi.Step("Clicking 'Add To Cart' button");
                    await SmartClickAsync(
                    [
                        "div[data-testid='x-atc-action'] a", // class id
                    ".vim x-atc-action a", //class name
                        "#atcBtn_btn_1", // selector to button
                ], "Add to Cart Button");

                    AllureApi.Step("Closing the pop-up after adding to cart");
                    await _page.Keyboard.PressAsync("Escape");
                    await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

                    //Screenshot on success 
                    AllureApi.Step($"Item (#{urlCount}) was added, taking screenshot");
                    await TakeScreenshotAsync($"Item_Number{urlCount}_Added");
                    urlCount++;
                }
            });
        }

        private async Task LookForItemOptionsAndSelect(string[] unwantedPhrases)
        {
            // Handle Variants (e.g., Size, Color dropdowns)
            // We select a random option for every dropdown found.
            var skuSelector = ".vim.x-sku";
            var skuBlocks = _page.Locator(skuSelector);
            await _page.Locator(skuSelector).First.WaitForAsync();

            int count = await skuBlocks.CountAsync();
            // If no option for this item search, move on to add item 
            if (count == 0) return;

            AllureApi.Step("Item has options to select, getting random values if possible");

            for (int i = 0; i < count; i++)
            {
                var sku = skuBlocks.Nth(i);

                // Click the dropdown button
                var button = sku.Locator("button.listbox-button__control");

                //check if a value is selected in the placeholder before clicking it
                var optionPlaceholder = await button.Locator(".btn__cell .btn__text").InnerTextAsync();
                if (!optionPlaceholder.Equals("Select"/*, StringComparison.OrdinalIgnoreCase*/))
                {
                    continue;
                }

                await button.ClickAsync();

                // Get all option elements 
                var optionElements = sku.Locator(".listbox__option");

                // Extract visible text 
                var optionTexts = await optionElements.Locator(".listbox__value").AllInnerTextsAsync();

                // Filter out unwanted options 
                var validOptions = optionTexts.Where(text => !unwantedPhrases.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase))).ToList();

                // Skip if no valid options
                if (validOptions.Count == 0) continue;

                // Skip the "Select" placeholder
                //int startIndex = validOptions[0].Equals("Select", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                var randomIndex = new Random().Next(0, validOptions.Count);///(startIndex, validOptions.Count);
                var randomText = validOptions[randomIndex];

                // Click the matching option 
                await optionElements.Filter(new() { HasText = randomText }).ClickAsync();
            }
        }
    }
}