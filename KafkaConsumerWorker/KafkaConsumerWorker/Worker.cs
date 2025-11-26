using Confluent.Kafka;
using System.Text.Json;
using KafkaConsumerWorker.Models;
using KafkaConsumerWorker.Services;

namespace KafkaConsumerWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly DatabaseService _databaseService;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public Worker(ILogger<Worker> logger, DatabaseService databaseService)
        {
            _logger = logger;
            _databaseService = databaseService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = "localhost:9092",
                GroupId = "log-consumer-group-v2",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true
            };
            using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            string[] topics = { "logs-hangfire", "logs-storage", "logs-email", "messages_logs", "reports_pdf" };
            consumer.Subscribe(topics);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    var logData = JsonSerializer.Deserialize<LogModel>(result.Message.Value, _jsonOptions);
                    if (logData != null)
                    {
                        var logInfo = $"{logData.Service} - {logData.Endpoint}";
                        _logger.LogInformation("Log recibido: {LogInfo}", logInfo);
                        await _databaseService.SaveLogToSql(logData);
                    }
                    else
                    {
                        _logger.LogWarning("Mensaje recibido no se pudo deserializar a LogModel: {Message}", result.Message.Value);
                    }
                }

                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al consumir el mensaje.");
            }
            }
            await Task.Delay(60000 * 2, stoppingToken);// Espera de 2 minutos
        }
    }
}
