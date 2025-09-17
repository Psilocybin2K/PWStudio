# Navigate Command Test Suite

This directory contains comprehensive tests for the Navigate Command functionality in the PlaywrightStudio CQRS system.

## Test Files

### Core Test Files
- **`TestNavigateCommand.cs`** - Basic navigate command tests with direct handler calls
- **`TestNavigateCommandWithCommandBus.cs`** - Tests using the Command Bus for proper CQRS flow
- **`ValidateNavigateCommand.cs`** - Comprehensive validation of the complete command flow
- **`TestRunner.cs`** - Test runner for executing all tests
- **`Program.cs`** - Console application for running tests

### CQRS Infrastructure
- **`ICommandBus.cs`** - Command bus interface
- **`CommandBus.cs`** - Command bus implementation for routing commands to handlers

## Test Coverage

### 1. Basic Functionality Tests
- ✅ Command creation and execution
- ✅ Direct handler invocation
- ✅ Event publishing and subscription
- ✅ State management integration

### 2. Command Bus Tests
- ✅ Command routing through Command Bus
- ✅ Handler resolution and execution
- ✅ Error handling and logging
- ✅ Multiple command execution

### 3. Event Integration Tests
- ✅ Event publishing on command execution
- ✅ Event subscription and handling
- ✅ Event timing and sequencing
- ✅ State change events

### 4. Error Handling Tests
- ✅ Invalid URL handling
- ✅ Uninitialized browser scenarios
- ✅ Null command validation
- ✅ Empty parameter validation

### 5. Complete Flow Validation
- ✅ End-to-end command flow
- ✅ Event sequence validation
- ✅ State progression tracking
- ✅ Performance timing

## Running Tests

### Run All Tests
```bash
dotnet run --project PlaywrightStudio/Commands/Program.cs
```

### Run Specific Test
```bash
dotnet run --project PlaywrightStudio/Commands/Program.cs basic
dotnet run --project PlaywrightStudio/Commands/Program.cs commandbus
dotnet run --project PlaywrightStudio/Commands/Program.cs complete
dotnet run --project PlaywrightStudio/Commands/Program.cs errors
```

### Available Test Types
- **`basic`** - Basic navigate command functionality
- **`eventbus`** - Event bus integration tests
- **`commandbus`** - Command bus integration tests
- **`validation`** - Command validation tests
- **`complete`** - Complete flow validation
- **`errors`** - Error handling validation

## Test Scenarios

### Navigate Command Flow
1. **Initialize Browser** - `InitializeBrowserCommand`
2. **Create Page** - `CreatePageCommand` with optional URL
3. **Navigate to URLs** - Multiple `NavigateToUrlCommand` executions
4. **Close Browser** - `CloseBrowserCommand`

### Expected Events
- `BrowserInitializedEvent` - When browser is ready
- `PageCreatedEvent` - When page is created
- `PageNavigatedEvent` - When page navigates to URL
- `BrowserClosedEvent` - When browser is closed
- `StateChangedEvent` - When state changes

### Expected State Progression
1. **Initial**: Browser=false, Pages=0, URL=null
2. **After Init**: Browser=true, Pages=0, URL=null
3. **After Page**: Browser=true, Pages=1, URL=initial
4. **After Navigate**: Browser=true, Pages=1, URL=target
5. **After Close**: Browser=false, Pages=0, URL=null

## Validation Criteria

### ✅ Command Execution
- Commands are properly routed to handlers
- Handlers execute successfully
- Errors are properly caught and logged

### ✅ Event Publishing
- Events are published for each command
- Event timing is correct
- Event data is accurate

### ✅ State Management
- State changes are tracked
- State updates are atomic
- State is consistent across operations

### ✅ Error Handling
- Invalid inputs are rejected
- Errors are properly logged
- System recovers gracefully

## Performance Metrics

The tests track and validate:
- **Command Processing Time** - Time from command send to completion
- **Event Publishing Time** - Time for events to be published
- **State Update Time** - Time for state changes to be applied
- **Total Flow Time** - End-to-end execution time

## Dependencies

The tests require:
- PlaywrightStudio services registered
- Command and event handlers configured
- Logging infrastructure
- Browser configuration (headless mode for testing)

## Example Output

```
🧪 Testing Navigate Command with Command Bus...
📢 14:30:15.123 - BrowserInitializedEvent
📊 State: Browser=True, Pages=0, URL=null
📢 14:30:15.456 - PageCreatedEvent
📊 State: Browser=True, Pages=1, URL=https://mistral-nemo:7b
📢 14:30:15.789 - PageNavigatedEvent
📊 State: Browser=True, Pages=1, URL=https://httpbin.org
🎉 Navigate Command with Command Bus test completed successfully!
```

This test suite provides comprehensive validation of the Navigate Command functionality within the CQRS architecture, ensuring reliable and observable command execution.
