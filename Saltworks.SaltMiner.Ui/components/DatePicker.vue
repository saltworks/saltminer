<template>
  <div :class="state.date">
    <span
      v-if="state.date === ''"
      class="datepicker-main"
      onclick="document.getElementById('datepicker').click();"
    >
      Select Date <CalLogo />
    </span>

    <span class="picked-calendar"><CalLogo /></span>
    <datepicker
      id="datepicker"
      class="datepicker_hidden"
      placeholder="Select Date"
      format="MMM dd yyyy"
      @input="onChange"
    ></datepicker>
  </div>
</template>

<script>
import Datepicker from 'vuejs-datepicker'
import CalLogo from '~/assets/svg/fi_calendar.svg?inline'

export default {
  components: {
    Datepicker,
    CalLogo,
  },
  data() {
    return {
      state: {
        date: '',
      },
    }
  },
  methods: {
    onChange() {
      this.state.date = 'active-calendar'
    },
  },
}
</script>
<style lang="scss">
.datepicker-main {
  display: flex;
  border: 1px solid $brand-color-scale-4;
  box-sizing: border-box;
  border-radius: 8px;
  padding: 16px;
  gap: 20px;
  width: 173px;
  font-family: $font-primary;
  font-style: normal;
  font-weight: 600;
  font-size: 14px;
  line-height: 18px;
  color: $brand-color-scale-6;
  cursor: pointer;
}
.datepicker-main svg {
  width: fit-content;
  margin-left: auto;
}
.datepicker_hidden input {
  display: none;
}
.active-calendar .datepicker_hidden input {
  display: flex;
  border: 1px solid $brand-color-scale-4;
  box-sizing: border-box;
  border-radius: 8px;
  padding: 16px;
  gap: 20px;
  width: 173px;
  font-family: $font-primary;
  font-style: normal;
  font-weight: 600;
  font-size: 14px;
  line-height: 18px;
  color: $brand-color-scale-6;
  outline: none !important;
  cursor: pointer;
  position: relative;
  top: -18px;
}
.picked-calendar {
  display: none;
  position: relative;
  z-index: 999;
  top: 16px;
  left: 136px;
  width: fit-content;
}
.active-calendar .picked-calendar {
  display: block;
}
</style>
