const state = () => ({
  loading: false
})

const getters = {
  loading: (state) => state.loading
}

const mutations = {
  saveLoadingStatus(state, payload) {
    state.loading = payload
  }
}

const actions = {
  setLoadingStatus({ commit }, status) {
    commit('saveLoadingStatus', status)
  },
}

const loading = {
  namespaced: true,
  state,
  getters,
  actions,
  mutations,
}

export default loading
