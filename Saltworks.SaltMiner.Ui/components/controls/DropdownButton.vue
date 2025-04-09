<template>
  <div v-click-outside="dropdownClickOutside" class="dropdownButton">
    <FormLabel v-if="label" :label="label" :label-id="labelId"></FormLabel>
    <div :class="dropdownClass">
      <div class="selectedOption" role="button" @click.stop="dropdownToggle">
        <span v-if="icon" class="buttonIcon">
          <component :is="icon"></component>
        </span>
        <span>{{ buttonText }}</span>
        <IconChevronUp />
      </div>
      <ul class="optionList">
        <li
          v-for="option in orderOptions"
          :key="`dropdown-${option.value}`"
          :class="getOptionClass(option.value)"
          @click.stop="update(option)"
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
    buttonText: {
      type: String,
      default: '',
    },
    icon: {
      type: Object,
      default: null,
    },
    options: {
      type: Array,
      default: () => [],
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
  },

  data() {
    return {
      toggle: false,
      selectedOption: this.options[0].value,
    }
  },

  computed: {
    orderOptions() {
      return [...this.options].sort((a, b) => b.order - a.order)
    },
    labelId() {
      return uniqueId('dropdown-')
    },
    dropdownClass() {
      return `dropdownOptions ${this.toggle ? 'dropdown--active' : ''}`
    },

    currentOption() {
      return this.options.find((option) => option.value === this.selectedOption)
        .label
    },
  },

  methods: {
    dropdownClickOutside() {
      this.toggle = false
    },
    dropdownToggle() {
      this.toggle = !this.toggle
    },
    update(option) {
      this.selectedOption = option.value
      this.toggle = false
      this.$emit('update', option)
      if (typeof option.onClick === 'function') {
        option.onClick()
      }
    },
    getOptionClass(val) {
      return `option ${this.selectedOption === val ? 'option--active' : ''}`
    },
  },
}
</script>

<style lang="scss" scoped>
.dropdownButton {
  display: flex;
  flex-flow: column;
  position: relative;
  gap: 8px;
}

.optionList {
  z-index: 100;
  position: absolute;
  top: 100%;
  left: 50%;
  overflow-x: hidden;
  overflow-y: scroll;
  max-height: 256px;

  min-width: 216px;

  padding: 16px 36px;

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
  transform: translateX(-50%) scaleY(0);
  transition: transform 0.35s ease-in-out, opacity 0.2s ease-in-out;

  .option {
    width: 100%;
    font-family: $font-opensans;
    font-style: normal;
    font-weight: 700;
    font-size: 16px;
    line-height: 24px;
    color: $brand-color-scale-6;
    transform-origin: top;
    opacity: 0;
    transform: scaleY(0);
    transition: transform 0.35s ease-in-out, opacity 0.2s ease-in-out;
    cursor: pointer;
    text-align: center;
    &.option--active {
      color: $brand-color-scale-6;
    }
  }
}

.dropdownOptions {
  flex: 0 0 auto;
}
.selectedOption {
  position: relative;
  padding: 11px 16px;
  border: 1px solid $brand-color-scale-2;
  cursor: pointer;

  span {
    position: relative;
    padding-right: 44px;
    font-size: 16px;
    line-height: 24px;
    font-weight: 700;
    color: $brand-white;
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
    transform: translateX(-50%) scaleY(1);

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

.optionList {
  background: $brand-white;
}

.selectedOption {
  background: $brand-primary-color;
  border-radius: 40px;

  .buttonIcon {
    svg {
      width: 24px;
      height: 24px;
    }
  }

  svg {
    path {
      stroke: $brand-white;
    }
  }
}
</style>
