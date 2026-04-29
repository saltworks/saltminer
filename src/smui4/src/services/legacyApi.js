import axios from 'axios'

const legacyApi = axios.create({
  baseURL: '/smuiapi',
  headers: {
    'Content-Type': 'application/json',
    'kbn-xsrf': 'true',
  },
  withCredentials: true,
})

legacyApi.interceptors.response.use(
  (response) => response.data,
  (error) => {
    const message = error.response?.data?.message || error.response?.data?.errorMessages?.[0] || error.message
    return Promise.reject(new Error(message))
  },
)

export default legacyApi
