from pydantic import BaseModel
from datetime import datetime

class LogEvent(BaseModel):
    correlationId: str
    service: str
    endpoint: str
    timestamp: datetime = datetime.utcnow()
    payload: str
    fileName: str
    success: bool
