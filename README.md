Aurora SDK is a modular, block-based framework for building algorithmic trading strategies on top of NinjaTrader 8’s NinjaScript engine. It focuses on separating signals, risk, and execution into reusable components that can be configured via YAML, so multiple developers can share and extend the same core pipeline.

## Features

- Built on `NinjaTrader.NinjaScript.Strategies.Strategy` in NinjaScript, integrates with standard events like `Strategy.OnBarUpdate()`, `Strategy.OnExecutionUpdate()`, `Strategy.OnOrderUpdate()`, and `Strategy.OnPositionUpdate()`.
- Block-based architecture via `SDK.LogicBlock` objects, each with a `SDK.BlockType` and `SDK.BlockSubType` (Signal, Risk, Update, Execution, Regime).
- Runtime configuration through YAML files (no recompiles to change logic wiring).
- Dedicated engines for signal, risk, execution, and updates.

## How It Works With NinjaScript

Aurora SDK provides an abstract base class:

```csharp
public abstract partial class AuroraStrategy : Strategy
{
    protected abstract void Register();
}
```

You inherit from `AuroraStrategy` instead of `Strategy`. In `Register()`, you register your custom LogicBlocks. The base class wires into NinjaTrader’s lifecycle:

- `OnStateChange` at `State.DataLoaded`:
  - Loads your YAML config (`CFGPATH`).
  - Builds all LogicBlocks.
  - Initializes `SignalEngine`, `RiskEngine`, `UpdateEngine`, and `ExecutionEngine`.

- `OnBarUpdate`:
  - SignalEngine → produces a direction (Long/Short/Flat).
  - RiskEngine → produces a size and any extra risk metadata.
  - UpdateEngine → runs update blocks for bar-level state.
  - ExecutionEngine → submits orders via `EnterLong` / `EnterShort`.

The strategy still has full access to all NinjaTrader data, indicators, drawing tools, and orders via the base `Strategy` methods and properties.

## Configuration (YAML)

Strategies are described in YAML, so logic wiring is externalized:

```yaml
Logic:
  - BID: "MySignal"
    BType: "Signal"
    BSubType: "Signal"
    BParameters:
      Period: 20
      Threshold: 0.5

  - BID: "MyRiskMul"
    BType: "Risk"
    BSubType: "Multiplier"
    BParameters:
      Factor: 1.5
```

At load time, the SDK:

- Deserializes the YAML.
- Creates each LogicBlock using a registry keyed by `BID`.
- Sorts blocks by type/subtype and runs them through the engines.

## Core Engines

### SignalEngine

- Uses only `BlockTypes.Signal` blocks.
- Subtypes:
  - `Bias`: high-level directional bias.
  - `Signal`: tactical entry signal.
  - `Filter`: boolean filter; if any filter is true, trading is skipped for that bar.
- Rule: only trades when Bias and Signal agree (both Long or both Short); otherwise stays Flat.

### RiskEngine

- Uses `BlockTypes.Risk` blocks.
- Subtypes:
  - `Multiplier`: returns a double factor to scale position size from `BASECONTRACTS`.
  - `Limit`: returns an int max position size.
  - `Extra`: pushes arbitrary key/value outputs into a dictionary for downstream use.
- Final size = floor(BASECONTRACTS × all multipliers), capped by the smallest Limit.

### ExecutionEngine

- Consumes `SignalProduct` and `RiskProduct`.
- Places entries using `EnterLong` / `EnterShort` with descriptive signal names.
- Calls `Entry`-subtype LogicBlocks so you can modularize stop-loss / targets / bracket logic.

### UpdateEngine

- Runs on:
  - `OnBarUpdate`
  - `OnExecutionUpdate`
  - `OnOrderUpdate`
  - `OnPositionUpdate`
- Routes those events to `BlockTypes.Update` blocks, so you can centralize state tracking and logging.

## Creating Your Own Strategy

1. **Inherit from AuroraStrategy**

```csharp
public class MyAlgo : AuroraStrategy
{
    protected override void Register()
    {
        LogicBlockRegistry.Register("MySignal",
            (host, parameters) => new MySignalBlock(host, parameters));

        LogicBlockRegistry.Register("MyRiskMul",
            (host, parameters) => new MyRiskMultiplierBlock(host, parameters));
    }
}
```

2. **Implement a LogicBlock**

```csharp
public class MySignalBlock : LogicBlock
{
    public MySignalBlock(AuroraStrategy host, Dictionary<string, object> parameters)
    {
        Initialize(host, new BlockConfig
        {
            BlockId = "MySignal",
            BlockType = BlockTypes.Signal,
            BlockSubType = BlockSubTypes.Signal,
            Parameters = parameters
        });
    }

    public override LogicTicket Forward(Dictionary<string, object> inputs)
    {
        // Example: compute a Long/Short/Flat MarketPosition
        var direction = MarketPosition.Flat;
        // use _host.Close, indicators, etc. to decide direction
        return new LogicTicket
        {
            BlockId = Id,
            Values = new List<object> { direction }
        };
    }
}
```

3. **Set up the YAML config and parameters in NinjaTrader**

- Copy your YAML file somewhere accessible.
- In the strategy parameters:
  - `CFGPATH`: full path to the YAML file.
  - `BASECONTRACTS`: base position size.
  - `DEBUG`: enable/disable debug prints.

## Design Guidelines

- Keep each block focused on a single job (one calculation or decision).
- Use the `Guard` and `Safe` helpers for validation and safe type conversion.
- Prefer YAML wiring over hard-coded orchestration, so strategies are easy to share and version.
- Use `DEBUG` mode sparingly to avoid performance hits in high-frequency environments.

## License and Contributions

This SDK is intended to be open sourced and shared between multiple developers. Contributions are welcome in the form of:

- New LogicBlocks (signals, risk models, execution patterns).
- Additional YAML examples.
- Documentation and usage patterns for different markets or bar types.
