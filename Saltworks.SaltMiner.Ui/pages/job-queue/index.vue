<template>
  <div class="page-layout">
    <div class="row">
      <HeadingText label="Job Queue" size="2" />
    </div>
    <div class="col">
      <LoadingComponent :is-visible="isLoading" />
      <TabsComponent>
        <TabComponent :name='pendingTabText' :selected="true">
          <TableData
            :headers="formattedHeaders"
            :rows="nonFinishedFormattedJobs"
            :toggle-rows="false"
            :disable-hover="true"
            :check-status="false"
            :sortable="false"
          />
          <PaginationComponent
            :current-page="nonFinishedCurrentPage"
            :total-results="nonFinishedPaginationTotalResults"
            @page-change="handleNonFinishedPageChange"
            @amount-change="handleNonFinishedPageChange"
            @pagination-mounted="handleNonFinishedPageChange"
          />
        </TabComponent>
        <TabComponent :name='completedTabText'>
          <TableData
            :headers="formattedHeaders"
            :rows="finishedFormattedJobs"
            :toggle-rows="false"
            :disable-hover="true"
            :sortable="false"
            :check-status="false"
          />
          <PaginationComponent
            :current-page="finishedCurrentPage"
            :total-results="finishedPaginationTotalResults"
            @page-change="handleFinishedPageChange"
            @amount-change="handleFinishedPageChange"
            @pagination-mounted="handleFinishedPageChange"
          />
        </TabComponent>
      </TabsComponent>     
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
import HeadingText from '../../components/HeadingText.vue'
import PaginationComponent from '../../components/PaginationComponent.vue'
import TableData from '../../components/TableData.vue'
import AlertComponent from '../../components/AlertComponent'
import LoadingComponent from '../../components/LoadingComponent'
import TabsComponent from '../../components/TabsComponent.vue'
import helpers from '../../components/Utility/helpers.js'
import TabComponent from '../../components/TabComponent.vue'

export default {
  name: 'EngagementsIndex',
  components: {
    HeadingText,
    PaginationComponent,
    TableData,
    LoadingComponent,
    AlertComponent,
    TabsComponent,
    TabComponent
},
  middleware: isValidUser,
  data() {
    return {
      heartbeat: null,
      finishedPaginationTotalResults: 0,
      finishedCurrentPerPage: 10,
      finishedCurrentPage: 1,
      nonFinishedPaginationTotalResults: 0,
      nonFinishedCurrentPerPage: 10,
      nonFinishedCurrentPage: 1,
      nonFinishedJobs: [],
      finishedJobs: [],
      defaultDateFormat: 'en-US',
      alert: {
          messages: [],
          type: "",
          title: ""
      },
    }
  },
  async fetch({ store: { dispatch } }) {
    await dispatch('configInit')
  },
  head() {
    return {
      title: `Job Queue | ${this.pageTitle}`,
    }
  },
  computed: {
    ...mapState({
      dateFormat: (state) => state.modules.user.dateFormat,
      user: (state) => state.modules.user,
      isLoading: (state) => state.loading.loading,
      pageTitle: (state) => state.config.pageTitle
    }),
    pendingTabText() {
      return "Pending (" + this.nonFinishedPaginationTotalResults + ")"
    },
    completedTabText() {
      return "Completed (" + this.finishedPaginationTotalResults + ")"
    },
    formattedHeaders() {
      return [
        {
          display: "Request By",
          field: 'userFullName',
          sortable: true,
          hide: false
        },
        {
          display: "Requested On",
          field: 'timestamp',
          sortable: true,
          hide: false,
          nowrap: true
        },
        {
          display: "Type",
          field: 'formatedType',
          sortable: true,
          hide: false,
          nowrap: true
        },
        {
          display: "Status",
          field: 'status',
          sortable: true,
          hide: false,
          nowrap: true
        },
        {
          display: "Target Id",
          field: 'targetId',
          sortable: true,
          hide: false,
          nowrap: true
        },
        {
          display: 'Message',
          field: 'message',
          sortable: true,
          hide: false
        }
      ]
    },
    finishedFormattedJobs() {
      return this.finishedJobs.map((job) => {
        return {
          ...job,
          formatedType: this.formatType(job.type),
          timestamp: helpers.formatDate(job.timestamp, 'M/D/yyyy h:mm a')
        }
      })
    },
    nonFinishedFormattedJobs() {
      return this.nonFinishedJobs.map((job) => {
        return {
          ...job,
          formatedType: this.formatType(job.type),
          timestamp: helpers.formatDate(job.timestamp, 'M/D/yyyy h:mm a')
        }
      })
    }
  },
  mounted() {
    this.handleHeartbeat()

    if (this.heartbeat) {
      clearInterval(this.heartbeat)
    }

    this.heartbeat = setInterval(() => {
      if(window.location.href.includes("job"))
      {
        this.handleHeartbeat()
      } else {
        clearInterval(this.heartbeat)
      }
    }, 30000) 
  },
  methods: {
    handleHeartbeat() {
      this.handleFinishedSearch(this.finishedCurrentPage, this.finishedCurrentPerPage);
      this.handleNonFinishedSearch(this.nonFinishedCurrentPage, this.nonFinishedCurrentPerPage);
    },
    handleAlertClose() {
      this.alert = {
        messages: [],
        type: "",
        title: ""
      }
    },
    formatType(type){
      const result = type.replace(/([A-Z])/g, " $1");
      return result.charAt(0).toUpperCase() + result.slice(1)
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
    handleNonFinishedPageChange(data) {
      this.handleNonFinishedSearch(data.page, data.size)
    },
    handleNonFinishedSearch(page, size) {
      this.nonFinishedCurrentPage = page
      this.nonFinishedCurrentPerPage = size

      const body = {
        pager: {
          size,
          page
        },
      }
      return this.$axios
        .$post(`${this.$store.state.config.api_url}/job/non-finished`, JSON.stringify(body), {
          headers: {
            'Content-Type': 'application/json',
            accept: 'application/json',
          }
        })
        .then((r) => {
          this.nonFinishedPaginationTotalResults = r?.pager?.total || 0
          this.nonFinishedJobs = r.data || []
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Searching Non-Finished Jobs")
        })
    },
    handleFinishedPageChange(data) {
      this.handleFinishedSearch(data.page, data.size)
    },
    handleFinishedSearch(page, size) {
      this.finishedCurrentPage = page
      this.finishedCurrentPerPage = size

      const body = {
        pager: {
          size,
          page
        },
      }
      return this.$axios
        .$post(`${this.$store.state.config.api_url}/job/finished`, JSON.stringify(body), {
          headers: {
            'Content-Type': 'application/json',
            accept: 'application/json',
          }
        })
        .then((r) => {
          this.finishedPaginationTotalResults = r?.pager?.total || 0
          this.finishedJobs = r.data || []
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Searching Finished Jobs")
        })
    },
    getPrimer() {
      this.handleFinishedSearch(1, 10);
      this.handleNonFinishedSearch(1, 10);
    }
  }
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
</style>
