import { useQuery } from '@tanstack/react-query'
import ProductCard from '@/features/catalog/products/components/ProductCard'
import { FiRefreshCw } from 'react-icons/fi'
import { getRecommendations } from '@/features/catalog/products/services/product-service'

interface RecommendationSectionProps {
  userId: string
  title?: string
  maxItems?: number
  showRefresh?: boolean
}

// Skeleton component for loading state
const RecommendationSkeleton = ({ count = 4 }: { count?: number }) => (
  <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
    {Array.from({ length: count }).map((_, index) => (
      <div key={index} className="animate-pulse">
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
  showRefresh = true
}: RecommendationSectionProps) {
  const { data, isLoading, error, refetch, isFetching } = useQuery({
    queryKey: ['recommendations', userId],
    queryFn: () => getRecommendations(userId, 1, maxItems).then(res => res.data),
    refetchOnWindowFocus: false,
    enabled: !!userId,
    staleTime: 1000 * 60 * 5,
  })

  const handleRefresh = () => {
    refetch()
  }

  // Show shimmer effect when fetching new data
  if (isFetching && !isLoading) {
    return (
      <section className="my-8">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-2xl font-bold text-gray-800">{title}</h2>
          {showRefresh && (
            <button
              onClick={handleRefresh}
              disabled={true}
              className="flex items-center gap-2 px-3 py-1 text-sm text-gray-400 border border-gray-200 rounded"
            >
              <FiRefreshCw className="w-4 h-4 animate-spin" />
              Refreshing...
            </button>
          )}
        </div>
        <div className="relative overflow-hidden">
          <RecommendationSkeleton count={maxItems} />
          <div className="absolute inset-0 -translate-x-full animate-shimmer bg-gradient-to-r from-transparent via-white/30 to-transparent"></div>
        </div>
      </section>
    )
  }

  if (isLoading) {
    return (
      <section className="my-8">
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-2xl font-bold text-gray-800">{title}</h2>
          {showRefresh && (
            <button
              onClick={handleRefresh}
              disabled={true}
              className="flex items-center gap-2 px-3 py-1 text-sm text-gray-400 border border-gray-200 rounded"
            >
              <FiRefreshCw className="w-4 h-4" />
              Refresh
            </button>
          )}
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
        {showRefresh && (
          <button
            onClick={handleRefresh}
            className="flex items-center gap-2 px-3 py-1 text-sm text-gray-600 hover:text-gray-800 border border-gray-300 rounded hover:bg-gray-50 transition-colors"
          >
            <FiRefreshCw className="w-4 h-4" />
            Refresh
          </button>
        )}
      </div>

      {data.explanation && (
        <div className="mb-4 p-3 bg-blue-50 border border-blue-200 rounded-lg">
          <p className="text-sm text-blue-800 italic">"{data.explanation}"</p>
        </div>
      )}

      <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
        {data.items.map((product) => (
          <ProductCard key={product.id} product={product} />
        ))}
      </div>

      <div className="mt-4 text-center">
        <p className="text-sm text-gray-500">
          Based on your activity and preferences
        </p>
      </div>
    </section>
  )
}