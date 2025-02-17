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

//using (var scale = SiScale.Create2400_8N1("COM8", HandleError))
//{
//    scale.WeightChanged += HandleReading;
//    Console.ReadLine();
//}

using (var scale = OhausScale.Create9600_8N1("COM14", HandleError))
{
    scale.WeightChanged += HandleReading;
    Console.ReadLine();
}
Console.WriteLine("Closed");