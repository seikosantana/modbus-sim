namespace ModbusSim.Models.Configuration;

public class SimpleIncrementRule
{
    public short StartReg { get; set; }
    public short? EndReg { get; set; }
    
    public short InitialValue { get; set; }
    public short MaxValue { get; set; }
    public long DelaySeconds { get; set; }
}