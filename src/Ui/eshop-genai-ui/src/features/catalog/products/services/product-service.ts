
import type { ProductDto } from '@/features/catalog/products/models/ProductDto';
import { api } from '@/shared/lib/api-client'

export const fetchProducts = (search = '', page = 1, size = 10) =>
  api.get<{ items: ProductDto[]; totalCount: number; pageNumber: number; pageSize: number }>(
    `catalog/api/v1/product?SearchTerm=${search}&PageNumber=${page}&PageSize=${size}`
  )

export const fetchProductById = (id: string) =>
  api.get<ProductDto>(`catalog/api/v1/product/${id}`)