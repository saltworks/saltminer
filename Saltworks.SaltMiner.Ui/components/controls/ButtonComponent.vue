<template>
  <button :class="btnClasses" @click="handleOnClick">
    <span v-if="!iconOnly" :class="btnInnerClasses.join(' ')">
      <span v-if="icon" class="btnIcon">
        <component :is="icon"></component>
      </span>
      <span v-if="!iconOnly" class="btnLabel">{{ label }}</span>
    </span>
    <span v-if="icon && iconOnly" class="btnIcon">
      <component :is="icon"></component>
    </span>
  </button>
</template>
<script>
export default {
  props: {
    label: {
      type: String,
      default: '',
    },

    inline: {
      type: Boolean,
      default: false,
    },

    // This accepts an svg component loaded through vue-svg-loader
    // ie: import AddIcon from 'icon-add.svg'
    icon: {
      type: Object,
      default: null,
    },

    iconPosition: {
      type: String,
      default: 'left',
      validator: (value) => ['left', 'right'].includes(value),
    },

    iconOnly: {
      type: Boolean,
      default: false,
    },

    disabled: {
      type: Boolean,
      default: false,
    },

    theme: {
      type: String,
      default: 'default',
      validator: (value) =>
        [
          'default',
          'primary',
          'primary-outline',
          'secondary',
          'danger',
          'save',
          'cancel'
        ].includes(value),
    },

    size: {
      type: String,
      default: 'medium',
      validator(value) {
        return ['medium', 'small', 'xsmall', 'large'].includes(value)
      },
    },

    onClick: {
      type: [Function, Boolean],
      required: false,
      default: false,
    },

    fullWidth: {
      type: Boolean,
      default: false,
    },
  },

  computed: {
    btnClasses() {
      const classes = ['btn', `btn--${this.theme}`]

      if (this.inline) {
        classes.push('btn--inline')
      }

      if (this.disabled) {
        classes.push('btn--disabled')
      }

      if (this.size) {
        classes.push(`btn--size-${this.size}`)
      }

      if (this.fullWidth) {
        classes.push('btn--block')
      }

      if (this.iconOnly) {
        classes.push('iconOnly')
      }

      return classes
    },

    btnInnerClasses() {
      const classes = ['btnInner']

      if (this.disabled) {
        classes.push('btnInner--disabled')
      }

      if (!this.icon) return classes

      if (this.iconPosition === 'left') {
        classes.push('btnInner--icon-left')
      } else {
        classes.push('btnInner--icon-right')
      }

      return classes
    },
  },

  mounted() {},

  methods: {
    handleOnClick() {
      if (this.disabled) return
      this.$emit('button-clicked', this.label)
    },
  },
}
</script>

<style lang="scss" scoped>
.btn {
  background: $button-primary-color;
  border-radius: 0;
  font-size: 18px;
  line-height: 24px;
  letter-spacing: 0.5px;
  text-align: center;
  font-weight: 500;
  font-family: $font-button;
  color: $brand-white;
  display: inline-block;
  margin: 0;
  padding: 11px 20px;

  &.iconOnly {
    padding: 0 0 0 0;
    width: 48px;
    height: 48px;
    display: flex;
    align-items: center;
    justify-content: center;
    box-shadow: 0 1px 4px rgba($brand-black, 0.15);
  }

  appearance: none;
  border: 1px solid $button-primary-color;
  width: auto;
  cursor: pointer;
  text-decoration: none;
  transition: 300ms cubic-bezier(0.16, 1, 0.3, 1) border-color,
    300ms cubic-bezier(0.16, 1, 0.3, 1) background-color,
    300ms cubic-bezier(0.16, 1, 0.3, 1) color,
    500ms cubic-bezier(0.16, 1, 0.3, 1) opacity;

  &:active {
    text-decoration: none;
    box-shadow: inset 0 1px 3px rgba($brand-black, 0.15);
  }

  &:hover {
    text-decoration: none;
    background-color: darken($button-primary-color, 5%);

    svg {
      path {
        transition: 300ms cubic-bezier(0.16, 1, 0.3, 1) fill;
      }
    }
  }
}

