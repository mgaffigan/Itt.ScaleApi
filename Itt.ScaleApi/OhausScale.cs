using System.IO.Ports;

namespace Itt.ScaleApi;

// Scale to be configured for auto-print
// Default RS232 is 2400 7 N 2
// Preferred is 9600 8 N 1
// Wait for scale to print a value "    53.98 g     "
// https://dmx.ohaus.com/WorkArea/showcontent.aspx?id=3348
public class OhausScale : IDisposable, IScale
{
    private readonly SerialPort Port;
    private readonly UnhandledExceptionEventHandler? Error;

    public bool Stable { get; private set; } = false;
    public decimal Weight { get; private set; }
    public event EventHandler<ScaleMeasurementEventArgs>? WeightChanged;

    public OhausScale(SerialPort port, UnhandledExceptionEventHandler? error)
    {
        this.Port = port;
        this.Error = error;
        this.Port.DataReceived += Port_DataReceived;
    }

    private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            // "    53.98 g     "
            var data = Port.ReadExisting().Trim();
            if (!data.EndsWith(" g"))
            {
                throw new FormatException($"Unexpected response from scale: '{data}'");
            }
            data = data.Substring(0, data.Length - " g".Length);

            if (!decimal.TryParse(data, out var grams))
            {
                throw new FormatException($"Invalid number format from scale: '{data}'");
            }
            this.Stable = true;
            this.Weight = grams;
            this.WeightChanged?.Invoke(this, new ScaleMeasurementEventArgs(grams, true));
        }
        catch (Exception ex)
        {
            Error?.Invoke(this, new UnhandledExceptionEventArgs(ex, false));
        }
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