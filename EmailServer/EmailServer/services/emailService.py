import os
import smtplib
from io import BytesIO
from email.mime.multipart import MIMEMultipart
from email.mime.application import MIMEApplication
from email.mime.text import MIMEText
from email.utils import formatdate
from dotenv import load_dotenv
from services.storageService import get_pdf_from_storage
from models.logEvent import LogEvent
from services.kafkaProducerService import KafkaProducerService

load_dotenv() #Cargar variables de entorno

kafka_producer = KafkaProducerService()

SMTP_SERVER= os.getenv("SMTP_SERVER")
SMTP_PORT= int(os.getenv("SMTP_PORT", 465))
GMAIL_USER = os.getenv("GMAIL_USER")
GMAIL_PASSWORD = os.getenv("GMAIL_PASSWORD")

async def send_email_with_pdf(to_email: str, correlation_id: str, subject: str = "Reporte", message: str = None):
    """
    Recupera el PDF desde StorageServer y envia un correo por Gmail con SMTP_SSL.
    """
    # Recuperar PDF
    pdf_buffer, pdf_file_name_or_error = await get_pdf_from_storage(correlation_id)
    if pdf_buffer is None:
        # Log de error al recuperar PDF
        log = LogEvent(
            message="Error al recuperar el PDF desde StorageServer",
            correlationId=correlation_id,
            fileName="N/A",
        )
        kafka_producer.send_log(log.model_dump())
        
        return False, f"Error al recuperar PDF: {pdf_file_name_or_error}"

    # Crear correo
    msg = MIMEMultipart()
    msg['From'] = GMAIL_USER
    msg['To'] = to_email
    msg['Date'] = formatdate(localtime=True)
    msg['Subject'] = subject

    # Cuerpo del mensaje
    body_text = message or f"Reporte adjunto.\nCorrelationId: {correlation_id}\nFecha: {formatdate(localtime=True)}"
    msg.attach(MIMEText(body_text, 'plain'))

    # Adjuntar PDF
    pdf_attachment = MIMEApplication(pdf_buffer.getvalue(), _subtype='pdf')
    pdf_attachment.add_header('Content-Disposition', 'attachment', filename=pdf_file_name_or_error)
    msg.attach(pdf_attachment)

    try:
        # Conectar con Gmail usando SMTP_SSL
        with smtplib.SMTP_SSL(SMTP_SERVER, SMTP_PORT) as server:
            server.login(GMAIL_USER, GMAIL_PASSWORD)
            server.send_message(msg)

        # Log de éxito en Kafka
        log = LogEvent(
            message="Reporte enviado por correo electronico correctamente",
            correlationId=correlation_id,
            fileName=pdf_file_name_or_error,
        )
        kafka_producer.send_log(log.model_dump())

        return True, f"Correo enviado correctamente a {to_email} con el PDF {pdf_file_name_or_error}"

    except smtplib.SMTPException as e:
        # Log de error en Kafka
            log = LogEvent(
                message=f"Error SMTP al enviar correo electronico: {e}",
                correlationId=correlation_id,
                fileName=pdf_file_name_or_error
            )
            kafka_producer.send_log(log.model_dump())

            return False, f"Error al enviar correo: {e}"
