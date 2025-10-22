from fastapi import APIRouter, HTTPException
from fastapi.responses import StreamingResponse
from services.storageService import get_pdf_from_storage

router = APIRouter(prefix="/api/email", tags=["PDF Email"])

@router.get("/pdf/{correlation_id}")
async def get_pdf(correlation_id: str):
    """
    Endpoint que recupera el PDF desde el StorageServer y lo devuelve al cliente.
    """
    buffer, error = await get_pdf_from_storage(correlation_id)

    if buffer is None:
        raise HTTPException(status_code=404, detail=error)

    # Retornar el PDF como stream
    return StreamingResponse(buffer, media_type="application/pdf")
