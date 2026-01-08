using Microsoft.Playwright;
using Allure.Net.Commons;
using System.Text;
using System.Threading.Tasks;

namespace EbayAutomation.Core
{
    public abstract class BasePage
    {
        protected readonly IPage _page;
        protected BasePage(IPage page) => _page = page;
       
        // Smart action with retries and multiple locators
        protected async Task RetryWithBackoffAsync(Func<Task> action, int maxRetries = 2, int initialDelayMs = 5000)
        {
            int delay = initialDelayMs;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await action();
                    return;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    Console.WriteLine($"Retry {attempt}/{maxRetries} failed: {ex.Message}. Waiting {delay}ms...");
                    await Task.Delay(delay);
                    delay *= 2; // Exponential backoff
                }
            }
            throw new Exception("All retries failed.");
        }

        protected async Task SmartClickAsync(string[] selectors, string description)
        {
            await AllureApi.Step($"Starting smart Click for action - '{description}'", async () => // Main step
            {
                // We pass a lambda function that tells the generic engine: "Do a Click"
                await ExecuteSmartAction(selectors, description, async (locator) => 
                {
                    await locator.ClickAsync();
                });
            });
        }

        protected async Task SmartFillAsync(string[] selectors, string value, string description)
        {
            await AllureApi.Step($"Starting smart Fill for action - '{description}'", async () => // Main step
            {
                // We pass a lambda function: "Do a Fill with this value"
                await ExecuteSmartAction(selectors, description, async (locator) => 
                {
                    await locator.FillAsync(value);
                });
            });
        }

        /// <summary>
        /// A generic wrapper that handles Retry Logic, Allure Logging, and Error Handling for ANY Playwright action.
        /// </summary>
        private async Task ExecuteSmartAction(string[] selectors, string description, Func<ILocator, Task> action)
        {
            await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await RetryWithBackoffAsync(async () =>
            {
                foreach (var selector in selectors)
                {
                    AllureApi.Step($"Attempt locator: {selector}"); // Sub-step

                    try
                    {
                        var locator = _page.Locator(selector).First;
                        await locator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 5000 });
                        
                        // Execute the specific action (Click, Fill, etc.) passed from outside
                        await action(locator);

                        // Success! Update report and return immediately
                        AllureApi.Step($"Locator Passed! - {selector}");
                        return;
                    }

                    catch
                    {
                        // Failure! Log it and continue to next locator
                        AllureApi.Step($"Locator Failed! - {selector}. trying the next one...");
                    }
                }

                // If we are here, the loop finished without hitting "return". This means ALL failed.
                // Requirement: Screenshot on final failure [cite: 24]
                await TakeScreenshotAsync($"Failure_{description}");
                throw new Exception($"All smart locators failed for action: {description}");
            });
        }

        public async Task TakeScreenshotAsync(string name)
        {
            var screenshotBytes = await _page.ScreenshotAsync();
            AllureApi.AddAttachment($"{name}", "image/png", screenshotBytes, fileExtension: ".png"); // Direct attach
            Console.WriteLine($"[SCREENSHOT] Attached to Allure: '{name}'");
        }

        protected double ParsePrice(string priceRaw)
        {
            // Removes currency symbols and text, keeping only digits and dots
            var clean = new string(priceRaw.Where(c => char.IsDigit(c) || c == '.').ToArray());
            //return max value if parse fails so it will fail on 'maxPrice' compre
            return double.TryParse(clean, out double result) ? result : double.MaxValue; 
        }
    }
}