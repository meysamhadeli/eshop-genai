import { useQuery } from '@tanstack/react-query'
import { useState, useEffect } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import ProductCard from '@/features/catalog/products/components/ProductCard'
import { FiSearch, FiLoader, FiInfo, FiChevronLeft, FiChevronRight, FiArrowRight } from 'react-icons/fi'
import type { ProductDto } from '@/features/catalog/products/models/ProductDto'
import { fetchProducts } from '@/features/catalog/products/services/product-service'
import type { PageList } from '@/shared/models/PagedList'

type ProductsResponse = PageList<ProductDto> & {
  explanation?: string
}

// Product Skeleton Component
const ProductSkeleton = ({ count = 4 }: { count?: number }) => (
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

  // Parse query params from URL
  const urlParams = new URLSearchParams(location.search)
  const initialSearchTerm = urlParams.get('q') || ''
  const initialPage = parseInt(urlParams.get('page') || '1')
  
  const [searchInput, setSearchInput] = useState(initialSearchTerm)
  const [searchTerm, setSearchTerm] = useState(initialSearchTerm)
  const [currentPage, setCurrentPage] = useState(initialPage)
  
  // Set page sizes - show 8 on home, 12 on products page
  const homePageSize = 8
  const productsPageSize = 12
  const pageSize = location.pathname === '/' ? homePageSize : productsPageSize

  // Properly type the useQuery hook
  const { data, isLoading, error, isFetching } = useQuery<ProductsResponse>({
    queryKey: ['products', searchTerm, currentPage, location.pathname],
    queryFn: () => fetchProducts(searchTerm, currentPage, pageSize).then(res => res.data as ProductsResponse),
    refetchOnWindowFocus: false,
  })

  const handleSearch = () => {
    const trimmed = searchInput.trim()
    const params = new URLSearchParams()
    if (trimmed) params.set('q', trimmed)
    params.set('page', '1')
    setSearchTerm(trimmed)
    setCurrentPage(1)
    navigate(`/search?${params.toString()}`)
  }

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleSearch()
    }
  }

  const handleClearSearch = () => {
    setSearchInput('')
    setSearchTerm('')
    setCurrentPage(1)
    navigate('/')
  }

  const handlePageChange = (newPage: number) => {
    setCurrentPage(newPage)
    const params = new URLSearchParams(location.search)
    params.set('page', newPage.toString())
    navigate(`${location.pathname}?${params.toString()}`, { replace: true })
  }

  const handleSeeMore = () => {
    setCurrentPage(1)
    navigate('/products?page=1')
  }

  const handleBackToHome = () => {
    setSearchInput('')
    setSearchTerm('')
    setCurrentPage(1)
    navigate('/')
  }

  useEffect(() => {
    const currentQ = urlParams.get('q') || ''
    const currentPage = parseInt(urlParams.get('page') || '1')
    
    setSearchInput(currentQ)
    setSearchTerm(currentQ)
    setCurrentPage(currentPage)
  }, [location.search, location.pathname])

  // Calculate pagination values
  const totalPages = data ? Math.ceil(data.totalCount / pageSize) : 1
  const hasNextPage = currentPage < totalPages
  const hasPrevPage = currentPage > 1

  // Check if we have search results
  const hasResults = data?.items && data.items.length > 0
  const isEmptySearch = searchTerm && !hasResults && !isLoading && !error
  const isHomePage = location.pathname === '/' && !searchTerm && currentPage === 1

  // Check if we should show the "See More" button (only on home page with more products)
  const shouldShowSeeMore = isHomePage && data && data.totalCount > homePageSize

  // Check if we should show pagination (not on home page and multiple pages)
  const shouldShowPagination = !isHomePage && totalPages > 1

  // Render loading state with skeleton
  if (isLoading) {
    return (
      <div>
        {/* Search Header */}
        <div className="mb-6 flex gap-2 items-center flex-wrap">
          <div className="relative flex-1 max-w-md">
            <input
              type="text"
              placeholder="Search products..."
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              onKeyPress={handleKeyPress}
              className="border p-2 rounded pl-10 w-full focus:outline-none focus:ring-2 focus:ring-blue-500"
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

        {/* Loading Skeleton */}
        <div className="relative overflow-hidden">
          <ProductSkeleton count={pageSize} />
          <div className="absolute inset-0 -translate-x-full animate-shimmer bg-gradient-to-r from-transparent via-white/20 to-transparent"></div>
        </div>
      </div>
    )
  }

  // Render error state
  if (error) {
    return (
      <div>
        {/* Search Header */}
        <div className="mb-6 flex gap-2 items-center flex-wrap">
          <div className="relative flex-1 max-w-md">
            <input
              type="text"
              placeholder="Search products..."
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              onKeyPress={handleKeyPress}
              className="border p-2 rounded pl-10 w-full focus:outline-none focus:ring-2 focus:ring-blue-500"
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
            disabled={!searchInput.trim()}
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

        {/* Error Message */}
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
      {/* Search Header */}
      <div className="mb-6 flex gap-2 items-center flex-wrap">
        <div className="relative flex-1 max-w-md">
          <input
            type="text"
            placeholder="Search products..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            onKeyPress={handleKeyPress}
            className="border p-2 rounded pl-10 w-full focus:outline-none focus:ring-2 focus:ring-blue-500"
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

      {/* Back to Home button when not on home page */}
      {!isHomePage && (
        <div className="mb-4">
          <button
            onClick={handleBackToHome}
            className="text-blue-500 hover:text-blue-700 flex items-center gap-1"
          >
            <FiArrowRight className="transform rotate-180 w-4 h-4" />
            Back to Home
          </button>
        </div>
      )}

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
              <FiSearch className="w-8 h-8 text-gray-400" />
            </div>
          </div>
          <h3 className="text-xl font-semibold text-gray-800 mb-2">No products found</h3>
          <p className="text-gray-600 mb-6">
            We couldn't find any products matching "{searchTerm}". Try different keywords.
          </p>
          <button
            onClick={handleClearSearch}
            className="px-6 py-2 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors"
          >
            Clear Search
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

          {/* Right-aligned "See More" Button */}
          {shouldShowSeeMore && (
            <div className="mt-6 flex justify-end">
              <div className="flex flex-col items-end">
                <button
                  onClick={handleSeeMore}
                  className="
                    px-6 py-3 bg-amazon-yellow text-amazon-dark rounded-lg 
                    hover:bg-amazon-yellow-dark transition-colors
                    font-medium text-base shadow-sm hover:shadow-md
                    border border-amazon-yellow-border
                    flex items-center justify-center gap-2
                    w-auto
                  "
                >
                  See more
                  <FiArrowRight className="w-4 h-4" />
                </button>
                <p className="text-sm text-gray-500 mt-2 text-right">
                  Showing {homePageSize} of {data.totalCount} products
                </p>
              </div>
            </div>
          )}

          {/* Pagination Controls for non-home pages */}
          {shouldShowPagination && (
            <div className="mt-8 flex justify-center items-center gap-4 flex-wrap">
              <button
                onClick={() => handlePageChange(currentPage - 1)}
                disabled={!hasPrevPage || isFetching}
                className="flex items-center gap-2 px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                <FiChevronLeft className="w-4 h-4" />
                Previous
              </button>
              
              <div className="flex items-center gap-2">
                <span className="text-sm text-gray-600">
                  Page {currentPage} of {totalPages}
                </span>
                <span className="text-sm text-gray-500">
                  ({data!.totalCount} total products)
                </span>
              </div>
              
              <button
                onClick={() => handlePageChange(currentPage + 1)}
                disabled={!hasNextPage || isFetching}
                className="flex items-center gap-2 px-4 py-2 text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                Next
                <FiChevronRight className="w-4 h-4" />
              </button>
            </div>
          )}

          {!shouldShowSeeMore && !shouldShowPagination && (
            <div className="mt-4 text-sm text-gray-500 text-center">
              Showing {data!.items.length} of {data!.totalCount} products
              {searchTerm ? ` for "${searchTerm}"` : ' (all products)'}
              {data!.hasExactMatches === false && (
                <span className="ml-2 text-amber-600">
                  • Showing similar results
                </span>
              )}
            </div>
          )}
        </>
      )}

      {/* Home page with no products at all */}
      {isHomePage && (!data?.items || data.items.length === 0) && (
        <div className="text-center py-12">
          <div className="flex justify-center mb-4">
            <div className="w-20 h-20 bg-gray-100 rounded-full flex items-center justify-center">
              <FiSearch className="w-10 h-10 text-gray-400" />
            </div>
          </div>
          <h3 className="text-xl font-semibold text-gray-800 mb-2">Discover Products</h3>
          <p className="text-gray-600">
            Search for products above to find what you're looking for
          </p>
        </div>
      )}
    </div>
  )
}