using Confluent.Kafka;

namespace ServerHangfire.Services
{
    public class KafkaProducerService
    //esto es lo que dijo el profe que debiamos agregar (la clase prevista del productor)
    {
        private readonly string? _bootstrap;
        private readonly string _topic = "logs";
        private readonly IProducer<Null, string>? _producer;

        public KafkaProducerService(IConfiguration config)
        {
            _bootstrap = config["Kafka:BootstrapServers"];
            if (!string.IsNullOrEmpty(_bootstrap))
            {
                var conf = new ProducerConfig { BootstrapServers = _bootstrap };
                _producer = new ProducerBuilder<Null, string>(conf).Build();
            }
        }

        public async Task SendLogAsync(string message)
        {
            if (_producer == null)
            {
                // Kafka no configurado: fallback: escribir en consola y retornar
                Console.WriteLine("[Kafka-Fallback] " + message);
                return;
            }

            try
            {
                var dr = await _producer.ProduceAsync(_topic, new Message<Null, string> { Value = message });
                Console.WriteLine($"[Kafka] Mensaje enviado a {_topic} (offset {dr.Offset})");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Kafka-Error] " + ex.Message);
            }
        }
    }
}
