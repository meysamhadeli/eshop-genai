export default function Footer() {
  return (
    <footer className="bg-amazon-gray text-white py-6 mt-10">
      <div className="container mx-auto px-4 text-center">
        &copy; {new Date().getFullYear()} eShop. Inspired by Amazon.
      </div>
    </footer>
  )
}