.btn--outline {
  background: $brand-white;
  color: $button-primary-color;

  ::v-deep {
    svg {
      path {
        stroke: $button-primary-color;
      }
    }
  }

  &:hover {
    color: $brand-white;
    background-color: $button-primary-color;

    ::v-deep {
      svg {
        path {
          stroke: $brand-white;
        }
      }
    }
  }
}

.btn--block {
  width: 100%;
  display: block;
}

.btn--size-xsmall {
  padding: 3px 5px;
  font-size: 16px;
  line-height: 16px;
  font-weight: 200;
}

.btn--size-small {
  padding: 6px 10px;
  font-size: 16px;
  line-height: 20px;
  font-weight: 400;
}

.btn--size-medium {
  font-size: 16px;
  line-height: 24px;
  font-weight: 700;
  font-family: $font-primary;
}

.btn--disabled {
  background-color: $brand-color-scale-3 !important;
  color: $button-disabled-color !important;
  border-color: $brand-color-scale-3 !important;
  pointer-events: none !important;
  transition: 300ms cubic-bezier(0.16, 1, 0.3, 1) border-color,
    300ms cubic-bezier(0.16, 1, 0.3, 1) background-color,
    300ms cubic-bezier(0.16, 1, 0.3, 1) color;
}

.btn--inline {
  display: inline-block;
  width: auto;
}

.btnInner {
  display: flex;
  align-items: center;
  justify-content: center;
}

.btnInner--disabled {
  span {
    color: $button-disabled-color !important;
  }
}
.btnInner--icon-right,
.btnInner--icon-left {
  gap: 8px;
}

.btnInner--icon-right {
  flex-direction: row-reverse;
}

.btnIcon {
  margin-right: 7px;
  width: 16px;

  @at-root .btnInner--icon-right #{&} {
    margin-right: 0;
    margin-left: 4px;
  }
}

.btnIcon svg {
  width: 24px;
}

// themes
.btn--default,
.btn--primary,
.btn--primary-outline,
.btn--secondary,
.btn--save,
.btn--cancel

.btn--danger {
  border-radius: 40px;
}

.btn--save{
  
  padding: 12px;
  font-style: normal;
  font-weight: 700;
  font-size: 16px;
  line-height: 24px;
  border-radius: 40px;
  cursor: pointer;
  transition: 400ms cubic-bezier(0.075, 0.82, 0.165, 1);
  width: 50%;
  text-align: center;
  gap: 20px;

  color: $brand-white;
  background: $brand-primary-color;
  border: 1px solid $brand-primary-color;

  &:hover {
  color: $brand-primary-color;
  background: $brand-white;
  border: 1px solid $brand-primary-color;
  }
}

.btn--cancel{
  padding: 12px;
  font-style: normal;
  font-weight: 700;
  font-size: 16px;
  line-height: 24px;
  border-radius: 40px;
  cursor: pointer;
  transition: 400ms cubic-bezier(0.075, 0.82, 0.165, 1);
  width: 50%;
  text-align: center;
  gap: 20px;

  color: $brand-color-scale-6;
  background: $brand-color-scale-1;
  border: 1px solid $brand-color-scale-1;

  &:hover {
  color: $brand-color-scale-1;
  background: $brand-color-scale-6;
  border: 1px solid $brand-color-scale-6;
  }
}


