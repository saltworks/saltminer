<template>
  <div v-click-outside="dropdownClickOutside" class="dropdownControl">
    <FormLabel v-if="label" :label="label" :label-id="labelId"></FormLabel>
    <div :class="dropdownClass">
      <div id="detect" class="selectedOption" role="button" tabindex="0" @focus="dropdownToggle">
        <span :id="reff">{{ currentOption }}</span>
        <template v-if="!disabled">
          <IconChevronUp />
        </template>
      </div>
      <ul class="optionList" @blur="dropdownToggle">
        <li
          v-for="option in options"
          :key="`dropdown-${option.value}`"
          :class="getOptionClass(option.value)"
          @click="update(option)" 
          @keyup.enter="update(option)" 
          @blur="handleBlur" 
          tabindex="0"
        >
          {{ option.display }}
        </li>
      </ul>
    </div>

    <FieldErrors v-if="errors" :errors="errors" />
  </div>
</template>

<script>
import uniqueId from 'lodash/uniqueId'
import FieldErrors from '../FieldErrors'
import IconChevronUp from '../../assets/svg/fi_chevron-up.svg?inline'
import FormLabel from './FormLabel'

export default {
  components: {
    FieldErrors,
    IconChevronUp,
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
      default: () => [
        {
          display: 'All Fields',
          value: 'all',
        },
      ],
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
    value: {
      type: String,
      default: null,
    }
  },

  data() {
    return {
      toggle: false,
      selectedOption: '',
      mousedown: false
    }
  },
  mounted() {
    this.selectedOption = this.value;
  },
  computed: {
    labelId() {
      return uniqueId('dropdown-')
    },
    dropdownClass() {
      return `dropdownOptions ${this.toggle ? 'dropdown--active' : ''} theme--${
        this.theme
      }`
    },

    currentOption() {
      let x = ''
      x =
        this.options.find((option) => option.value === this.selectedOption)?.display ||
        this.options.find((option) => option.selected)?.display ||
        this.options.find((option) => option.order === 1)?.display ||
        this.options[0]?.display ||
        'None'
      return x
    },
  },

  methods: {
    dropdownClickOutside() {
      this.toggle = false
    },
    dropdownToggle() {
      if (this.disabled) return

      this.toggle = !this.toggle
    },
    handleBlur() {
      this.$emit('blur')
    },
    update(option) {
      if (this.disabled) return

      this.selectedOption = option.value
      this.toggle = false
      this.$emit('update', option)
      this.$emit('resetDropdown', '')
    },
    getOptionClass(val) {
      return `option ${this.selectedOption === val ? 'option--active' : ''}`
    },
  },
}
</script>

<style lang="scss" scoped>
.dropdownControl {
  display: flex;
  flex-flow: column;
  position: relative;
  min-width: 200px;
  gap: 8px;
  flex: 0 0 auto;
}

.optionList {
  z-index: 100;
  position: absolute;
  top: 100%;
  left: 0;
  overflow-x: hidden;
  overflow-y: scroll;
  max-height: 256px;
  width: 100%;

  padding: 16px;

  border: 1px solid $brand-color-scale-2;
  box-shadow: 0px 0px 48px rgba(0, 0, 0, 0.05);
  border-radius: 8px;
  list-style: none;
  margin: 0 0 0 0;

  display: flex;
  flex-flow: column;
  gap: 16px;

  transform-origin: top;
  opacity: 0;
  transform: scaleY(0);
  transition: transform 0.35s ease-in-out, opacity 0.2s ease-in-out;

  .option {
    width: 100%;
    font-family: $font-opensans;
    font-style: normal;
    font-weight: 400;
    font-size: 16px;
    line-height: 24px;
    color: $brand-color-scale-6;
    transform-origin: top;
    opacity: 0;
    transform: scaleY(0);
    transition: transform 0.35s ease-in-out, opacity 0.2s ease-in-out;
    cursor: pointer;

    &.option--active {
      color: $brand-primary-color;
    }
  }
}

.dropdownOptions {
  flex: 0 0 auto;
}
.selectedOption {
  position: relative;
  padding: 16px;
  border: 1px solid $brand-color-scale-4;
  cursor: pointer;

  span {
    position: relative;
    padding-right: 44px;
    font-size: 14px;
    line-height: 18px;
    font-weight: 400;
    color: $brand-color-scale-6;
    font-family: $font-opensans;
    pointer-events: none;
    white-space: nowrap;
  }

  svg {
    position: absolute;
    right: 21px;
    top: 50%;
    transform: translateY(-50%) rotate(180deg);
    height: 8px;
    width: 14px;
    pointer-events: none;
  }
}

.dropdownOptions.dropdown--active {
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
