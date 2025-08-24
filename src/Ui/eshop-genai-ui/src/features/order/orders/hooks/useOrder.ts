import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createOrder } from '@/features/order/orders/services/order-service'
import { clearBasket } from '@/features/basket/baskets/services/basket-service'
import { toast } from 'react-toastify'
import type { ProblemDetails } from '@/shared/models/ProblemDetails'
import { showErrorToast } from '@/shared/lib/toast'
import type { CreateOrderRequest } from '@/features/order/orders/models/OrderDto'

export const useCreateOrder = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async (request: CreateOrderRequest) => {
      const orderResponse = await createOrder(request)
      
      // Clear basket after successful order creation
      if (orderResponse.data) {
        await clearBasket(request.userId)
      }
      
      return orderResponse.data
    },
    onSuccess: (order) => {
      // Invalidate basket query to reflect empty basket
      queryClient.invalidateQueries({ queryKey: ['basket'] })
      
      toast.success(`Order with number: ${order?.id} created successfully!`, {
        position: "top-right",
        autoClose: 5000,
        hideProgressBar: false,
        closeOnClick: true,
        pauseOnHover: true,
        draggable: true,
      })
    },
    onError: (error: Error & { response?: { data: ProblemDetails } }) => {
      if (error.response?.data) {
        showErrorToast(error.response.data)
      } else {
        toast.error('Failed to create order. Please try again.', {
          position: "top-right",
          autoClose: 5000,
          hideProgressBar: false,
          closeOnClick: true,
          pauseOnHover: true,
          draggable: true,
        })
      }
    }
  })
}