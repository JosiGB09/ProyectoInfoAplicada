from fastapi import APIRouter, HTTPException
from models.emailRequest import EmailRequest
from services.emailService import send_email_with_pdf

router = APIRouter(prefix="/api/email", tags=["Email"])

@router.post("/send")
async def send_email(request: EmailRequest):
    # Enviar correo usando la funcion que recupera el PDF
    success, result = await send_email_with_pdf(
        to_email=request.to_email,
        correlation_id=request.correlationId,
        subject=request.subject,
        message=request.message
    )

    if not success:
        raise HTTPException(status_code=500, detail=result)

    return {"message": result}
