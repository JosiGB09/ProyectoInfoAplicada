import os
from datetime import datetime
from fastapi import UploadFile
from pathlib import Path
import base64

STORAGE_ROOT = Path("storage")

async def save_file(file: UploadFile, metadata) -> str:
    """
    Guarda un archivo PDF en una carpeta organizada por fecha (YYYY-MM-DD)
    """
    # Leer contenido
    content = await file.read()

    # Serializar
    encoded_pdf = base64.b64encode(content).decode("utf-8")

    # Crear carpeta por fecha
    folder_date = metadata.generationDate.strftime("%Y-%m-%d")
    folder_path = STORAGE_ROOT / folder_date
    folder_path.mkdir(parents=True, exist_ok=True)

    # Ruta final
    file_path = folder_path / metadata.fileName

    # Guardar archivo
    with open(file_path, "wb") as buffer:
        buffer.write(content)

    return str(file_path)

def get_file_path_by_correlationId(correlation_id: str):
    """
    Busca archivo empezando por las fechas más recientes
    """
    # Obtener carpetas ordenadas por fecha (más reciente primero)
    if STORAGE_ROOT.exists():
        date_folders = sorted([d for d in STORAGE_ROOT.iterdir() if d.is_dir()], reverse=True)
        
        for folder in date_folders:
            for file_path in folder.iterdir():
                # Busca archivos cuyo nombre empiece con 'Report_{correlation_id}_'
                if file_path.name.startswith(f"Report_{correlation_id}"):
                    return str(file_path)
    
    return None
