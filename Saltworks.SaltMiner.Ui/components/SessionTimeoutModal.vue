<template>
  <div class="session-timeout__wrapper">
    <Alert />
    <h4>{{ title }}</h4>
    <p>
      {{ message }}
    </p>

    <div class="session-timeout__buttons">
      <a :href="loginUrl" target="_blank">
        <span class="st-button_R">Login</span>
      </a>
      <div class="timeout-buttons-subtext">opens in new tab</div>
    </div>
  </div>
</template>

<script>
import Alert from '~/assets/svg/fi_alert-triangle.svg?inline'
export default {
  components: {
    Alert,
  },
  props: {
    title: {
      type: String,
      default: 'Warning',
    },
    message: {
      type: String,
      default: 'You have been logged out due to inactivity.',
    },
    loginUrl: {
      type: String,
      default: '/login',
    },
  },
}
</script>

<style lang="scss" scoped>
.timeout-buttons-subtext {
  flex: 0 0 auto;
  font-size: 12px;
  color: $brand-color-scale-4;
}
.session-timeout__wrapper {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background: $brand-white;
  width: 100%;
  height: 100%;
  border-radius: 14px;
  font-family: $font-primary;
  padding: 24px;
  gap: 28px;
}
.session-timeout__wrapper h4 {
  font-style: normal;
  font-weight: 700;
  font-size: 32px;
  line-height: 48px;
  display: flex;
  align-items: center;
  letter-spacing: -0.25px;
  color: $brand-color-scale-6;
}
.session-timeout__wrapper p {
  font-style: normal;
  font-weight: 400;
  font-size: 14px;
  line-height: 18px;
  color: $brand-color-scale-6;
  max-width: 320px;
  text-align: center;
}
.session-timeout__buttons {
  display: flex;
  flex-flow: column;
  align-items: center;
  width: 100%;
  gap: 4px;
}
.st-button_L,
.st-button_R {
  display: flex;
  align-items: center;
  justify-content: center;
  flex: 0 0 auto;
  padding: 12px;
  font-style: normal;
  font-weight: 700;
  font-size: 16px;
  line-height: 24px;
  border-radius: 40px;
  min-width: 150px;
  cursor: pointer;
  transition: 400ms cubic-bezier(0.075, 0.82, 0.165, 1);
}

.st-button_L {
  color: $brand-white;
  background: $brand-danger;
  border: 1px solid $brand-danger;
}
.st-button_L:hover {
  color: $brand-danger;
  background: $brand-white;
  border: 1px solid $brand-danger;
}
.st-button_R {
  color: $brand-primary-color;
  border: 1px solid $brand-primary-color;
}
.st-button_R:hover {
  color: $brand-white;
  background: $brand-primary-color;
  border: 1px solid $brand-primary-color;
}
</style>
