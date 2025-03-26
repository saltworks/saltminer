<template>
  <div class="heading" :class="headingClasses.join(' ')">
    <h1 v-if="size === '1'">{{ label }}</h1>
    <h2 v-if="size === '2'">{{ label }}</h2>
    <h3 v-if="size === '3'">{{ label }}</h3>
    <h4 v-if="size === '4'">{{ label }}</h4>
  </div>
</template>
<script>
export default {
  props: {
    label: {
      type: String,
      default: '',
    },
    size: {
      // This is a string to allow for custom classes to be generated that
      // are outside of your often typical 1-6 heading range
      type: String,
      default: '1',
    },
  },

  computed: {
    headingClasses() {
      const headingClasses = ['heading']
      headingClasses.push(`heading-${this.size}`)
      return headingClasses
    },
  },
}
</script>
<style lang="scss" scoped>
.heading-1 h1,
.heading-2 h2,
.heading-3 h3,
.heading-4 h4 {
  color: $brand-color-scale-6;
  letter-spacing: -0.25px;
  font-family: 'Open Sans', sans-serif;
}

.heading-1 h1 {
  font-size: 48px;
  line-height: 48px;
  font-weight: 600;
}

.heading-2 h2 {
  font-size: 32px;
  line-height: 48px;
  font-weight: 700;
}

.heading-3 h3 {
  font-size: 24px;
  line-height: 32px;
  font-weight: 700;
}

.heading-4 h4 {
  font-size: 20px;
  line-height: 32px;
  font-weight: 700;
}
</style>
