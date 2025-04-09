<template>
  <div>
    <div :class="checkboxControlClasses.join(' ')" @click="handleToggle">
      <div class="checkbox-control__box">
        <CheckboxBox
          :disabled="disabled"
          :handle-clicks="false"
          :checked="checked"
        />
      </div>

      <div v-if="label.length > 0" class="checkbox-control__label">
        {{ label }}
      </div>
    </div>
    <FieldErrors v-if="errors" :errors="errors" />
  </div>
</template>

<script>
import FieldErrors from '../FieldErrors'
import CheckboxBox from './CheckboxBox'

export default {
  components: {
    CheckboxBox,
    FieldErrors,
  },

  props: {
    checked: {
      type: Boolean,
      default: false,
    },
    label: {
      type: String,
      default: '',
    },
    size: {
      type: String,
      default: 'default',
    },
    disabled: {
      type: Boolean,
      default: false,
    },
    errors: {
      type: Array,
      required: false,
      default: () => [],
    },
  },

  computed: {
    checkboxControlClasses() {
      const classes = ['checkbox-control', `checkbox-control--${this.size}`]

      if (this.checked) {
        classes.push('checkbox-control--checked')
      }

      return classes
    },
  },

  methods: {
    handleToggle() {
      if (this.disabled) return

      this.$emit('input', !this.checked)
    },
  },
}
</script>

<style lang="scss" scoped>
.checkbox-control {
  display: flex;
  align-items: center;
  cursor: pointer;
  user-select: none;
  -webkit-touch-callout: none;
  -webkit-tap-highlight-color: transparent;
}

.checkbox-control--checked {
  color: $button-primary-color;
}

.checkbox-control__box {
  margin-right: 4px;
}

.checkbox-control__label {
  font-size: 14px;
  line-height: 20px;

  &::v-deep {
    a {
      color: $brand-color-scale-6;
      text-decoration: underline;

      &:hover {
        color: $button-primary-color;
      }
    }
  }

  @at-root .checkbox-control--large #{&} {
    font-size: 16px;
    line-height: 24px;
  }
}
</style>
