<template>
  <div :class="textAreaControlClass">
    <FormLabel v-if="label" :label="label" :label-id="labelId"></FormLabel>

    <div class="inputContainer">
      <textarea
        :id="reff === '' ? labelId : reff"
        ref="inputEl"
        :disabled="disabled"
        :readonly="disabled"
        :class="controlClass"
        :value="value"
        :placeholder="placeholder"
        @keyup="handleUpdate"
        @input="handleUpdate"
        @blur="handleBlur"
      ></textarea>
    </div>

    <FieldErrors v-if="errors" :errors="errors" />
  </div>
</template>

<script>
import uniqueId from 'lodash/uniqueId'
import FieldErrors from '../FieldErrors'
import FormLabel from './FormLabel'

export default {
  components: {
    FieldErrors,
    FormLabel,
  },
  props: {
    label: {
      type: String,
      default: '',
    },
    inputId: {
      type: String,
      default: '',
    },
    value: {
      type: String,
      default: '',
    },
    resize: {
      type: Boolean,
      default: false,
    },
    disabled: {
      type: Boolean,
      default: false,
    },
    fullWidth: {
      type: Boolean,
      default: false,
    },
    placeholder: {
      type: String,
      default: '',
    },
    errors: {
      type: Array,
      required: false,
      default: () => [],
    },
    reff: {
      type: String,
      default: null,
    },
  },

  data() {
    return {
      isMounted: false,
    }
  },

  computed: {
    labelId() {
      if (this.inputId) {
        return this.inputId
      }
      return uniqueId('text-input-')
    },
    controlClass() {
      return `form-control ${this.resize ? 'form-control--resize' : ''}`
    },
    textAreaControlClass() {
      return `textAreaControl ${
        this.fullWidth ? 'textAreaControl--fullWidth' : ''
      }`
    },
  },

  mounted() {
    this.isMounted = true
  },

  methods: {
    handleUpdate() {
      if (!this.isMounted) return

      this.$emit('update', this.$refs.inputEl.value)
    },
    handleBlur() {
      if (!this.isMounted) return

      this.$emit('blur', this.$refs.inputEl.value)
    },
  },
}
</script>

<style lang="scss" scoped>
.textAreaControl {
  display: flex;
  flex-direction: column;
  position: relative;
  gap: 8px;
  width: auto;
  &.textAreaControl--fullWidth {
    width: 100%;
  }
}

.form-control {
  background: $brand-white;
  display: block;
  border-radius: 8px;
  font-family: $font-form-control;
  font-style: normal;
  font-weight: 400;
  font-size: 14px;
  line-height: 18px;
  padding: 16px;
  width: 100%;
  min-height: 140px;
  height: 100%;
  border: 1px solid $brand-form-control-border;
  color: $brand-form-control-color;
  resize: none;

  &.form-control--resize {
    resize: both;
  }
}

.inputContainer {
  position: relative;
}
.summary-wrapper .inputContainer {
  height: 152px;
}
.summary-wrapper #text-input-2 {
  height: 152px;
}
</style>
