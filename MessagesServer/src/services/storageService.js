import axios from 'axios';
import dotenv from 'dotenv';
dotenv.config();

export const getFileFromStorage = async (correlationId) => {
    const url = `${process.env.STORAGE_SERVER_URL}/${correlationId}`;

    const response = await axios.get(url, { responseType: 'json' });
    const data = response.data;

    if (data && typeof data === 'object' && typeof data.pdfData === 'string') {
        try {
            return Buffer.from(data.pdfData, 'base64');
        } catch (err) {
            throw new Error('Error decodificando pdfData base64: ' + err.message);
        }
    }
};