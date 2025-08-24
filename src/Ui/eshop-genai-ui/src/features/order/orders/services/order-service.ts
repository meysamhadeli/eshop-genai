import { api } from "@/shared/lib/api-client"
import type { CreateOrderRequest, OrderDto } from "@/features/order/orders/models/OrderDto"

export const createOrder = (request: CreateOrderRequest) =>
  api.post<OrderDto>('order/api/v1/order', request)

export const getOrderById = (id: string) =>
  api.get<OrderDto>(`order/api/v1/order/${id}`)