<template>
  <div class="inputDateControl">
    <FormLabel v-if="label" :label="label" :label-id="labelId"></FormLabel>

    <div class="inputContainer">
      <input
        :id="reff === '' ? labelId : reff"
        ref="inputEl"
        type="date"
        :disabled="disabled"
        :readonly="disabled"
        :value="dateVal"
        class="form-control"
        :placeholder="placeholder"
        @keyup="handleUpdate"
        @input="handleUpdate"
        @change="handleUpdate"
        @blur="handleBlur"
      />
    </div>

    <FieldErrors v-if="errors" :errors="errors" />
  </div>
</template>

<script>
import moment from 'moment'
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
      default: null,
    },
    inputId: {
      type: String,
      default: null,
    },
    value: {
      type: String,
      default: new Date(),
    },
    disabled: {
      type: Boolean,
      default: false,
    },
    placeholder: {
      type: String,
      default: null,
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
    dateVal() {
      return moment(this.value).format('YYYY-MM-DD')
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
