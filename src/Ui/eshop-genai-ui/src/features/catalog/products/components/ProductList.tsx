import { useQuery } from '@tanstack/react-query'
import { fetchProducts } from '@/features/catalog/products/services/productService'
import ProductCard from '@/features/catalog/products/components/ProductCard'
import { useState } from 'react'
import type { ProductDto } from '@/features/catalog/products/models/ProductDto'

export default function ProductList() {
  const [search, setSearch] = useState('')

  const { data, isLoading, error } = useQuery({
    queryKey: ['products', search],
    queryFn: () => fetchProducts(search).then(res => res.data),
  })

  if (isLoading) return <p className="text-center">Loading products...</p>
  if (error) return <p className="text-red-500">Failed to load products.</p>

  return (
    <div>
      <div className="mb-6">
        <input
          type="text"
          placeholder="Search products..."
          value={search}
          onChange={e => setSearch(e.target.value)}
          className="border p-2 rounded w-full max-w-md"
        />
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
        {data?.items.map((product: ProductDto) => (
          <ProductCard key={product.id} product={product} />
        ))}
      </div>

      <div className="mt-6 text-sm text-gray-500">
        Showing {data?.items.length} of {data?.totalCount} products
      </div>
    </div>
  )
}