<template>
  <div class="service_job__wrapper">
  <FormLabel label="Service Jobs"></FormLabel>
    <div class="button-row">
      <div class="button-left">
        <h1>This page can be used to add, edit, or delete schedules to run other processes</h1>
      </div>
      <div class="button-right">
        <SearchControl
          reff="txtSearchIssues"
          :facets="search.facets"
          :query="search.query"
          @input="handleSearchUpdate"
          @facet-click="handleFilterChange"
          @search="handleSearchControl"
        />
      </div>
    </div>
    <TableData
      :headers="tableHeaders"
      :disable-hover="true"
      :rows="formattedServiceJobs"
      row-size="medium"
      :toggle-rows="true"
      :sortable="true"
      @row-click="handleRowClick"
      @checked-rows-changed="handleCheckedRowsChanged"
    />
    <PaginationComponent
      :current-page="currentPage"
      :total-results="paginationTotalResults"
      @page-change="handlePageChange"
      @amount-change="handlePageChange"
      @pagination-mounted="handlePageChange"
    />
    <div class="extraRoom">
      <ButtonComponent
        label="Add"
        theme="primary"
        size="medium"
        @button-clicked="handleAdd"
      />
      <ButtonComponent
        label="Delete"
        theme="danger"
        size="medium"
        :disabled="checkedRows.length < 1"
        @button-clicked="handleDeleteChecked"
      />
    </div>
    <SlideModal
      label="Add Service Job"
      :open="toggleAdd"
      size="medium"
      @toggle="() => (toggleAdd = !toggleAdd)"
    >
      <div id="job-name" class="input-flex">
        <FormLabel class="input-label" label="Name" />
        <InputText
          reff="txtJobName"
          class="text-field_asset btm-input"
          placeholder="Enter Name"
          :value="selectedJob && selectedJob.name || ''"
          @update="(val) => handleInput('name', val)"
        />
      </div>

      <div id="job-desc" class="input-flex">
        <FormLabel class="input-label" label="Description" />
        <InputText
          reff="txtJobDescription"
          class="text-field_asset btm-input"
          placeholder="Enter Description (optional)"
          :value="selectedJob && selectedJob.description || ''"
          @update="(val) => handleInput('description', val)"
        />
      </div>

      <DropdownControl
        theme="outline"
        label="Option"
        reff="txtJobOption"
        :options="optionsDropdown"
        :value="selectedJob && selectedJob.option || ''"
        @update="(val) => handleInput('option', val.value)"
      />

      <div id="job-parameters" class="input-flex">
        <FormLabel class="input-label" label="Parameters" />
        <InputText
          reff="txtJobParameters"
          class="text-field_asset btm-input"
          placeholder="Enter Parameters (optional)"
          :value="selectedJob && selectedJob.parameters || ''"
          @update="(val) => handleInput('parameters', val)"
        />
      </div>

      <DropdownControl
        theme="outline"
        label="Type"
        reff="txtJobType"
        :options="formattedJobTypeDropdownOptions"
        @update="handleJobTypeUpdate"
      />

      <div>
        <FormLabel class="input-label" label="Schedule" />
        <CronEditorComponent v-model="cronExpression"/>
      </div>

      <div class="extraRoom">
        <ButtonComponent
          label="Save"
          theme="primary"
          :disabled="false"
          size="medium"
          @button-clicked="handleNewSave"
        />
      </div>
    </SlideModal>
    
    <SlideModal
      label="Edit Service Job"
      :open="toggleEdit"
      size="medium"
      @toggle="() => (toggleEdit = !toggleEdit)"
    >   
      <div id="job-name" class="input-flex">
        <FormLabel class="input-label" label="Name" />
        <InputText
          reff="txtJobName"
          class="text-field_asset btm-input"
          placeholder="Enter Name"
          :value="selectedJob && selectedJob.name || ''"
          @update="(val) => handleInput('name', val)"
        />
      </div>

      <div id="job-desc" class="input-flex">
        <FormLabel class="input-label" label="Description" />
        <InputText
          reff="txtJobDescription"
          class="text-field_asset btm-input"
          placeholder="Enter Description (optional)"
          :value="selectedJob && selectedJob.description || ''"
          @update="(val) => handleInput('description', val)"
        />
      </div>

      <DropdownControl
        theme="outline"
        label="Option"
        reff="txtJobOption"
        :options="optionsDropdown"
        :value="selectedJob && selectedJob.option || ''"
        @update="(val) => handleInput('option', val.value)"
      />

      <div id="job-parameters" class="input-flex">
        <FormLabel class="input-label" label="Parameters" />
        <InputText
          reff="txtJobParameters"
          class="text-field_asset btm-input"
          placeholder="Enter Parameters (optional)"
          :value="selectedJob && selectedJob.parameters || ''"
          @update="(val) => handleInput('parameters', val)"
        />
      </div>

      <DropdownControl
        theme="outline"
        label="Type"
        reff="txtJobType"
        :options="formattedJobTypeDropdownOptions"
        @update="handleJobTypeUpdate"
      />
     
      <div>
        <FormLabel class="input-label" label="Schedule" />
        <CronEditorComponent v-model="cronExpression"/>
      </div>

      <div style="color:red">{{ selectedJob?.message }}</div>

        <div class="extraRoom">
          <ButtonComponent
            label="Save"
            theme="primary"
            size="medium"
            @button-clicked="handleEditSave"
          />
          <div v-show="selectedJob != null && selectedJob.disabled === false">
            <ButtonComponent
              label="Disable"
              theme="danger"
              size="medium"
              @button-clicked="handleDisable"
            />
          </div>
          <div v-show="selectedJob != null && selectedJob.disabled === true">
            <ButtonComponent
              label="Enable"
              theme="primary"
              size="medium"
              @button-clicked="handleEnable"
            />
          </div>
          <div>
            <ButtonComponent
              label="Run"
              theme="primary"
              size="medium"
              :disabled="selectedJob != null && selectedJob.disabled === true"
              @button-clicked="handleRunNow"
            />
          </div>
          <div>
            <ButtonComponent
              label="Cancel Job"
              theme="primary"
              size="medium"
              @button-clicked="handleCancelJob"
            />
          </div>
          <ConfirmDialog ref="confirmDialog" />
      </div>

    </SlideModal>
    <AlertComponent
      v-if="alert.messages.length > 0"
      class="alert-margin"
      :alert = alert
      @close="handleAlertClose"
    />
