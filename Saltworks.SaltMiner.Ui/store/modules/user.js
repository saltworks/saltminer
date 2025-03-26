const state = () => ({
  cookie: null,
  dateFormat: 'M/D/yyyy',
  maxImportFileSize: null,
  email: null,
  enabled: true,
  fullName: null,
  roles: [],
  userName: null,
})

const getters = {
}

const mutations = {
  SET_FIELD(state, payload) {
    state[payload.field] = payload.value
  },
}

const actions = {
  async redirectLogin(){
    const config = await fetch('/smpgui/config.json').then((res) => res.json())
    if(config.login_url.startsWith('/')) {
      window.location.href = window.location.origin + config.login_url
    } else {
      window.location.href = config.login_url
    }
  },
  async redirectAccess(){
    const config = await fetch('/smpgui/config.json').then((res) => res.json())
    window.location.href = config.redirectAccess
  },
  async fetchUser({ commit, rootState }) {
    const rootConfig = rootState?.config

    const config =
      rootConfig === null
        ? await fetch('/smpgui/config.json').then((res) => res.json())
        : rootConfig

    if(config.auth_url.startsWith('/')){
      config.auth_url = window.location.origin + config.auth_url
    }

    return await this.$axios
      .$get(`${config.auth_url}`)
      .then((r) => {
        if (r.data) {
          commit('SET_FIELD', { field: 'cookie', value: r.data.cookie })
          commit('SET_FIELD', { field: 'dateFormat', value: r.data.dateFormat })
          commit('SET_FIELD', { field: 'maxImportFileSize', value: formatFileSize(r.data.maxImportFileSize)})
          commit('SET_FIELD', { field: 'email', value: r.data.email })
          commit('SET_FIELD', { field: 'enabled', value: r.data.enabled })
          commit('SET_FIELD', { field: 'fullName', value: r.data.fullName })
          commit('SET_FIELD', { field: 'roles', value: r.data.roles })
          commit('SET_FIELD', { field: 'userName', value: r.data.userName })
        } else {
          // eslint-disable-next-line no-lonely-if
          if (config.login_url.startsWith('/')) {
            window.location.href = window.location.origin + config.login_url
          } else {
            window.location.href = config.login_url
          }
        }
      })
    }
}

function formatFileSize(sizeInBytes) {
  if (sizeInBytes === 0) return '0 Bytes';

  const k = 1024;
  const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];

  const i = parseInt(Math.floor(Math.log(sizeInBytes) / Math.log(k)));

  return Math.round(100 * (sizeInBytes / Math.pow(k, i))) / 100 + ' ' + sizes[i];
}

const user = {
  namespaced: true,
  state,
  getters,
  actions,
  mutations,
}

export default user
