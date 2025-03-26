<template>
  <div class="inputTextControl">
    <FormLabel v-if="label" :label="label" :label-id="labelId"></FormLabel>

    <div class="inputContainer">
      <input
        :id="reff === '' ? labelId : reff"
        ref="inputEl"
        :disabled="disabled"
        :readonly="disabled"
        :type="currentInputType"
        class="form-control"
        :value="value"
        :placeholder="placeholder"
        @keyup="handleUpdate"
        @input="handleUpdate"
        @blur="handleBlur"
      />

      <button
        v-if="inputtype === 'password'"
        tabindex="-1"
        class="revealPasswordButton"
        @click="revealPassword = !revealPassword"
      >
        <EyeIcon />
      </button>
    </div>

    <FieldErrors v-if="errors" :errors="errors" />
  </div>
</template>

<script>
import uniqueId from 'lodash/uniqueId'
import FieldErrors from '../FieldErrors'
import FormLabel from './FormLabel'
import EyeIcon from '~/assets/svg/fi_eye.svg'

export default {
  components: {
    EyeIcon,
    FieldErrors,
    FormLabel,
  },
  props: {
    label: {
      type: String,
      default: null,
    },
    inputId: {
      type: String,
      default: null,
    },
    value: {
      type: String,
      default: null,
    },
    disabled: {
      type: Boolean,
      default: false,
    },
    placeholder: {
      type: String,
      default: null,
    },
    inputtype: {
      type: String,
      required: false,
      default: 'text',
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
      revealPassword: false,
    }
  },
  computed: {
    labelId() {
      if (this.inputId) {
        return this.inputId
      }
      return uniqueId('text-input-')
    },

    currentInputType() {
      if (this.inputtype === 'password') {
        if (this.revealPassword) {
          return 'text'
        }
      }

      return this.inputtype
    },
  },

  methods: {
    handleUpdate() {
      this.$emit('update', this.$refs.inputEl.value)
    },
    handleBlur() {
      this.$emit('blur')
    },
    handleInput(event) {
      this.$emit('input', event.target.value)
    },
    handleKeyUp(event) {
      this.$emit('keyup', event)
    },
  },
}
</script>

<style lang="scss" scoped>
.inputTextControl {
  display: flex;
  flex-direction: column;
  position: relative;
  gap: 8px;
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
  border: 1px solid $brand-form-control-border;
  color: $brand-form-control-color;
}

.inputContainer {
  position: relative;
}

.revealPasswordButton {
  position: absolute;
  right: 16px;
  top: 16px;
  width: 24px;
  height: 24px;
  appearance: none;
  padding: 0;
  border: none;
  background: none;
  cursor: pointer;
}
</style>
