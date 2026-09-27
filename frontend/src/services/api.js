import axios from 'axios';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5030/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

export default api;
