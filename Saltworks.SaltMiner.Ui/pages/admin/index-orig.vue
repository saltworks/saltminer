<template>
    <div class="page-layout">
      <div class="row">
        <HeadingText label="Admin Panel" size="2" />
      </div>
        <div class="page-content">
            <LoadingComponent :is-visible="isLoading" />
            <div class="page-row-left">
                <sidebar-menu 
                    :menu="menu"
                    @item-click="onItemClick" 
                />
                <div v-if="systemType === 'Application Roles'">
                    <AppRoles></AppRoles>     
                </div>
                <div v-if="systemType === 'Custom Field Definitions'">
                    <CustomFieldDefinition></CustomFieldDefinition>     
                </div>
                <div v-if="systemType === 'Custom Attribute Definitions'">
                    <CustomAttribute></CustomAttribute>                     
                </div>
                <div v-if="systemType === 'Look Ups'">
                    <LookUp></LookUp>     
                </div>
                <div v-if="systemType === 'Search Filter Definitions'"> 
                    <SearchFilter></SearchFilter>                                  
                </div>
                <div v-if="systemType === 'Report Templates'"> 
                    <ReportTemplates></ReportTemplates>                                  
                </div>
                <div v-if="systemType === 'Service Job Scheduler'">
                    <ServiceJobScheduler></ServiceJobScheduler>
                </div>
                <div v-if="systemType === 'Configuration Services'">
                    <h2>Configuration Services</h2>
                    text editor and load the document in???
                </div>
            </div>
        </div>
        <AlertComponent
        v-if="alert.messages.length > 0"
        class="alert-margin"
        :alert="alert"
        @close="handleAlertClose"
        />
    </div>
</template>

<script>
import { mapState } from 'vuex'
import { SidebarMenu } from 'vue-sidebar-menu'
import isValidUser from '../../middleware/is-valid-user'
import IconSettings from '../../assets/svg/fi_settings.svg?inline'
import HeadingText from '../../components/HeadingText.vue'
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
    HeadingText,
    LoadingComponent,
    AlertComponent,
    CustomFieldDefinition,
    SidebarMenu,
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
      menu: [
        {
          header: true,
          title: 'Main Navigation',
          hiddenOnCollapse: true
        },
        {
          title: 'Application Roles'
        },
        {
          title: 'Custom Field Definitions'
        },
        {
          title: 'Custom Attribute Definitions'
        },
        { title: 'Look Ups'
        },
        {
          title: 'Search Filter Definitions'
        },
        {
          title: 'Report Templates'
        },
        {
          title: 'Service Job Scheduler'
        },
        {
          // implement later?
          // title: 'Configuration Services'
        }
      ],
      icons: {
        settings: IconSettings,
      },
      defaultDateFormat: 'en-US',
      alert: {
          messages: [],
          type: "",
          title: ""
      },
      systemDropdownOptions: [],
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
  },
  mounted() {
  },
  methods: {
    onItemClick(event, item, node) {
        this.systemType = node.item.title
    },
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
@import "vue-sidebar-menu/src/scss/vue-sidebar-menu.scss";
.page-layout {
  padding: 24px;
  display: flex;
  flex-flow: column;
  gap: 24px;
  max-width: 1872px;
  width: 100%;

  .row {
    width: 100%;
    justify-content: space-between;
    margin: unset;
  }
}
.page-content {
    padding: 24px;
    display: flex;
    flex-flow: column;
    gap: 0px;

    .page-row {
      display: flex;
      align-items: flex-end;
      justify-content: space-between;
      width: 100%;
    }

    .page-row-left {
      flex: 0 0 auto;
      display: flex;
      flex-flow: row;
      align-items: center;
      justify-content: flex-start;
      gap: 24px;
    }

    .page-row-right {
      flex: 0 0 auto;
      display: flex;
      flex-flow: row;
      align-items: flex-end;
      justify-content: flex-end;
      gap: 24px;
    }
  }

.v-sidebar-menu{
    position: relative;
    // width: 15rem;
    height: auto;
    width: 450px;
    line-height: 20px;
    margin-right: 10px;
    text-align: right;
}
.v-sidebar-menu.vsm_expanded{
  background-color: rgba(36, 36, 36, 0.397);
}

.v-sidebar-menu .vsm--toggle-btn {
  height: 25px;
  width: 25px;
}
</style>
