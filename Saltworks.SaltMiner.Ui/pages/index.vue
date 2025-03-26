<template>
  <div class="wrapper">redirecting...</div>
</template>
<script>
import isValidUser from '../middleware/is-valid-user'

export default {
  name: 'IndexPage',
  middleware: isValidUser,
  async fetch({ store: { dispatch } }) {
    await dispatch('configInit')
  },
  mounted() {
    this.$router.push({
      path: `/engagements`
    })
  },
}
</script>
