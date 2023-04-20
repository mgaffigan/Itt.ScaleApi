using Itt.ScaleApi;

void HandleError(object? sender, UnhandledExceptionEventArgs e)
{
    Console.WriteLine(e.ExceptionObject);
}

void HandleReading(object? sender, ScaleMeasurementEventArgs e)
{
    Console.SetCursorPosition(0, 0);
    var stable = e.Stable ? "Stable" : "      ";
    Console.WriteLine($"{e.Weight,10:0.##} g {stable}");
}

using (var scale = SiScale.Create9600_8N1("COM8", HandleError))
{
    scale.WeightChanged += HandleReading;
    Console.ReadLine();
}
Console.WriteLine("Closed");