<template>
  <div class="loginPage"></div>
</template>

<script>
export default {
  name: 'LogoutPage',
  layout: 'auth',
  async fetch({ store: { dispatch } }) {
    await dispatch('configInit')
  },
  mounted() {
    return this.$axios
      .$post(`${this.$store.state.config.api_url}/Auth/logout`)
      .then((res) => {
        if (res.success) {
          window.location.href = this.$store.state.config.login_url

          if(this.$store.state.config.login_url.startsWith('/')){
            window.location.href = window.location.origin + this.$store.state.config.login_url
          }
        }
      })
  },
}
</script>

<style lang="scss" scoped></style>
