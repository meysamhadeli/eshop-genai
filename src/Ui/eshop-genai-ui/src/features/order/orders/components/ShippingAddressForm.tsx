import { useState } from 'react'

interface ShippingAddressFormProps {
  onSubmit: (address: string) => void
  onCancel: () => void
  isLoading?: boolean
}

export default function ShippingAddressForm({ 
  onSubmit, 
  onCancel, 
  isLoading = false 
}: ShippingAddressFormProps) {
  const [address, setAddress] = useState('')

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (address.trim()) {
      onSubmit(address.trim())
    }
  }

  return (
    <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center p-4 z-50">
      <div className="bg-white rounded-lg p-6 w-full max-w-md">
        <h2 className="text-xl font-bold mb-4">Shipping Address</h2>
        
        <form onSubmit={handleSubmit}>
          <div className="mb-4">
            <label htmlFor="address" className="block text-sm font-medium text-gray-700 mb-2">
              Enter your shipping address:
            </label>
            <textarea
              id="address"
              value={address}
              onChange={(e) => setAddress(e.target.value)}
              required
              rows={4}
              className="w-full border border-gray-300 rounded-md p-3 focus:ring-2 focus:ring-amazon-light focus:border-transparent"
              placeholder="123 Main St, City, State, ZIP Code"
              disabled={isLoading}
            />
          </div>
          
          <div className="flex gap-3 justify-end">
            <button
              type="button"
              onClick={onCancel}
              disabled={isLoading}
              className="px-4 py-2 text-gray-600 border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={isLoading || !address.trim()}
              className="px-4 py-2 bg-amazon-light text-amazon-black font-medium rounded-md hover:bg-amazon-dark hover:text-purple-700 disabled:opacity-50"
            >
              {isLoading ? 'Processing...' : 'Confirm Order'}
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}