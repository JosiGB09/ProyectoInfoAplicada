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
            string[] topics = { "logs-hangfire", "logs-storage", "logs-email", "messages_logs", "pdf_reports" };
            consumer.Subscribe(topics);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    _logger.LogWarning(result.ToString());
                    var logData = JsonSerializer.Deserialize<LogModel>(result.Message.Value, new JsonSerializerOptions { PropertyNameCaseInsensitive=true});
                    _logger.LogInformation($"Log recibido: {logData?.Service} - {logData?.Endpoint}");
                    await _databaseService.SaveLogToSql(logData);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al consumir el mensaje: {ex.Message}");
                }
            }
            //await Task.Delay(60000 * 2, stoppingToken);// Espera de 5 minutos
        }
    }
}
