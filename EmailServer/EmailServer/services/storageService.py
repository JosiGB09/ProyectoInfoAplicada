import httpx
import base64
from io import BytesIO
from dotenv import load_dotenv
import os
from services.kafkaProducerService import KafkaProducerService
from models.logEvent import LogEvent

load_dotenv() # Cargar variables del .env

kafka_producer = KafkaProducerService()

async def get_pdf_from_storage(correlation_id: str):
    """
    Recupera el PDF desde el StorageServer y lo guarda como buffer en memoria.
    """
    storage_url = os.getenv("STORAGE_SERVER_URL")
    url = f"{storage_url}/{correlation_id}"

    async with httpx.AsyncClient() as client:
        response = await client.get(url)

        if response.status_code != 200:
            # Log de error al recuperar PDF
            log = LogEvent(
                message="Error al recuperar el PDF desde StorageServer",
                correlationId=correlation_id,
                fileName="N/A",
                endpoint="/get_pdf",
                service="Email"
            )
            kafka_producer.send_log(log.model_dump())
            
            return None, f"Error al recuperar PDF. Codigo: {response.status_code}"

        try:
            data = response.json()  # Leer el JSON
            pdf_base64 = data.get("pdfData")
            file_name = data.get("fileName")

            # Validar que venga el campo pdfData
            if not pdf_base64:
                return None, "El servidor no devolvio el campo 'pdfData'."

            # Decodificar de base64 a bytes
            pdf_bytes = base64.b64decode(pdf_base64)

            # Guardar en buffer de memoria
            buffer = BytesIO(pdf_bytes)

            # Log de éxito
            log = LogEvent(
                message="PDF recuperado exitosamente desde StorageServer",
                correlationId=correlation_id,
                fileName=file_name,
                endpoint="/get_pdf",
                service="Email"
            )
            kafka_producer.send_log(log.model_dump())

            return buffer, file_name

        except Exception as e:
            log = LogEvent(
                message="Error al procesar la respuesta de StorageServer",
                correlationId=correlation_id,
                fileName="N/A",
                endpoint="/get_pdf",
                service="Email"
            )
            kafka_producer.send_log(log.model_dump())
            
            return None, f"Error al procesar la respuesta: {str(e)}"
