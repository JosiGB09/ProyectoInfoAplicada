# StorageServer
API REST para almacenamiento y recuperación de archivos PDF.

### Pasos para ejecutar:

### Paso 1: Levantar Kafka y Zookeeper
```bash
docker-compose up -d
```

### Paso 2 Ejecutar StorageServer

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
uvicorn StorageServer:app --reload
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
