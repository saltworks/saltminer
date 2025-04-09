export default function (context) {
    if (context.from && context.from.name) {
      // Store referrer in the Vuex store or in the app instance
      context.store.commit('setReferrer', context.from.name);
    }
  }
  