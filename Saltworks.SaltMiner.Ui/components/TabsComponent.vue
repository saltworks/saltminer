<template>
    <div>
        <div class="tabs">
        <ul>
            <li v-for="(tab, i) in tabs" :key="i" :class="{ 'is-active': tab.isActive }">
                <a @click="selectTab(tab)">{{ tab.name }}</a>
            </li>
        </ul>
        </div>

        <div class="tabs-details">
            <slot></slot>
        </div>
    </div>
</template>


<script>
export default {
    components: { },
    props: {},
    data() {
        return {tabs: [] };
    },
    created() {
        this.tabs = this.$children;
    },
    methods: {
        selectTab(selectedTab) {
            this.tabs.forEach(tab => {
                tab.isActive = (tab.name === selectedTab.name);
            });
        }
    }
}
</script>

<style lang="scss" scoped>

ul li {
    display: inline-block;
}

.tabs {
    -webkit-overflow-scrolling: touch;
    -webkit-touch-callout: none;
    -webkit-user-select: none;
    -moz-user-select: none;
    -ms-user-select: none;
    user-select: none;
    -webkit-box-align: stretch;
    -ms-flex-align: stretch;
    align-items: stretch;
    display: -webkit-box;
    display: -ms-flexbox;
    display: flex;
    font-size: 1rem;
    -webkit-box-pack: justify;
    -ms-flex-pack: justify;
    justify-content: space-between;
    overflow: hidden;
    overflow-x: auto;
    white-space: nowrap;
}
.tabs li.is-active a {
    border-bottom-color: black;
    color: $brand-primary-color;
    background-color: $brand-primary-color-transparent;
    border-radius: 15px 15px 0 0;
}
.tabs a {
    -webkit-box-align: center;
    -ms-flex-align: center;
    align-items: center;
    border-bottom: 1px solid #dbdbdb;
    color: #4a4a4a;
    display: -webkit-box;
    display: -ms-flexbox;
    display: flex;
    -webkit-box-pack: center;
    -ms-flex-pack: center;
    justify-content: center;
    margin-bottom: -1px;
    padding: 0.5em 1em;
    vertical-align: top;
    cursor: pointer;
}
</style>