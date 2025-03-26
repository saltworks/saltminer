<template>
  <div @click="handleLeftClick" @contextmenu.prevent="handleRightClick" class="icon-container">
    <img :src="iconSrc" :alt="iconAlt" />
    <ul v-if="menuVisible" class="context-menu">
      <li @click="openInTab">Open in New Tab</li>
      <li @click="openInWindow">Open in New Window</li>
    </ul>
  </div>
</template>

<script>
export default {
  props: {
    iconSrc: String,  // Source URL for the icon/image
    iconAlt: String,  // Alt text for the image
    linkUrl: String,   // The URL to open when clicked
    windowWidth: {
      type: Number,
      default: 800 // Default width if not provided
    },
    windowHeight: {
      type: Number,
      default: 600 // Default height if not provided
    },
    windowLeft: {
      type: Number,
      default: 100 // Default left position if not provided
    },
    windowTop: {
      type: Number,
      default: 100 // Default top position if not provided
    }
  },
  mounted() {
    window.addEventListener("click", this.handleClickOutside);
    window.addEventListener("contextmenu", this.handleClickOutside);
  },
  beforeUnmount() {
    window.removeEventListener("click", this.handleClickOutside);
    window.removeEventListener("contextmenu", this.handleClickOutside);
  },
  data() {
    return {
      menuVisible: false
    };
  },
  methods: {
    handleLeftClick() {
      window.open(this.linkUrl, '_blank');
    },
    handleRightClick(event) {
      this.menuVisible = true;
    },
    handleClickOutside(event) {
      // Close if click is outside the menu
      if (!this.$el.contains(event.target)) {
        this.menuVisible = false;
      }
    },
    openInTab() {
      window.open(this.linkUrl, '_blank');
      this.menuVisible = false;
    },
    openInWindow() {
      window.open(this.linkUrl, '_blank', `width=${this.windowWidth},height=${this.windowHeight},left=${this.windowLeft},top=${this.windowTop}`);
      this.menuVisible = false;
    }
  }
};
</script>

<style scoped>
.icon-container {
  display: inline-block;
  position: relative;
}

.context-menu {
  position: absolute;
  background: white;
  border: 1px solid #ccc;
  list-style: none;
  padding: 5px 0;
  margin: 0;
  box-shadow: 2px 2px 10px rgba(0, 0, 0, 0.2);
  z-index: 1000;
  width: 195px;
}

.context-menu li {
  padding: 8px 15px;
  cursor: pointer;
}

.context-menu li:hover {
  background: #f0f0f0;
}

img {
  cursor: pointer;
  width: 13px;
  height: 13px;
}
</style>
