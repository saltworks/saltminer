<template>
  <div id="app" class="app">
    <Nuxt />
    <FooterComponent :copyright="copyright"></FooterComponent>
  </div>
</template>

<script>
import { mapState } from 'vuex'
import FooterComponent from '../components/FooterComponent.vue';


export default {
  components: {
    FooterComponent
  },
  computed: {
    ...mapState({
      copyright: (state) => state.modules.user.copyright
    })
  }
}
</script>

<style lang="scss" scoped>
#app {
  display: flex;
  flex-flow: column;
  align-items: center;
  width: 100%;
}
</style>