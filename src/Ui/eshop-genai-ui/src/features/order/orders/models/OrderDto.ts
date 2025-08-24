export type OrderDto = {
  id: string
  userId: string
  status: string
  totalAmount: number
  shippingAddress: string
  items: OrderItemDto[]
  orderDate: string
  createdAt: string
  lastModified: string | null
}

export type OrderItemDto = {
  productId: string
  productName: string
  unitPrice: number
  quantity: number
  totalPrice: number
  imageUrl: string
}

export type CreateOrderRequest = {
  userId: string
  shippingAddress: string
}