import { Link, useNavigate } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { FaShoppingCart } from 'react-icons/fa'
import { fetchBasket } from '@/features/basket/baskets/api/basketService'

export default function Header() {
  const navigate = useNavigate()

  const { data: basket, isError, isLoading } = useQuery({
    queryKey: ['basket'],
    queryFn: () => fetchBasket('user-123').then(res => res.data),
    refetchOnWindowFocus: false,
    staleTime: 1000 * 60, // 1 minute
    retry: 1,
  })

  const totalItems = basket?.items.reduce((sum, item) => sum + item.quantity, 0) || 0

  return (
    <header className="bg-amazon-dark text-white shadow-md sticky top-0 z-50">
      <div className="container mx-auto px-4 py-3 flex items-center justify-between">
        {/* Logo */}
        <Link
          to="/"
          className="text-2xl font-bold text-amazon-light hover:text-purple-700 transition"
        >
          eShop
        </Link>

        {/* Navigation & Basket */}
        <nav className="flex items-center gap-8">

          {/* Basket Button */}
          <button
            onClick={() => navigate('/basket')}
            className="
              relative flex items-center gap-1 p-2
              text-white hover:text-amazon-light
              transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-amazon-light rounded
            "
            aria-label={`View basket (${totalItems} items)`}
          >
            {/* Shopping Cart Icon - White */}
            <FaShoppingCart className="h-6 w-6 text-gray-300" />
            {/* Red Badge */}
            {!isLoading && totalItems > 0 && (
              <span
                className="
                  absolute -top-1 -right-1
                  bg-red-500 text-white text-xs font-bold
                  rounded-full w-5 h-5 flex items-center justify-center
                  animate-pulse
                "
                aria-hidden="true"
              >
                {totalItems}
              </span>
            )}
          </button>
        </nav>
      </div>
    </header>
  )
}