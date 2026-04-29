import axios from 'axios'

const apiClient = axios.create({
  baseURL: '/smuiapi4',
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true,
})

apiClient.interceptors.response.use(
  (response) => response.data,
  (error) => {
    const message = error.response?.data?.error?.message || error.message
    return Promise.reject(new Error(message))
  },
)

export default apiClient
