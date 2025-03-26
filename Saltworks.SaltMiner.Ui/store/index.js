import issues from './modules/issues'
import user from './modules/user'
import loading from './modules/loading'

// eslint-disable-next-line import/no-named-as-default-member

export const modules = {
  issues,
  user,
  loading
}

export const state = {
  config: null,
  referrer: null,
}

export const mutations = {
  SET_CONFIG(state, config) {
    state.config = config
  },
  setReferrer(state, referrer) {
    state.referrer = referrer;
  },
}

export const getters = {
  config: (state) => state.config
}

export const actions = {
  async configInit({ commit }) {
    console.log("configInit");
    const config = await fetch('/smpgui/config.json').then((res) => res.json())
    
    if(config.api_url.startsWith('/')){
      config.api_url = window.location.origin + config.api_url
    }

    if(config.login_url.startsWith('/')){
      config.login_url = window.location.origin + config.login_url
    }
    
    if(config.home_url.startsWith('/')){
      config.home_url = window.location.origin + config.home_url
    }

    if(config.auth_url.startsWith('/')){
      config.auth_url = window.location.origin + config.auth_url
    }
    
    commit('SET_CONFIG', config)
  }
}
