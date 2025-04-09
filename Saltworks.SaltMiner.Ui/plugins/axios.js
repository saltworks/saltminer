export default function ({ $axios, store }) {

    $axios.onRequest(request => {
      store.dispatch('modules/loading/setLoadingStatus', true)
    })
  
    $axios.onError(error => {
      store.dispatch('modules/loading/setLoadingStatus', false)
    })
  
    $axios.onResponse(response => {
      store.dispatch('modules/loading/setLoadingStatus', false)
    })

    $axios.interceptors.response.use(function (response) {
      return response
    }, function (error) {
      if (error.response.status === 401) {
        store.dispatch('modules/user/redirectLogin')
      }

      if (error.response.status === 403) {
        store.dispatch('modules/user/redirecAccess')
      }
      
      return Promise.reject(error)
    })
  }