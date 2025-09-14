import ProductList from '@/features/catalog/products/components/ProductList'
import { useLocation } from 'react-router-dom'
import RecommendationSection from '@/features/catalog/products/components/RecommendationSection'

export default function ProductsPage() {
  const location = useLocation()
  const userId = 'user-123'
  const isHomePage = !location.search.includes('q=')


  return (
    <div>      
      {/* Main product list - shows all products on home page, search results on search */}
      <ProductList />
      
      {/* Recommendations section at the bottom - only on home page */}
      {isHomePage && (
        <div className="mt-12 pt-8 border-t border-gray-200">
          <RecommendationSection 
            userId={userId} 
            title="Recommended For You"
            maxItems={8}
            showRefresh={true}
          />
        </div>
      )}
    </div>
  )
}