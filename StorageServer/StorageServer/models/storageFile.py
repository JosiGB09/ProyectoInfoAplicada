from pydantic import BaseModel
from datetime import datetime

class FileMetadata(BaseModel):
    correlationId: int
    clientId: int
    generationDate: datetime
    fileName: str
