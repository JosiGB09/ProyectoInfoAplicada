import express from 'express';
import messaginRoutes from './routes/messaging.js';

const app = express();
app.use(express.json());

app.use('/api/messaging', messaginRoutes);

export default app;