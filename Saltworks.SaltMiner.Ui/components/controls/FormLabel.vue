<template>
  <div>
    <label :for="labelId">
      <span>{{ label }}</span>
    </label>
  </div>
</template>

<script>

export default {
  // eslint-disable-next-line vue/require-prop-types
  props: ['labelId', 'label', 'theme', 'value']
}
</script>

<style lang="scss" scoped>
label {
  user-select: none;
  font-family: $font-primary;
  font-style: normal;
  text-align: left;
  cursor: pointer;
  display: block;
  font-weight: 700;
  font-size: 16px;
  line-height: 24px;
  color: $brand-color-scale-6;
}
</style>
