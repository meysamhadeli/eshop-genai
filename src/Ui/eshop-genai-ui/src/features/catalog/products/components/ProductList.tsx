import { useQuery } from '@tanstack/react-query'
import { fetchProducts } from '@/features/catalog/products/services/product-service'
import ProductCard from '@/features/catalog/products/components/ProductCard'
import { useState } from 'react'
import type { ProductDto } from '@/features/catalog/products/models/ProductDto'
import { FiSearch } from 'react-icons/fi'

export default function ProductList() {
  const [searchInput, setSearchInput] = useState('')
  const [searchTerm, setSearchTerm] = useState('')

  const { data, isLoading, error } = useQuery({
    queryKey: ['products', searchTerm],
    queryFn: () => fetchProducts(searchTerm).then(res => res.data),
  })

  const handleSearch = () => {
    setSearchTerm(searchInput) // This triggers the query
  }

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleSearch()
    }
  }

  if (isLoading) return <p className="text-center">Loading products...</p>
  if (error) return <p className="text-red-500">Failed to load products.</p>

  return (
    <div>
      <div className="mb-6 flex gap-2 items-center">
        <div className="relative flex-1 max-w-md">
          <input
            type="text"
            placeholder="Search products..."
            value={searchInput}
            onChange={e => setSearchInput(e.target.value)}
            onKeyPress={handleKeyPress}
            className="border p-2 rounded pl-10 w-full"
          />
          <FiSearch className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
        </div>
        <button
          onClick={handleSearch}
          className="bg-blue-500 text-white p-3 rounded hover:bg-blue-600 transition-colors flex items-center justify-center"
        >
          <FiSearch className="w-5 h-5" />
        </button>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
        {data?.items.map((product: ProductDto) => (
          <ProductCard key={product.id} product={product} />
        ))}
      </div>

      <div className="mt-6 text-sm text-gray-500">
        Showing {data?.items.length} of {data?.totalCount} products
        {searchTerm && ` for "${searchTerm}"`}
      </div>
    </div>
  )
}