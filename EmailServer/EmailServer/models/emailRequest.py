from pydantic import BaseModel, EmailStr
from typing import Optional

class EmailRequest(BaseModel):
    correlationId: str
    to_email: EmailStr
    subject: Optional[str] = "Reporte"
    message: Optional[str] = None
