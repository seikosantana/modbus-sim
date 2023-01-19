# ModbusSim
A cross-platform Modbus server (slave) simulator with configurable rules for
changing register values periodically for modbus-based development testing purposes.

## Configuration
One purpose of this simple simulator is to simulate changing values on the server-side.
Configuration is done in the `appsettings.json` file.

### Modbus Settings
The `ModbusSettings` key in the JSON configures which port the server will be listening at,
and how many seconds an idle connection is timed-out.
```json
"ModbusSettings": {
    "Port": 5502,
    "ConnectionTimeoutSeconds": 60
}
```

### Rules
The `Rules` key in the JSON configures how holding registers will change values.
The `SimpleIncrementRules` is the only type of rule implemented for now, where
the register range defined between`StartReg` and `EndReg` inclusively will be
incremented until the `MaxValue`, and then will be reset to its `InitialValue`.
`EndReg` is optional. When `EndReg` is not defined, the rule applies only to the
register `StartReg`.

```json
"Rules": {
    "SimpleIncrementRules": [
        {
            "StartReg": 1,
            "EndReg": 10,
            "InitialValue": 0,
            "MaxValue": 4,
            "DelaySeconds": 5
        },
        {
            "StartReg": 11,
            "EndReg": 20,
            "InitialValue": 0,
            "MaxValue": 2,
            "DelaySeconds": 8
        }
    ]
},
```

More type of rules will be implemented in the future, if necessary.

## Tips and Notes
- If `EndReg` is not defined, rule applies only to a register `StartReg`.
- If a register value should only be initialized, but not changing, set `MaxValue`
as in `InitialValue`
- The default configuration has `Port` set to 5502 to avoid `Permission Denied`. Most
modbus servers listens on port 502.
- Rules are checked and validated when ModbusSim starts. Any other rule validation 
details can be seen on outputs or logs.

