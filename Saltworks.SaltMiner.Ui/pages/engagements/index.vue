<template>
  <div class="page-layout">
    <div class="row">
      <HeadingText label="All Engagements" size="2" />
    </div>
    <SlideModal
      :label="`Engagement Import`"
      :open="toggleImport"
      :size="'small'"
      @toggle="() => (toggleImport = !toggleImport)"
    >        
      <p>
        Please select zipped engagement file to import.
        <br/><br/>
        Warning: importing an engagement that was exported earlier will overwrite the existing engagement.
        <br/><br/>
        Import files larger than {{user.maxImportFileSize}}, will be placed in the Job Queue and processed.
      </p>
      <div class="template-buttons">
        <span @click="downloadTemplate">Click here to download engagement import template</span>
      </div>
      <CheckboxBox
          label="Import as new Engagement"
          :checked="importNewEngagement"
          @check-clicked="importNewEngagement = !importNewEngagement"
        />
      <input
        ref="file"
        accept=".zip"
        type="file"
        @change="onImportChange"
      />         
      <div class="import-submit" @click="importEngagement">
        
        <ButtonComponent
          label="Import"
          :icon-only="false"
          theme="primary"
          size="medium"
        />
      </div>
    </SlideModal>
    <div class="button-row">
      <div class="button-left">
        <ButtonComponent
          :label="newEngagementButton.label"
          :icon="newEngagementButton.icon"
          :icon-position="newEngagementButton.iconPosition"
          :icon-only="newEngagementButton.iconOnly"
          :theme="newEngagementButton.theme"
          :size="newEngagementButton.size"
          :disabled="!this.isAdmin()"
          @button-clicked="handleNewEngagement"
        />
        <ButtonComponent
          :label="importEngagementButton.label"
          :icon="importEngagementButton.icon"
          :icon-position="importEngagementButton.iconPosition"
          :icon-only="importEngagementButton.iconOnly"
          :theme="importEngagementButton.theme"
          :size="importEngagementButton.size"
          :disabled="importEngagementButton.disabled"
          @button-clicked="handleSettingsToggle('import')"
        />
        <ButtonComponent
          :label="templateEngagementButton.label"
          :icon="templateEngagementButton.icon"
          :icon-position="templateEngagementButton.iconPosition"
          :icon-only="templateEngagementButton.iconOnly"
          :theme="templateEngagementButton.theme"
          :size="templateEngagementButton.size"
          :disabled="!this.isAdmin()"
          @button-clicked="naviagteToTemplate"
        />
      </div>
      <div class="button-right">
        <SlideModal
          :label="'Create New Engagement'"
          :open="toggleNewEngagement"
          :size="'small'"
          @toggle="() => (toggleNewEngagement = !toggleNewEngagement)"
        >
          <CreateEngagement
            :subtypes="subtypeDropdownOptions"
            @save-engagement="createNewEngagement"
            @close-slide-modal="() => (toggleNewEngagement = false)"
          />
        </SlideModal>
        <CheckboxBox
          label="Show Historical"
          :checked="showHistorical"
          @check-clicked="showHistoricalClicked()"
          :stacked=false
        />
        <SearchControl
          reff="txtEngagementSearch"
          :placeholder="searchControlPlaceholder"
          :facets="search.facets"
          :query="search.query"
          @input="handleSearchUpdate"
          @facet-click="handleFilterChange"
          @search="handleSearchControl"
        />
      </div>
    </div>
    <div class="col">
      <LoadingComponent :is-visible="isLoading" />
      <TableData
        :headers="formattedHeaders"
        :rows="formattedEngagements"
        :toggle-rows="false"
        :show-open-links="true"
        link-type="engagementDetails"
        :disable-hover="true"
        :check-status="true"
        :isEngagement="true"
        :current-filter="currentFilter.field"
        @header-click="handleHeaderClick"
        @row-click="handleRowClick"
      />

      <PaginationComponent
        :current-page="currentPage"
        :total-results="paginationTotalResults"
        @page-change="handlePageChange"
        @amount-change="handlePageChange"
        @pagination-mounted="handlePageChange"
      />
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
import isValidUser from '../../middleware/is-valid-user'
import IconSettings from '../../assets/svg/fi_settings.svg?inline'
import HeadingText from '../../components/HeadingText.vue'
import ButtonComponent from '../../components/controls/ButtonComponent'
import IconPlus from '../../assets/svg/fi_plus.svg?inline'
import IconList from '../../assets/svg/fi_paper.svg?inline'
import SearchControl from '../../components/controls/SearchControl.vue'
import PaginationComponent from '../../components/PaginationComponent.vue'
import TableData from '../../components/TableData.vue'
import helpers from '../../components/Utility/helpers.js'
import SlideModal from '../../components/controls/SlideModal.vue'
import CreateEngagement from '../../components/CreateEngagement.vue'
import AlertComponent from '../../components/AlertComponent'
import LoadingComponent from '../../components/LoadingComponent'
import CheckboxBox from '../../components/controls/CheckboxBox.vue'

