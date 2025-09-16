import { useQuery } from '@tanstack/react-query'
import ProductCard from '@/features/catalog/products/components/ProductCard'
import { FiChevronLeft, FiChevronRight } from 'react-icons/fi'
import { useState, useRef } from 'react'
import { getRecommendations } from '@/features/recomendation/recomendations/services/recommendation-service'

interface RecommendationSectionProps {
  userId: string
  title?: string
  maxItems?: number
  showPagination?: boolean
}

// Skeleton component for loading state
const RecommendationSkeleton = ({ count = 4 }: { count?: number }) => (
  <div className="flex gap-6 overflow-hidden">
    {Array.from({ length: count }).map((_, index) => (
      <div key={index} className="flex-shrink-0 w-64 animate-pulse">
        <div className="aspect-square bg-gradient-to-r from-gray-100 to-gray-200 rounded-lg mb-4"></div>
        <div className="h-4 bg-gradient-to-r from-gray-100 to-gray-200 rounded mb-2"></div>
        <div className="h-3 bg-gradient-to-r from-gray-100 to-gray-200 rounded w-3/4"></div>
        <div className="h-3 bg-gradient-to-r from-gray-100 to-gray-200 rounded w-1/2 mt-2"></div>
      </div>
    ))}
  </div>
)

export default function RecommendationSection({
  userId,
  title = "Recommended For You",
  maxItems = 8,
  showPagination = true
}: RecommendationSectionProps) {
  const [currentPage, setCurrentPage] = useState(1)
  const scrollContainerRef = useRef<HTMLDivElement>(null)

  const { data, isLoading, error, isFetching } = useQuery({
    queryKey: ['recommendations', userId, currentPage],
    queryFn: () => getRecommendations(userId, currentPage, maxItems).then(res => res.data),
    refetchOnWindowFocus: false,
    enabled: !!userId,
    staleTime: 1000 * 60 * 5,
  })

  const scrollLeft = () => {
    if (scrollContainerRef.current) {
      scrollContainerRef.current.scrollBy({ left: -300, behavior: 'smooth' })
    }
  }

  const scrollRight = () => {
    if (scrollContainerRef.current) {
      scrollContainerRef.current.scrollBy({ left: 300, behavior: 'smooth' })
    }
  }

  const handleNextPage = () => {
    setCurrentPage(prev => prev + 1)
  }

  const handlePrevPage = () => {
    setCurrentPage(prev => Math.max(1, prev - 1))
  }

  const totalPages = data ? Math.ceil(data.totalCount / maxItems) : 1
  const hasNextPage = currentPage < totalPages
  const hasPrevPage = currentPage > 1

  if (isLoading) {
    return (
      <section className="my-8">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-2xl font-bold text-gray-800">{title}</h2>
        </div>
        <RecommendationSkeleton count={maxItems} />
      </section>
    )
  }

  if (error) {
    console.error('Recommendations error:', error)
    return null
  }

  if (!data?.items?.length) {
    return null
  }

  return (
    <section className="my-8">
      <div className="flex items-center justify-between mb-6">
        <h2 className="text-2xl font-bold text-gray-800">{title}</h2>
        {showPagination && totalPages > 1 && (
          <div className="flex items-center gap-2">
            <button
              onClick={handlePrevPage}
              disabled={!hasPrevPage || isFetching}
              className="
                w-8 h-8 flex items-center justify-center
                bg-white border border-gray-300 rounded-full
                text-gray-600 hover:text-gray-800 hover:bg-gray-50
                disabled:opacity-30 disabled:cursor-not-allowed
                transition-all duration-200 shadow-sm
                hover:shadow-md focus:outline-none focus:ring-2 focus:ring-blue-500
              "
              aria-label="Previous page"
            >
              <FiChevronLeft className="w-5 h-5" />
            </button>
            
            <span className="text-sm text-gray-600 mx-1">
              {currentPage} / {totalPages}
            </span>
            
            <button
              onClick={handleNextPage}
              disabled={!hasNextPage || isFetching}
              className="
                w-8 h-8 flex items-center justify-center
                bg-white border border-gray-300 rounded-full
                text-gray-600 hover:text-gray-800 hover:bg-gray-50
                disabled:opacity-30 disabled:cursor-not-allowed
                transition-all duration-200 shadow-sm
                hover:shadow-md focus:outline-none focus:ring-2 focus:ring-blue-500
              "
              aria-label="Next page"
            >
              <FiChevronRight className="w-5 h-5" />
            </button>
          </div>
        )}
      </div>

      {data.explanation && (
        <div className="mb-4 p-4 bg-blue-50 border border-blue-200 rounded-lg">
          <p className="text-sm text-blue-800 italic">"{data.explanation}"</p>
        </div>
      )}

      {/* Horizontal Scrolling Container with Arrows */}
      <div className="relative group">
        {/* Left Arrow */}
        <button
          onClick={scrollLeft}
          className="
            absolute left-0 top-1/2 transform -translate-y-1/2 -translate-x-4
            w-12 h-12 flex items-center justify-center
            bg-white border border-gray-300 rounded-full
            text-gray-600 hover:text-gray-800 hover:bg-gray-50
            shadow-xl hover:shadow-2xl
            transition-all duration-300 z-20
            focus:outline-none focus:ring-4 focus:ring-blue-200
            opacity-0 group-hover:opacity-100
            hidden md:flex
          "
          aria-label="Scroll left"
        >
          <FiChevronLeft className="w-6 h-6" />
        </button>

        {/* Right Arrow */}
        <button
          onClick={scrollRight}
          className="
            absolute right-0 top-1/2 transform -translate-y-1/2 translate-x-4
            w-12 h-12 flex items-center justify-center
            bg-white border border-gray-300 rounded-full
            text-gray-600 hover:text-gray-800 hover:bg-gray-50
            shadow-xl hover:shadow-2xl
            transition-all duration-300 z-20
            focus:outline-none focus:ring-4 focus:ring-blue-200
            opacity-0 group-hover:opacity-100
            hidden md:flex
          "
          aria-label="Scroll right"
        >
          <FiChevronRight className="w-6 h-6" />
        </button>

        {/* Horizontal Scrolling Products */}
        <div
          ref={scrollContainerRef}
          className="
            flex gap-6 overflow-x-auto pb-4
            scrollbar-hide
            scroll-smooth
            -mx-4 px-4
          "
          style={{ scrollbarWidth: 'none', msOverflowStyle: 'none' }}
        >
          {data.items.map((product) => (
            <div key={product.id} className="flex-shrink-0 w-64">
              <ProductCard product={product} />
            </div>
          ))}
        </div>
      </div>

      <div className="mt-4 text-center">
        <p className="text-sm text-gray-500">
          Based on your activity and preferences • Page {currentPage} of {totalPages}
        </p>
      </div>
    </section>
  )
}