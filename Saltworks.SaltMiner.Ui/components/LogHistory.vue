<template>
  <div class="log-history__wrapper">
    <TableData
      :headers="headers"
      :disable-hover="true"
      :rows="logs"
      :row-size="'medium'"
      :toggle-rows="false"
      :sortable="false"
      :current-filter="currentFilter"
      @header-click="handleHeaderClick"
      @row-click="handleRowClick"
    />

    <PaginationComponent
      :current-page="currentPage"
      :total-results="paginationTotalResults"
      :current-per-page="currentPerPage"
      @page-change="handlePageChange"
      @amount-change="handlePageChange"
      @pagination-mounted="handlePageChange"
    />
    <AlertComponent
      v-if="alert.messages.length > 0"
      class="alert-margin"
      :alert="alert"
      @close="handleAlertClose"
    />
  </div>
</template>

<script>
import helpers from './Utility/helpers.js'
import TableData from './TableData.vue'
import AlertComponent from './AlertComponent'

export default {
  components: {
    TableData,
    AlertComponent,
  },
  props: {
    engagementId: {
      type: String,
      default: '',
    },
    scanId: {
      type: String,
      default: '',
    },
    assetId: {
      type: String,
      default: '',
    },
    issueId: {
      type: String,
      default: '',
    },
    dateFormat: {
      type: String,
      default: 'en-US',
    },
    config: {
      type: Object,
      default: () => ({}),
    },
  },
  data() {
    return {
      currentFilter: '',
      currentPage: 1,
      oddRow: 'table-row odd-row',
      normalRow: 'table-row',
      logEntries: [],
      paginationTotalResults: 0,
      currentPerPage: 25,
      headers: [
        {
          display: 'Date',
          field: 'timestamp',
          sortable: true,
          hide: false,
        },
        {
          display: 'Type',
          field: 'type',
          sortable: true,
          hide: false,
        },
        {
          display: 'Comment',
          field: 'message',
          sortable: true,
          hide: false,
        },
        {
          display: 'User',
          field: 'user',
          sortable: true,
          hide: false,
        },
      ],
      alert: {
        messages: [],
        type: "",
        title: ""
      },
    }
  },
  computed: {
    logs() {
      return this.logEntries.map((entry) => {
        return {
          timestamp: helpers.formatDate(entry.added),
          type: entry.type,
          message: entry.message,
          user: entry.user,
        }
      })
    },
  },
  mounted() {
    this.getLogs(1, 25)
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
    },
    handleHeaderClick(header) {
      this.currentFilter = header.field
      this.getLogs()
    },
    handlePageChange(data) {
      this.currentPerPage = data.size
      this.getLogs(data.page, data.size)
    },
    handleRowClick(row) {
      return row
    },
    formatDate(timestamp) {
      return helpers.formatDate(timestamp)
    },
    getLogs(page, size) {
      this.currentPage = page

      const filterData = {}

      if (this.currentFilter !== '') filterData[this.currentFilter] = true

      const data = {
        engagementId: this.engagementId,
        issueId: this.issueId || null,
        scanId: this.scanId || null,
        assetId: this.assetId || null,
        pager: {
          size,
          page,
          sortFilters: filterData,
        },
      }

      return this.$axios
        .$post(`${this.$store.state.config.api_url}/Comment/Search`, JSON.stringify(data), {
          headers: {
            'Content-Type': 'application/json'
          }
        })
        .then((r) => {
          this.logEntries = r.data || []
          this.paginationTotalResults = r.pager.total
          if (typeof window !== 'undefined') {
            window.scrollTo({
              top: 0,
              behavior: 'smooth',
            })
          }
        })
        .catch((error) => {
          return this.handleErrorResponse(error, "Error Searching Comment")
        })
    },
  },
}
</script>

<style lang="scss" scoped>
.log-history__wrapper::v-deep {
  position: relative;
  display: flex;
  box-sizing: border-box;
  flex-direction: column;
  justify-content: flex-start;
  width: 100%;
  height: 100%;
  max-height: 100%;
  flex: 1;
  overflow: auto;

  font-family: $font-primary;
  table {
    flex: 0 0 auto;
    width: 100%;

    thead {
      box-shadow: inset 0px -1px 0px rgba(0, 0, 0, 0.12);
    }

    tbody {
      width: 100%;
      overflow-y: auto;

      tr {
        max-height: 60px;
        td {
          padding: 14.5px 16px 14.5px 16px;

          &:first-child {
            font-weight: inherit;
          }
        }
      }
    }
  }
}
</style>
