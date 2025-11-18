using Confluent.Kafka;
using System.Text.Json;
using System;
using System.Threading.Tasks;

namespace PDFServer.Services
{
    public class KafkaProducerService : IDisposable
    {
        private readonly IProducer<Null, string> _producer;
        private readonly string _topic;
        private bool _disposed;

        public KafkaProducerService(string bootstrapServers, string topic = "pdf_reports")
        {
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                Acks = Acks.All,
                MessageTimeoutMs = 30000,
                EnableIdempotence = true,
                RetryBackoffMs = 1000
            };

            _producer = new ProducerBuilder<Null, string>(config).Build();
            _topic = topic;
        }

        public async Task SendLogAsync(object log)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(KafkaProducerService));

            string json = JsonSerializer.Serialize(log);

            try
            {
                var deliveryResult = await _producer.ProduceAsync(_topic, new Message<Null, string> { Value = json });
                Console.WriteLine($"[KafkaProducer] Delivered to {deliveryResult.TopicPartitionOffset}");
            }
            catch (ProduceException<Null, string> ex)
            {
                Console.WriteLine($"[KafkaProducer] Produce error: {ex.Error.Reason}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KafkaProducer] Unexpected error: {ex.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                _producer.Flush(TimeSpan.FromSeconds(5));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KafkaProducer] Flush error: {ex.Message}");
            }

            _producer.Dispose();
            _disposed = true;
        }
    }
}
