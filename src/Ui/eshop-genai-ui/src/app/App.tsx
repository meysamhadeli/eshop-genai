// App.tsx
import { RouterProvider, createBrowserRouter } from 'react-router-dom'
import { routes } from '@/app/routes'
import Layout from '@/shared/components/Layout'
import { ToastContainer } from 'react-toastify'
import 'react-toastify/dist/ReactToastify.css'

const router = createBrowserRouter(
  routes.map(route => ({
    ...route,
    element: <Layout>{route.element}</Layout>,
  }))
)

function App() {
  return (
    <>
      <RouterProvider router={router} />
      <ToastContainer
        position="top-right"
        autoClose={5000}
        hideProgressBar={false}
        newestOnTop={false}
        closeOnClick
        rtl={false}
        pauseOnFocusLoss
        draggable
        pauseOnHover
      />
    </>
  )
}

export default App