from fastapi import FastAPI
from controllers.storageController import router as storage_router

app = FastAPI(title="StorageServer", version="1.0", description="API para almacenamiento de archivos PDF's")

# Registrar rutas
app.include_router(storage_router, prefix="/api/storage")
