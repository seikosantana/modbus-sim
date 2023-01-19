using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModbusSim.Models.Configuration;
using ModbusSim.Services;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) => {
        services
            .Configure<ModbusSettings>(context.Configuration.GetSection(nameof(ModbusSettings)))
            .Configure<Rules>(context.Configuration.GetSection(nameof(Rules)))
            .AddHostedService<ModbusSlaveService>();
    })
    .Build();

await host.RunAsync();