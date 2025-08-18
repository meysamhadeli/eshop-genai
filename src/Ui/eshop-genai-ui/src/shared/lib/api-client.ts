import axios, { AxiosError } from 'axios';
import type { ProblemDetails } from '@/shared/models/ProblemDetails';
import { showErrorToast } from '@/shared/lib/toast';

export const api = axios.create({
  baseURL: import.meta.env.VITE_GATEWAY_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

api.interceptors.response.use(
  (response) => response,
  (error: AxiosError<ProblemDetails>) => {
    if (error.response) {
      const { status, data, headers } = error.response;

      const isProblemDetails =
        headers['content-type']?.includes('application/problem+json') ||
        (data && typeof data === 'object' && ('type' in data || 'title' in data || 'status' in data));

      if (isProblemDetails) {
        error.response.data = data;
        showErrorToast(data);
      } else {
        const fallback: ProblemDetails = {
          type: `https://httpstatuses.com/${error.response?.status || 500}`,
          title: error.response?.statusText || "Internal Server Error",
          status: status,
          detail: error.message,
          instance: error.config?.url,
        };
        
        showErrorToast(fallback);
        error.response.data = fallback;
      }
    }

    return Promise.reject(error.response?.data);
  }
);

