using Microsoft.Playwright;
using Microsoft.SemanticKernel;
using PlaywrightStudio.Models;

namespace PlaywrightStudio.Interfaces;

/// <summary>
/// Factory interface for creating KernelPlugin instances from PageObjectModel
/// </summary>
public interface IPluginFactory
{
    /// <summary>
    /// Creates a KernelPlugin from a PageObjectModel and IPage instance
    /// </summary>
    /// <param name="model">The page object model to create a plugin from</param>
    /// <param name="page">The Playwright page instance</param>
    /// <returns>A configured KernelPlugin</returns>
    KernelPlugin CreatePagePlugin(PageObjectModel model, IPage page);

    /// <summary>
    /// Creates multiple plugins from a collection of models
    /// </summary>
    /// <param name="models">Collection of page object models</param>
    /// <param name="page">The Playwright page instance</param>
    /// <returns>Collection of configured KernelPlugins</returns>
    IEnumerable<KernelPlugin> CreatePagePlugins(IEnumerable<PageObjectModel> models, IPage page);
}
