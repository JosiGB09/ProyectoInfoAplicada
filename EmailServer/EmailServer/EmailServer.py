from fastapi import FastAPI
from controllers import storageController
from controllers import emailController

app = FastAPI(title="Email Server")

# Incluir las rutas
app.include_router(storageController.router)
app.include_router(emailController.router)

@app.get("/")
def root():
    return {"message": "Servidor EmailServer funcionando correctamente."}
