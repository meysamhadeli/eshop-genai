
export type BasketItemDto = {
  id: string
  productId: string
  productName: string
  productPrice: number
  productImageUrl: string
  quantity: number
  createdAt: string
}

export type BasketDto = {
  id: string
  userId: string
  items: BasketItemDto[]
  createdAt: string
  lastModified: string | null
}