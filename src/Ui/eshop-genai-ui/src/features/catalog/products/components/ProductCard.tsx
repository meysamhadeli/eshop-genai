import { Link } from 'react-router-dom'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { ProductDto } from '@/entities/ProductDto'
import { api } from '@/shared/lib/axiosInstance'
import fallbackImg from '@/assets/images/default_product.jpg';

// Price formatting utility (can be moved to a shared utils file if used elsewhere)
const formatPrice = (price: number) => {
  return new Intl.NumberFormat('en-US', {
    style: 'decimal',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  }).format(price);
};

interface Props {
  product: ProductDto
}

export default function ProductCard({ product }: Props) {
  const queryClient = useQueryClient()

  const addToBasket = useMutation({
    mutationFn: () =>
      api.post('/api/v1/basket', {
        userId: 'user-123',
        productId: product.id,
        quantity: 1, // Changed from 0 to 1 as default add to cart quantity
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['basket'] })
    },
  })

  return (
    <div
      className="
        block
        border 
        rounded-lg 
        overflow-hidden 
        shadow-sm 
        bg-white
        hover:shadow-xl 
        hover:scale-105 
        transition-all 
        duration-300 
        ease-in-out
        transform
        will-change-transform
      "
    >
      <Link to={`/product/${product.id}`} className="block">
        <div className="aspect-square overflow-hidden">
          <img
            src={product.imageUrl ? `${import.meta.env.VITE_GATEWAY_BASE_URL}/catalog/${product.imageUrl}` : fallbackImg}
            alt={product.name}
            className="
              w-full h-full object-cover
              transition-transform duration-500 ease-in-out
              hover:scale-110
            "
          />
        </div>
        <div className="p-4">
          <h3 className="font-semibold text-lg text-gray-800 line-clamp-1">{product.name}</h3>
          <p className="text-gray-600 text-sm line-clamp-2 mt-1">{product.description}</p>
          <p className="text-amazon-dark font-bold mt-2 text-lg">
            ${formatPrice(product.price)}
          </p>
        </div>
      </Link>
    </div>
  )
}