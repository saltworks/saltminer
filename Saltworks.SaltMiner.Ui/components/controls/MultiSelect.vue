<template>
  <div class="multiselectControl">
    <FormLabel v-if="label" :label="label" :label-id="labelId"></FormLabel>
    <div :class="multiselectClass">
      <ul class="optionList">
        <li
          v-for="option in options"
          :key="`multiselect-${option}`"
          :class="getOptionClass(option)"
          @click="update(option)"
          @blur="handleBlur"
        >
          {{ option }}
        </li>
      </ul>
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
      default: null,
    },
    disabled: {
      type: Boolean,
      default: false,
    },
    options: {
      type: Array,
      default: () => ['a', 'b', 'c', 'd', 'e'],
    },
    theme: {
      type: String,
      default: 'solid',
    },
    onClick: {
      type: [Function, Boolean],
      required: false,
      default: false,
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
    values: {
      type: Array,
      default: () => [],
    },
  },

  data() {
    return {
      toggle: false,
      selectedOptions: [],
      valueP: 'set',
    }
  },

  computed: {
    labelId() {
      return uniqueId('multiselect-')
    },
    multiselectClass() {
      return `multiselectOptions ${
        this.toggle ? 'multiselect--active' : ''
      } theme--${this.theme}`
    },

    currentOption() {
      let x = ''
      if (this.value === '' || this.value === null || this.valueP === '') {
        x =
          this.options.find((option) => option.value === this.selectedOption)
            ?.display ||
          this.options[0]?.display ||
          'All'
      } else {
        x = this.value
      }
      return x
    },
  },

  mounted() {
    this.selectedOptions = this.values
  },

  methods: {
    multiselectClickOutside() {
      this.toggle = false
    },
    multiselectToggle() {
      if (this.disabled) return

      this.toggle = !this.toggle
    },
    update(option) {
      if (this.disabled) return

      if (this.selectedOptions.includes(option)) {
        this.selectedOptions = this.selectedOptions.filter(
          (selectedOption) => selectedOption !== option
        )
      } else {
        this.selectedOptions.push(option)
      }

      this.$emit('update', [...this.selectedOptions])
    },
    handleBlur() {
      this.$emit("blur")
    },
    getOptionClass(val) {
      return `option ${
        this.selectedOptions.includes(val) ? 'option--active' : ''
      }`
    },
  },
}
</script>

<style lang="scss" scoped>
.multiselectControl {
  display: flex;
  flex-flow: column;
  position: relative;
  width: 344px;
  gap: 8px;
  flex: 0 0 auto;
}

.optionList {
  width: 100%;

  padding: 0;

  border: 1px solid $brand-color-scale-2;
  box-shadow: 0px 0px 48px rgba(0, 0, 0, 0.05);
  border-radius: 8px;
  list-style: none;
  margin: 0 0 0 0;

  gap: 1px;

  display: flex;
  flex-flow: column;

  transform-origin: top;
  opacity: 1;
  transform: scaleY(1);
  transition: transform 0.35s ease-in-out, opacity 0.2s ease-in-out;

  .option {
    width: 100%;
    padding: 8px 16px;
    font-family: $font-opensans;
    font-style: normal;
    font-weight: 400;
    font-size: 16px;
    line-height: 24px;
    color: $brand-color-scale-6;
    transform-origin: top;
    opacity: 1;
    border-radius: 8px;
    transform: scaleY(1);
    transition: transform 0.35s ease-in-out, opacity 0.2s ease-in-out;
    cursor: pointer;

    &.option--active {
      color: $brand-white;
      background-color: $brand-primary-color;
    }
  }
}

.multiselectOptions {
  flex: 0 0 auto;
}

.multiselectOptions.multiselect--active {
  .optionList {
    opacity: 1;
    transform: scaleY(1);

    .option {
      opacity: 1;
      transform: scaleY(1);
    }
  }
  .selectedOption {
    svg {
      transform: translateY(-50%) rotate(0deg);
    }
  }
}

.theme--solid {
  .optionList {
    background: $brand-color-scale-1;
  }

  .selectedOption {
    background: $brand-color-scale-1;
    border-radius: 40px;
  }
}

.theme--outline {
  .optionList {
    background: $brand-white;
  }

  .selectedOption {
    background: none;
    border-radius: 8px;
  }
}
</style>
