from confluent_kafka import Producer
import json

class KafkaProducerService:
    def __init__(self, bootstrap_servers="localhost:9092", topic="logs-email"):
        self.topic = topic
        self.producer = Producer({"bootstrap.servers": bootstrap_servers})

    def delivery_report(self, err, msg):
        """Cuando el mensaje se entrega o falla"""
        if err is not None:
            print(f"Error al enviar el log a Kafka: {err}")
        else:
            print(f"Log enviado a Kafka: {msg.value()} a la particion {msg.partition()} offset {msg.offset()}")

    def send_log(self, log_data: dict):
        """
        Envia un mensaje JSON al topico de Kafka de forma asincronica.
        """
        try:
            json_message = json.dumps(log_data, default=str)
            # Se envia sin bloquear
            self.producer.produce(
                self.topic,
                key=str(log_data.get("correlationId")),
                value=json_message,
                callback=self.delivery_report
            )
            # Procesa eventos del productor sin bloquear
            self.producer.poll(1)
        except Exception as e:
            print(f"Error al enviar el log a Kafka: {e}")
