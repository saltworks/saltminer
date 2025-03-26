<template>
  <div :class="stackedClass">
    <FormLabel v-if="label" :label="label"></FormLabel>
    <div role="button" :class="activeClasses" @click="handleClick">
      <div class="checkboxBorderBox">
        <div class="checkboxIcon">
          <CheckIcon />
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import FormLabel from './FormLabel.vue'
import CheckIcon from '~/assets/svg/checkboxCheck.svg?inline'

export default {
  name: 'CheckboxBox',
  components: {
    FormLabel,
    CheckIcon
  },
  props: {   
     label: {
      type: String,
      default: null,
    },
    checked: {
      type: Boolean,
      default: false,
    },
    handlesClicks: {
      type: Boolean,
      default: true,
    },
    disabled: {
      type: Boolean,
      default: false,
    },
    stacked: {
      type: Boolean,
      default: true,
    },
  },

  computed: {
    activeClasses() {
      const classes = ['checkbox-box']

      if (this.disabled) classes.push('checkbox-box--disabled')

      if (this.checked) classes.push('checkbox-box--checked')

      if(!this.stacked) classes.push("not-stacked")

      return classes.join(' ')
    },
    stackedClass() {
      const classes = ['checkBoxControl']

      if(!this.stacked) classes.push("not-stacked")

      return classes.join(' ')
    }
  },

  methods: {
    handleClick() {
      if (this.disabled) return

      if (this.handlesClicks) {
        this.$emit('check-clicked', !this.checked)
      }
    },
  },
}
</script>

<style lang="scss" scoped>
.checkbox-box {
  width: 24px;
  height: 24px;
  position: relative;
  //transform: translateY(-4px) translateX(-4px);
}
.not-stacked {
  display: flex;
}
.mobile-show .checkbox-box,
.instructor-select .checkbox-box {
  display: none;
}

.checkboxBorderBox {
  border: 2px solid $brand-checkbox-border;
  border-radius: 2px;
  background: $brand-white;
  width: 18px;
  height: 18px;
  display: block;
  top: 50%;
  left: 50%;
  transform: translateX(-50%) translateY(-50%);
  cursor: pointer;
  position: relative;
}

.checkbox-box--checked {
  .checkboxBorderBox {
    background: $button-primary-color;
    border-color: $brand-checkbox-border-checked;

    ::v-deep {
      svg {
        path {
          fill: $brand-white;
        }
      }
    }
  }

  .checkboxIcon {
    opacity: 1;
  }
}

.checkbox-box--disabled {
  .checkboxBorderBox {
    background: $brand-color-scale-2;
    border-color: $brand-color-scale-4;
  }
}

.checkboxIcon {
  background-position: center;
  background-repeat: no-repeat;
  width: 12px;
  height: 9px;
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translateX(-50%) translateY(-50%);
  opacity: 0;
}

.checkboxIcon__svg {
  width: 14px;
  height: 11px;
  position: absolute;
  top: 0;
  left: 0;
}

.checkboxSvg {
  &:focus {
    outline: none;
  }
}
</style>
