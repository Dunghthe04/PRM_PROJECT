import axios from 'axios'

const baseURL = import.meta.env.VITE_API_BASE_URL ?? '/api'

export const api = axios.create({
  baseURL,
  timeout: 30000,
})

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('fschool_token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

api.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err.response?.status === 401) {
      localStorage.removeItem('fschool_token')
      localStorage.removeItem('fschool_user')
      if (!window.location.pathname.startsWith('/login')) {
        window.location.href = '/login'
      }
    }
    return Promise.reject(err)
  },
)

/** Tải file (template Excel) kèm JWT. Lỗi JSON từ API vẫn đọc được khi responseType=blob. */
export async function downloadBlob(url: string, filename: string) {
  try {
    const res = await api.get(url, { responseType: 'blob' })
    const blob = new Blob([res.data])
    const link = document.createElement('a')
    link.href = URL.createObjectURL(blob)
    link.download = filename
    link.click()
    URL.revokeObjectURL(link.href)
  } catch (e: unknown) {
    const err = e as { response?: { data?: Blob } }
    if (err.response?.data instanceof Blob) {
      const text = await err.response.data.text()
      try {
        const json = JSON.parse(text) as { message?: string }
        throw Object.assign(new Error(json.message ?? text), {
          response: { data: json },
        })
      } catch (inner) {
        if (inner instanceof Error && (inner as { response?: unknown }).response)
          throw inner
      }
    }
    throw e
  }
}

/** Upload Excel multipart. */
export async function uploadExcel(url: string, file: File) {
  const form = new FormData()
  form.append('file', file)
  const res = await api.post(url, form, {
    headers: { 'Content-Type': 'multipart/form-data' },
  })
  return res.data
}
