
import { getFileFromStorage } from './storageService.js';
import { sendLog } from './kafkaProducer.js';
import { Client, GatewayIntentBits, Partials } from 'discord.js';
import fs from 'fs';
import path from 'path';

export const sendMessage = async ({ CorrelationId, Recipient, Platform, Message }) => {
    if (Platform !== 'discord') {
        throw new Error('Plataforma no soportada');
    }

    //recuperar pdf 
    let pdfBuffer;
    try {
        pdfBuffer = await getFileFromStorage(CorrelationId);
        if (!pdfBuffer || pdfBuffer.length === 0) {
            throw new Error('Archivo PDF vacío');
        }
    } catch (err) {
        await sendLog({ event: 'error_recuperando_pdf', correlationId: CorrelationId, error: err.message });
        throw err;
    }

    // archivo temporal
    const tempPath = path.join(process.cwd(), `${CorrelationId}.pdf`);
    fs.writeFileSync(tempPath, pdfBuffer);

    // Enviar PDF a Discord
    try {
        const discordClient = new Client({ intents: [GatewayIntentBits.Guilds, GatewayIntentBits.GuildMessages, GatewayIntentBits.MessageContent] });
        await discordClient.login(process.env.DISCORD_BOT_TOKEN);

        const channel = await discordClient.channels.fetch(Recipient);
        if (!channel || !channel.isTextBased()) {
            throw new Error('Canal de Discord no válido');
        }

        await channel.send({ 
            content: Message || 'PDF procesado',
            files: [tempPath]
        });

        await sendLog({ event: 'mensaje_enviado', correlationId: CorrelationId, recipient: Recipient });
        fs.unlinkSync(tempPath);
        await discordClient.destroy();
        return { success: true };
    } catch (err) {
        await sendLog({ event: 'error_envio_mensaje', correlationId: CorrelationId, error: err.message });
        fs.unlinkSync(tempPath);
        throw err;
    }
};
