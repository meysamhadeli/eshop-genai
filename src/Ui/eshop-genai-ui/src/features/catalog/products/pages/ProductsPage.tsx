import ProductList from '@/features/catalog/products/components/ProductList'

export default function ProductsPage() {
  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">All Products</h1>
      <ProductList />
    </div>
  )
}