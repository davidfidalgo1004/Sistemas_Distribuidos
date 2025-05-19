using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using ConverterService; // Namespace from the proto file's csharp_namespace

// Allow insecure HTTP/2 for local development if needed (use with caution!)
// AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

// --- Configuration ---
// Get the server address from launchSettings.json or use a default
// Make sure this matches the port your server is running on! Check ConverterService/Properties/launchSettings.json
// Common ports are 5001 (HTTPS) or 5000 (HTTP) for Kestrel, or different ones for IIS Express.
string serverAddress = "http://localhost:5264"; // <-- !!! UPDATE THIS PORT if needed !!!

Console.WriteLine($"Connecting to gRPC server at: {serverAddress}");
Console.WriteLine("------------------------------------");

// Create a channel
using var channel = GrpcChannel.ForAddress(serverAddress);

// Create a client stub
var client = new Converter.ConverterClient(channel);

// --- Call Unary RPCs (Challenge 2) ---
Console.WriteLine("Testing Unary Conversions...");
await CallEuroToDollar(client, 10.0);
await CallDollarToEuro(client, 10.80);
await CallKmToMiles(client, 100.0);
await CallMilesToKm(client, 62.14);
await CallCelsiusToFahrenheit(client, 20.0);
await CallFahrenheitToCelsius(client, 68.0);

Console.WriteLine("\n------------------------------------");
Console.WriteLine("Testing Server Streaming (Challenge 3 - GetMultipleTemperatureConversions)...");
await CallServerStreaming(client);

Console.WriteLine("\n------------------------------------");
Console.WriteLine("Testing Bidirectional Streaming (Challenge 3 - CelsiusToFahrenheitStream)...");
await CallBidirectionalStreaming(client);


Console.WriteLine("\n------------------------------------");
Console.WriteLine("Press any key to exit...");
Console.ReadKey();


// --- Helper Methods for Calling RPCs ---

async Task CallEuroToDollar(Converter.ConverterClient cl, double value)
{
    try
    {
        var request = new ConvertRequest { Value = value };
        var reply = await cl.EuroToDollarAsync(request);
        Console.WriteLine($" -> EuroToDollar Reply: {reply.Description}");
    }
    catch (RpcException ex)
    {
        Console.WriteLine($" !! RPC Error: {ex.Status}");
    }
}

async Task CallDollarToEuro(Converter.ConverterClient cl, double value)
{
    try
    {
        var request = new ConvertRequest { Value = value };
        var reply = await cl.DollarToEuroAsync(request);
        Console.WriteLine($" -> DollarToEuro Reply: {reply.Description}");
    }
    catch (RpcException ex)
    {
        Console.WriteLine($" !! RPC Error: {ex.Status}");
    }
}
async Task CallKmToMiles(Converter.ConverterClient cl, double value)
{
    try
    {
        var request = new ConvertRequest { Value = value };
        var reply = await cl.KmToMilesAsync(request);
        Console.WriteLine($" -> KmToMiles Reply: {reply.Description}");
    }
    catch (RpcException ex)
    {
        Console.WriteLine($" !! RPC Error: {ex.Status}");
    }
}
async Task CallMilesToKm(Converter.ConverterClient cl, double value)
{
    try
    {
        var request = new ConvertRequest { Value = value };
        var reply = await cl.MilesToKmAsync(request);
        Console.WriteLine($" -> MilesToKm Reply: {reply.Description}");
    }
    catch (RpcException ex)
    {
        Console.WriteLine($" !! RPC Error: {ex.Status}");
    }
}
async Task CallCelsiusToFahrenheit(Converter.ConverterClient cl, double value)
{
    try
    {
        var request = new ConvertRequest { Value = value };
        var reply = await cl.CelsiusToFahrenheitAsync(request);
        Console.WriteLine($" -> CelsiusToFahrenheit Reply: {reply.Description}");
    }
    catch (RpcException ex)
    {
        Console.WriteLine($" !! RPC Error: {ex.Status}");
    }
}

async Task CallFahrenheitToCelsius(Converter.ConverterClient cl, double value)
{
    try
    {
        var request = new ConvertRequest { Value = value };
        var reply = await cl.FahrenheitToCelsiusAsync(request);
        Console.WriteLine($" -> FahrenheitToCelsius Reply: {reply.Description}");
    }
    catch (RpcException ex)
    {
        Console.WriteLine($" !! RPC Error: {ex.Status}");
    }
}


// --- Challenge 3: Streaming Examples ---

async Task CallServerStreaming(Converter.ConverterClient cl)
{
    var request = new MultiConvertRequest
    {
        StartValue = 0,
        Count = 5,
        ConversionType = MultiConvertRequest.Types.ConversionType.CelsiusToFahrenheit
    };
    Console.WriteLine($" -> Requesting {request.Count} conversions starting from {request.StartValue}°C...");

    try
    {
        using var call = cl.GetMultipleTemperatureConversions(request);

        await foreach (var response in call.ResponseStream.ReadAllAsync())
        {
            Console.WriteLine($"    -> Server Stream Response: {response.Description}");
        }
        Console.WriteLine(" -> Server stream finished.");
    }
    catch (RpcException ex)
    {
         Console.WriteLine($" !! RPC Error during server stream: {ex.Status}");
    }
}


async Task CallBidirectionalStreaming(Converter.ConverterClient cl)
{
    var valuesToSend = new List<double> { 0, 10, 20, -5, 30 };
    Console.WriteLine($" -> Will send {valuesToSend.Count} Celsius values via stream...");

    try
    {
        using var call = cl.CelsiusToFahrenheitStream();

        // Background task to read responses from the server
        var readTask = Task.Run(async () =>
        {
            await foreach (var response in call.ResponseStream.ReadAllAsync())
            {
                Console.WriteLine($"    <- BiDi Stream Response: {response.Description}");
            }
            Console.WriteLine("    <- BiDi stream response reading complete.");
        });

        // Send requests to the server
        foreach (var val in valuesToSend)
        {
            Console.WriteLine($"    -> BiDi Stream Sending: {val}°C");
            await call.RequestStream.WriteAsync(new ConvertRequest { Value = val });
            await Task.Delay(300); // Simulate delay between sending requests
        }

        // Signal that we are done sending requests
        Console.WriteLine("    -> BiDi Stream Finished Sending.");
        await call.RequestStream.CompleteAsync();

        // Wait for the reading task to complete (server finished sending)
        await readTask;
         Console.WriteLine(" -> Bidirectional stream interaction finished.");

    }
     catch (RpcException ex)
    {
         Console.WriteLine($" !! RPC Error during bidirectional stream: {ex.Status}");
    }
}