import { useQuery } from '@tanstack/react-query'
import { useState, useEffect } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import ProductCard from '@/features/catalog/products/components/ProductCard'
import { FiSearch, FiLoader, FiInfo, FiShoppingBag } from 'react-icons/fi'
import type { ProductDto } from '@/features/catalog/products/models/ProductDto'
import { fetchProducts } from '@/features/catalog/products/services/product-service'
import type { PageList } from '@/shared/models/PagedList'

type ProductsResponse = PageList<ProductDto> & {
  explanation?: string
}

// Product Skeleton Component
const ProductSkeleton = ({ count = 8 }: { count?: number }) => (
  <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
    {Array.from({ length: count }).map((_, index) => (
      <div key={index} className="animate-pulse border rounded-lg overflow-hidden bg-white shadow-sm">
        <div className="aspect-square bg-gradient-to-r from-gray-100 to-gray-200"></div>
        <div className="p-4">
          <div className="h-5 bg-gradient-to-r from-gray-100 to-gray-200 rounded mb-3"></div>
          <div className="h-4 bg-gradient-to-r from-gray-100 to-gray-200 rounded mb-2 w-3/4"></div>
          <div className="h-4 bg-gradient-to-r from-gray-100 to-gray-200 rounded w-1/2"></div>
        </div>
      </div>
    ))}
  </div>
)

