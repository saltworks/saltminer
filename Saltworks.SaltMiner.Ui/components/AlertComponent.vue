<template>
  <div :class="`alert alert--${alert.type}`">
    <h4>{{ alert.title }}</h4>
    <p
      v-for="(error, index) in alert.messages"
      :key="`message-${index}`"
      class="messages"
    >
      {{ getErrorMessage(error, index) }}
    </p>
    <div class="alert-close" @click="handleAlertClose">&#9587;</div>
  </div>
</template>
<script>
export default {
  props: {
    alert: {
      type: Object,
      default: () => ({}),
    },
  },
  methods: {
    handleAlertClose() {
      this.$emit('close')
    },
    getErrorMessage(error, index){
      if(index == 0){
        return error;
      }
      return "\n" + error
    }
  },
}
</script>
<style lang="scss" scoped>
.alert {
  z-index: 1000;
  padding: 8px;
  border-radius: 8px;
  background: $brand-off-white-1;
  font-family: $font-primary;

  margin-top: 24px;
  margin-bottom: 24px;
  position: fixed;
  bottom: 3rem;
  left: 50%;
  transform: translateX(-50%);

  display: flex;
  flex-flow: column;
  justify-content: center;
  gap: 1rem;
  padding: 16px 24px;
  min-width: 325px;

  &.alert--danger {
    background: $brand-danger;
    border: 1px solid $brand-danger;
    color: white !important;

    h4 {
      color: white !important;
    }
  }

  &.alert--warning {
    background: $brand-warning;
    border: 1px solid $brand-warning;
  }
  &.alert--success {
    background: $brand-success;
    border: 1px solid $brand-success;
  }

  h4 {
    font-style: normal;
    font-weight: 700;
    font-size: 14px;
    line-height: 18px;
    color: $brand-color-scale-6;
  }

  li,
  ul > li:before {
    font-style: normal;
    font-weight: 400;
    font-size: 14px;
    line-height: 18px;
    margin-left: 25px;
  }
  ul > li:before {
    content: '';
    display: list-item;
    position: absolute;
    margin-left: 0px;
  }
}

.alert-close {
  position: absolute;
  top: 0;
  right: 0;
  padding: 0.65rem;
  cursor: pointer;
  font-size: 0.75rem;

  &:hover {
    font-weight: bold;
  }
}
</style>
