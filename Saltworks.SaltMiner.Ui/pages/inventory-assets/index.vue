<template>
  <div class="page-layout">
    <div class="row">
      <HeadingText label="Inventory Assets" size="2" />
    </div>
    <div class="button-row">
      <div class="button-left">
      <ButtonComponent
          :label="newInventoryAssetButton.label"
          :icon="newInventoryAssetButton.icon"
          :icon-position="newInventoryAssetButton.iconPosition"
          :icon-only="newInventoryAssetButton.iconOnly"
          :theme="newInventoryAssetButton.theme"
          :size="newInventoryAssetButton.size"
          :disabled="newInventoryAssetButton.disabled"
          @button-clicked="handleNewInventoryAsset"
        />
        <ButtonComponent
          :label="'Delete'"
          :disabled="checkedRows.length < 1"
          theme="danger"
          size="medium"
          @button-clicked="handleAssetDelete"
        />
      </div>
      <div class="button-right">
        <SearchControl
          reff="txtInventoryAssetSearch"
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
        :headers="tableHeaders"
        :rows="formattedInvetoryAssets"
        :toggle-rows="true"
        :disable-hover="true"
        :current-filter="currentFilter.field"
        @header-click="handleHeaderClick"
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
    </div>
    <AlertComponent
      v-if="alert.messages.length > 0"
      class="alert-margin"
      :alert="alert"
      @close="handleAlertClose"
    />
    <ConfirmDialog ref="confirmDialog" />
  </div>
</template>

<script>
import { mapState } from 'vuex'
import isValidUser from '../../middleware/is-valid-user'
import IconSettings from '../../assets/svg/fi_settings.svg?inline'
import HeadingText from '../../components/HeadingText.vue'
import ButtonComponent from '../../components/controls/ButtonComponent'
import IconPlus from '../../assets/svg/fi_plus.svg?inline'
import SearchControl from '../../components/controls/SearchControl.vue'
import PaginationComponent from '../../components/PaginationComponent.vue'
import TableData from '../../components/TableData.vue'
import helpers from '../../components/Utility/helpers.js'
import LoadingComponent from '../../components/LoadingComponent'
import AlertComponent from '../../components/AlertComponent'
import ConfirmDialog from '../../components/ConfirmDialog.vue';

export default {
  name: 'InventoryAssetsIndex',
  components: {
    HeadingText,
    ButtonComponent,
    SearchControl,
    PaginationComponent,
    TableData,
    LoadingComponent,
    AlertComponent,
    ConfirmDialog
  },
  middleware: isValidUser,
  data() {
    return {
      sortFilters: {},
      paginationTotalResults: 0,
      inventoryAssets: [],
      fieldDefCustomizations: {},
      tableHeaders: [
        {
          display: 'Key',
          field: 'key',
          sortable: true,
          hide: false,
        },
        {
          display: 'Name',
          field: 'name',
          sortable: true,
          hide: false,
        }
      ],
      subtypeDropdownOptions: [],
      icons: {
        settings: IconSettings,
      },
      toggleSettingsOptions: false,
      newInventoryAssetButton: {
        label: 'Add',
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
      checkedRows: [],
      currentPerPage: 10,
      currentPage: 1,
      defaultDateFormat: 'en-US',
      searchControlPlaceholder: 'Search Inventory Assets....',
      alert: {
          messages: [],
          type: "",
          title: ""
      }
    }
  },
  async fetch({ store: { dispatch } }) {
    await dispatch('configInit')
  },
  head() {
    return {
      title: `Inventory Assets | ${this.pageTitle}`,
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
    formattedInvetoryAssets() {
      return this.inventoryAssets.map((inventoryAsset) => {
        const dateString = inventoryAsset.timestamp
        return {
          ...inventoryAsset,
          name: this.getInventoryAssetName(inventoryAsset),
          timestamp: helpers.formatDate(dateString),
        }
      })
    },
  },
  mounted() {
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
    addAlert(messages, type, title) {
      this.alert.title = title
      this.alert.messages = messages
      this.alert.type = type
    },
    handleResponseAlertCheck(r, callback) {
      if (r?.success || r?.ok || r?.status === 200) {
        if (typeof callback === 'function') callback()
      } else if (r?.message) {
        this.addAlert([`${r?.status}: ${r?.message}`], "danger", "Error")
      } else if (r?.errorMessages) {
        this.addAlert(r?.errorMessages, "danger", "Error")
      }
    },
    handleNewInventoryAsset() {
      this.$router.push({
        path: `/inventory-assets/create`,
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
        path: `/inventory-assets/${row.id}`,
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
        pager: {
          size,
          page,
          sortFilters: this.sortFilters,
        },
      }
      return this.$axios
        .$post(`${this.$store.state.config.api_url}/InventoryAsset/search`, JSON.stringify(body), {
          headers: {
            'Content-Type': 'application/json',
            accept: 'application/json',
          }
        })
        .then((r) => {
          this.handleResponseAlertCheck(r, () => {
            this.paginationTotalResults = r?.pager?.total || 0

            this.fieldDefCustomizations = r.data || [];

            this.inventoryAssets = this.fieldDefCustomizations.map((obj) => {
              return Object.fromEntries(
                Object.entries(obj).map(([key, value]) => {
                  if (typeof value === "object" && value !== null) {
                    // modify the column header for the inventory asset list to match custom name
                    const tableHdr = this.tableHeaders.find(x => x.field === key)
                    if (tableHdr !== undefined) {
                      tableHdr.display = this.fieldDefCustomizations.map(item => item[key]).find(x => x.name === key).label
                    }
                    return [key, value.value];
                  }
                  return [key, value];
                })
              );
            });
          })
        })
        .catch((e) => {
          return e
        })
    },
    getPrimer() {
      return this.$axios
        .$get(`${this.$store.state.config.api_url}/InventoryAsset/primer`)
        .then((r) => {
          this.handleResponseAlertCheck(r, () => {
            this.paginationTotalResults = r?.data?.pager?.total || 0
            this.search.facets = r.data.searchFilters.map((facet) => {
              return {
                display: facet.value,
                field: facet.field.toLowerCase(),
              }
            })
          })
        })
    },
    getInventoryAssetName(inventoryAsset) {
      return inventoryAsset.name || 'Anonymous Inventory Asset'
    },
    handleCheckedRowsChanged(checkedRows) {
      this.checkedRows = checkedRows
    },
    handleErrorResponse(r, title) {
      this.errorResponse = this.$getErrorResponse(r)

      if (this.errorResponse != null) {
        this.addAlert(this.errorResponse, "danger", title)
      }
    },
    async handleAssetDelete() {
      if (this.isDisabled) return

      const confirm = await this.$refs.confirmDialog.show("Are you sure you want to delete the selected Inventory Asset(s)?", "Confirm Delete");

      if (confirm) {
        const ids = this.checkedRows.map((row) => row.id)

        if (ids?.length < 1) return

        const data = {
          ids,
        }

        return this.$axios
        .$post(`${this.$store.state.config.api_url}/InventoryAsset/delete`, JSON.stringify(data), {
          headers: {
            'Content-Type': 'application/json',
          }
        })
        .then((r) => {
          this.addAlert(["Success Deleting Inventory Asset(s)"], "success", "Success")
          this.checkedRows = []
          this.handleSearch(1, this.currentPerPage)
          this.handleInit()
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Deleting Inventory Asset(s)")
        })
      }
    }
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
