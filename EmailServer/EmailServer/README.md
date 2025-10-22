# EmailServer
API REST para recuperación de archivos PDF para enviar correos electronicos.

### Pasos para ejecutar:

### Paso 1: Levantar Kafka y Zookeeper
```bash
docker-compose up -d
```

### Paso 2 Ejecutar EmailServer

**Crear entorno virtual:**
```bash
python -m venv env
```

**Activar entorno virtual:**
```bash
# Windows
env\Scripts\activate
```

**Instalar dependencias:**
```bash
pip install -r requirements.txt
```

**Ejecutar la aplicación:**
```bash
# Importante se debe de iniciar en el puerto 8001
uvicorn StorageServer:app --reload --port 8001
```

## Comandos útiles

### Docker (Kafka/Zookeeper)
```bash
# Ver estado de contenedores
docker-compose ps

# Ver logs de Kafka
docker-compose logs -f kafka

# Ver logs de Zookeeper
docker-compose logs -f zookeeper

# Detener servicios
docker-compose down

# Eliminar datos de Kafka (cuidado)
docker-compose down -v
```
