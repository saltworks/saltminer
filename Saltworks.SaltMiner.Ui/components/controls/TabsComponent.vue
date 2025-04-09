<template>
  <div class="tabs-wrapper">
    <div v-for="(tab, i) in tabs" :key="i" class="tab">
      <div :id="tab.label" class="tab-btn" @click="tabClick($event, tab.label)">
        {{ tab.label }}
        <span v-if="tab.count">{{ tab.count }}</span>
      </div>

      <div v-if="tab.openInNewTab" style="margin-top: -10px;">
        <OpenLinkComponent
          iconSrc="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAoAAAAKCAYAAACNMs+9AAAAQElEQVR42qXKwQkAIAxDUUdxtO6/RBQkQZvSi8I/pL4BoGw/XPkh4XigPmsUgh0626AjRsgxHTkUThsG2T/sIlzdTsp52kSS1wAAAABJRU5ErkJggg=="
          :linkUrl="tab.linkUrl"
          :windowWidth="1000"
        />
      </div>
    </div>
  </div>
</template>

<script>
import OpenLinkComponent from './OpenLinkComponent'

export default {
  components: {
    OpenLinkComponent
  },
  props: {
    tabs: {
      type: Array,
      default: null,
    },
  },
  methods: {
    testInfoClick() {
      this.$emit('infoclick')
    },
    tabClick(event, label) {
      const slug = label
        .toLowerCase()
        .replace(/ /g, '-')
        .replace(/[^\w-]+/g, '')
      document.getElementById('app').classList = 'app ' + slug
      document.getElementById(slug).scrollIntoView()
      event.target.classList = 'tab-btn ' + slug
    },
  },
}
</script>

<style lang="scss" scoped>
.tabs-wrapper {
  display: flex;
  gap: 50px;
  font-style: normal;
  font-weight: 700;
  font-size: 16px;
  line-height: 24px;
  font-family: $font-primary;
  color: $brand-color-scale-5;
}
.tabs-wrapper .tab-btn {
  padding-bottom: 12px;
  padding-left: 15px;
  padding-right: 15px;
  cursor: pointer;
}

.tab {
  display: flex;
  align-items: center;

  .tab-icon {
    flex: 0 0 auto;
    align-self: center;
    position: relative;
    width: 12px;
    height: 12px;
    border: none;
    opacity: 0.5;
    border-radius: 0;
    padding: 0 0 0 0;
    margin: 0 0 12px 0;
    font-weight: 900;

    cursor: pointer;

    &:hover {
      opacity: 1;
    }
  }
}
.audit .audit,
.basic-info .basic-info,
.details .details,
.supporting-info .supporting-info {
  color: $brand-color-scale-6;
  border-bottom: 4px solid $brand-primary-color;
}
.tabs-wrapper span {
  background: $brand-off-white-2;
  border: 1px solid $brand-color-scale-2;
  box-sizing: border-box;
  border-radius: 24px;
  padding: 4px 8px;
  font-size: 14px;
  margin-left: 8px;
}
</style>