.btn--secondary {
  background-color: $button-secondary-background;
  border: 1px solid $button-secondary-border;
  .btnLabel {
    color: $button-secondary-label;
  }

  ::v-deep {
    svg {
      path {
        stroke: $button-secondary-label;
      }
    }
  }

  &:hover {
    color: $button-secondary-hover-color;
    background: $button-secondary-hover-background;
    border: 1px solid $button-secondary-hover-border;

    .btnLabel {
      color: $button-secondary-hover-label;
    }

    ::v-deep {
      svg {
        path {
          stroke: $button-secondary-hover-label;
        }
      }
    }
  }
  &:active {
    color: $button-secondary-active-color;
    background: $button-secondary-active-background;
    border: 1px solid $button-secondary-active-border;

    .btnLabel {
      color: $button-secondary-active-label;
    }

    ::v-deep {
      svg {
        path {
          stroke: $button-secondary-active-label;
        }
      }
    }
  }
}
.btn--primary {
  background: $button-primary-background;
  border: 1px solid $button-primary-border;

  .btnLabel {
    color: $button-primary-label;
  }

  ::v-deep {
    svg {
      path {
        stroke: $button-primary-label;
      }
    }
  }

  &:hover {
    color: $button-primary-hover-color;
    background: $button-primary-hover-background;
    border: 1px solid $button-primary-hover-border;

    .btnLabel {
      color: $button-primary-hover-label;
    }

    ::v-deep {
      svg {
        path {
          stroke: $button-primary-hover-label;
        }
      }
    }
  }

  &:active {
    color: $button-primary-active-color;
    background: $button-primary-active-background;
    border: 1px solid $button-primary-active-border;

    .btnLabel {
      color: $button-primary-active-label;
    }

    ::v-deep {
      svg {
        path {
          stroke: $button-primary-active-label;
        }
      }
    }
  }
}
.btn--danger {
  background: $button-danger-background;
  border: 1px solid $button-danger-border;
  border-radius: 40px;

  .btnLabel {
    color: $button-danger-label;
  }

  ::v-deep {
    svg {
      path {
        stroke: $button-danger-label;
      }
    }
  }

  &:hover {
    color: $button-danger-hover-color;
    background: $button-danger-hover-background;
    border: 1px solid $button-danger-hover-border;

    .btnLabel {
      color: $button-danger-hover-button-label;
    }

    ::v-deep {
      svg {
        path {
          stroke: $button-danger-hover-label;
        }
      }
    }
  }

  &:active {
    color: $button-danger-active-color;
    background: $button-danger-active-background;
    border: 1px solid $button-danger-active-border;

    .btnLabel {
      color: $button-danger-active-button-label;
    }

    ::v-deep {
      svg {
        path {
          stroke: $button-danger-active-label;
        }
      }
    }
  }
}
.btn--primary-outline {
  background: $button-primary-outline-background;
  border: 1px solid $button-primary-outline-border;

  .btnLabel {
    color: $button-primary-outline-label;
  }

  ::v-deep {
    svg {
      path {
        stroke: $button-primary-outline-label;
      }
    }
  }

  &:hover {
    background: $button-primary-outline-hover-background;
    color: $button-primary-outline-hover-color;
    border: 1px solid $button-primary-outline-hover-border;

    .btnLabel {
      color: $button-primary-outline-hover-label;
    }

    ::v-deep {
      svg {
        path {
          stroke: $button-primary-outline-hover-label;
        }
      }
    }
  }
  &:active {
    background: $button-primary-outline-active-background;
    color: $button-primary-outline-active-color;
    border: 1px solid $button-primary-outline-active-border;

    .btnLabel {
      color: $button-primary-outline-active-label;
    }

    ::v-deep {
      svg {
        path {
          stroke: $button-primary-outline-active-label;
        }
      }
    }
  }
}
.btn--default {
  background: $button-default-background;
  border: 1px solid $button-default-border;

  .btnLabel {
    color: $button-default-label;
  }

  ::v-deep {
    svg {
      path {
        stroke: $button-default-label;
      }
    }
  }

  &:hover {
    background: $button-default-hover-background;
    color: $button-default-hover-color;
    border: 1px solid $button-default-hover-border;

    .btnLabel {
      color: $button-default-hover-label;
    }

    ::v-deep {
      svg {
        path {
          stroke: $button-default-hover-label;
        }
      }
    }
  }
  &:active {
    background: $button-default-active-background;
    color: $button-default-active-color;
    border: 1px solid $button-default-active-border;

    .btnLabel {
      color: $button-default-active-label;
    }

    ::v-deep {
      svg {
        path {
          stroke: $button-default-active-label;
        }
      }
    }
  }
}
</style>
