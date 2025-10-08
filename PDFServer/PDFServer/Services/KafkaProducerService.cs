using Confluent.Kafka;
using System.Text.Json;

namespace PDFServer.Services
{
    public class KafkaProducerService
    {
        private readonly IProducer<Null, string> _producer;
        private readonly string _topic;

        public KafkaProducerService(string bootstrapServers)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                Acks = Acks.All,
            };//configuracion del productor
            _producer =new ProducerBuilder<Null, string>(config).Build();//se crea el productor
            _topic = "pdf_reports";//se nombra el topico a donde se enviaran los mensajes
        }
        public async Task SendLogAsync(object log)
        {
            string json=JsonSerializer.Serialize(log);//se convierte el objeto log a formato json
            await _producer.ProduceAsync(_topic, new Message<Null, string> { Value = json });//se envia el mensaje al topico
            _producer.Flush(TimeSpan.FromSeconds(3));
        }
    }
}
