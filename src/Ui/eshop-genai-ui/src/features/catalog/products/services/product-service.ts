import { api } from '@/shared/lib/api-client'
import type { ProductDto } from '@/features/catalog/products/models/ProductDto'
import type { PageList } from '@/shared/models/PagedList';
import type { TrackUserActivityDto } from '@/features/catalog/products/models/TrackUserActivityDto';

export const fetchProducts = (
  search = '',
  page = 1,
  size = 10,
  useSemanticSearch = true
) =>
  api.get<{ items: ProductDto[]; totalCount: number; pageNumber: number; pageSize: number }>(
    `catalog/api/v1/product?SearchTerm=${encodeURIComponent(search)}&PageNumber=${page}&PageSize=${size}&UseSemanticSearch=${useSemanticSearch}`
  )

export const fetchProductById = (id: string) =>
  api.get<ProductDto>(`catalog/api/v1/product/${id}`)


export const trackUserActivity = (data: TrackUserActivityDto) =>
  api.post('catalog/api/v1/product/recommendations/activity', data)

export const getRecommendations = (userId: string, pageNumber = 1, pageSize = 5) =>
  api.get<PageList<ProductDto>>(
    `catalog/api/v1/product/recommendations/${userId}?PageNumber=${pageNumber}&PageSize=${pageSize}`
  )