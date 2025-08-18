import type { RouteObject } from 'react-router-dom'
import BasketPage from '@/features/basket/baskets/pages/BasketPage'
import ProductsPage from '@/features/catalog/products/pages/ProductsPage'
import ProductDetailPage from '@/features/catalog/products/pages/ProductDetailPage'

export const routes: RouteObject[] = [
  { path: '/', element: <ProductsPage /> },
  { path: '/product/:id', element: <ProductDetailPage /> },
  { path: '/basket', element: <BasketPage /> },
]