<template>
  <div class="modal-wrapper">
    <div
      :class="`modal-outer modal-${openId ? 'open' : 'closed'}`"
      @click="clickedOutside"
    >
      <div :class="`modal-inner_container modal-${size}`">
        <h4>
          <span>{{ label }}</span> <Close @click="onCloseSlideModal" />
        </h4>

        <slot v-if="openId"></slot>
      </div>
    </div>
  </div>
</template>

<script>
import Close from '~/assets/svg/fi_x.svg?inline'

export default {
  components: {
    Close,
  },
  props: {
    open: {
      type: Boolean,
    },
    size: {
      type: String,
      default: null,
    },
    label: {
      type: String,
      default: '',
    },
  },
  data() {
    return {
      child_component: this.component,
      closing: false,
    }
  },
  computed: {
    openId() {
      return this.open && !this.closing
    },
  },
  methods: {
    clickedOutside(e) {
      if (this.closing) return

      if (e.target.classList[0] === 'modal-outer') {
        this.closing = true
        setTimeout(() => {
          this.$emit('toggle', !this.open)
          this.closing = false
        }, 600)
      }
    },
    onCloseSlideModal(e) {
      if (this.closing) return

      this.closing = true

      setTimeout(() => {
        this.$emit('toggle', e)
        this.closing = false
      }, 600)
    },
  },
}
</script>

<style lang="scss" scoped>
.modal-outer {
  width: 100vw;
  height: 100vh;
  opacity: 0;
  right: 0;
  box-sizing: border-box;
  top: 0;
  pointer-events: none;
  background-color: rgba(0, 0, 0, 0);
  position: fixed;
  transition: opacity 600ms 600ms cubic-bezier(0.075, 0.82, 0.165, 1),
    background-color 600ms cubic-bezier(0.075, 0.82, 0.165, 1);
  display: flex;
  justify-content: flex-end;
  align-items: flex-start;
  z-index: 800;
}
.modal-wrapper .modal-open {
  pointer-events: all;
  opacity: 1;
  background-color: rgba(0, 0, 0, 0.4);
  transition: opacity 600ms 0s cubic-bezier(0.075, 0.82, 0.165, 1),
    background-color 600ms cubic-bezier(0.075, 0.82, 0.165, 1);
  transform: unset;
}

.modal-inner_container {
  z-index: 999;
  display: flex;
  transform: translateX(100%);
  transition: transform 600ms cubic-bezier(0.075, 0.82, 0.165, 1);

  flex-direction: column;
  justify-content: flex-start;
  background: $brand-white;
  height: 100%;
  font-family: $font-primary;
  padding: 40px 32px 0 32px;
  gap: 24px;

  overflow-y: scroll;

  h4 {
    flex: 0 0 auto;
    font-style: normal;
    font-weight: 700;
    font-size: 24px;
    line-height: 32px;
    display: flex;
    align-items: center;
    justify-content: center;
    letter-spacing: -0.25px;
    color: $brand-color-scale-6;
    width: 100%;
    gap: 24px;
    padding: 0 12px;

    span {
      flex: 1;
      display: flex;
      align-items: center;
      justify-content: flex-start;
    }

    svg {
      flex: 0 0 auto;
      width: 24px;
      height: 24px;
      cursor: pointer;
    }
  }
}
.modal-wrapper .modal-open .modal-inner_container {
  transform: translateX(0);
  transition: transform 600ms cubic-bezier(0.075, 0.82, 0.165, 1);
}

.modal-xsmall {
  width: 15%;
  height: 100%;
}

.modal-small {
  width: 26.5%;
  height: 100%;
}
.modal-medium {
  width: 62%;
  height: 100%;
}
.modal-large {
  width: 80%;
  height: 100%;
}
</style>
