# eBay E2E Automation Framework (Playwright C#)

Senior Automation Engineer Take-Home Test


## Overview

End-to-end automation for eBay:
- Search items under a price limit
- Add items to cart (with variant selection)
- Verify cart total does not exceed budget

Built with **Playwright .NET**, **NUnit**, **Allure Reports**.


## Architecture

`BasePage`: SmartClick/SmartFill, retry, screenshot attachment to Allure
Page classes: Business logic only
Test: Clean E2E flow with Allure steps


## Features

- **Page Object Model (POM)** with clean separation
- **Smart locators** with multiple fallbacks and retry logic (exponential backoff)
- **Resilience**: Handles flaky elements, timeouts, graceful screenshots
- **Data-Driven**: Configuration via `test-settings.json`
- **Allure Reports**: Rich steps, parameters, screenshots
- **Parallel Ready**: `[Parallelizable]`
- **Cross-Browser Ready**: Multiple .runsettings files (Chromium/Firefox/WebKit)
- **Moon / Cloud Grid Ready**: Remote connection example
- **CI/CD Ready**: GitHub Actions example


## Cloud & Parallel Execution

This project is designed for scalable execution in cloud and CI/CD environments.

### Cross-Browser Testing

Run the same test on different browsers using dedicated .runsettings files:
```bash
dotnet test --settings EbayAutomation/chromium.runsettings
dotnet test --settings EbayAutomation/firefox.runsettings
dotnet test --settings EbayAutomation/webkit.runsettings
```

### Moon / Cloud Grid Support

Remote execution ready via Playwright's `ConnectAsync` (see commented example in `EbayE2ETests.cs`).
To run on Moon:
```Bash
export REMOTE_BROWSER_URL=wss://your-moon-instance.aerokube.com/playwright/chromium
dotnet test
```

### CI/CD Integration Example (GitHub Actions)
```YAML
# .github/workflows/playwright.yml
name: Playwright E2E Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - name: Install Playwright browsers
        run: dotnet build && npx playwright install --with-deps
      - name: Run Tests (headless)
        run: dotnet test --settings EbayAutomation/normal.runsettings
      - name: Upload Allure Results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: allure-results
          path: allure-results
```
Allure reporting can be extended with dedicated actions for HTML reports.


## How to Run

```bash
# Install browsers
dotnet build
npx playwright install

# Demo mode (visible browser + slow motion - for presentation)
dotnet test --settings EbayAutomation/demo.runsettings

# Normal/CI mode (headless) on chromium
dotnet test --settings EbayAutomation/.runsettings

# View Allure report
allure serve bin/Debug/USER_NET_VERSION/allure-results
```


### Notes
- Guest mode (no login)
- Anti-bot avoidance: SlowMo + human-like delays in demo mode **but still not 100%**
- Currency parsing supports ILS (or local currency) with getting the approx' values after convertion
- Tests are not passing for all search results, Ebay is very dynemic and bot proof
- Allure CLI was not installed on local PC (hardware/usage limitations) but Allure logs are present after run under debug folder