export default function ProductList() {
  const location = useLocation()
  const navigate = useNavigate()

  // Parse query param `q` from URL
  const urlParams = new URLSearchParams(location.search)
  const initialSearchTerm = urlParams.get('q') || ''
  const [searchInput, setSearchInput] = useState(initialSearchTerm)
  const [searchTerm, setSearchTerm] = useState(initialSearchTerm)

  // Properly type the useQuery hook
  const { data, isLoading, error, isFetching } = useQuery<ProductsResponse>({
    queryKey: ['products', searchTerm],
    queryFn: () => fetchProducts(searchTerm).then(res => res.data as ProductsResponse),
    refetchOnWindowFocus: false,
  })

  const handleSearch = () => {
    const trimmed = searchInput.trim()
    if (trimmed) {
      navigate(`/search?q=${encodeURIComponent(trimmed)}`) 
    } else {
      navigate('/') 
    }
  }

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleSearch()
    }
  }

  const handleClearSearch = () => {
    setSearchInput('')
    setSearchTerm('')
    navigate('/') 
  }

  useEffect(() => {
    const currentQ = urlParams.get('q')
    if (currentQ !== searchTerm) {
      setSearchInput(currentQ || '')
      setSearchTerm(currentQ || '')
    }
  }, [location.search]) 

  // Check if we have search results - safely access data properties
  const hasResults = data?.items && data.items.length > 0
  const isEmptySearch = searchTerm && !hasResults && !isLoading && !error

  // Render loading state with skeleton
  if (isLoading) {
    return (
      <div>
        <div className="mb-6 flex gap-2 items-center">
          <div className="relative flex-1 max-w-md">
            <input
              type="text"
              placeholder="Search products..."
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              onKeyPress={handleKeyPress}
              className="border p-2 rounded pl-10 w-full focus:outline-none focus:ring-2 focus:ring-amazon-light"
              disabled={isLoading}
            />
            <FiSearch className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />

            {searchTerm && (
              <button
                onClick={handleClearSearch}
                className="absolute right-10 top-1/2 transform -translate-y-1/2 text-gray-500 hover:text-gray-700"
                aria-label="Clear search"
                disabled={isLoading}
              >
                ×
              </button>
            )}
          </div>

          <button
            onClick={handleSearch}
            disabled={isLoading || !searchInput.trim()}
            className="
              bg-blue-500 text-white p-3 rounded hover:bg-blue-600 
              transition-colors flex items-center justify-center
              disabled:opacity-50 disabled:cursor-not-allowed
              min-w-[44px] min-h-[44px]
            "
            aria-label="Search products"
          >
            <FiSearch className="w-5 h-5" />
          </button>
        </div>

        <div className="relative overflow-hidden">
          <ProductSkeleton count={8} />
          <div className="absolute inset-0 -translate-x-full animate-shimmer bg-gradient-to-r from-transparent via-white/20 to-transparent"></div>
        </div>
      </div>
    )
  }

  // Render error state
  if (error) {
    return (
      <div>
        <div className="mb-6 flex gap-2 items-center">
          <div className="relative flex-1 max-w-md">
            <input
              type="text"
              placeholder="Search products..."
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              onKeyPress={handleKeyPress}
              className="border p-2 rounded pl-10 w-full focus:outline-none focus:ring-2 focus:ring-amazon-light"
            />
            <FiSearch className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />

            {searchTerm && (
              <button
                onClick={handleClearSearch}
                className="absolute right-10 top-1/2 transform -translate-y-1/2 text-gray-500 hover:text-gray-700"
                aria-label="Clear search"
              >
                ×
              </button>
            )}
          </div>

          <button
            onClick={handleSearch}
            disabled={isLoading || !searchInput.trim()}
            className="
              bg-blue-500 text-white p-3 rounded hover:bg-blue-600 
              transition-colors flex items-center justify-center
              disabled:opacity-50 disabled:cursor-not-allowed
            "
            aria-label="Search products"
          >
            <FiSearch className="w-5 h-5" />
          </button>
        </div>

        <div className="text-center py-12">
          <div className="text-red-500 text-4xl mb-4">⚠️</div>
          <h3 className="text-lg font-semibold text-gray-800 mb-2">Failed to load products</h3>
          <p className="text-gray-600 text-sm">
            {error instanceof Error ? error.message : 'Please try refreshing the page.'}
          </p>
          <button
            onClick={() => window.location.reload()}
            className="mt-4 px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-600 transition-colors"
          >
            Refresh Page
          </button>
        </div>
      </div>
    )
  }

  return (
    <div>
      <div className="mb-6 flex gap-2 items-center">
        <div className="relative flex-1 max-w-md">
          <input
            type="text"
            placeholder="Search products..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            onKeyPress={handleKeyPress}
            className="border p-2 rounded pl-10 w-full focus:outline-none focus:ring-2 focus:ring-amazon-light"
          />
          
          {/* Show loading spinner or search icon based on isFetching state */}
          {isFetching ? (
            <FiLoader className="absolute left-3 top-1/2 transform -translate-y-1/2 text-blue-500 w-4 h-4 animate-spin" />
          ) : (
            <FiSearch className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
          )}

          {searchTerm && (
            <button
              onClick={handleClearSearch}
              className="absolute right-10 top-1/2 transform -translate-y-1/2 text-gray-500 hover:text-gray-700"
              aria-label="Clear search"
              disabled={isFetching}
            >
              ×
            </button>
          )}
        </div>

        <button
          onClick={handleSearch}
          disabled={isFetching || !searchInput.trim()}
          className="
            bg-blue-500 text-white p-3 rounded hover:bg-blue-600 
            transition-colors flex items-center justify-center
            disabled:opacity-50 disabled:cursor-not-allowed
            min-w-[44px] min-h-[44px]
          "
          aria-label="Search products"
        >
          {isFetching ? (
            <FiLoader className="w-5 h-5 animate-spin" />
          ) : (
            <FiSearch className="w-5 h-5" />
          )}
        </button>
      </div>

      {/* Search Explanation */}
      {data?.explanation && (
        <div className="mb-6 p-4 bg-blue-50 border border-blue-200 rounded-lg">
          <div className="flex items-start gap-3">
            <FiInfo className="w-5 h-5 text-blue-600 mt-0.5 flex-shrink-0" />
            <div>
              <h3 className="font-semibold text-blue-800 mb-1">Search Insight</h3>
              <p className="text-blue-700 text-sm leading-relaxed">{data.explanation}</p>
            </div>
          </div>
        </div>
      )}

      {/* Empty Search Results */}
      {isEmptySearch && (
        <div className="text-center py-16">
          <div className="flex justify-center mb-4">
            <div className="w-16 h-16 bg-gray-100 rounded-full flex items-center justify-center">
              <FiShoppingBag className="w-8 h-8 text-gray-400" />
            </div>
          </div>
          <h3 className="text-xl font-semibold text-gray-800 mb-2">No products found</h3>
          <p className="text-gray-600 mb-6">
            We couldn't find any products matching "{searchTerm}". Try different keywords or browse our categories.
          </p>
          <button
            onClick={handleClearSearch}
            className="px-6 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors"
          >
            Browse All Products
          </button>
        </div>
      )}

      {/* Search Results */}
      {hasResults && (
        <>
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
            {data!.items.map((product: ProductDto) => (
              <ProductCard key={product.id} product={product} />
            ))}
          </div>

          <div className="mt-6 text-sm text-gray-500">
            Showing {data!.items.length} of {data!.totalCount} products
            {searchTerm ? ` for "${searchTerm}"` : ' (all products)'}
            {data!.hasExactMatches === false && (
              <span className="ml-2 text-amber-600">
                • Showing similar results
              </span>
            )}
          </div>
        </>
      )}

      {/* Home page with no products at all */}
      {!searchTerm && (!data?.items || data.items.length === 0) && (
        <div className="text-center py-12">
          <div className="flex justify-center mb-4">
            <div className="w-20 h-20 bg-gray-100 rounded-full flex items-center justify-center">
              <FiSearch className="w-10 h-10 text-gray-400" />
            </div>
          </div>
          <h3 className="text-xl font-semibold text-gray-800 mb-2">Discover Products</h3>
          <p className="text-gray-600">
            Search for products above or browse our entire collection
          </p>
        </div>
      )}
    </div>
  )
}