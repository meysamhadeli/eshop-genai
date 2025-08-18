import type { ProblemDetails } from '@/shared/models/problem-details';
import { toast } from 'react-toastify';

export function showErrorToast(problemDetails: ProblemDetails): void {
    const message = problemDetails.title || "An error occurred";

    toast.error(message, {
      position: "top-right",
      autoClose: 5000,
      hideProgressBar: false,
      closeOnClick: true,
      pauseOnHover: true,
      draggable: true,
      progress: undefined,
    });
}