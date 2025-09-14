export interface PageList<T> {
  items: T[]
  pageNumber: number
  pageSize: number
  totalCount: number
  hasExactMatches: boolean
  explanation?: string
}