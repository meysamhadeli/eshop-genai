
export type BasketItemDto = {
  productId: string
  productName: string
  productPrice: number
  productImageUrl: string
  quantity: number
}

export type BasketDto = {
  id: string
  userId: string
  items: BasketItemDto[]
  createdAt: string
  lastModified: string | null
  ExpirationTime: string | null
}