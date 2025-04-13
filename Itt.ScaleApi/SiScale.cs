using System.IO.Ports;

namespace Itt.ScaleApi;

// Poll scale with '#' to print immediate
// Response as CRLF delimited "   -0.02  GS"
// 'S' = Stable measurement indicator
// Default RS232 is 2400 8 N 1
// Found is 9600 8 N 1
// https://www.setra.com/hubfs/SI%20EL%20Manual%20v.C.pdf
public class SiScale : IScale, IDisposable
{
    private readonly UnhandledExceptionEventHandler? Error;
    private readonly SerialPortLineReader Port;
    private readonly Timer PollTimer;
    private bool isDisposed;
    private readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

    public bool Stable { get; private set; }
    public decimal Weight { get; private set; }
    public event EventHandler<ScaleMeasurementEventArgs>? WeightChanged;
    public SiScale(SerialPort port, UnhandledExceptionEventHandler? error)
    {
        this.Error = error;
        this.Port = new SerialPortLineReader(port, error, ProcessData);
        this.PollTimer = new System.Threading.Timer(PollTimer_Tick, null, 
            PollInterval, Timeout.InfiniteTimeSpan);
    }

    private void PollTimer_Tick(object? state)
    {
        // Scale does not auto-send, poll
        try
        {
            PrintImmediateReading();
        }
        catch (IOException ex)
        {
            // Port closed
            this.Port.Dispose();
            throw new ObjectDisposedException("Port closed", ex);
        }
        catch (Exception ex)
        {
            this.Error?.Invoke(this, new UnhandledExceptionEventArgs(ex, true));
        }
        finally
        {
            if (!this.isDisposed)
            {
                // Restart the timer
                this.PollTimer.Change(PollInterval, Timeout.InfiniteTimeSpan);
            }
        }
    }

    private void ProcessData(string data)
    {
        // "   -0.02  GS"
        data = data.Trim();
        bool stable = data.EndsWith('S');
        if (stable)
        {
            data = data[..^1];
        }
        const string SUFFIX = "  G";
        if (!data.EndsWith(SUFFIX, StringComparison.Ordinal))
        {
            // "Fast mode" can cause a different response
            throw new FormatException($"Unexpected response from scale: '{data}'");
        }
        data = data[..^SUFFIX.Length];

        if (!decimal.TryParse(data, out var grams))
        {
            throw new FormatException($"Invalid number format from scale: '{data}'");
        }
        this.Weight = grams;
        this.Stable = stable;
        this.WeightChanged?.Invoke(this, new ScaleMeasurementEventArgs(grams, stable));
    }

    //private Task Tare() => WriteCommand("t");
    //private Task PrintStableReading() => WriteCommand("p");
    private void PrintImmediateReading() => this.Port.WriteLine("#");

    public void Dispose()
    {
        if (this.isDisposed) return;
        this.isDisposed = true;
        try
        {
            this.PollTimer.Dispose();
        }
        finally
        {
            this.Port.Dispose();
        }
    }

    public static SiScale Create2400_8N1(string portName, UnhandledExceptionEventHandler? error)
    {
        var port = new SerialPort(portName, 2400, Parity.None, 8, StopBits.One);
        port.Open();
        try
        {
            return new SiScale(port, error);
        }
        catch
        {
            port.Dispose();
            throw;
        }
    }

    public static SiScale Create9600_8N1(string portName, UnhandledExceptionEventHandler? error)
    {
        var port = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);
        port.Open();
        try
        {
            return new SiScale(port, error);
        }
        catch
        {
            port.Dispose();
            throw;
        }
    }
}