export default {
  name: 'EngagementsIndex',
  components: {
    HeadingText,
    ButtonComponent,
    SearchControl,
    PaginationComponent,
    TableData,
    SlideModal,
    CheckboxBox,
    CreateEngagement,
    LoadingComponent,
    AlertComponent,
  },
  middleware: isValidUser,
  data() {
    return {
      sortFilters: {},
      paginationTotalResults: 0,
      engagements: [],
      showHistorical: false,
      engagementHeader: "Engagement",
      customerHeader: "Customer",
      createdHeader: 'Created Date',
      subtypeDropdownOptions: [],
      icons: {
        settings: IconSettings,
      },
      importNewEngagement: false,
      toggleSettingsOptions: false,
      toggleImport: false,
      newEngagementButton: {
        label: 'New Engagement',
        theme: 'primary',
        icon: IconPlus,
        iconPosition: 'left',
        iconOnly: false,
        disabled: false,
        size: 'medium',
        onClick: () => {
        },
        fullWidth: false,
      },
      importEngagementButton: {
        label: 'Import Engagement',
        theme: 'primary',
        icon: IconPlus,
        iconPosition: 'left',
        iconOnly: false,
        disabled: false,
        size: 'medium',
        onClick: () => {
        },
        fullWidth: false,
      },
      templateEngagementButton: {
        label: 'Template Issues',
        theme: 'primary',
        icon: IconList,
        iconPosition: 'left',
        iconOnly: false,
        disabled: false,
        size: 'medium',
        onClick: () => {
        },
        fullWidth: false,
      },
      search: {
        facets: [
          {
            display: 'All Fields',
            field: 'all',
          },
        ],
        query: '',
      },
      currentFilter: {
        display: 'All Fields',
        field: 'all',
      },
      tmp: {
        clickedHeader: {},
        clickedRow: {},
      },
      defaultFilter: {
        display: 'All Fields',
        field: 'all',
      },
      currentPerPage: 10,
      currentPage: 1,
      defaultDateFormat: 'en-US',
      toggleNewEngagement: false,
      searchControlPlaceholder: 'Search Engagements....',
      alert: {
          messages: [],
          type: "",
          title: ""
      },
      importFile: null,
      configFile: null
    }
  },
  async fetch({ store: { dispatch } }) {
    await dispatch('configInit')
  },
  head() {
    return {
      title: `All Engagements | ${this.pageTitle}`,
    }
  },
  computed: {
    ...mapState({
      dateFormat: (state) => state.modules.user.dateFormat,
      maxImportFileSize: (state) => state.modules.user.maxImportFileSize,
      user: (state) => state.modules.user,
      isLoading: (state) => state.loading.loading,
      pageTitle: (state) => state.config.pageTitle
    }),
    formattedFilters() {
      return this.search.facets.map((facet) => {
        return {
          display: facet.display,
          field: facet.field,
          sortable: true,
        }
      })
    },
    formattedHeaders() {
      return [
        {
          display: this.engagementHeader,
          field: 'name',
          sortable: true,
          hide: false,
          nowrap: false
        },
        {
          display: this.customerHeader,
          field: 'customer',
          sortable: true,
          hide: false
        },
        {
          display: this.createdHeader,
          field: 'timestamp',
          sortable: true,
          hide: false
        },
        {
          display: 'Status',
          field: 'status',
          sortable: true,
          hide: false
        },
        {
          display: 'Critical',
          field: 'issueCount.critical',
          sortable: false,
          hide: false
        },
        {
          display: 'High',
          field: 'issueCount.high',
          sortable: false,
          hide: false
        },
        {
          display: 'Medium',
          field: 'issueCount.medium',
          sortable: false,
          hide: false
        },
        {
          display: 'Low',
          field: 'issueCount.low',
          sortable: false,
          hide: false
        },
        {
          display: 'Info',
          field: 'issueCount.info',
          sortable: false,
          hide: false,
        }
      ]
    }, 
    formattedEngagements() {
      return this.engagements.map((engagement) => {
        return {
          ...engagement,
          status: engagement.status,
          name: this.getEngagementName(engagement),
          timestamp: helpers.formatDate(engagement.timestamp),
        }
      })
    }
  },
  async mounted() {
    this.configFile = await fetch('/smpgui/config.json').then((res) => res.json())
    this.getPrimer()
  },
  methods: {
   handleAlertClose() {
      this.alert = {
        messages: [],
        type: "",
        title: ""
      }
    },
    downloadTemplate() {
      this.$axios
        .$get(`${this.$store.state.config.api_url}/engagement/import/template`, {
          headers: {
            accept: '*/*',
          },
          responseType: 'blob',
        })
        .then((response) => {
          const url = window.URL.createObjectURL(new Blob([response]))
          const link = document.createElement('a')
          link.href = url
          link.setAttribute('download', 'engagement_import_template.json')
          document.body.appendChild(link)
          link.click()
        })
    },
    showHistoricalClicked() {
      this.showHistorical = !this.showHistorical;
      this.handleSearch();
    },
    isAdmin() {
      // allows superuser or pentest admin roles
      if (this.configFile !== null) {
        return (this.user.roles.includes(this.configFile.pentest_admin_role) ||
          this.user.roles.includes(this.configFile.admin_role))
      }
    },
    onImportChange() {
      this.importFile = this.$refs.file.files[0];
    },
    importEngagement() {
      const formData = new FormData()

      formData.append('file', this.importFile)

      const tmpname = this.importFile?.name ?? null

      if (tmpname === null) return
      
      let url = `${this.$store.state.config.api_url}/engagement/import`;
      if(this.importNewEngagement){
        url = url + "/new"
      }
      
      this.$axios
        .$post(url, formData, {
          headers: {
            'Content-Type': 'multipart/form-data',
            'accept': 'application/json'
          },
      })
      .then((r) => {
        this.toggleImport = false;
        if (r.data === "0") {
          this.addAlert(["The engagement import has been placed into queue for processing."], "success", "In Queue")
        }
        else {
          this.$router.push({
          path: `/engagements/${r.data}`,
        })
        }
      })
      .catch((error) => {
        this.handleErrorResponse(error, "Error Importing Issues")
      })
    },
    handleSettingsToggle(opt = null) {
      if (opt === 'import') {
        this.toggleImport = true
      } 

      this.toggleSettingsOptions = false
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
    },
    handleNewEngagement() {
      this.toggleNewEngagement = !this.toggleNewEngagement
    },
    naviagteToTemplate() {
      return this.$axios
        .$get(`${this.$store.state.config.api_url}/Engagement/template/primer`, {
          headers: {
            'Content-Type': 'application/json',
          }
        })
        .then((r) => {
          this.$router.push({
            path: `/engagements/${r.data.id}`,
          })
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Get Template")
        })
    },
    createNewEngagement(data) {

      if (data.name.trim() === '' || data.summary.trim() === '' || data.customer.trim() === '') {
        this.addAlert([`Must enter values for all fields before creating engagement`], "danger", "Create new engagement error")
        return;
      }
      
      return this.$axios
        .$post(`${this.$store.state.config.api_url}/Engagement/create`, JSON.stringify(data), {
          headers: {
            'Content-Type': 'application/json',
          }
        })
        .then((r) => {
          this.$router.push({
            path: `/engagements/${r.data.id}`,
          })
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Creating New Engagement")
        })
    },
    findFacet(field) {
      return (
        this.search.facets.find((facet) => {
          return facet.field.toLowerCase() === field.toLowerCase()
        }) || this.defaultFilter
      )
    },
    handleSearchUpdate(query) {
      this.search.query = query
    },
    handleSearchControl(data) {
      this.search.query = data.query
      this.currentFilter = data.facet
      this.handleSearch(1, this.currentPerPage)
    },
    handleFilterChange(newFilter) {
      this.currentFilter =
        this.currentFilter.field.toLowerCase() === newFilter.field.toLowerCase()
          ? this.defaultFilter
          : newFilter
    },
    handleHeaderClick(header) {
      const field = header.field === 'timestamp' ? 'date' : header.field

      if (field in this.sortFilters) {
        this.sortFilters[field] = !this.sortFilters[field]
      } else {
        this.sortFilters = {}
        this.sortFilters[field] = true
      }
      this.tmp.clickedHeader = header
      this.handleSearch(1, this.currentPerPage)
    },
    handleRowClick(row) {
      this.tmp.clickedRow = row
      this.$router.push({
        path: `/engagements/${row.id}`,
      })
    },
    handlePageChange(data) {
      this.currentPerPage = data.size
      this.handleSearch(data.page, data.size)
    },
    handleSearch(page, size) {
      this.currentPage = page
      const searchFilter = {
        field: this.currentFilter.field.toLowerCase(),
        value: this.search.query,
      }

      const body = {
        searchFilters: [searchFilter],
        showHistorical: this.showHistorical,
        pager: {
          size,
          page,
          sortFilters: this.sortFilters,
        },
      }
      return this.$axios
        .$post(`${this.$store.state.config.api_url}/Engagement/search`, JSON.stringify(body), {
          headers: {
            'Content-Type': 'application/json',
            accept: 'application/json',
          }
        })
        .then((r) => {
          this.paginationTotalResults = r?.pager?.total || 0
          this.engagements = r.data || []
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Searching Engagements")
        })
    },
    getPrimer() {
      return this.$axios
        .$get(`${this.$store.state.config.api_url}/Engagement/primer`)
        .then((r) => {
          this.subtypeDropdownOptions = this.subtypeDropdownOptions.concat(r?.data?.subtypeDropdowns || [])
          this.search.facets = r.data.searchFilters.map((facet) => {
            return {
              display: facet.value,
              field: facet.field.toLowerCase(),
            }
          })
          this.engagementHeader = r.data.engagementHeader
          this.customerHeader = r.data.customerHeader
          this.createdHeader = r.data.createdHeader
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Getting Engagements Primer")
        })
    },
    getEngagementName(engagement) {
      return engagement.name || 'Anonymous Engagement'
    },
  },
}
</script>

