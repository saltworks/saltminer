<template>
  <div class="modal-wrapper">
    <div class="modal-outer">
      <div v-click-outside="closeModal" class="modal-inner_container">
        <slot></slot>
      </div>
    </div>
  </div>
</template>

<script>
export default {
  props: {
    size: {
      type: String,
      default: null,
    },
  },
  computed: {
    wrapperClass() {
      const classes = ['modal-wrapper']
      return classes.join(' ')
    },
    outerClass() {
      const classes = ['modal-outer']
      classes.push(this.size)
      return classes.join(' ')
    },
  },
  methods: {
    closeModal() {
      this.$emit('toggle', false)
    },
  },
}
</script>

<style lang="scss" scoped>
.modal-wrapper {
  .modal-outer {
    display: flex;
    position: fixed;
    z-index: 800;
    width: 100vw;
    height: 100vh;
    left: 0;
    top: 0;
    background-color: rgba(0, 0, 0, 0.4);
    justify-content: center;
    align-items: center;
    transition: all 800ms 200ms cubic-bezier(0.075, 0.82, 0.165, 1),
      background-color 200ms ease-in-out;
    transform: unset;
  }
  .modal-inner_container {
    z-index: 999;
    display: flex;
    align-items: center;
    justify-content: center;
  }
}
</style>
