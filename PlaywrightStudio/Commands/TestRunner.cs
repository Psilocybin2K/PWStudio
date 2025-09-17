using Microsoft.Extensions.Logging;

namespace PlaywrightStudio.Commands;

/// <summary>
/// Test runner for navigate command tests
/// </summary>
public class TestRunner
{
    /// <summary>
    /// Runs all navigate command tests
    /// </summary>
    public static async Task RunAllTestsAsync()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<TestRunner>();
        
        try
        {
            Console.WriteLine("🚀 Starting Navigate Command Test Suite...");
            Console.WriteLine(new string('=', 60));
            
            // Test 1: Basic navigate command test
            Console.WriteLine("\n📋 Test 1: Basic Navigate Command Test");
            Console.WriteLine(new string('-', 40));
            await TestNavigateCommand.TestNavigateCommandAsync();
            
            // Test 2: Navigate command with event bus
            Console.WriteLine("\n📋 Test 2: Navigate Command with Event Bus");
            Console.WriteLine(new string('-', 40));
            await TestNavigateCommand.TestNavigateCommandWithEventBusAsync();
            
            // Test 3: Navigate command with command bus
            Console.WriteLine("\n📋 Test 3: Navigate Command with Command Bus");
            Console.WriteLine(new string('-', 40));
            await TestNavigateCommandWithCommandBus.TestNavigateCommandWithCommandBusAsync();
            
            // Test 4: Command validation
            Console.WriteLine("\n📋 Test 4: Command Validation");
            Console.WriteLine(new string('-', 40));
            await TestNavigateCommandWithCommandBus.TestCommandValidationAsync();
            
            // Test 5: Complete flow validation
            Console.WriteLine("\n📋 Test 5: Complete Flow Validation");
            Console.WriteLine(new string('-', 40));
            await ValidateNavigateCommand.ValidateCompleteFlowAsync();
            
            // Test 6: Error handling validation
            Console.WriteLine("\n📋 Test 6: Error Handling Validation");
            Console.WriteLine(new string('-', 40));
            await ValidateNavigateCommand.ValidateErrorHandlingAsync();
            
            Console.WriteLine("\n🎉 All tests completed successfully!");
            Console.WriteLine(new string('=', 60));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Test suite failed");
            Console.WriteLine($"\n❌ Test suite failed: {ex.Message}");
        }
        finally
        {
            loggerFactory.Dispose();
        }
    }
    
    /// <summary>
    /// Runs a specific test
    /// </summary>
    /// <param name="testName">Name of the test to run</param>
    public static async Task RunSpecificTestAsync(string testName)
    {
        Console.WriteLine($"🧪 Running specific test: {testName}");
        Console.WriteLine(new string('-', 40));
        
        switch (testName.ToLowerInvariant())
        {
            case "basic":
                await TestNavigateCommand.TestNavigateCommandAsync();
                break;
            case "eventbus":
                await TestNavigateCommand.TestNavigateCommandWithEventBusAsync();
                break;
            case "commandbus":
                await TestNavigateCommandWithCommandBus.TestNavigateCommandWithCommandBusAsync();
                break;
            case "validation":
                await TestNavigateCommandWithCommandBus.TestCommandValidationAsync();
                break;
            case "complete":
                await ValidateNavigateCommand.ValidateCompleteFlowAsync();
                break;
            case "errors":
                await ValidateNavigateCommand.ValidateErrorHandlingAsync();
                break;
            default:
                Console.WriteLine($"❌ Unknown test: {testName}");
                Console.WriteLine("Available tests: basic, eventbus, commandbus, validation, complete, errors");
                break;
        }
    }
}
