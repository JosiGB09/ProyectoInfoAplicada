using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using ServerHangfire.Models;
using System.Text.Json;

namespace ServerHangfire.Services
{
    public class KafkaProducerService
    {
        private readonly IProducer<Null, string>? _producer;
        private readonly string _topic;
        private readonly string? _bootstrap;

        public KafkaProducerService(IConfiguration config)
        {
            _bootstrap = config["Kafka:BootstrapServers"];
            _topic = "logs-hangfire";

            if (!string.IsNullOrEmpty(_bootstrap))
            {
                var conf = new ProducerConfig
                {
                    BootstrapServers = _bootstrap,
                    Acks = Acks.All,
                    MessageTimeoutMs = 5000
                };

                _producer = new ProducerBuilder<Null, string>(conf).Build();
                Console.WriteLine($"[Kafka] Productor inicializado: {_bootstrap}, tópico {_topic}");
            }
            else
            {
                Console.WriteLine("[Kafka] Sin configuración.");
            }
        }

        public async Task SendLogAsync(LogEvent log)
        {
            string json = JsonSerializer.Serialize(log);

            if (_producer == null)
            {
                Console.WriteLine("[Kafka-Fallback] " + json);
                return;
            }

            try
            {
                var result = await _producer.ProduceAsync(_topic, new Message<Null, string> { Value = json });
                Console.WriteLine($"[Kafka]  Enviado {_topic} (Offset {result.Offset})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Kafka]  Error: {ex.Message}");
            }
        }
    }
}
