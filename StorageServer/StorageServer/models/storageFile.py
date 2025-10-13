from pydantic import BaseModel
from datetime import datetime

class FileMetadata(BaseModel):
    correlationId: str
    clientId: str
    generationDate: datetime
    fileName: str
