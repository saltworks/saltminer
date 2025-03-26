<template>
  <div :class="`engagement-title__wrapper`">
    <h1
      v-if="label === '' && editing === false"
      :class="`et-placeholder title-type-${type}`"
    >
      {{ placeholder }}
      <span v-if="!disabled" @click="onEdit"><Pencil /> {{changeLabel}}</span>
    </h1>
    <h1
      v-if="label !== '' && editing === false"
      :class="`et-label title-type-${type}`"
    >
      {{ label }}
      <span v-if="!disabled" @click="onEdit"><Pencil /> {{changeLabel}}</span>
    </h1>

    <h1 :class="`${editing ? 'active' : ''} input-area title-type-${type}`">
      <input
        ref="eInput"
        type="text"
        :value="label"
        :class="`et-input title-type-${type}`"
        :readonly="disabled"
        @input="onInput"
        @keyup.enter="onSave"
        @blur="onSave()"
      />
    </h1>
  </div>
</template>

<script>
import Pencil from '~/assets/svg/fi_edit-3.svg?inline'

export default {
  components: { Pencil },
  props: {
    type: {
      type: String,
      default: 'default',
    },
    changeLabel: {
      type: String,
      default: '',
    },
    label: {
      type: String,
      default: '',
    },
    placeholder: {
      type: String,
      default: null,
    },
    disabled: {
      type: Boolean,
      default: false,
    },
  },
  data() {
    return {
      editing: false,
      currentTitle: this.label,
    }
  },
  methods: {
    onSave() {
      this.editing = !this.editing

      if (this.$refs.eInput.value === this.label) return

      this.$emit('saveTitle', this.$refs.eInput.value)
    },
    onEdit() {
      this.editing = !this.editing
      this.$refs.eInput.focus()
    },
    onInput(e) {
      this.currentTitle = e.target.value
    },
  },
}
</script>

<style lang="scss" scoped>
.engagement-title__wrapper {
  font-family: $font-primary;
  font-weight: 700;
  font-size: 32px;
  line-height: 48px;
}
.engagement-title__wrapper h1 {
  display: flex;
  gap: 25px;
  align-items: center;
}
.engagement-title__wrapper h1 svg {
  width: fit-content;
}

.et-label,
.et-placeholder,
.et-input {
  font-family: $font-primary;
  font-style: normal;
  font-weight: 700;
  font-size: 32px;
  line-height: 48px;
  letter-spacing: -0.25px;
}

.et-placeholder {
  color: $brand-color-scale-4;
  &.title-type-subtitle {
    color: $brand-color-scale-4;
    font-size: 24px;
  }
}
.et-label {
  color: $brand-color-scale-6;
  &.title-type-subtitle {
    color: $brand-color-scale-4;
    font-size: 24px;
  }
}
.engagement-title__wrapper h1 span {
  display: flex;
  gap: 10px;
  width: fit-content;
  color: $header1-color;
  font-style: normal;
  font-weight: 700;
  font-size: 14px;
  line-height: 16px;
  cursor: pointer;
}
.engagement-title__wrapper h1 span:hover {
  padding-bottom: 5px;
}
.engagement-title__wrapper input {
  border-top: none;
  border-right: none;
  border-left: none;
  outline: none;
  font-family: $font-primary;
  font-weight: 700;
  font-size: 32px;
  line-height: 48px;
  background: none;

  &.title-type-subtitle {
    color: $brand-color-scale-4;
    font-size: 24px;
    line-height: 24px;
    border-color: $brand-color-scale-4;
  }
}
.input-area {
  opacity: 0;
  position: absolute;
  z-index: -1;
}
.engagement-title__wrapper .active {
  opacity: 1 !important;
  position: relative !important;
  z-index: unset !important;

  input {
    border-bottom: 2px solid $brand-color-scale-4;
  }
}
</style>
