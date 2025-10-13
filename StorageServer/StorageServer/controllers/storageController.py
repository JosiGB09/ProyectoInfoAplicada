from fastapi import APIRouter, UploadFile, Form, HTTPException
from datetime import datetime
from models.storageFile import FileMetadata
from models.logEvent import LogEvent
from services.kafkaProducerService import KafkaProducerService
from services.storageService import save_file, get_file_path_by_correlationId
import base64, os

router = APIRouter()
kafka_service = KafkaProducerService()

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
    metadata = FileMetadata(
        correlationId=correlationId,
        clientId=clientId,
        generationDate=generationDate,
        fileName=fileName
    )
    
    try:
        file_path = await save_file(file, metadata)

        # Crear el objeto de log
        log = LogEvent(
            correlationId=metadata.correlationId,
            service="StorageServer",
            endpoint="/api/storage/upload",
            payload=f"Archivo {metadata.fileName} almacenado correctamente",
            fileName=metadata.fileName,
            success=True
        )

        # Enviar el log al broker Kafka
        kafka_service.send_log(log.dict())

        return {
            "message": "Archivo almacenado y log enviado a Kafka correctamente",
            "correlationId": metadata.correlationId
        }

    except Exception as e:
        # En caso de error, tambien se registra en Kafka
        log = LogEvent(
            correlationId=metadata.correlationId,
            service="StorageServer",
            endpoint="/api/storage/upload",
            payload=f"Error: {str(e)}",
            fileName=metadata.fileName,
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
        file_path = get_file_path_by_correlationId(correlationId)

        if file_path:
            with open(file_path, "rb") as f:
                encoded_pdf = base64.b64encode(f.read()).decode("utf-8")

            file_name = os.path.basename(file_path)

            # Crear y enviar log de recuperacion
            log = LogEvent(
                correlationId=correlationId,
                service="StorageServer",
                endpoint="/api/storage/file",
                payload=f"Archivo {file_name} recuperado correctamente",
                fileName=file_name,
                success=True
            )
            kafka_service.send_log(log.dict())

            return {
                "correlationId": correlationId,
                "fileName": file_name,
                "pdfData": encoded_pdf
            }

        # Si no se encontro, registrar error
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
