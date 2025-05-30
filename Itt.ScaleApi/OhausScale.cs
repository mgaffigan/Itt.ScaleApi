﻿using System.IO.Ports;

namespace Itt.ScaleApi;

// Scale to be configured for auto-print
// Default RS232 is 2400 7 N 2
// Preferred is 9600 8 N 1
// Wait for scale to print a value "    53.98 g     "        (Adventurer)
//                              or "       0.01     g     "  (Scout)
// https://dmx.ohaus.com/WorkArea/showcontent.aspx?id=3348
public class OhausScale : IDisposable, IScale
{
    private readonly SerialPortLineReader Port;

    public bool Stable { get; private set; } = false;
    public decimal Weight { get; private set; }
    public event EventHandler<ScaleMeasurementEventArgs>? WeightChanged;

    public OhausScale(SerialPort port, UnhandledExceptionEventHandler? error)
    {
        this.Port = new SerialPortLineReader(port, error, ProcessData);
    }

    private void ProcessData(string data)
    {
        // "    53.98 g     "
        data = data.Trim();
        var unstable = data.EndsWith("?", StringComparison.Ordinal);
        if (unstable) data = data.TrimEnd('?').TrimEnd();
        if (!data.EndsWith(" g"))
        {
            throw new FormatException($"Unexpected response from scale: '{data}'");
        }
        data = data.Substring(0, data.Length - " g".Length);

        if (!decimal.TryParse(data, out var grams))
        {
            throw new FormatException($"Invalid number format from scale: '{data}'");
        }
        this.Stable = !unstable;
        this.Weight = grams;
        this.WeightChanged?.Invoke(this, new ScaleMeasurementEventArgs(grams, !unstable));
    }

    public void Configure()
    {
        // Come out of standby
        Port.WriteLine("ON");
        // Enter weigh mode
        Port.WriteLine("1M");
        // Configure unit: grams
        Port.WriteLine("1U");
        // Enable continuous printing
        Port.WriteLine("CP");
    }

    public void Poll() => Port.Write("P\r\n");
    public void Tare() => Port.Write("T\r\n");

    public void Dispose()
    {
        this.Port.Dispose();
    }

    public static OhausScale Create2400_7N2(string portName, UnhandledExceptionEventHandler? error)
    {
        var port = new SerialPort(portName, 2400, Parity.None, 7, StopBits.Two);
        port.Open();
        try
        {
            return new OhausScale(port, error);
        }
        catch
        {
            port.Dispose();
            throw;
        }
    }

    public static OhausScale Create9600_8N1(string portName, UnhandledExceptionEventHandler? error)
    {
        var port = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
        port.Open();
        try
        {
            return new OhausScale(port, error);
        }
        catch
        {
            port.Dispose();
            throw;
        }
    }
}