using PlaywrightStudio.Commands;

namespace PlaywrightStudio;

/// <summary>
/// Test runner for the PlaywrightStudio application
/// </summary>
public class TestRunner
{
    /// <summary>
    /// Runs all navigate command tests
    /// </summary>
    public static async Task RunNavigateCommandTestsAsync()
    {
        Console.WriteLine("🧪 PlaywrightStudio Navigate Command Test Suite");
        Console.WriteLine("===============================================");
        
        try
        {
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
            
            Console.WriteLine("\n🎉 All navigate command tests completed successfully!");
            Console.WriteLine(new string('=', 60));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Test suite failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
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
        
        try
        {
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
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Test failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}
