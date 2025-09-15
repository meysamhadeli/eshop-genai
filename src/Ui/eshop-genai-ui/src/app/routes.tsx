// In your App.tsx or routing file
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom'
import Layout from '@/shared/components/Layout'
import ProductsPage from '@/features/catalog/products/pages/ProductsPage'
import ProductDetailPage from '@/features/catalog/products/pages/ProductDetailPage'
import BasketPage from '@/features/basket/baskets/pages/BasketPage'

function App() {
  return (
    <Router>
      <Layout>
        <Routes>
          <Route path="/" element={<ProductsPage />} />
          <Route path="/products" element={<ProductsPage />} />
          <Route path="/search" element={<ProductsPage />} />
          <Route path="/product/:id" element={<ProductDetailPage />} />
          <Route path="/basket" element={<BasketPage />} />
          <Route path="*" element={<NotFound />} />
        </Routes>
      </Layout>
    </Router>
  )
}

// Simple 404 component
function NotFound() {
  return (
    <div className="text-center py-20">
      <h1 className="text-2xl font-bold text-gray-800 mb-4">404 - Page Not Found</h1>
      <p className="text-gray-600">The page you're looking for doesn't exist.</p>
    </div>
  )
}

export default App