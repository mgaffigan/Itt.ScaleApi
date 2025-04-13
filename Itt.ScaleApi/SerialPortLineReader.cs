using System.Diagnostics.CodeAnalysis;
using System.IO.Ports;

namespace Itt.ScaleApi;

internal sealed class SerialPortLineReader : IDisposable
{
    private readonly SerialPort Port;
    private readonly UnhandledExceptionEventHandler? Error;
    private readonly Action<string> ProcessData;
    private string pending = "";

    public SerialPortLineReader(SerialPort port, UnhandledExceptionEventHandler? error, Action<string> ProcessData)
    {
        this.Port = port;
        this.Error = error;
        this.ProcessData = ProcessData;
        this.Port.DataReceived += Port_DataReceived;
    }

    public void Dispose()
    {
        Port.DataReceived -= Port_DataReceived;
        Port.Dispose();
    }

    public void Write(string data) => Port.Write(data);
    public void WriteLine(string data) => Port.WriteLine(data);

    private bool TryGetNextLine([MaybeNullWhen(false)] out string nextLine)
    {
        var data = Port.ReadExisting();
        data = data.Replace("\r", "");
        if (!data.Contains('\n'))
        {
            pending += data;
            nextLine = null;
            return false;
        }

        pending += data;

        var ind = pending.IndexOf('\n');
        nextLine = pending.Substring(0, ind);
        pending = pending.Substring(ind + 1);
        return true;
    }

    private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        while (TryGetNextLine(out var data))
        {
            try
            {
                ProcessData(data);
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, new UnhandledExceptionEventArgs(ex, false));
            }
        }
    }
}
