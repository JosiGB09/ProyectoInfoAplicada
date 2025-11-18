from pydantic import BaseModel
from datetime import datetime
from typing import Optional

class LogEvent(BaseModel):
    message: str
    correlationId: str
    timestamp: datetime = datetime.utcnow().isoformat()+"Z"
    service: str
    endpoint:str
    fileName: Optional[str] = None
