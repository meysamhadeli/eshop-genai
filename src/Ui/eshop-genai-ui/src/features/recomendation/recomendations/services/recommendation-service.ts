import { api } from '@/shared/lib/api-client'
import type { ProductDto } from '@/features/catalog/products/models/ProductDto'
import type { PageList } from '@/shared/models/PagedList';
import type { TrackUserActivityDto } from '@/features/recomendation/recomendations/models/TrackUserActivityDto';

export const trackUserActivity = (data: TrackUserActivityDto) =>
  api.post('recommendation/api/v1/recommendation/activity', data)

export const getRecommendations = (userId: string, pageNumber = 1, pageSize = 5) =>
  api.get<PageList<ProductDto>>(
    `recommendation/api/v1/recommendation/${userId}?PageNumber=${pageNumber}&PageSize=${pageSize}`
  )