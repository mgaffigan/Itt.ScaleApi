# ITT ScaleAPI

A set of API's for reading from laboratory milligram scales over serial.  Includes 
tested support for Setra SI-2000S and Ohaus Adventurer.

Setra and Ohaus are trademarks of their respective owners.  ITT has no relation 
with scale manufacturers.

## Example usage

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

    using (var scale = SiScale.Create2400_8N1("COM8", HandleError))
    {
        scale.WeightChanged += HandleReading;
        Console.ReadLine();
    }
    Console.WriteLine("Closed");

# Notes on specific scales

## Setra SI-2000S
* Manual is available at https://www.setra.com/hubfs/SI%20EL%20Manual%20v.C.pdf
* Default configuration is for 2400 8N1.  Option to speed up to 9600 8N1 through mode/power button

## Ohaus Adventurer ARA520
* Manual is available at https://dmx.ohaus.com/WorkArea/showcontent.aspx?id=3348
* Multiple modes available through front panel buttons
 * Option to print when stable reading changes
* Default configuration is for 2400 **7N2**
* RS232 commands to scale seem to be ignored (cabling issue?)
* API is setup assuming "print when stable" is set