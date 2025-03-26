<template>
  <div :class="radioControlClasses.join(' ')">
    <div class="radio-control__label">{{ label }}</div>
    <div class="radio-controls__box">
      <RadioButton
        v-for="option in options"
        :key="option.label"
        :value="option.value"
        :label="option.label"
        :selected="option.value === value"
        @input="handleUpdate"
        @blur="handleBlur"
      ></RadioButton>
    </div>
    <FieldErrors v-if="errors" :errors="errors" />
  </div>
</template>

<script>
import FieldErrors from '../FieldErrors'
import RadioButton from './RadioButton'

export default {
  components: {
    RadioButton,
    FieldErrors,
  },

  props: {
    label: {
      type: String,
      default: '',
    },
    options: {
      type: Array,
      default: () => [
        {
          label: 'Yes',
          value: 'yes',
        },
        {
          label: 'No',
          value: 'no',
        },
      ],
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

  data() {
    return {
      value: this.options[0].value,
    }
  },

  computed: {
    radioControlClasses() {
      const classes = ['radio-control', `radio-control--${this.size}`]

      if (this.checked) {
        classes.push('radio-control--checked')
      }

      return classes
    },

    currentValue() {
      return this.options.find((option) => option.selected).value
    },
  },

  methods: {
    handleUpdate(val) {
      this.value = val
      this.$emit('input', val)
    },
    handleBlur(l) {
      this.$emit('blur')
    },
  },
}
</script>

<style lang="scss" scoped>
.radio-control {
  display: flex;
  flex-direction: column;
  position: relative;
  gap: 8px;
}
.radio-controls__box {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}
</style>
