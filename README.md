# Aurora SDK

**A modular, block-based framework for building algorithmic trading strategies in NinjaTrader 8**[1]

Aurora SDK provides a composable architecture that separates trading logic into discrete, reusable components (LogicBlocks) managed by specialized engines for signal generation, risk management, execution, and state updates.[2][3][4]

## Overview

Aurora SDK extends NinjaTrader's NinjaScript Strategy framework through an abstract base class `AuroraStrategy` that orchestrates four core engines:[1]

- **SignalEngine**: Processes bias, signal, and filter blocks to generate directional trading decisions
- **RiskEngine**: Calculates position sizing using multipliers, limits, and custom risk parameters
- **ExecutionEngine**: Handles order placement and entry logic
- **UpdateEngine**: Manages strategy lifecycle hooks (bar updates, execution updates, order updates, position updates)

Strategies are configured via YAML files that define LogicBlock chains, enabling rapid prototyping and A/B testing without code changes.[5]

## Architecture

### NinjaScript Integration

Aurora SDK integrates with NinjaTrader 8's NinjaScript engine by inheriting from `Strategy` and implementing the standard lifecycle methods:[1]

- `OnStateChange()`: Initializes engines during `State.DataLoaded` after calling the abstract `Register()` method
- `OnBarUpdate()`: Evaluates signal and risk engines, then executes trades
- `OnExecutionUpdate()`, `OnOrderUpdate()`, `OnPositionUpdate()`: Delegate to UpdateEngine for custom handling

The SDK maintains full access to NinjaTrader's built-in indicators, drawing tools, data series, and order management through the inherited `Strategy` base class.[1]

### LogicBlock System

LogicBlocks are the fundamental building units of trading logic. Each block:[6]

- Implements the abstract `LogicBlock` class with a `Forward()` method that returns a `LogicTicket`
- Declares a `BlockType` (Signal, Risk, Update, Execution, Regime) and `BlockSubType` for specialized behavior
- Receives initialization parameters via `Dictionary<string, object>`
- Maintains a reference to the host strategy for accessing market data and indicators

**Example LogicBlock Structure:**
```csharp
public abstract class LogicBlock
{
    internal AuroraStrategy _host;
    public BlockTypes Type { get; private set; }
    public BlockSubTypes SubType { get; private set; }
    public Dictionary<string, object> Parameters { get; private set; }
    
    public abstract LogicTicket Forward(Dictionary<string, object> inputs);
}
```

Blocks are instantiated via the `LogicBlockRegistry` and `LogicBlockFactory` pattern, enabling runtime registration and dependency injection.[5]

### Engine Pipeline

The SDK processes each bar through a sequential engine pipeline:[1]

1. **SignalEngine.Evaluate()** → Returns `SignalProduct` (Direction, OrderType, Name)[2]
2. **RiskEngine.Evaluate()** → Returns `RiskProduct` (Size, MiscValues)[4]
3. **UpdateEngine.Update()** → Executes custom bar update logic[7]
4. **ExecutionEngine.Execute()** → Places orders based on signal and risk products[3]

Each engine filters LogicBlocks by `BlockType` and processes them according to their `BlockSubType`.[4][2]

## Configuration System

Strategies are defined in YAML configuration files loaded at runtime:[5]

```yaml
Logic:
  - BID: "MySignalBlock"
    BType: "Signal"
    BSubType: "Signal"
    BParameters:
      Period: 14
      Threshold: 0.5
    Debug:
      Enabled: true
      Type: "Verbose"
```

The configuration system:
- Deserializes YAML using YamlDotNet[5]
- Validates block types and subtypes during initialization[2][4]
- Passes parameters as strongly-typed dictionaries to LogicBlock constructors[5]
- Supports multi-developer workflows by isolating strategy configuration from core SDK code[5]

## Core Components

### SignalEngine

The SignalEngine reconciles three types of signal blocks:[2]

- **Bias blocks**: Emit long-term directional preference (e.g., trend filter)
- **Signal blocks**: Emit tactical entry signals (e.g., indicator crossover)
- **Filter blocks**: Return boolean to suppress trading (e.g., volatility filter)

**Logic Flow:**
1. Evaluate all filter blocks first—if any return `true`, immediately return `Flat` signal
2. Collect bias and signal from respective blocks (last valid assignment wins)
3. Only generate Long/Short signal when bias and signal **both agree**
4. Otherwise return `Neutral Bias` flat signal

### RiskEngine

The RiskEngine computes position size through three block subtypes:[4]

- **Multiplier blocks**: Return `double` factors applied to base contracts (e.g., Kelly multiplier)
- **Limit blocks**: Return `int` maximum position size (e.g., max risk limit)
- **Extra blocks**: Store arbitrary key-value pairs in `RiskProduct.MiscValues`

**Calculation:**
```csharp
finalSize = floor(BASECONTRACTS * multiplier1 * multiplier2 * ...)
if (finalSize > limit) finalSize = limit
```

### ExecutionEngine

The ExecutionEngine translates signal and risk products into NinjaTrader orders:[3]

- Calls `EnterLong()` or `EnterShort()` with computed position size
- Passes entry order and metadata to Entry subtype blocks for stop-loss/target placement
- Assigns unique signal names for order tracking

