using Grpc.Core;
using ConverterService; // Namespace from the 'option csharp_namespace' in proto file

namespace ConverterService.Services
{
    // Inherits from the generated base class (Converter.ConverterBase)
    public class ConverterServiceImpl : Converter.ConverterBase
    {
        private readonly ILogger<ConverterServiceImpl> _logger;
        private const double EuroToUsdRate = 1.08; // Example static rate
        private const double KmToMilesRate = 0.621371;

        public ConverterServiceImpl(ILogger<ConverterServiceImpl> logger)
        {
            _logger = logger;
        }

        // --- Challenge 2 Implementations ---

        public override Task<ConvertResponse> EuroToDollar(ConvertRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Converting {request.Value} EUR to USD");
            double result = request.Value * EuroToUsdRate;
            return Task.FromResult(new ConvertResponse
            {
                Result = result,
                Description = $"{request.Value:F2} EUR = {result:F2} USD"
            });
        }

        public override Task<ConvertResponse> DollarToEuro(ConvertRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Converting {request.Value} USD to EUR");
            double result = request.Value / EuroToUsdRate;
            return Task.FromResult(new ConvertResponse
            {
                Result = result,
                Description = $"{request.Value:F2} USD = {result:F2} EUR"
            });
        }

        public override Task<ConvertResponse> KmToMiles(ConvertRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Converting {request.Value} Km to Miles");
            double result = request.Value * KmToMilesRate;
            return Task.FromResult(new ConvertResponse
            {
                Result = result,
                Description = $"{request.Value:F2} Km = {result:F2} Miles"
            });
        }

        public override Task<ConvertResponse> MilesToKm(ConvertRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Converting {request.Value} Miles to Km");
            double result = request.Value / KmToMilesRate;
            return Task.FromResult(new ConvertResponse
            {
                Result = result,
                Description = $"{request.Value:F2} Miles = {result:F2} Km"
            });
        }

        public override Task<ConvertResponse> CelsiusToFahrenheit(ConvertRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Converting {request.Value} C to F");
            double result = (request.Value * 9.0 / 5.0) + 32.0;
            return Task.FromResult(new ConvertResponse
            {
                Result = result,
                Description = $"{request.Value:F1}°C = {result:F1}°F"
            });
        }

        public override Task<ConvertResponse> FahrenheitToCelsius(ConvertRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"Converting {request.Value} F to C");
            double result = (request.Value - 32.0) * 5.0 / 9.0;
            return Task.FromResult(new ConvertResponse
            {
                Result = result,
                Description = $"{request.Value:F1}°F = {result:F1}°C"
            });
        }


        // --- Challenge 3 Implementations ---

        // Example: Bidirectional Streaming (Client sends stream, Server sends stream)
        public override async Task CelsiusToFahrenheitStream(
            IAsyncStreamReader<ConvertRequest> requestStream,
            IServerStreamWriter<ConvertResponse> responseStream,
            ServerCallContext context)
        {
            _logger.LogInformation("Starting Celsius to Fahrenheit bidirectional stream.");

            await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
            {
                 if (context.CancellationToken.IsCancellationRequested)
                 {
                    _logger.LogInformation("Stream cancelled by client.");
                    break;
                 }

                _logger.LogInformation($"Streaming conversion for {request.Value} C");
                double result = (request.Value * 9.0 / 5.0) + 32.0;
                var response = new ConvertResponse
                {
                    Result = result,
                    Description = $"{request.Value:F1}°C = {result:F1}°F (Streamed)"
                };
                await responseStream.WriteAsync(response);
                await Task.Delay(200); // Simulate some work
            }
             _logger.LogInformation("Finished Celsius to Fahrenheit bidirectional stream.");
        }

        // Example: Server Streaming (Client sends one request, Server streams multiple responses)
        public override async Task GetMultipleTemperatureConversions(
            MultiConvertRequest request,
            IServerStreamWriter<ConvertResponse> responseStream,
            ServerCallContext context)
        {
            _logger.LogInformation($"Starting server stream for {request.Count} conversions.");

            for (int i = 0; i < request.Count; i++)
            {
                if (context.CancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Stream cancelled by client.");
                    break;
                }

                double inputValue = request.StartValue + i;
                double result;
                string description;

                switch (request.ConversionType)
                {
                    case MultiConvertRequest.Types.ConversionType.CelsiusToFahrenheit:
                        result = (inputValue * 9.0 / 5.0) + 32.0;
                        description = $"{inputValue:F1}°C = {result:F1}°F";
                        break;
                    case MultiConvertRequest.Types.ConversionType.FahrenheitToCelsius:
                         result = (inputValue - 32.0) * 5.0 / 9.0;
                         description = $"{inputValue:F1}°F = {result:F1}°C";
                        break;
                    default:
                        // Handle unknown type or throw exception
                        _logger.LogWarning($"Unknown conversion type requested: {request.ConversionType}");
                        continue; // Skip this iteration
                }

                 _logger.LogInformation($"Streaming result {i+1}: {description}");
                 var response = new ConvertResponse { Result = result, Description = description };
                 await responseStream.WriteAsync(response);
                 await Task.Delay(500); // Simulate some work/delay between results
            }
             _logger.LogInformation("Finished server stream.");
        }
    }
}