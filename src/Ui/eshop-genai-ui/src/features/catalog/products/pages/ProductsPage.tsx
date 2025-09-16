import ProductList from '@/features/catalog/products/components/ProductList'
import RecommendationSection from '@/features/recomendation/recomendations/components/RecommendationSection'
import { useLocation } from 'react-router-dom'

export default function ProductsPage() {
  const location = useLocation()
  const userId = 'user-123'
  
  // Only show recommendations on home page (no search term, no pagination)
  const isHomePage = location.pathname === '/' && 
                    !location.search.includes('q=') && 
                    !location.search.includes('page=') &&
                    location.search === ''

  return (
    <div>      
      {/* Main product list */}
      <ProductList />
      
      {/* Recommendations section at the bottom - only on home page */}
      {isHomePage && (
        <div className="mt-12 pt-8 border-t border-gray-200">
          <RecommendationSection 
            userId={userId} 
            title="Recommended For You"
            maxItems={8}
          />
        </div>
      )}
    </div>
  )
}