</div>
</template>

<script>
import { mapState } from 'vuex'
import ButtonComponent from './../controls/ButtonComponent'
import SlideModal from './../controls/SlideModal.vue'
import TableData from './../TableData.vue'
import AlertComponent from './../AlertComponent'
import FormLabel from './../controls/FormLabel.vue'
import InputText from './../../components/controls/InputText'
import helpers from './../../components/Utility/helpers.js'
import PaginationComponent from './../../components/PaginationComponent.vue'
import CronEditorComponent from './../../components/CronEditorComponent.vue'
import DropdownControl from './../../components/controls/DropdownControl.vue'
import SearchControl from './../../components/controls/SearchControl.vue'
import ConfirmDialog from './../../components/ConfirmDialog.vue';


export default {
  components: {
    ButtonComponent,
    TableData,
    SlideModal,
    AlertComponent,
    FormLabel,
    InputText,
    PaginationComponent,
    CronEditorComponent,
    DropdownControl,
    SearchControl,
    ConfirmDialog
},
  props: {
  },
  data() {
    return {
      servicejob: [],
      selectedJob: {},
      selectedJobId: '',
      jobFields: ["name", "description", "type", "option", "parameters", "disabled", "id"],
      editServiceJob: {},
      toggleAdd: false,
      toggleEdit: false,
      newField: '',
      newHidden: false,
      newDefault: '',
      checkedRows: [],
      optionsDropdown: [],
      alert: {
        messages: [],
        type: "",
        title: ""
      },
      defaultDateFormat: 'en-US',
      defaultFilter: {
        display: 'All Fields',
        field: 'all',
      },
      currentFilter: {
        display: 'All Fields',
        field: 'all',
      },
      search: {
        facets: [
          {
            label: 'All Fields',
            value: 'all',
          },
        ],
        query: '',
      },
      paginationTotalResults: 0,
      currentPerPage: 10,
      currentPage: 1,
      cronExpression: '',
      defaultCronExpression: '0 0/1 * * * ? *'
    }
  },
  computed: {
    ...mapState({
      dateFormat: (state) => state.modules.user.dateFormat,
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
    tableHeaders() {
      return [
        {
          display: 'Name',
          field: 'name',
          sortable: false,
          hide: false,
          nowrap: false
        },
        {
          display: 'Option',
          field: 'option',
          sortable: false,
          hide: false,
          nowrap: false
        },
        {
          display: 'Parameters',
          field: 'parameters',
          sortable: false,
          hide: false,
          nowrap: true
        },
        {
          display: 'Last run-time',
          field: 'lastRunTime',
          sortable: false,
          hide: false,
          nowrap: true
        },
        {
          display: 'Next run-time',
          field: 'nextRunTime',
          sortable: false,
          hide: false,
          nowrap: true
        },
        {
          display: 'Status',
          field: 'status',
          sortable: false,
          hide: false,
          nowrap: true
        },
        {
          display: '',
          field: 'disabled',
          sortable: false,
          hide: false
        }
      ]
    },
    formattedServiceJobs() {
      return this.servicejob.map((job) => {
        if (job.disabled) job.nextRunTime = null;
        const nextRunTime = job.schedule ? 'Unknown' : 'On Demand'
        return {
          ...job,
          nextRunTime: helpers.formatDate(job.nextRunTime, 'M/D/yyyy h:mm:ss a', nextRunTime),
          lastRunTime: helpers.formatDate(job.lastRunTime, 'M/D/yyyy h:mm:ss a', 'Unknown')
        }
      })
    },
    formattedJobTypeDropdownOptions() {
      const defaultOption = {
        display: 'Command',
        value: 'Command',
        selected: true,
        order: 0
      }

      const result = [
        defaultOption
      ]

      // const result = [
      //   defaultOption,
      //   ...(this.stateDropdownOptions.map((option) => {
      //     return {
      //       display: option.display,
      //       value: option.value,
      //       selected: option.value.toLowerCase() === 'isactive',
      //     }
      //   })
      //   .sort((a, b) => {
      //     return a.order - b.order
      //   })
      //    || []),
      // ]

      return result
    }
  },
  async mounted() {
    await this.getServiceJobsPrimer()
  },
  methods: { 
    handleDisable() {
      this.selectedJob.disabled = true;
    },
    handleEnable() {
      this.selectedJob.disabled = false;
    },
    handleRunNow() {
      this.selectedJob.runNow = true;
      this.handleEditSave();
    },
    async handleCancelJob() {
      const confirm = await this.$refs.confirmDialog.show("This will stop the job if it is running. Do you want to cancel?", "Confirm Cancel");
      if (confirm) {
        this.selectedJob.cancel = true;
        this.handleEditSave();
      }
    },
    handleInput(field, value) {
      this.selectedJob[field] = value
    },
    handleCheckedRowsChanged(checkedRows) {
      this.checkedRows = checkedRows
    },
    handleFilterChange(newFilter) {
      this.currentFilter =
        this.currentFilter.field.toLowerCase() === newFilter.field.toLowerCase()
          ? this.defaultFilter
          : newFilter
    },
    handleRowClick(row) {
      const formattedJob = {};
      this.selectedJob = JSON.parse(JSON.stringify(row));
      for (const job of this.jobFields) {
        if (job in this.selectedJob) {
          formattedJob[job] = this.selectedJob[job]
        }
      }
      this.selectedJob = formattedJob;
      this.selectedJob.message = row.message;
      this.cronExpression = row.schedule;
      this.toggleEdit = true;
    },
    getServiceJobsPrimer() {
      return this.$axios
        .$get(`${this.$store.state.config.api_url}/admin/servicejob/primer`)
        .then((r) => {
          this.search.facets =
            r.data?.searchFilters?.map((facet) => {
              return {
                display: facet.value,
                field: facet.field.toLowerCase(),
              }
            }) || []

          this.optionsDropdown =
            r.data?.serviceJobCommandDropdowns
              .map((option) => {
                return {
                  ...option,
                  display: option.display,
                  value: option.value
                  // selected: option.value === r.data?.issue.assetId
                }
              })
              .sort((a, b) => {
                return ((a, b) => (a.order || 0) - (b.order || 0))
              }) || []
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Getting Service Jobs")
        })
    },
    handleAlertClose() {
      this.alert = {
        messages: [],
        type: "",
        title: ""
      }
    },
    validateJob() {
      if (this.selectedJob && this.selectedJob.name === "") {
        this.addAlert(["The Name field is required to save"], "danger", "Required");
        return false;
      }
      if (this.selectedJob && this.selectedJob.option === "") {
        this.addAlert(["The Option field is required to save"], "danger", "Required");
        return false;
      }

      return true;
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
    handleFieldUpdate(field) {
      this.newField = field.value
    },
    handleNewSave() {
      if (!this.validateJob()) {
        return;
      }

      this.selectedJob.schedule = this.cronExpression ?? '';

      return this.$axios
        .$post(`${this.$store.state.config.api_url}/admin/servicejob`, JSON.stringify(this.selectedJob), {
          headers: {
            'Content-Type': 'application/json',
          }
        })
        .then((r) => {
          this.toggleAdd = false;
          this.addAlert(["Success Adding Service Job"], "success", "Success")
          this.servicejob.push(r.data);
          this.paginationTotalResults = this.servicejob.length
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Adding Service Job")
        })
    },
    handleEditSave() {
      if (!this.validateJob()) {
        return;
      }
      this.selectedJob.lastRunTime = this.servicejob.find(item => item.id === this.selectedJob.id).lastRunTime;
      this.selectedJob.schedule = this.cronExpression ?? '';
      return this.$axios
        .$post(`${this.$store.state.config.api_url}/admin/servicejob`, JSON.stringify(this.selectedJob), {
          headers: {
            'Content-Type': 'application/json',
          }
        })
        .then((r) => {
          this.toggleEdit = false;
          this.addAlert(["Success Editing Service Job"], "success", "Success")
          const itemIndex = this.servicejob.findIndex(item => item.id === this.selectedJob.id);
  
          if (itemIndex !== -1) {
            this.servicejob.splice(itemIndex, 1, this.selectedJob);
          }
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Editing Service Job")
        })
    },
    handleDeleteChecked() {
      const ids = this.checkedRows.map((row) => row.id)

      if (ids?.length < 1) return

      const data = {
        ids,
      }

      return this.$axios
        .$post(`${this.$store.state.config.api_url}/admin/servicejob/delete`, JSON.stringify(data), {
          headers: {
            'Content-Type': 'application/json',
          }
        })
        .then((r) => {
          this.servicejob = this.servicejob.filter(item => !ids.includes(item.id))
          this.addAlert(["Success Removed Service Job(s)"], "success", "Success")
          this.selectedJob = r?.data
          this.checkedRows = []
          this.paginationTotalResults = this.servicejob.length
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Removing Service Job(s)")
        })
    },
    initNewJob() {
      this.selectedJob = {
        "name": "",
        "description": "",
        "type": "Command", // only have the one type so hard-code for now
        "option": this.optionsDropdown[0].value,
        "parameters": "",
        "runNow": false,
        "disabled": false,
        "cancel": false,

        "message": "",
        "status": ""
      }
    },
    handleAdd() {
      this.cronExpression = this.defaultCronExpression
      this.newField = ''
      this.newHidden = false
      this.newDefault = ''
      this.toggleAdd = !this.toggleAdd
      this.initNewJob()
    },
    handleSearchUpdate(query) {
      this.search.query = query
    },
    handleSearchControl(data) {
      this.search.query = data.query
      this.currentFilter = data.facet
      this.handleSearch(1, this.currentPerPage)
    },
    handlePageChange(data) {
      this.currentPerPage = data.size
      this.handleSearch(data.page, data.size)
    },
    handleJobTypeUpdate(option) {
      this.selectedJob.type = option.value;
    },
    handleSearch(page, size) {
      this.currentPage = page
      this.currentPerPage = size

      const searchFilter = {
        field: this.currentFilter.field.toLowerCase(),
        value: this.search.query,
      }

      const body = {
        searchFilters: [searchFilter],
        pager: {
          size,
          page
        },
      }

      return this.$axios
        .$post(`${this.$store.state.config.api_url}/admin/servicejob/search`, JSON.stringify(body), {
          headers: {
            'Content-Type': 'application/json',
            accept: 'application/json',
          }
        })
        .then((r) => {
          this.paginationTotalResults = r?.pager?.total || 0
          this.servicejob = r.data || []
        })
        .catch((e) => {
          this.handleErrorResponse(e, "Error searching service jobs")
          return e
        })
    }
  }
}
</script>

<style lang="scss" scoped>
.new-definition__buttons {
  margin-top: 30px;
  display: flex;
  gap: 20px;
}

.extraRoom {
  margin-top: 30px;
  display: flex;
  gap: 20px;
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

</style>
