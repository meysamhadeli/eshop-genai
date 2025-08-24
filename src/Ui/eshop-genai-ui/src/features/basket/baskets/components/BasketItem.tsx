import fallbackImg from '@/assets/images/default_product.jpg'
import { formatCurrency } from '@/shared/lib/currency'

interface Props {
  item: {
    productId: string
    productName: string
    productPrice: number
    productImageUrl: string
    quantity: number
  }
  onUpdate: (quantity: number) => void
}

export default function BasketItem({ item, onUpdate }: Props) {
  const lineTotal = item.productPrice * item.quantity

  return (
    <div className="flex items-center gap-4 p-4 border rounded-lg shadow-sm bg-white hover:shadow-md transition-shadow">
      {/* Product Image */}
      <div className="h-20 w-20 flex-shrink-0">
        <img
          src={item?.productImageUrl ? `${import.meta.env.VITE_GATEWAY_BASE_URL}/catalog/${item?.productImageUrl}` : fallbackImg}
          alt={item.productName}
          className="h-full w-full object-cover rounded"
        />
      </div>

      {/* Product Info */}
      <div className="flex-1 min-w-0">
        <h3 className="font-semibold text-gray-800 truncate">{item.productName}</h3>
        <p className="text-amazon-dark font-bold">{formatCurrency(item.productPrice)}</p>
      </div>

      {/* Quantity Controls */}
      <div className="flex items-center gap-3">
        <button
          type="button"
          onClick={() => onUpdate(item.quantity - 1)}
          className="w-8 h-8 flex items-center justify-center bg-gray-200 hover:bg-gray-300 rounded font-bold transition"
        >
          −
        </button>
        <span className="w-8 text-center font-medium">{item.quantity}</span>
        <button
          type="button"
          onClick={() => onUpdate(item.quantity + 1)}
          className="w-8 h-8 flex items-center justify-center bg-gray-200 hover:bg-gray-300 rounded font-bold transition"
        >
          +
        </button>
      </div>

      {/* Line Total */}
      <div className="text-right font-medium text-gray-800 min-w-20">
        {formatCurrency(lineTotal)}
      </div>
    </div>
  )
}