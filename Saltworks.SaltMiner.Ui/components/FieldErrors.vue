<template>
  <div v-if="errors.length" class="fieldErrors">
    <div v-for="(error, i) in errors" :key="i" class="fieldError">
      {{ error }}
    </div>
  </div>
</template>
<script>
export default {
  props: {
    errors: {
      type: Array,
      required: true,
      default: () => [],
    },
  },
}
</script>
<style lang="scss" scoped>
.fieldErrors {
  color: $brand-color-scale-6;
  font-size: 13px;
  line-height: 18px;
  padding-top: 8px;
}
</style>
