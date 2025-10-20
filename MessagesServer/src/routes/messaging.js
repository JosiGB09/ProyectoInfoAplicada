import express from 'express';
import { sendMessage} from  "../services/messagingService.js";
import { sendLog } from '../services/kafkaProducer.js';

const router = express.Router();

router.post('/send', async (req, res) => {
    try {
        const response = await sendMessage(req.body);
        res.status(200).json(response);
        await sendLog({ event: 'mensaje_enviado', details: response });
    } catch (error) {
        console.error('Error al enviar mensaje:', error);
        res.status(500).json({ error: 'Error al enviar mensaje' });
        await sendLog({ event: 'error_envio_mensaje', error: error.message });
    }
});

export default router;