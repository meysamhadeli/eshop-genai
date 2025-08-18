import { RouterProvider, createBrowserRouter } from 'react-router-dom'
import { routes } from './routes'
import Layout from '../features/layout/Layout'

const router = createBrowserRouter(
  routes.map(route => ({
    ...route,
    element: <Layout>{route.element}</Layout>,
  }))
)

function App() {
  return <RouterProvider router={router} />
}

export default App