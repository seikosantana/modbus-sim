namespace ModbusSim.Models.Configuration;

public class ModbusSettings
{
    public int Port { get; set; } = 502;
    public long ConnectionTimeoutSeconds { get; set; } = 60;
}