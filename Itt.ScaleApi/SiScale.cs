using System.IO.Ports;
using System.Text;

namespace Itt.ScaleApi;

// Poll scale with '#' to print immediate
// Response as CRLF delimited "   -0.02  GS"
// 'S' = Stable measurement indicator
// Default RS232 is 2400 8 N 1
// Found is 9600 8 N 1
// https://www.setra.com/hubfs/SI%20EL%20Manual%20v.C.pdf
public class SiScale : IScale, IDisposable
{
    private readonly SerialPort Port;
    private readonly UnhandledExceptionEventHandler? Error;
    private readonly CancellationTokenSource cts;
    private readonly StreamWriter SS;
    private readonly StreamReader SR;
    private readonly Task readTask;

    public bool Stable { get; private set; }
    public decimal Weight { get; private set; }
    public event EventHandler<ScaleMeasurementEventArgs>? WeightChanged;

    public SiScale(SerialPort port, UnhandledExceptionEventHandler? error)
    {
        this.Port = port;
        this.Error = error;
        this.cts = new CancellationTokenSource();
        this.SS = new StreamWriter(port.BaseStream, Encoding.ASCII, leaveOpen: true);
        this.SR = new StreamReader(port.BaseStream, Encoding.ASCII, leaveOpen: true);
        this.readTask = RunAsync();
    }

    private async Task RunAsync()
    {
        try
        {
            while (!cts.IsCancellationRequested)
            {
                await PrintImmediateReading();
                HandleReading(await SR.ReadLineAsync(cts.Token) ?? throw new EndOfStreamException("Unexpected end of stream"));
            }
        }
        catch (ObjectDisposedException)
        {
            // nop
        }
        catch (OperationCanceledException)
        {
            // nop
        }
        catch (Exception ex)
        {
            Error?.Invoke(this, new UnhandledExceptionEventArgs(ex, true));
        }
    }

    private void HandleReading(string data)
    {
        try
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
        catch (Exception ex)
        {
            Error?.Invoke(this, new UnhandledExceptionEventArgs(ex, false));
        }
    }

    //private Task Tare() => WriteCommand("t");
    //private Task PrintStableReading() => WriteCommand("p");
    private Task PrintImmediateReading() => WriteCommand("#");
    private async Task WriteCommand(string s)
    {
        await SS.WriteAsync(s);
        await SS.FlushAsync();
    }

    public void Dispose()
    {
        cts.Cancel();
        this.readTask.Wait();
        this.Port.Dispose();
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