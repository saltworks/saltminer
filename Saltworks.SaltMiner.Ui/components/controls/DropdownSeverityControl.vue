<template>
  <div
    v-click-outside="dropdownClickOutside"
    :class="`${getRoundedClass} ${isSearching ? 'isSearching' : ''}`"
  >
    <FormLabel v-if="label" :label="label" :label-id="labelId"></FormLabel>
    <div :class="dropdownClass">
      <div class="selectedOption" role="button" @click="dropdownToggle">
        <div :class="severityColorClass(currentOption.value)"></div>
        <span :id="reff">{{ currentOption.display }}</span>
        <template v-if="!disabled">
          <IconChevronUp />
        </template>
      </div>
      <ul class="optionList">
        <li
          v-if="!hideAll"
          :class="getOptionClass('all')"
          @click="update({ display: defaultDisplay, value: 'all' })"
        >
          <span>All</span>
        </li>
        <div class="list-section-header">Vulnerability</div>
        <li
          v-for="option in vulnerableOptions"
          :key="`dropdown-${option.value}`"
          :class="getOptionClass(option.value)"
          @click="update(option)"
        >
          <div :class="severityColorClass(option.value)"></div>
          <span>{{ option.display }}</span>
        </li>
        <div class="m-t-2 list-section-header">Not a Vulnerability</div>
        <li
          v-for="option in nonVulnerableOptions"
          :key="`dropdown-${option.value}`"
          :class="getOptionClass(option.value)"
          @click="update(option)"
        >
          <div :class="severityColorClass(option.value)"></div>
          <span>{{ option.display }}</span>
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
    options: {
      type: Array,
      default: () => [],
    },
    hideAll: {
      type: Boolean,
      default: false,
    },
    defaultDisplay: {
      type: String,
      default: 'All',
    },
    theme: {
      type: String,
      default: 'default',
    },
    rounded: {
      type: Boolean,
      default: false,
    },
    isSearching: {
      type: Boolean,
      default: false,
    },
    errors: {
      type: Array,
      required: false,
      default: () => [],
    },
    disabled: {
      type: Boolean,
      default: false,
    },
    reff: {
      type: String,
      default: null,
    },
    value: {
      type: String,
      default: null
    }
  },

  data() {
    return {
      toggle: false,
      selectedOption: '',
    }
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
      return (
        this.options.find((option) => option.value === this.value) ||
        this.options.find((option) => option.value === this.selectedOption) ||
        this.options.find((option) => option.selected) ||
        this.options.find((option) => option.order === 0) ||
        {
          display: this.defaultDisplay,
          value: 'all',
          order: 0
        }
      )
    },

    vulnerableOptions() {
      return this.options.filter(
        (option) => !option.value.toLowerCase().includes('info')
      )
    },

    nonVulnerableOptions() {
      return this.options.filter((option) =>
        option.value.toLowerCase().includes('info')
      )
    },

    getRoundedClass() {
      return this.rounded
        ? 'dropdownSeverityControl rounded'
        : 'dropdownSeverityControl'
    },
  },
  methods: {
    severityColorClass(val) {
      return `severity-color severity-color--${val?.toLowerCase()}`
    },
    dropdownClickOutside() {
      this.toggle = false
    },
    dropdownToggle() {
      if (this.disabled) return

      this.toggle = !this.toggle
    },
    update(option) {
      if (this.disabled) return
      
      this.selectedOption = option.value
      this.toggle = false
      this.$emit('update', option)
    },
    isSelected(val) {
      return this.selectedOption === val
    },
    getOptionClass(val = '') {
      return `option ${this.isSelected(val) ? 'option--active' : ''}`
    },
  },
}
</script>

<style lang="scss" scoped>
.dropdownSeverityControl {
  display: flex;
  flex-flow: column;
  position: relative;
  gap: 8px;
  min-width: 200px;
  max-width: 250px;
  flex: 0 0 auto;
  transition: filter 0.25s 0.5s ease-in-out;
  &.isSearching {
    filter: brightness(0.75) grayscale(2);
    transition: filter 0s ease-in-out;
  }
}

.severity-color--critical,
.severity-color--high,
.severity-color--medium,
.severity-color--low,
.severity-color--info,
.severity-color--best-practice {
  position: relative;
  display: block;
  width: 20px;
  height: 20px;
  border-radius: 50%;
  flex: 0 0 auto;
}

.severity-color--best-practice {
  background-color: $issue-severity-best-practice;
}
.severity-color--info {
  background-color: $issue-severity-info;
}
.severity-color--low {
  background-color: $issue-severity-low;
}
.severity-color--medium {
  background-color: $issue-severity-medium;
}
.severity-color--high {
  background-color: $issue-severity-high;
}
.severity-color--critical {
  background-color: $issue-severity-critical;
}

.optionList {
  z-index: 100;
  position: absolute;
  top: 100%;
  left: 0;
  overflow-x: hidden;
  overflow-y: scroll;
  width: 100%;
  padding: 8px 0 8px 0;
  border: 1px solid $brand-color-scale-2;
  box-shadow: 0px 0px 48px rgba(0, 0, 0, 0.05);
  border-radius: 8px;
  list-style: none;
  margin: 0 0 0 0;

  display: flex;
  flex-flow: column;

  transform-origin: top;
  opacity: 0;
  transform: scaleY(0);
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
    opacity: 0;
    transform: scaleY(0);
    transition: transform 0.35s ease-in-out, opacity 0.2s ease-in-out;
    cursor: pointer; 
    display: flex;
    gap: 8px;
    white-space: nowrap;

    &:hover {
      background-color: $brand-color-scale-1;
    }

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
  display: flex;
  border-radius: 8px;
  align-items: center;
  gap: 8px;

  min-width: 216px;

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

.list-section-header {
  width: calc(100% - 32px);
  font-size: 14px;
  line-height: 18px;
  font-weight: 600;
  font-style: normal;
  font-family: $font-opensans;
  color: $brand-color-scale-6;
  border-bottom: 1px solid $brand-color-scale-2;
  padding: 8px 0 8px 0;
  margin: 0 auto 8px auto;
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

.theme--default {
  .selectedOption {
    background: $brand-white;
  }
  .optionList {
    background: $brand-white;
  }
}

.theme--default {
  .selectedOption {
    border: 1px solid $brand-color-scale-4;
    background: $brand-white;
  }
  .optionList {
    background: $brand-white;
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
