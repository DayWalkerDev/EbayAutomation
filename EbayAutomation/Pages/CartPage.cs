using Allure.Net.Commons;
using EbayAutomation.Core;
using Microsoft.Playwright;

namespace EbayAutomation.Pages
{
    public class CartPage : BasePage
    {
        public CartPage(IPage page) : base(page) { }

        public async Task AssertCartTotalNotExceeds(double budgetPerItem, int itemsCount)
        {
            await AllureApi.Step($"Checking the cart 'Subtotal' for {itemsCount} items, max budget per item is {budgetPerItem}ILS", async () =>
            {
                try
                {
                    // Navigate via cart icon to avoid verification
                    AllureApi.Step("Pressing the cart icon");
                    await SmartClickAsync(
                    [
                        ".gh-cart a.gh-flyout__target",  // path to href class
                        "button:has-text(\"Expand Cart\")",  // get by button text
                        "button.gh-flyout__target-a11y-btn", // get by the button class
                    ], "Open Cart");

                    var limit = budgetPerItem * itemsCount; //Get the expected limit of all items
                    AllureApi.Step($"Checking the cart 'Subtotal', max budget per item({itemsCount}): {budgetPerItem}ILS, Overall: {limit}ILS");

                    // Locate subtotal and overall price values
                    var subtotalLocator = _page.Locator("[data-test-id='SUBTOTAL']"); //this works ***
                    await subtotalLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 15000 });

                    var subtotalText = await subtotalLocator.InnerTextAsync();
                    var total = ParsePrice(subtotalText);

                    AllureApi.Step($"Verify that the subtotal is less than our limit budget");

                    //Check that we do not exceed the expected price
                    if (total > limit)
                    {
                        AllureApi.Step($"Verification Failed: Found subtotal of {total}ILS, The Limit: is {limit}ILS");
                        await TakeScreenshotAsync("Price_Exceeded_Error");
                        throw new Exception($"Cart total {total} exceeds the limit of {limit}");
                    }

                    AllureApi.Step($"Verification Passed: Found subtotal of {total}ILS, The Limit: is {limit}ILS");
                    //Success printscreen
                    await TakeScreenshotAsync("Cart_Verified");

                }
                catch (Exception ex)
                {
                    await TakeScreenshotAsync($"Failure_AssertCartTotalNotExceeds");
                    throw new Exception($"Failure: {ex.Message}");
                }
            });
        }

        //  private double ParsePrice(string priceRaw)
        // {
        //     var clean = new string(priceRaw.Where(c => char.IsDigit(c) || c == '.').ToArray());
        //     //returns 0 by default and not 'max' in case of discount (optional)
        //     return double.TryParse(clean, out double result) ? result : 0; 
        // }
    }
}