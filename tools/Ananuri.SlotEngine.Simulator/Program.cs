using System.Globalization;
using Ananuri.SlotEngine.Simulator.Commands;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};
try
{
    SimulatorApplication.Run(args, cancellation.Token);
    return 0;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Run cancelled. No completed-run statistics were reported.");
    return 130;
}
catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or IOException or System.Text.Json.JsonException or OverflowException)
{
    Console.Error.WriteLine($"Run failed: {exception.Message}");
    return 1;
}
