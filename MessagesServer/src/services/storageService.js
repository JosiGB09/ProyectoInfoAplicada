import axios from 'axios';
import dotenv from 'dotenv';
dotenv.config();

export const getFileFromStorage = async (correlationId) => {
    const url = `${process.env.STORAGE_SERVER_URL}/${correlationId}`;
    const response = await axios.get(url, { responseType: 'arraybuffer' });
    return response.data;// Devuelve el archivo como un buffer
};