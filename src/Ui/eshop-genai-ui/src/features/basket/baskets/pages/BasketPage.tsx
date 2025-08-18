import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import { fetchBasket } from '@/features/basket/baskets/api/basketService'
import BasketItem from '@/features/basket/baskets/components/BasketItem'
import { api } from '@/shared/lib/axiosInstance'
import type { BasketDto } from '@/entities/BasketDto'
import { FaCreditCard } from 'react-icons/fa'

export default function BasketPage() {
  const queryClient = useQueryClient()

  const {  data, isLoading, isError } = useQuery({
    queryKey: ['basket'],
    queryFn: () => fetchBasket('user-123').then(res => res.data),
    initialData: {
      id: '',
      userId: 'user-123',
      items: [],
      createdAt: new Date().toISOString(),
      lastModified: null,
    } satisfies BasketDto,
  })

  var basket = data

  const updateMutation = useMutation({
    mutationFn: ({ productId, quantity }: { productId: string; quantity: number }) => {
      console.log('🚀 Updating basket item:', { productId, quantity })
      return api.put('basket/api/v1/basket', {
        userId: 'user-123',
        productId,
        quantity,
      })
    },
    onSuccess: () => {
      console.log('✅ Basket updated')
      queryClient.refetchQueries({ queryKey: ['basket'] })
    },
    onError: (err: any) => {
      console.error('❌ API Error:', err.message)
    },
  })

  if (isLoading) return <p className="text-center py-6">Loading your basket...</p>
  if (isError) return <p className="text-red-500 text-center">Failed to load basket.</p>

  const total = basket.items.reduce((sum, item) => sum + item.productPrice * item.quantity, 0)

  return (
    <div className="max-w-4xl mx-auto px-4 py-6">
      <h1 className="text-2xl font-bold mb-6 text-gray-800">Your Shopping Basket</h1>

      {basket.items.length === 0 ? (
        <p className="text-gray-500 text-center py-10 text-lg">Your basket is empty.</p>
      ) : (
        <>
          <div className="space-y-6">
            {basket.items.map(item => (
              <BasketItem
                key={item.id}
                item={item}
                onUpdate={quantity => {
                  console.log(`Update ${item.productId} to quantity ${quantity}`)
                  updateMutation.mutate({ productId: item.productId, quantity })
                }}
              />
            ))}
          </div>

          <div className="mt-8 p-6 bg-white rounded-lg border border-gray-200 shadow-sm">
            <div className="flex justify-between items-center text-xl font-bold text-gray-800">
              <span>Total:</span>
              <span className="text-amazon-dark">${total.toFixed(2)}</span>
            </div>
            <button className="w-full mt-4 flex items-center justify-center gap-2 bg-amazon-light text-amazon-black py-3 font-medium rounded hover:bg-amazon-dark hover:text-purple-700 transition">
              <FaCreditCard className="text-lg" />
              <span>Proceed to Checkout</span>
            </button>
          </div>
        </>
      )}
    </div>
  )
}