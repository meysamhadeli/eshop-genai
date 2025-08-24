// features/basket/baskets/components/BasketPage.tsx
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { fetchBasket, updateBasketItem } from '@/features/basket/baskets/services/basket-service'
import BasketItem from '@/features/basket/baskets/components/BasketItem'
import type { BasketDto } from '@/features/basket/baskets/models/BasketDto'
import { FaCreditCard } from 'react-icons/fa'
import { useState } from 'react'
import { useCreateOrder } from '@/features/order/orders/hooks/useOrder'
import ShippingAddressForm from '@/features/order/orders/components/ShippingAddressForm'
import { formatCurrency } from '@/shared/lib/currency'

export default function BasketPage() {
  const queryClient = useQueryClient()
  const [showAddressForm, setShowAddressForm] = useState(false)
  const createOrderMutation = useCreateOrder()

  const { data, isLoading, isError } = useQuery({
    queryKey: ['basket'],
    queryFn: () => fetchBasket('user-123').then(res => res.data),
    initialData: {
      id: '',
      userId: 'user-123',
      items: [],
      createdAt: new Date().toISOString(),
      lastModified: null,
      ExpirationTime: null
    } satisfies BasketDto,
  })

  const basket = data

  const updateMutation = useMutation({
    mutationFn: ({ productId, quantity }: { productId: string; quantity: number }) =>
      updateBasketItem(productId, quantity),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['basket'] })
    }
  })

  const handleProcessOrder = () => {
    setShowAddressForm(true)
  }

  const handleSubmitOrder = async (shippingAddress: string) => {
    await createOrderMutation.mutateAsync({
      userId: 'user-123',
      shippingAddress
    })
    setShowAddressForm(false)
  }

  const handleCancelOrder = () => {
    setShowAddressForm(false)
  }

  if (isLoading) return <p className="text-center py-6">Loading your basket...</p>
  if (isError) return <p className="text-red-500 text-center">Failed to load basket.</p>

  const total = basket.items.reduce((sum, item) => sum + item.productPrice * item.quantity, 0)

  return (
    <div className="max-w-4xl mx-auto px-4 py-6">
      <h1 className="text-2xl font-bold mb-6 text-gray-800">Your Shopping Basket</h1>

      {basket.items.length === 0 ? (
        <div className="text-center py-10">
          <p className="text-gray-500 text-lg mb-4">Your basket is empty.</p>
        </div>
      ) : (
        <>
          <div className="space-y-6">
            {basket.items.map(item => (
              <BasketItem
                key={item.productId}
                item={item}
                onUpdate={quantity => {
                  updateMutation.mutate({ productId: item.productId, quantity })
                }}
              />
            ))}
          </div>

          <div className="mt-8 p-6 bg-white rounded-lg border border-gray-200 shadow-sm">
            <div className="flex justify-between items-center text-xl font-bold text-gray-800 mb-4">
              <span>Total:</span>
              <span className="text-amazon-dark">{formatCurrency(total)}</span>
            </div>

            <button
              onClick={handleProcessOrder}
              disabled={updateMutation.isPending || basket.items.length === 0}
              className="w-full flex items-center justify-center gap-2 bg-amazon-light text-amazon-black py-3 font-medium rounded hover:bg-amazon-dark hover:text-purple-700 transition disabled:opacity-50 disabled:cursor-not-allowed"
            >
              <FaCreditCard className="text-lg" />
              <span>Process Order</span>
            </button>
          </div>
        </>
      )}

      {showAddressForm && (
        <ShippingAddressForm
          onSubmit={handleSubmitOrder}
          onCancel={handleCancelOrder}
          isLoading={createOrderMutation.isPending}
        />
      )}
    </div>
  )
}