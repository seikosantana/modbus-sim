using System.Net;
using FluentModbus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModbusSim.Models.Configuration;

namespace ModbusSim.Services;

public class ModbusSlaveService : BackgroundService
{
    public Rules Rules { get; }
    public ILogger<ModbusSlaveService> Logger { get; }
    public IHostApplicationLifetime Host { get; }
    private ModbusTcpServer Slave { get; }
    private ModbusSettings ModbusSettings { get; }

    public ModbusSlaveService(IOptions<ModbusSettings> modbusSettingsOptions,
        IOptions<Rules> rules,
        ILogger<ModbusSlaveService> logger,
        IHostApplicationLifetime host)
    {
        Rules = rules.Value;
        Logger = logger;
        Host = host;
        ModbusSettings = modbusSettingsOptions.Value;
        Slave = new ModbusTcpServer(logger)
        {
            ConnectionTimeout = TimeSpan.FromSeconds(ModbusSettings.ConnectionTimeoutSeconds)
        };
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            Slave.Start(new IPEndPoint(IPAddress.Any, ModbusSettings.Port));
            Logger.LogInformation("Modbus Server Started on port {Port}", ModbusSettings.Port);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Unable to start Modbus Server and see logs for more details");
            Logger.LogCritical("Stopping application as server is unable to start");
            Host.StopApplication();
        }

        return base.StartAsync(cancellationToken);
    }

    void RunIncrementalRule(SimpleIncrementRule rule)
    {
        Span<short> holdingRegisters = Slave.GetHoldingRegisters();
        if (!rule.EndReg.HasValue)
        {
            short oldValue = holdingRegisters[rule.StartReg];
            short newValue;
            if (oldValue == rule.MaxValue)
            {
                newValue = rule.InitialValue;
            }
            else
            {
                newValue = (short)(oldValue + 1);
            }
            holdingRegisters.SetBigEndian(rule.StartReg, newValue);
        }
        else
        {
            for (int reg = rule.StartReg; reg <= rule.EndReg.Value; reg++)
            {
                short oldValue = holdingRegisters[reg];
                short newValue = (short)((oldValue + 1) % (rule.MaxValue + 1));
                holdingRegisters.SetBigEndian(reg, newValue);
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (Rules.SimpleIncrementRules is null || Rules.SimpleIncrementRules.Count <= 0)
        {
            Logger.LogWarning("There are no rules specified in appsettings.json therefore the application will" +
                              "only serve as a Modbus TCP Server simulator");
        }
        else
        {
            Logger.LogInformation("{RuleCount} rule(s) found", Rules.SimpleIncrementRules.Count);
            Logger.LogInformation("Checking rules");

            bool anyRuleInvalid = false;

            for (int index = 0; index < Rules.SimpleIncrementRules.Count; index++)
            {
                SimpleIncrementRule rule = Rules.SimpleIncrementRules[index];
                if (rule.DelaySeconds <= 0)
                {
                    Logger.LogError("At rule {RuleNumber}: Rules cannot have 0 wait seconds", index + 1);
                    anyRuleInvalid = true;
                }

                if (rule.StartReg <= 0)
                {
                    Logger.LogError("At rule {RuleNumber}: StartReg cannot be less than 0", index + 1);
                    anyRuleInvalid = true;
                }

                if (rule.EndReg.HasValue && rule.EndReg.Value <= rule.StartReg)
                {
                    Logger.LogError("At rule {RuleNumber}: EndReg has to be greater than StartReg if defined",
                        index + 1);
                    anyRuleInvalid = true;
                }

                if (rule.MaxValue < rule.InitialValue)
                {
                    Logger.LogError("At rule {RuleNumber}: MaxValue has to be greater than InitialValue",
                        index + 1);
                    anyRuleInvalid = true;
                }
            }

            if (anyRuleInvalid)
            {
                Logger.LogCritical("There are invalid rules. Application will not continue");
                Host.StopApplication();
            }
            else
            {
                Logger.LogInformation("Rule checking passed");
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                foreach (SimpleIncrementRule rule in Rules.SimpleIncrementRules)
                {
                    RunIncrementalRule(rule);
                    await Task.Delay(TimeSpan.FromSeconds(rule.DelaySeconds), stoppingToken).ConfigureAwait(false);
                }
            }
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogWarning("Stopping Modbus Server");
        Slave.Stop();
        Slave.Dispose();
        return base.StopAsync(cancellationToken);
    }
}