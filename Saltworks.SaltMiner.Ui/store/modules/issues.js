const state = () => ({
  issues: [],
  currentIssue: null,
  inviteToken: null,
  issuePrimer: null,
  importMessage: 'test',
  importIssues: null,
  importCheck: false,
})

const mutations = {
  SET_FIELD(state, payload) {
    state[payload.field] = payload.value
  },
}

const actions = {
  async downloadCsvTemplate({ commit }) {
    const response = await this.$axios
      .$get(`${this.state.config.api_url}/issue/import/csv/template`, {
        headers: {
          accept: '*/*',
        },
        responseType: 'blob',
      })
      .then((response) => {
        const url = window.URL.createObjectURL(new Blob([response]))
        const link = document.createElement('a')
        link.href = url
        link.setAttribute('download', 'template.csv')
        document.body.appendChild(link)
        link.click()
      })
    return response
  },
  async downloadEngagementTemplate({ commit }) {
    const response = await this.$axios
      .$get(`${this.state.config.api_url}/issue/import/json/engagement`, {
        headers: {
          accept: '*/*',
        },
        responseType: 'blob',
      })
      .then((response) => {
        const url = window.URL.createObjectURL(new Blob([response]))
        const link = document.createElement('a')
        link.href = url
        link.setAttribute('download', 'engagement-template.json')
        document.body.appendChild(link)
        link.click()
      })
    return response
  },
  async downloadTemplateTemplate({ commit }) {
    const response = await this.$axios
      .$get(`${this.state.config.api_url}/issue/import/json/template`, {
        headers: {
          accept: '*/*',
        },
        responseType: 'blob',
      })
      .then((response) => {
        const url = window.URL.createObjectURL(new Blob([response]))
        const link = document.createElement('a')
        link.href = url
        link.setAttribute('download', 'template.json')
        document.body.appendChild(link)
        link.click()
      })
    return response
  }
}

export default {
  namespaced: true,
  state,
  actions,
  mutations,
}
