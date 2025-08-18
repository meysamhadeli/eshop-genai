import type { BasketDto } from "@/entities/BasketDto"
import { api } from "@/shared/lib/axiosInstance"


export const fetchBasket = (userId = 'user-123') =>
  api.get<BasketDto>(`basket/api/v1/basket?UserId=${userId}`)

export const addItemToBasket = (productId: string, quantity: number) =>
  api.post('basket/api/v1/basket', { userId: 'user-123', productId, quantity })