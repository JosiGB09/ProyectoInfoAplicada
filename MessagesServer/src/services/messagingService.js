
import { getFileFromStorage } from './storageService.js';
import { sendLog } from './kafkaProducer.js';
import { Client, GatewayIntentBits, Partials } from 'discord.js';
import fs from 'fs';
import path from 'path';

export const sendMessage = async ({ correlationId, recipient, platform, message }) => {
    if (platform !== 'discord') {
        throw new Error('Plataforma no soportada');
    }

    //recuperar pdf 
    let pdfBuffer;
    try {
        pdfBuffer = await getFileFromStorage(correlationId);
        if (!pdfBuffer || pdfBuffer.length === 0) {
            throw new Error('Archivo PDF vacío');
        }
    } catch (err) {
        await sendLog({  
            CorrelationId: correlationId,
            Timestamp: new Date().toISOString(),
            Service: 'Messaging',
            Endpoint: 'sendMessage',
            Payload: 'error_recuperando_pdf',
            Success: false,
        });
        throw err;
    }

    // archivo temporal
    const tempPath = path.join(process.cwd(), `${correlationId}.pdf`);

    fs.writeFileSync(tempPath, pdfBuffer);

    // Enviar PDF a Discord
    try {
        const discordClient = new Client({ intents: [GatewayIntentBits.Guilds, GatewayIntentBits.GuildMessages, GatewayIntentBits.MessageContent] });
        await discordClient.login(process.env.DISCORD_BOT_TOKEN);

        const channel = await discordClient.channels.fetch(recipient);
        if (!channel || !channel.isTextBased()) {
            throw new Error('Canal de Discord no válido');
        }

        await channel.send({ 
            content: message || 'PDF procesado',
            files: [tempPath]
        });

        await sendLog({ 
            CorrelationId: correlationId,
            Timestamp: new Date().toISOString(),
            Service: 'Messaging',
            Endpoint: 'sendMessage',
            Payload: 'mensaje_enviado',
            Success: true,
        });
        try { fs.unlinkSync(tempPath); } catch (e) { /* ignore */ }
        await discordClient.destroy();
        return { success: true };
    } catch (err) {
        await sendLog({ 
            CorrelationId: correlationId,
            Timestamp: new Date().toISOString(),
            Service: 'Messaging',
            Endpoint: 'sendMessage',
            Payload: 'error_envio_mensaje',
            Success: false,
        });
        try { fs.unlinkSync(tempPath); } catch (e) { /* ignore */ }
        throw err;
    }
};