<style lang="scss" scoped>
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

.template-buttons {
  font-family: $font-primary;
  display: flex;
  gap: 40px;
  justify-content: center;
  font-style: normal;
  font-weight: 700;
  font-size: 14px;
  line-height: 18px;
  letter-spacing: -0.25px;
  color: $brand-primary-color;
  margin-top: 24px;
  cursor: pointer;
}
.template-buttons a:hover {
  color: $button-hover-color;
}

.button-row {
  display: flex;
  justify-content: space-between;
  padding: 0 0 0 0;
  gap: 20px;

  .button-left {
    display: flex;
    align-items: center;
    justify-content: flex-start;
    gap: 24px;
  }
  .button-right {
    display: flex;
    align-items: center;
    justify-content: flex-end;
    gap: 24px;
  }
}

.settings-wrap {
  position: relative;
  .settings-options {
    position: absolute;
    width: 220px;
    text-align: center;
    top: 100%;
    left: 50%;
    transform: translateX(-50%);
    background-color: $brand-white;
    box-shadow: 0px 0px 48px rgba(0, 0, 0, 0.05);
    border-radius: 8px;
    border: 1px solid $brand-color-scale-2;
    padding: 8px;
    z-index: 2;
    display: flex;
    flex-flow: column;

    .settings-option {
      cursor: pointer;
      width: 100%;
      padding: 8px;
      border-radius: 8px;

      &:hover {
        background-color: $brand-color-scale-1;
      }
    }
  }
}
</style>
