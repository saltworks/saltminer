<template>
  <div class="layout">
    <aside class="sidebar" @mouseenter="hover = true" @mouseleave="hover = false">
     <div class="sidebar-item arrow">▶</div>
      <button
        v-for="tab in tabs"
        :key="tab.name"
        @click="currentTab = tab.name"
        :class="['sidebar-item', { active: currentTab === tab.name }]"
      >
        {{ tab.label }}
      </button>
    </aside>

    <main class="main" :class="{ 'with-sidebar': hover }">
      <component :is="currentTabComponent" />
    </main>
  </div>
</template>


<script>
import { mapState } from 'vuex'
import isValidUser from '../../middleware/is-valid-user'
import AlertComponent from '../../components/AlertComponent'
import LoadingComponent from '../../components/LoadingComponent'
import CustomFieldDefinition from '../../components/admin/CustomFieldDefinition.vue'
import LookUp from '../../components/admin/LookUp.vue'
import CustomAttribute from '../../components/admin/CustomAttribute.vue'
import ReportTemplates from '../../components/admin/ReportTemplates.vue'
import ServiceJobScheduler from '../../components/admin/ServiceJobScheduler.vue'
import AppRoles from '../../components/admin/AppRoles.vue'
import SearchFilter from '../../components/admin/SearchFilter.vue'

export default {
  name: 'EngagementsIndex',
  components: {
    LoadingComponent,
    AlertComponent,
    CustomFieldDefinition,
    LookUp,
    CustomAttribute,
    ReportTemplates,
    ServiceJobScheduler,
    AppRoles,
    SearchFilter
  },
  middleware: isValidUser,
  data() {
    return {
      currentTab: 'AppRoles',
      tabs: [
        { name: 'AppRoles', label: 'Application Roles' },
        { name: 'CustomFieldDefinition', label: 'Custom Field Definitions' },
        { name: 'CustomAttribute', label: 'Custom Attribute Definitions' },
        { name: 'LookUp', label: 'Look Ups' },
        { name: 'SearchFilter', label: 'Search Filter Definitions' },
        { name: 'ReportTemplates', label: 'Report Templates' },
        { name: 'ServiceJobScheduler', label: 'Service Job Scheduler' }
      ],
      componentsMap: {
        AppRoles,
        CustomFieldDefinition,
        CustomAttribute,
        LookUp,
        SearchFilter,
        ReportTemplates,
        ServiceJobScheduler
      },
      defaultDateFormat: 'en-US',
      alert: {
          messages: [],
          type: "",
          title: ""
      },
      systemType: 'Application Roles'
    }
  },
  async fetch({ store: { dispatch } }) {
    await dispatch('configInit')
  },
  head() {
    return {
      title: `Admin | ${this.pageTitle}`,
    }
  },
  computed: {
    ...mapState({
      dateFormat: (state) => state.modules.user.dateFormat,
      user: (state) => state.modules.user,
      isLoading: (state) => state.loading.loading,
      pageTitle: (state) => state.config.pageTitle
    }),
    currentTabComponent() {
      return this.componentsMap[this.currentTab]
    },
  },
  mounted() {
  },
  methods: {
    handleAlertClose() {
      this.alert = {
        messages: [],
        type: "",
        title: ""
      }
    },
    addAlert(messages, type, title) {
      this.alert.title = title
      this.alert.messages = messages
      this.alert.type = type
    },
    handleErrorResponse(r, title) {
      this.errorResponse = this.$getErrorResponse(r)
      if (this.errorResponse != null) {
        this.addAlert(this.errorResponse, "danger", title)
      }
    }
  }
}
</script>

<style lang="scss" scoped>

.sidebar {
  position: fixed;
  top: 0;
  left: 0;
  height: 100vh;
  width: 24px;
  background-color: #343a40;
  transition: width 0.3s ease;
  overflow: hidden;
  z-index: 1000;
  display: flex;
  flex-direction: column;
  align-items: center;
  padding-top: 1rem;
}

.sidebar:hover {
  width: 200px;
  align-items: flex-start;
  padding-left: 1rem;
}

.sidebar-item {
  padding: 0.75rem 0.25rem;
  color: white;
  background: none;
  border: none;
  text-align: left;
  white-space: nowrap;
  cursor: pointer;
  transition: background-color 0.2s;
  display: flex;
  align-items: center;
  width: 100%;
}

.sidebar-item:hover {
  background-color: #495057;
}

.sidebar-item.active {
  background-color: #187308;
  font-weight: bold;
}

.arrow {
  color: white;
  font-size: 1.2rem;
  position: absolute;
  top: 50%;
  left: 0;
  right: 0;
  transform: translateY(-50%);
  display: flex;
  justify-content: center;
  pointer-events: none;
}

.sidebar:not(:hover) .sidebar-item:not(.arrow) {
  display: none;
}

.sidebar:hover .sidebar-item {
  padding-right: 1rem;
}

.sidebar:hover .arrow {
  display: none;
}

.main {
  margin-left: 0;
  width: 100vw;
  padding: 2rem;
  background-color: #fFFFF;
  min-height: 100vh;
  overflow: auto;
  transition: all 0.3s ease;
}


</style>
