import express from 'express';
import { sendMessage} from  "../services/messagingService.js";
import { sendLog } from '../services/kafkaProducer.js';

const router = express.Router();

router.post('/send', async (req, res) => {
    try {
        console.log('Body recibido:', req.body); 
        const response = await sendMessage(req.body);
        res.status(200).json(response);
        await sendLog({
            CorrelationId: req.body.correlationId,
            Timestamp: new Date().toISOString(),
            Service: 'Messaging',
            Endpoint: '/send',
            Payload: 'mensaje_enviado', 
            Success: true
        });
    } catch (error) {
        console.error('Error al enviar mensaje:', error);
        res.status(500).json({ error: 'Error al enviar mensaje' });
        await sendLog({           
            CorrelationId: req.body.correlationId,
            Timestamp: new Date().toISOString(),
            Service: 'Messaging',            
            Endpoint: '/send',
            Payload: 'error_envio_mensaje', 
            Success: false,
        });
    }
});

export default router;