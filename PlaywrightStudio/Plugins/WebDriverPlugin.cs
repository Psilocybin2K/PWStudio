using System.ComponentModel;
using Microsoft.SemanticKernel;
using PlaywrightStudio.Commands;
using PlaywrightStudio.Interfaces;

namespace PlaywrightStudio.Plugins;

public class WebDriverPlugin(IPlaywrightManagementService playwrightManagementService, ICommandBus commandBus)
{
    [KernelFunction, Description("Navigates the browser to the specified URL.")]
    public async Task GoToUrlAsync(
        [Description("The URL to navigate to.")] string url)
    {
        await commandBus.SendAsync(new NavigateToUrlCommand(url));
    }

    [KernelFunction, Description("Hovers the mouse over the element specified by the CSS selector.")]
    public async Task HoverAsync(
        [Description("The CSS selector of the element to hover over.")] string cssPath)
    {
        var activePage = playwrightManagementService.ActivePage;
        
        if (activePage == null)
        {
            throw new InvalidOperationException("No active page available. Create a page first.");
        }
        
        await activePage.HoverAsync(cssPath);
    }

    [KernelFunction, Description("Clicks the element specified by the CSS selector.")]
    public async Task ClickAsync(
        [Description("The CSS selector of the element to click.")] string cssPath)
    {
        var activePage = playwrightManagementService.ActivePage;
        if (activePage == null)
        {
            throw new InvalidOperationException("No active page available. Create a page first.");
        }
        
        await activePage.ClickAsync(cssPath);
    }

    [KernelFunction, Description("Double-clicks the element specified by the CSS selector.")]
    public async Task DoubleClickAsync(
        [Description("The CSS selector of the element to double-click.")] string cssPath)
    {
        var activePage = playwrightManagementService.ActivePage;
        if (activePage == null)
        {
            throw new InvalidOperationException("No active page available. Create a page first.");
        }
        
        await activePage.DblClickAsync(cssPath);
    }

    [KernelFunction, Description("Sends a key to the element specified by the CSS selector.")]
    public async Task SendKeyAsync(
        [Description("The key to send.")] string key,
        [Description("The CSS selector of the element to send the key to.")] string cssPath)
    {
        var activePage = playwrightManagementService.ActivePage;
        if (activePage == null)
        {
            throw new InvalidOperationException("No active page available. Create a page first.");
        }
        
        await activePage.PressAsync(cssPath, key);
    }

    [KernelFunction, Description("Drags the element specified by the source CSS selector to the target CSS selector.")]
    public async Task DragToElementAsync(
        [Description("The CSS selector of the source element to drag.")] string sourceCssPath,
        [Description("The CSS selector of the target element to drop onto.")] string targetCssPath)
    {
        var activePage = playwrightManagementService.ActivePage;
        if (activePage == null)
        {
            throw new InvalidOperationException("No active page available. Create a page first.");
        }
        
        await activePage.DragAndDropAsync(sourceCssPath, targetCssPath);
    }

    [KernelFunction, Description("Enters text into the element specified by the CSS selector.")]
    public async Task EnterTextAsync(
        [Description("The CSS selector of the element to enter text into.")] string cssPath,
        [Description("The text to enter.")] string text)
    {
        var activePage = playwrightManagementService.ActivePage;
        if (activePage == null)
        {
            throw new InvalidOperationException("No active page available. Create a page first.");
        }
        
        await activePage.FillAsync(cssPath, text);
    }

    [KernelFunction, Description("Selects an option from a dropdown element specified by the CSS selector.")]
    public async Task SelectOptionAsync(
        [Description("The CSS selector of the dropdown element.")] string cssPath,
        [Description("The option to select.")] string option)
    {
        var activePage = playwrightManagementService.ActivePage;
        if (activePage == null)
        {
            throw new InvalidOperationException("No active page available. Create a page first.");
        }
        
        await activePage.SelectOptionAsync(cssPath, option);
    }

    [KernelFunction, Description("Waits until the element specified by the CSS selector is visible.")]
    public async Task WaitForElementVisibleAsync(
        [Description("The CSS selector of the element to wait for.")] string cssPath)
    {
        var activePage = playwrightManagementService.ActivePage;
        if (activePage == null)
        {
            throw new InvalidOperationException("No active page available. Create a page first.");
        }
        
        await activePage.WaitForSelectorAsync(cssPath, new Microsoft.Playwright.PageWaitForSelectorOptions { State = Microsoft.Playwright.WaitForSelectorState.Visible });
    }

    [KernelFunction, Description("Waits until the element specified by the CSS selector is hidden.")]
    public async Task WaitForElementHiddenAsync(
        [Description("The CSS selector of the element to wait for.")] string cssPath)
    {
        var activePage = playwrightManagementService.ActivePage;

        if (activePage == null)
        {
            throw new InvalidOperationException("No active page available. Create a page first.");
        }
        
        await activePage.WaitForSelectorAsync(cssPath, new Microsoft.Playwright.PageWaitForSelectorOptions { State = Microsoft.Playwright.WaitForSelectorState.Hidden });
    }

    [KernelFunction, Description("Waits until the page URL matches the specified URL.")]
    public async Task WaitForUrlAsync(
        [Description("The URL to wait for.")] string url)
    {
        var activePage = playwrightManagementService.ActivePage;
        if (activePage == null)
        {
            throw new InvalidOperationException("No active page available. Create a page first.");
        }
        
        await activePage.WaitForURLAsync(url);
    }

    [KernelFunction, Description("Waits until the network is idle.")]
    public async Task WaitForNetworkIdleAsync()
    {
        var activePage = playwrightManagementService.ActivePage;
        if (activePage == null)
        {
            throw new InvalidOperationException("No active page available. Create a page first.");
        }
        
        await activePage.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle);
    }
}