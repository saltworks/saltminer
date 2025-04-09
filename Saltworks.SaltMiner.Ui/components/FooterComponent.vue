<template>
    <footer class="footer">
        <p class="text-sm leading-none text-gray-800 mt-4 dark:text-white">{{ formatCopyright(copyrightTemplate) }}</p>
    </footer>
</template>


<script>

import { mapState } from 'vuex'

export default { 
  async fetch({ store: { dispatch } }) {
    await dispatch('configInit')
  },
  computed: {
    ...mapState({
      copyrightTemplate: (state) => state?.config?.copyright ?? ""
    }),
  },
  methods: {
    formatCopyright(val) {
      return val.replace("{year}", new Date().getFullYear())
    }
  }
}
</script>

<style>

</style>
