import type { BasketDto } from "@/features/basket/baskets/models/BasketDto"
import { api } from "@/shared/lib/api-client"


export const fetchBasket = (userId = 'user-123') =>
  api.get<BasketDto>(`basket/api/v1/basket?UserId=${userId}`)

export const addItemToBasket = (productId: string, quantity: number) =>
  api.post('basket/api/v1/basket', { userId: 'user-123', productId, quantity })