### UpdateEngine

The UpdateEngine routes NinjaScript lifecycle events to Update blocks:[7]

- Maps `OnBarUpdate()`, `OnExecutionUpdate()`, `OnOrderUpdate()`, `OnPositionUpdate()` to `UpdateTypes` enum
- Forwards calls to all registered Update blocks
- Enables custom logging, state tracking, or indicator recalculations

## Helper Utilities

The SDK provides two static utility classes for safe operations:[1]

**Guard**: Validates preconditions with descriptive exceptions
- `Guard.NotNull()`: Null checks with parameter name
- `Guard.Require()`: Custom assertion logic

**Safe**: Type-safe conversions without exceptions
- `Safe.TryToMarketPosition()`: Converts `int`, `string`, or `MarketPosition` enum
- `Safe.TryToDouble()`, `Safe.TryToInt()`, `Safe.TryToBool()`: Flexible type coercion
- `Safe.TryGetAt()`: Bounds-checked list access

## Quick Start

### 1. Create a Custom Strategy

```csharp
using NinjaTrader.Custom.Strategies.Aurora.SDK;

public class MyAlgoStrategy : AuroraStrategy
{
    protected override void Register()
    {
        // Register your custom LogicBlocks
        LogicBlockRegistry.Register("MySignalBlock", 
            (host, params) => new MySignalBlock(host, params));
    }
}
```

### 2. Implement a LogicBlock

```csharp
public class MySignalBlock : LogicBlock
{
    private int _period;
    
    public MySignalBlock(AuroraStrategy host, Dictionary<string, object> parameters)
    {
        Initialize(host, new BlockConfig
        {
            BlockId = "MySignalBlock",
            BlockType = BlockTypes.Signal,
            BlockSubType = BlockSubTypes.Signal,
            Parameters = parameters
        });
        
        _period = Convert.ToInt32(parameters["Period"]);
    }
    
    public override LogicTicket Forward(Dictionary<string, object> inputs)
    {
        // Your signal logic using _host.Close, _host.SMA(), etc.
        MarketPosition direction = /* calculate signal */;
        
        return new LogicTicket
        {
            BlockId = Id,
            Values = new List<object> { direction }
        };
    }
}
```

### 3. Configure the Strategy

Create `config.yaml`:
```yaml
Logic:
  - BID: "MySignalBlock"
    BType: "Signal"
    BSubType: "Signal"
    BParameters:
      Period: 20
```

### 4. Add to NinjaTrader

1. Compile the strategy in NinjaScript Editor
2. Add to chart with `CFGPATH` pointing to your YAML file
3. Set `BASECONTRACTS` and enable `DEBUG` mode for logging

## Development Guidelines

### Block Design Patterns

- **Single Responsibility**: Each block should perform one calculation (e.g., ATR multiplier, trend filter)
- **Stateless Forward()**: Avoid instance state between calls—use strategy properties or indicators
- **Flexible Outputs**: Return `object` lists in `LogicTicket.Values` for polymorphic consumption
- **Parameter Validation**: Use `Guard` and `Safe` helpers to validate inputs in constructors

### Error Handling

All engines catch and log exceptions with context:[3][4][2]

```csharp
try 
{
    ticket = lb.Forward([]);
}
catch (Exception ex)
{
    _host.ATDebug($"Engine: Forward() failed for {lb.Id}. {ex}", LogMode.Log, LogLevel.Error);
    throw;
}
```

Use `ATDebug()` with appropriate `LogMode` (Log, Print, Debug) and `LogLevel`.[1]

### Testing Strategy

1. **Unit Test Blocks**: Test `Forward()` logic with mock host and parameters
2. **Integration Test Engines**: Verify engine pipelines with sample block configurations
3. **Backtest Validation**: Run strategies on historical data with debug logging enabled
4. **Live Simulation**: Test on NinjaTrader Sim101 before live deployment

## Advanced Features

### Custom Engine Extensions

Extend engines by creating additional BlockSubTypes:[6]

```csharp
public enum BlockSubTypes
{
    // ... existing types
    MyCustomSubType  // Add new subtype
}
```

Then handle in the appropriate engine's evaluation loop.[4][2]

### Multi-Instrument Strategies

Access additional data series through NinjaTrader's `AddDataSeries()` in the `Configure` state, then reference via `BarsArray` in LogicBlocks.[1]

### Performance Optimization

- Minimize `Forward()` calls by caching indicator values
- Use `BarsInProgress` checks to avoid redundant calculations
- Leverage NinjaTrader's indicator repository instead of reimplementing calculations

## Dependencies

- **NinjaTrader 8**: Core trading platform and NinjaScript framework
- **YamlDotNet**: YAML configuration deserialization[5]
- **C# 8.0+**: Requires null-coalescing, collection expressions, pattern matching

## Contributing

To contribute custom LogicBlocks or engines:

1. Fork the repository
2. Create blocks in the `NinjaTrader.Custom.Strategies.Aurora.SDK` namespace
3. Add unit tests for `Forward()` logic
4. Submit pull request with sample YAML configuration
5. Document block parameters and expected outputs

## License

This SDK is open source for collaborative algorithmic trading development.[1]
