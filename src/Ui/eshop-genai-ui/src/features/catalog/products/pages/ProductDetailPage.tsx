import { useParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { fetchProductById } from '@/features/catalog/products/services/product-service'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, useEffect } from 'react'
import type { ProductDto } from '@/features/catalog/products/models/ProductDto'
import fallbackImg from '@/assets/images/default_product.jpg'
import { fetchBasket, updateBasketItem } from '@/features/basket/baskets/services/basket-service'

// Price formatting utility
const formatPrice = (price: number) => {
  return new Intl.NumberFormat('en-US', {
    style: 'decimal',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(price);
};

export default function ProductDetailPage() {
  const { id } = useParams<{ id: string }>()
  const queryClient = useQueryClient()

  const [quantity, setQuantity] = useState(0)
  const [inputValue, setInputValue] = useState('0')

  const { data: product, isLoading, error } = useQuery<ProductDto>({
    queryKey: ['product', id],
    queryFn: () => fetchProductById(id!).then(res => res.data),
    enabled: !!id,
  })

  const { data: basket } = useQuery({
    queryKey: ['basket'],
    queryFn: () => fetchBasket('user-123').then(res => res.data),
    initialData: {
      id: '',
      userId: 'user-123',
      items: [],
      createdAt: new Date().toISOString(),
      lastModified: null,
    },
  })

  // Sync quantity from basket
  useEffect(() => {
    const existingItem = basket?.items.find(item => item.productId === id)
    if (existingItem) {
      setQuantity(existingItem.quantity)
      setInputValue(existingItem.quantity.toString())
    } else {
      setQuantity(0)
      setInputValue('0')
    }
  }, [basket, id])

  const updateBasket = useMutation({
    mutationFn: (qty: number) =>  updateBasketItem(id!, qty),
    onSuccess: () => {
      queryClient.refetchQueries({ queryKey: ['basket'] })
    },
  })

  const handleIncrement = () => {
    const newQty = quantity + 1
    updateQuantity(newQty)
  }

  const handleDecrement = () => {
    const newQty = Math.max(0, quantity - 1)
    updateQuantity(newQty)
  }

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value
    // Allow empty string (will be treated as 0) or numeric values
    if (value === '' || /^\d+$/.test(value)) {
      setInputValue(value)
    }
  }

  const handleInputBlur = () => {
    let newQty = parseInt(inputValue) || 0
    newQty = Math.max(0, newQty) // Ensure it's not negative
    updateQuantity(newQty)
  }

  const updateQuantity = (newQty: number) => {
    setQuantity(newQty)
    setInputValue(newQty.toString())
    updateBasket.mutate(newQty)
  }

  if (isLoading) return <p className="text-center py-10">Loading product...</p>
  if (error || !product) return <p className="text-red-500 text-center">Product not found.</p>

  return (
    <div className="grid md:grid-cols-2 gap-10 px-4 py-6 max-w-6xl mx-auto">
      <div>
        <img
          src={product.imageUrl ? `${import.meta.env.VITE_GATEWAY_BASE_URL}/catalog/${product.imageUrl}` : fallbackImg}
          alt={product.name}
          className="w-full h-auto max-h-96 object-contain rounded-lg shadow-lg"
        />
      </div>

      <div>
        <h1 className="text-3xl font-bold mb-3 text-gray-800">{product.name}</h1>
        <p className="text-gray-700 mb-6 leading-relaxed">{product.description}</p>
        <p className="text-amazon-dark text-2xl font-bold mb-6">
          ${formatPrice(product.price)}
        </p>

        <div className="flex items-center gap-4 mb-6">
          <label className="text-sm font-medium text-gray-700">Quantity:</label>
          <button
            type="button"
            onClick={handleDecrement}
            disabled={quantity <= 0 || updateBasket.isPending}
            className="w-10 h-10 flex items-center justify-center bg-gray-200 hover:bg-gray-300 disabled:opacity-50 rounded-full font-bold transition"
          >
            −
          </button>
          <input
            type="text"
            value={inputValue}
            onChange={handleInputChange}
            onBlur={handleInputBlur}
            className="w-16 h-10 border border-gray-300 rounded text-center font-medium"
            disabled={updateBasket.isPending}
          />
          <button
            type="button"
            onClick={handleIncrement}
            disabled={updateBasket.isPending}
            className="w-10 h-10 flex items-center justify-center bg-gray-200 hover:bg-gray-300 disabled:opacity-50 rounded-full font-bold transition"
          >
            +
          </button>
        </div>

        {updateBasket.isPending && (
          <p className="text-sm text-gray-500">Updating basket...</p>
        )}
      </div>
    </div>
  )
}