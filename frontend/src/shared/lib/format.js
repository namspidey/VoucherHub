export const formatPoints = (value = 0) =>
  new Intl.NumberFormat('vi-VN').format(value)

export const formatDateTime = (value) =>
  value
    ? new Intl.DateTimeFormat('vi-VN', {
        dateStyle: 'short',
        timeStyle: 'short',
      }).format(new Date(value))
    : '—'

export const toLocalInput = (value) => {
  if (!value) return ''
  const date = new Date(value)
  const offset = date.getTimezoneOffset()
  return new Date(date.getTime() - offset * 60000).toISOString().slice(0, 16)
}

export const toIso = (value) => (value ? new Date(value).toISOString() : null)

export const phaseLabel = {
  UPCOMING: 'Sắp diễn ra',
  ONGOING: 'Đang diễn ra',
  ENDED: 'Đã kết thúc',
}

export const statusLabel = {
  DRAFT: 'Bản nháp',
  PUBLISHED: 'Đã xuất bản',
  CANCELLED: 'Đã hủy',
  ACTIVE: 'Đang hoạt động',
  BLOCKED: 'Đã khóa',
}

export const badgeClass = (value) => {
  if (['ONGOING', 'ACTIVE', 'PUBLISHED', 'ADMIN_GRANT'].includes(value)) return 'text-bg-success'
  if (['UPCOMING', 'DRAFT'].includes(value)) return 'text-bg-warning'
  if (['BLOCKED', 'CANCELLED'].includes(value)) return 'text-bg-danger'
  return 'text-bg-secondary'
}

export const shortId = (value) => value ? `${value.slice(0, 8)}…` : '—'
