<template>
  <div role="button" :class="activeClasses.join(' ')" @click="handleClick">
    <div class="radioButton--control"></div>
    <div class="radioLabel">{{ label }}</div>
  </div>
</template>

<script>
export default {
  props: {
    selected: {
      type: Boolean,
      default: false,
    },
    value: {
      type: String,
      default: '',
    },
    label: {
      type: String,
      default: '',
    },
    disabled: {
      type: Boolean,
      default: false,
    },
  },

  computed: {
    activeClasses() {
      const classes = ['radioButton']

      if (this.selected) {
        classes.push('radioButton--active')
      }

      return classes
    },
  },

  methods: {
    handleClick() {
      if (this.disabled) return

      this.$emit('input', this.value)
    },
  },
}
</script>

<style lang="scss" scoped>
.radioButton {
  display: flex;
  gap: 8px;
  align-items: center;
  cursor: pointer;

  .radioLabel {
    font-family: 'Open Sans', sans-serif;
    font-size: 14px;
    font-weight: 700;
    line-height: 18px;
    color: $brand-color-scale-6;
  }

  .radioButton--control {
    width: 18px;
    height: 18px;
    border: 2px solid $brand-black;
    display: flex;
    justify-content: center;
    align-items: center;
    padding: 0 0 0 0;
    margin: 0 0 0 0;
    border-radius: 18px;
  }

  &.radioButton--active {
    .radioButton--control {
      border-color: $button-primary-color;

      &::after {
        content: '';
        width: 8px;
        height: 8px;
        border-radius: 50%;
        background-color: $button-primary-color;
        position: relative;
      }
    }
  }
}
</style>
