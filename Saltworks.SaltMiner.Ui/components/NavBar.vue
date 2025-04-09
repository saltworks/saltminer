<template>
  <div class="NavBar">
    <div class="NavBar-content">
      <span class="nav-logo">
        <a :href=homeUrl>
          <Logo />
        </a>
      </span>
      <div class="nav-right">
        <span class="nav-engagement">
          <router-link to="/engagements">
            All Engagements
          </router-link>
          </span>
        <span class="nav-engagement">
          <a :href=homeUrl>
            Home
          </a>
        </span>
        <span class="nav-engagement">
          <router-link to="/inventory-assets">Inv. Assets</router-link>
         </span>
         <span class="nav-engagement">
          <router-link to="/job-queue">Job Queue</router-link>
         </span>
        <span class="nav-engagement">
          <router-link to="/admin">Admin</router-link>
        </span>
        <span>
          <router-link class="nav-logout" to="/logout">
            Logout <Logout/>
          </router-link>
        </span>
      </div>
    </div>
  </div>
</template>

<script>
import Logo from '../assets/svg/logo.svg?inline'
import Logout from '../assets/svg/fi_log-out.svg?inline'

export default {
  components: {
    Logo,
    Logout,
  }, 
  data() {
    return {
      homeUrl: ""
    }
  },
  async fetch({ store: { dispatch } }) {
    await dispatch('configInit')
  },
  mounted() {
    this.homeUrl = this.$store.state.config?.home_url ?? ""
  }
}
</script>

<style lang="scss" scoped>
.NavBar {
  max-width: 1872px;
  width: 100%;
  box-shadow: inset 0px -1px 0px rgba(0, 0, 0, 0.1);
  position: sticky;
}
.NavBar-content {
  max-width: 1920px;

  display: flex;
  justify-content: space-between;
  width: 100%;
  padding: 16px 24px;
}
.nav-right {
  margin-left: auto;
  display: flex;
  gap: 32px;
  font-family: $font-primary;
  font-style: normal;
  font-weight: 700;
  font-size: 16px;
  line-height: 24px;
  color: $brand-color-scale-6;
}
.nav-logout {
  display: flex;
  gap: 10px;
  cursor: pointer;
}
</style>
