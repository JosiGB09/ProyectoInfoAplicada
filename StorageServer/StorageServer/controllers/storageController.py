from fastapi import APIRouter, UploadFile, Form, HTTPException
from datetime import datetime
from models.storageFile import FileMetadata
from models.logEvent import LogEvent
from services.kafkaProducerService import KafkaProducerService
import base64, os

router = APIRouter()
kafka_service = KafkaProducerService()

STORAGE_DIR = "./storage"

@router.post("/upload")
async def upload_file(
    file: UploadFile,
    correlationId: str = Form(...),
    clientId: str = Form(...),
    generationDate: datetime = Form(...),
    fileName: str = Form(...)
):
    """
    Recibe un archivo PDF, lo serializa, lo guarda en disco y envia un log a Kafka.
    """
    try:
        # Leer el contenido binario del archivo
        content = await file.read()

        # Serializar (para transporte o registro)
        encoded_pdf = base64.b64encode(content).decode("utf-8")

        # Crear carpeta por fecha (YYYY-MM-DD)
        folder_path = os.path.join(STORAGE_DIR, generationDate.strftime("%Y-%m-%d"))
        os.makedirs(folder_path, exist_ok=True)
        file_path = os.path.join(folder_path, fileName)

        # Guardar el archivo f�sicamente
        with open(file_path, "wb") as f:
            f.write(content)

        # Crear el objeto de log
        log = LogEvent(
            correlationId=correlationId,
            service="StorageServer",
            endpoint="/api/storage/upload",
            payload=f"Archivo {fileName} almacenado correctamente",
            fileName=fileName,
            generation=generationDate,
            success=True
        )

        # Enviar el log al broker Kafka
        kafka_service.send_log(log.dict())

        return {
            "message": "Archivo almacenado y log enviado a Kafka correctamente",
            "correlationId": correlationId,
            "encodedLength": len(encoded_pdf)
        }

    except Exception as e:
        # En caso de error, tambi�n se registra en Kafka
        log = LogEvent(
            correlationId=correlationId,
            service="StorageServer",
            endpoint="/api/storage/upload",
            payload=f"Error: {str(e)}",
            fileName=fileName,
            success=False
        )
        kafka_service.send_log(log.dict())
        raise HTTPException(status_code=500, detail=f"Error al almacenar archivo: {e}")


@router.get("/file/{correlationId}")
async def get_file(correlationId: str):
    """
    Busca un archivo por CorrelationId y devuelve el PDF serializado.
    Tambien envia un log a Kafka.
    """
    try:
        for root, _, files in os.walk(STORAGE_DIR):
            for file in files:
                if correlationId in file:
                    file_path = os.path.join(root, file)
                    with open(file_path, "rb") as f:
                        encoded_pdf = base64.b64encode(f.read()).decode("utf-8")

                    # Crear y enviar log de recuperaci�n
                    log = LogEvent(
                        correlationId=correlationId,
                        service="StorageServer",
                        endpoint="/api/storage/file",
                        payload=f"Archivo {file} recuperado correctamente",
                        fileName=file,
                        success=True
                    )
                    kafka_service.send_log(log.dict())

                    return {
                        "correlationId": correlationId,
                        "fileName": file,
                        "pdfData": encoded_pdf
                    }

        # Si no se encontr�, registrar error
        log = LogEvent(
            correlationId=correlationId,
            service="StorageServer",
            endpoint="/api/storage/file",
            payload="Archivo no encontrado",
            fileName="N/A",
            success=False
        )
        kafka_service.send_log(log.dict())

        raise HTTPException(status_code=404, detail="Archivo no encontrado")

    except Exception as e:
        log = LogEvent(
            correlationId=correlationId,
            service="StorageServer",
            endpoint="/api/storage/file",
            payload=f"Error: {str(e)}",
            fileName="N/A",
            success=False
        )
        kafka_service.send_log(log.dict())
        raise HTTPException(status_code=500, detail=str(e))
