<template>
  <div class="page-layout">
    <LoadingComponent :is-visible="isLoading" />
    <div class="header">
      <div class="header-row m-b-3">
        <div class="header-left">
          <ButtonComponent
            label=""
            :icon="arrowLeft"
            icon-position="left"
            :icon-only="true"
            theme="default"
            size="medium"
            :disabled="false"
            @button-clicked="goBack"
          />
          <div class="header-title-wrapper">
            <EngagementTitle
              type="subtitle"
              :label="engagement.customer"
              :disabled="isDisabled || isTemplate"
              placeholder="Add customer..."
              @saveTitle="(val) => handleUpdateEngagement('customer', val)"
            />
            <EngagementTitle
              placeholder="Add name..."
              :label="engagement.name"
              :disabled="isDisabled || isTemplate"
              @saveTitle="(val) => handleUpdateEngagement('name', val)"
            />
          </div>
          <div class="header-title-wrapper-self">
             <EngagementTitleDropdown
                type="subtitle"
                :label="engagement.subtype"
                :options="subtypeDropdownOptions"
                :disabled="isDisabled || isTemplate"
                @saveTitle="(val) => handleUpdateEngagement('subtype', val)"
              />
          </div>
        </div>
        <div v-click-outside="handleSettingsToggle" class="header-right">
          <span class="last-modified">{{ lastModified }}</span>
          <template v-if="engagement.status === 'Error'">
            <ButtonComponent
            label="Reset to Draft"
            :theme="`danger`"
            :size="`small`"
            :disabled="false"
            @button-clicked="handleResetPublish()"
          />
          </template>
          <ButtonComponent
            :icon="icons.chatBubble"
            :icon-position="`right`"
            :icon-only="true"
            :theme="`default`"
            :size="`medium`"
            :disabled="false"
            @button-clicked="() => (toggleLogHistory = true)"
          />
          <div class="settings-wrap" v-if="!isTemplate">
            <ButtonComponent
              :icon="icons.settings"
              :icon-position="`right`"
              :icon-only="true"
              :theme="`default`"
              :size="`medium`"
              :disabled="false"
              @button-clicked="() => (toggleSettingsOptions = true)"
            />
            <div v-if="toggleSettingsOptions" class="settings-options">
              <div
                :class=this.cancelClass()
                @click="handleSettingsToggle('cancel')"
              >
                {{cancelButtonLabel()}}
              </div>
              <div
                class="settings-option"
                @click="handleSettingsToggle('attr')"
              >
                Engagement Attributes
              </div>
              <div
                class="settings-option"
                @click="handleSettingsToggle('export')"
              >
                Engagement Export
              </div>
              <div
                class="settings-option"
                @click="handleSettingsToggle('issues')"
              >
                Export Issues As Import
              </div>
            </div>
          </div>
          <div v-if="!isTemplate">
            <ButtonComponent
              v-if="statusStep == 'Publish'"
              label='Publish'
              :icon="icons.unlock"
              :icon-position="`right`"
              :icon-only="false"
              :theme="`primary`"
              :size="`medium`"
              :disabled="false"
              @button-clicked="handleAction"
            />
            <ButtonComponent
              v-if="statusStep == 'Checkout'"
              label='Checkout'
              :icon="icons.unlock"
              :icon-position="`right`"
              :icon-only="false"
              :theme="`primary`"
              :size="`medium`"
              :disabled="engagement.status != 'Published' || actionRoleRestricted('Checkout')"
              @button-clicked="handleAction"
            />
            <ButtonComponent
              :label="'Generate Report'"
              :icon="icons.report"
              :icon-position="`right`"
              :icon-only="false"
              :theme="`primary`"
              :size="`medium`"
              :disabled="false"
              @button-clicked="handleSettingsToggle('report')"
            />
          </div>
          <SlideModal
            :label="`Log History`"
            :open="toggleLogHistory"
            :size="'medium'"
            @toggle="() => (toggleLogHistory = !toggleLogHistory)"
          >
            <LogHistory
              :config="$store.state.config"
              :engagement-id="id"
              :scanId="this.engagement.scan_id"
              :date-format="dateFormat"
            />
          </SlideModal>
          <SlideModal
            :label="`Template Issue Add`"
            :open="toggleTemplateIssueAdd"
            :size="'medium'"
            @toggle="() => (toggleTemplateIssueAdd = !toggleTemplateIssueAdd)"
          >
            <SearchControl
              reff="txtSearchTemplateIssues"
              :placeholder="searchTemplateControlPlaceholder"
              :facets="templateSearch.facets"
              :query="templateSearch.query"
              @input="handleTemplateSearchUpdate"
              @search="handleTemplateSearchControl"
            />
            <DropdownControl
              theme="solid"
              label="Select Asset"
              :options="formattedTemplateAssetDropdownOptions"
              @update="handleTemplateAssetUpdate"
            />
            <p>
              Select Template Issue to Add
            </p>
            <TableData
              :headers="templateTableHeaders"
              :rows="formattedTemplateIssues"
              :toggle-rows="false"
              :disable-hover="true"
              :row-size="'medium'"
              :disabled="false"
              @row-click="handleTemplateRowClick"
            />

            <div class="page-row">
              <PaginationComponent
                :current-page="currentTemplatePage"
                :total-results="paginationTemplateTotalResults"
                @page-change="handleTemplatePageChange"
                @amount-change="handleTemplatePageChange"
                @pagination-mounted="handleTemplatePageChange"
              />
            </div>
          </SlideModal>
          <SlideModal
            :label="cancelButtonLabel()"
            :open="toggleCancel"
            :size="'small'"
            @toggle="() => (toggleCancel = !toggleCancel)"
          >
          <CancelEngagement
            :engagement-name="engagement.name"
            @cancel-engagement="handleCancel"
            @toggle="() => (toggleCancel = !toggleCancel)"
            :markHistorical="isDisabled"
            @save="handleSave"
          />
          </SlideModal>
          <SlideModal
            :label="`Engagement Attributes`"
            :open="toggleAttributes"
            :size="'medium'"
            @toggle="() => (toggleAttributes = !toggleAttributes)"
          >
            <template v-if="attributeDefinitions.length > 0">
              <div class="attributes-list">
                <div
                  v-for="(attribute, index) in attributeDefinitions.filter(def => this.attributeCustomizations.find(x => x.name === def.name && x.isHidden === false))"
                  ref="attributes"
                  :key="`attribute-${attribute.name}-${index}`"
                >
                  <FormLabel
                    class="input-label input-small md-label"
                    :label="getAttributeCustomization(attribute.name)?.label"
                  />

                  <template
                    v-if="getAttributeField(attribute.type) === 'string'"
                  >
                    <InputText
                      :reff="`attribute-${attribute.name}`"
                      class="text-field_asset btm-input"
                      :placeholder="attribute.display"
                      :value="getAttributeValue(attribute)"
                      :disabled="isDisabled || getAttributeCustomization(attribute.name)?.isReadOnly"
                      @update="
                        (val) => handleUpdateAttribute(attribute.name, val)
                      "
                    />
                  </template>
                  <template
                    v-if="getAttributeField(attribute.type) === 'number'"
                  >
                    <InputNumber
                      :reff="`attribute-${attribute.name}`"
                      class="text-field_asset btm-input"
                      :type="getNumType(attribute.type)"
                      :placeholder="attribute.display"
                      :value="getAttributeValue(attribute)"
                      :disabled="isDisabled || getAttributeCustomization(attribute.name)?.isReadOnly"
                      @update="
                        (val) => handleUpdateAttribute(attribute.name, val)
                      "
                    />
                  </template>
                  <template
                    v-else-if="getAttributeField(attribute.type) === 'text'"
                  >
                    <InputTextArea
                      :placeholder="attribute.display"
                      :resize="false"
                      :reff="`attribute-${attribute.name}`"
                      class="btm-input"
                      :value="getAttributeValue(attribute)"
                      :disabled="isDisabled || getAttributeCustomization(attribute.name)?.isReadOnly"
                      @update="
                        (val) => handleUpdateAttribute(attribute.name, val)
                      "
                    />
                  </template>
                  <template
                    v-else-if="getAttributeField(attribute.type) === 'date'"
                  >
                    <InputDate
                      :reff="`attribute-${attribute.name}`"
                      class="text-field_asset btm-input"
                      :value="getAttributeValue(attribute)"
                      :disabled="isDisabled || getAttributeCustomization(attribute.name)?.isReadOnly"
                      @update="
                        (val) => handleUpdateAttribute(attribute.name, val)
                      "
                    />
                  </template>
                  <template
                    v-else-if="getAttributeField(attribute.type) === 'select'"
                  >
                    <DropdownControl
                      theme="outline"
                      :options="
                        formatAttributeOptions(attribute.options || [])
                      "
                      class="assesment-dropdown btm-input"
                      :reff="`attribute-${attribute.name}`"
                      :disabled="isDisabled || getAttributeCustomization(attribute.name)?.isReadOnly"
                      :value="getAttributeValue(attribute)"
                      @update="
                        (opt) =>
                          handleUpdateAttribute(attribute.name, opt.value)
                      "
                    />
                  </template>
                  <template
                    v-else-if="
                      getAttributeField(attribute.type) === 'markdown'
                    "
                  >
                    <MarkdownEditor
                      class="md-editor"
                      :reff="`markdownAttribute-${attribute.name}`"
                      :type="attribute.name"
                      :dont-show="false"
                      :value="getAttributeValue(attribute)"
                      :disabled="isDisabled || getAttributeCustomization(attribute.name)?.isReadOnly"
                      @input="
                        (val) => handleUpdateAttribute(attribute.name, val)
                      "
                    />
                  </template>
                  <template
                    v-else-if="
                      getAttributeField(attribute.type) === 'multiselect'
                    "
                  >
                    <MultiSelect
                      :options="
                        formatAttributeMultiOptions(attribute.options)
                      "
                      :values="getAttributeValue(attribute)"
                      :disabled="isDisabled || getAttributeCustomization(attribute.name)?.isReadOnly"
                      @update="
                        (val) => handleUpdateAttribute(attribute.name, val)
                      "
                    />
                  </template>
                </div>

                <br />

                <div class="attr-submit" @click="saveAttributes">
                  <ButtonComponent
                    label="Save Attributes"
                    :icon-only="false"
                    theme="primary"
                    size="medium"
                    :disabled="isDisabled"
                  />
                </div>
              </div>

              <br />
              <br />
              <br />
            </template>
          </SlideModal>
          <SlideModal
            :label="`Engagement Export`"
            :open="toggleExport"
            :size="'xsmall'"
            @toggle="() => (toggleExport = !toggleExport)"
          >                 
            <div class="export-engagement__wrapper">
              <p>
               Confirm you want to export this engagement, including all its assets, issues, and attachments.
              </p>
              <div class="export-submit" @click="exportEngagement">
                <ButtonComponent
                  label="Export"
                  :icon-only="false"
                  theme="primary"
                  size="xsmall"
                  :disabled="false"
                />
            </div>
            </div>
          </SlideModal>
          <SlideModal
            :label="`Engagement Issues Export as Import`"
            :open="toggleIssue"
            :size="'xsmall'"
            @toggle="() => (toggleIssue = !toggleIssue)"
          >                 
            <div class="export-engagement__wrapper">
              <p>
               Confirm you want to export this engagement's issues as an import.
              </p>
              <div class="export-submit" @click="exportIssueImport">
                <ButtonComponent
                  label="Export"
                  :icon-only="false"
                  theme="primary"
                  size="xsmall"
                  :disabled="false"
                />
            </div>
            </div>
          </SlideModal>
          <SlideModal
            :label="`Generate Engagement Report`"
            :open="toggleReport"
            :size="'xsmall'"
            @toggle="handleReportToggle()"
          >                 
            <div class="report-engagement__wrapper">
              <p>
                After you click 'Generate', a report will be queued that will then process and be set in the attachment list on this engagement.
              </p>
              <p>
                Please select a template to use.
              </p>
              <DropdownControl
                theme="solid"
                :options="reportTemplateDropdownOptions"
                @update="handleTemplateUpdate"
              />
              <div class="report-submit" @click="reportEngagement">
                <ButtonComponent
                  label="Generate"
                  :icon-only="false"
                  theme="primary"
                  size="small"
                  :disabled="false"
                />
            </div>
            </div>
          </SlideModal>
        </div>
      </div>
      <div class="header-row">
        <IssueSummary :summary="summary" />
        <div class="summary-wrapper">
          <FormLabel class="input-label" label="Engagement Summary" />
          <InputTextArea
            placeholder="Write a brief summary..."
            :resize="false"
            class="engagement-summary"
            reff="txtEngagementSummary"
            :value="engagement.summary"
            :disabled="isDisabled || isTemplate"
            @blur="(val) => handleUpdateEngagement('summary', val)"
          />
        </div>
        <div v-if="!isTemplate">
          <AttachmentList
            :files="engagement.attachments"
            :disabled="isDisabled"
            :validFileExtensions="validFileExtensions"
            @file-added="handleFileAdded"
            @file-not-found="handleFileNotFound"
            @file-removed="handleFileRemoved"
          />
        </div>
      </div>
    </div>

    <div class="page-content">
      <template v-if="!isDisabled">
        <div class="page-row">
          <HeadingText label="Issues" size="3" />
        </div>
      </template>
      <div class="page-row m-b-4">
        <div class="page-row-left">
          <template v-if="!isDisabled">
            <DropdownButton
              theme="outline"
              label=""
              button-text="Add Item"
              :options="dropdownButtonOptions"
              :icon="dropdownButtonIcon"
              @update="handleDropdownButtonClick"
            />
            <div v-if="!isTemplate">
              <ButtonComponent
                :label="'Mark as Removed'"
                :disabled="checkedRows.length < 1 || actionRoleRestricted('MarkRemovedMulti')"
                theme="primary-outline"
                size="medium"
                @button-clicked="handleMarkAsRemoved"
              />
            </div>
            <div v-if="isTemplate">
              <ButtonComponent
                :label="'Delete Issue'"
                :disabled="checkedRows.length < 1"
                theme="primary-outline"
                size="medium"
                @button-clicked="confirmTemplateIssueDelete"
              />
            </div>
          </template>
          <template v-else>
            <div class="m-2">
              <HeadingText label="Issues" size="3" />
            </div>
          </template>
        </div>
        <div v-if="!isTemplate" class="page-row-right">
          <DropdownControl
            theme="solid"
            label="State"
            :options="formattedStateDropdownOptions"
            @update="handleStateUpdate"
          />
          <DropdownControl
            theme="solid"
            label="Tested"
            :options="formattedTestedDropdownOptions"
            @update="handleTestedUpdate"
          />
          <DropdownSeverityControl
            label="Severity"
            theme="solid"
            :options="severityDropdownOptions"
            default-display="All"
            :hide-all="false"
            :rounded="true"
            @update="handleSeverityUpdate"
          />
          <DropdownControl
            theme="solid"
            label="Asset"
            :options="formattedAssetDropdownOptions"
            @update="handleAssetUpdate"
          />
          <SearchControl
            :placeholder="searchControlPlaceholder"
            reff="txtSearchIssues"
            :facets="search.facets"
            :query="search.query"
            @input="handleSearchUpdate"
            @facet-click="handleFilterChange"
            @search="handleSearchControl"
          />
        </div>
        <div  v-if="isTemplate" class="page-row-right">
          <DropdownSeverityControl
            label="Severity"
            theme="solid"
            :options="severityDropdownOptions"
            default-display="All"
            :hide-all="false"
            :rounded="true"
            @update="handleSeverityUpdate"
          /> 
          <SearchControl
            :placeholder="searchControlPlaceholder"
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
        :rows="formattedIssues"
        :toggle-rows="true"
        :show-open-links="true"
        link-type="issueDetails"
        :engagement-id="id"
        :disable-hover="true"
        :row-size="'medium'"
        :current-filter="currentFilter.field"
        :disabled="isDisabled"
        @header-click="handleHeaderClick"
        @row-click="handleRowClick"
        @checked-rows-changed="handleCheckedRowsChanged"
      />

      <div class="page-row">
        <PaginationComponent
          :current-page="currentPage"
          :total-results="paginationTotalResults"
          @page-change="handlePageChange"
          @amount-change="handlePageChange"
          @pagination-mounted="handlePageChange"
        />
      </div>
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
import { mapActions } from 'vuex'
import IconChatBubble from '../../../assets/svg/fi_chat-bubble.svg?inline'
import IconSettings from '../../../assets/svg/fi_settings.svg?inline'
import IconUnlock from '../../../assets/svg/fi_unlock.svg?inline'
import IconReport from '../../../assets/svg/fi_paper.svg?inline'
import IconPlus from '../../../assets/svg/fi_plus.svg?inline'
import EngagementTitle from '../../../components/EngagementTitle.vue'
import EngagementTitleDropdown from '../../../components/EngagementTitleDropdown.vue'
import ButtonComponent from '../../../components/controls/ButtonComponent'
import IssueSummary from '../../../components/IssueSummary.vue'
import AttachmentList from '../../../components/AttachmentList.vue'
import HeadingText from '../../../components/HeadingText'
import DropdownSeverityControl from '../../../components/controls/DropdownSeverityControl.vue'
import DropdownControl from '../../../components/controls/DropdownControl.vue'
import MultiSelect from '../../../components/controls/MultiSelect.vue'
import SearchControl from '../../../components/controls/SearchControl.vue'
import TableData from '../../../components/TableData.vue'
import PaginationComponent from '../../../components/PaginationComponent.vue'
import DropdownButton from '../../../components/controls/DropdownButton.vue'
import IconLeftArrow from '../../../assets/svg/fi_arrow-left.svg?inline'
import InputNumber from '../../../components/controls/InputNumber'
import InputDate from '../../../components/controls/InputDate'
import InputText from '../../../components/controls/InputText'
import InputTextArea from '../../../components/controls/InputTextArea'
import MarkdownEditor from '../../../components/controls/MarkdownEditor'
import FormLabel from '../../../components/controls/FormLabel.vue'
import SlideModal from '../../../components/controls/SlideModal.vue'
import CancelEngagement from '../../../components/CancelEngagement.vue'
import LogHistory from '../../../components/LogHistory.vue'
import helpers from '../../../components/Utility/helpers'
import AlertComponent from '../../../components/AlertComponent'
import isValidUser from '../../../middleware/is-valid-user'
import LoadingComponent from '../../../components/LoadingComponent'
import ConfirmDialog from '../../../components/ConfirmDialog.vue';

export default {
  name: 'EngagementShow',
  components: {
    EngagementTitle,
    EngagementTitleDropdown,
    ButtonComponent,
    IssueSummary,
    AttachmentList,
    HeadingText,
    DropdownSeverityControl,
    LoadingComponent,
    DropdownControl,
    MultiSelect,
    SearchControl,
    PaginationComponent,
    DropdownButton,
    TableData,
    InputNumber,
    InputDate,
    InputText,
    InputTextArea,
    MarkdownEditor,
    FormLabel,
    SlideModal,
    LogHistory,
    CancelEngagement,
    AlertComponent,
    ConfirmDialog
  },
  middleware: isValidUser,
  asyncData({ params, store }) {
    return {
      id: params.id,
      label: params.label,
    }
  },
  data() {
    return {
      attributes: {},
      attributeTypes: [
        {
          field: 'string',
          types: ['Single line text (text)'],
        },
        {
          field: 'text',
          types: ['Multi line text (text)'],
        },
        {
          field: 'markdown',
          types: ['Markdown (text)'],
        },
        {
          field: 'number',
          types: ['Integer (long)', 'Number (double)'],
        },
        {
          field: 'date',
          types: ['Date (date)'],
        },
        {
          field: 'select',
          types: ['Single select drop down'],
        },
        {
          field: 'multiselect',
          types: ['Multi select drop down'],
        },
      ],
      arrowLeft: IconLeftArrow,
      currentButton: '',
      configFile: '',
      toggleLogHistory: false,
      toggleTemplateIssueAdd: false,
      toggleCancel: false,
      toggleAttributes: false,
      toggleExport: false,
      toggleReport: false,
      toggleIssue: false,
      toggleSettingsOptions: false,
      isTemplate: false,
      sortFilters: {},
      engagement: {
        name: '',
        summary: '',
        customer: '',
        subtype: '',
        attachments: [],
        attributes: {},
        status: 'Published',
        scan_id: ''
      },
      searchControlPlaceholder: 'Search Issues ...',
      searchTemplateControlPlaceholder: 'Search Template Issues ...',
      tableHeaders: [
        {
          display: 'Issue',
          sort: 'Name',
          field: 'name',
          sortable: true,
          hide: false,
        },
        {
          display: 'Severity',
          sort: 'Severity',
          field: 'severity',
          sortable: true,
          hide: false,
        },
        {
          display: 'Asset',
          sort: 'Asset Id',
          field: 'assetName',
          sortable: false,
          hide: false,
          nowrap: true
        },
        {
          display: 'Date',
          sort: 'Date',
          field: 'foundDate',
          sortable: false,
          hide: false,
        },
        {
          display: 'Location',
          sort: 'Location',
          field: 'location',
          sortable: false,
          hide: false,
        },
        {
          display: 'Tested',
          sort: 'Tested',
          field: 'testStatus',
          sortable: false,
          hide: false,
        },
        {
          display: 'State',
          sort: 'State',
          field: 'state',
          sortable: false,
          hide: false,
        },
      ],
      templateTableHeaders: [
        {
          display: 'Issue',
          sort: 'Name',
          field: 'name',
          sortable: false,
          hide: false,
        },
        {
          display: 'Severity',
          sort: 'Severity',
          field: 'severity',
          sortable: false,
          hide: false,
        },
        {
          display: 'Location',
          sort: 'Location',
          field: 'location',
          sortable: false,
          hide: false,
        },
      ],
      dropdownButtonTemplateOptions: [
        {
          display: 'Add template issue',
          value: 'template-issue',
          order: 1,
        },
        {
          display: 'Import template issue',
          value: 'template-import',
          order: 2,
        },
      ],
      dropdownButtonOptions: [],
      dropdownButtonIcon: IconPlus,
      icons: {
        chatBubble: IconChatBubble,
        settings: IconSettings,
        unlock: IconUnlock,
        report: IconReport,
      },
      assetDropdownOptions: [],
      assetFilter: '',
      testedDropdownOptions: [],
      subtypeDropdownOptions: [],
      testedFilter: '',
      stateFilter: 'isActive',
      search: {
        facets: [
          {
            label: 'All Fields',
            value: 'all',
          },
        ],
        query: '',
      },
      templateSearch: {
        facets: [],
        query: '',
      },
      templateAsset: "",
      severityDropdownOptions: [],
      severityFilter: '',
      stateDropdownOptions: [],
      reportTemplateDropdownOptions: [],
      reportTemplate: '',
      statusFilters: [],
      defaultFilter: {
        display: 'All Fields',
        field: 'all',
      },
      currentFilter: {
        display: 'All Fields',
        field: 'all',
      },
      tmp: {
        clickedHeader: {},
        clickedRow: {},
      },
      issues: [],
      templateIssues: [],
      paginationTotalResults: 0,
      paginationTemplateTotalResults: 0,
      checkedRows: [],
      summary: {
        totalIssues: 0,
        critical: 0,
        high: 0,
        medium: 0,
        low: 0,
        info: 0,
        criticalBar: 0,
        highBar: 0,
        mediumBar: 0,
        lowBar: 0,
        infoBar: 0,
      },
      lastModified: '',
      statusStep: 'Publish',
      alert: {
        messages: [],
        type: "",
        title: ""
      },
      currentPage: 1,
      currentTemplatePage: 1,
      attributeDefinitions: [],
      validFileExtensions: "",
      attributeCustomizations: [],
      fieldDefCustomizations: {},
      actionRestrictions: [],
      templateIssueFieldDefCustomizations: {},
      fieldInfo: {
        "value": "",
        "defaultValue": "",
        "name": "",
        "label": "",
        "isReadOnly": false,
        "isHidden": false,
        "isRequired": false,
        "isSystem": false
      }
    }
  },
  async fetch({ store: { dispatch } }) {
    await dispatch('configInit')
  },
  head() {
    return {
      title: `${this.engagement.name} | ${this.pageTitle}`,
    }
  },
  computed: { 
    ...mapState({
      isLoading: (state) => state.modules.loading.loading,
      dateFormat: (state) => state.modules.user.dateFormat,
      user: (state) => state.modules.user,
      roles: (state) => state.modules.user.roles,
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
    formattedIssues() {
      return this.issues.map((issue) => {
        return {
          ...issue,
          assetName: this.formatAsset(issue),
          foundDate: helpers.formatDate(issue.foundDate),
          removedDate: helpers.formatDate(issue.removedDate)
        }
      })
    },
    formattedTemplateIssues() {
      return this.templateIssues.map((issue) => {
        return {
          ...issue,
          assetName: this.formatAsset(issue),
          foundDate: helpers.formatDate(issue.foundDate),
        }
      })
    },
    formattedTemplateAssetDropdownOptions() {
      return this.assetDropdownOptions
        .map((asset) => {
          return {
            ...asset,
            display: asset.name,
            value: asset.assetId,
            name: asset.name,
            id: asset.assetId,
          }
        })
        .sort((a, b) => {
          return a.name.localeCompare(b.name)
        })
    },
    formattedAssetDropdownOptions() {
      const defaultOption = {
        display: 'All',
        value: 'all',
      }

      const mappedOptions = this.assetDropdownOptions
        .map((asset) => {
          return {
            ...asset,
            display: asset.name,
            value: asset.assetId,
            name: asset.name,
            id: asset.assetId,
          }
        })
        .sort((a, b) => {
          return a.name.localeCompare(b.name)
        })

      return [defaultOption, ...mappedOptions]
    },
    formattedTestedDropdownOptions() {
      const defaultOption = {
        display: 'All',
        value: 'all',
        selected: true,
        order: 0
      }

      const mappedOptions = this.testedDropdownOptions
        .map((tested) => {
          return {
            ...tested,
            display: tested.display,
            value: tested.value,
            order: tested.order
          }
        })
        .sort((a, b) => {
          return a.order - b.order
        })

      return [defaultOption, ...mappedOptions]
    },
    formattedStateDropdownOptions() {
      const defaultOption = {
        display: 'All',
        value: 'all',
        selected: false,
        order: 0
      }

      const result = [
        defaultOption,
        ...(this.stateDropdownOptions.map((option) => {
          return {
            display: option.display,
            value: option.value,
            selected: option.value.toLowerCase() === 'isactive',
          }
        })
        .sort((a, b) => {
          return a.order - b.order
        })
         || []),
      ]

      return result
    },
    formattedAttributeDate() {
      const today = new Date();
      return today.toISOString().split('T')[0];
    },
    isDraft() {
      return this.engagement?.status === 'Draft'
    },
    isDisabled() {
      return !this.isDraft;
    },
    isPublished() {
      return this.engagement?.status === 'Published'
    },
    isHistorical() {
      return this.engagement?.status === 'Historical'
    },
    isError() {
      return this.engagement?.status === 'Error'
    }
  },
  async mounted() {
    this.configFile = await fetch('/smpgui/config.json').then((res) => res.json())
    this.handleInit()
  },
  methods: {
    ...mapActions(['user/setFields']),
    cancelButtonLabel() {
      if(this.isDisabled){
        return "Mark Historical"
      }
      return "Cancel Engagement"
    },
    cancelClass() {
      if(!this.isDisabled && !this.isAdmin()) {  // not published and not admin
        return "settings-option-disabled"
      }
      
      return "settings-option"
    },
    getAttributeCustomization(name) {
      return this.attributeCustomizations.find(x => x.name === name)
    },
    actionRoleRestricted(val) {
      return this.actionRestrictions.includes(val)
    },
    checkRequiredValue(val) {
      return val == null || (typeof val === 'string' ? val.replace(/<br\s*\/?>/gi, "").replace(/\s+/g, "").trim() === '' : val === '' || JSON.stringify(val) === "[]") || val === []
    },
    allRequiredFieldsComplete() {
      const requiredList = this.attributeCustomizations.filter((v) => v.isRequired && Object.entries(this.engagement.attributes).find(([key, value]) => key === v.name && this.checkRequiredValue(value)))

      if (requiredList.length > 0) {
        const attrNames = requiredList.map(field => `${field.label}`).join(", ");
        const msg = (requiredList.length === 1) ? "This attribute is required to publish" : "These attributes are required to publish";
        this.addAlert([`${msg}: ${attrNames}`], "danger", "Publish Error")
        return false;
      }

      return true;
    },
    allRequiredTemplateIssueFieldsComplete(templateIssue) {
      const requiredList = [];
      const templateFieldDef = this.templateIssueFieldDefCustomizations.find(x => x.id === templateIssue.id);

      // get required fields
      const fieldKeysWithNoValue = Object.keys(templateIssue).filter((key) => templateIssue[key] == null || templateIssue[key] === "" || templateIssue[key] === "[]")
      const requiredFields = fieldKeysWithNoValue.filter((key) => templateFieldDef[key]?.isRequired === true).map((key) => templateFieldDef[key])
      requiredList.push(...requiredFields)

      // get required attributes
      const requiredAttributes = templateFieldDef.attributes.filter((v) => v.isRequired && Object.entries(templateIssue.attributes).find(([key, value]) => key === v.name && (value === '' || value === null || value === "[]")))
      requiredList.push(...requiredAttributes)

      if (requiredList.length > 0) {
        const attrNames = requiredList.map(field => `${field.label}`).join(", ");
        const msg = (requiredList.length === 1) ? "This field is" : "These fields are";
        this.addAlert([`${msg} required to add template: ${attrNames}. Update the template with required values.`], "danger", "Add Template Issue Error")
        return false;
      }

      return true;
    },
    handleSettingsToggle(opt = null) {
      if (opt === 'cancel') {
        if(this.isDisabled) {  // disabled = published
          this.toggleCancel = true
        }
        if(this.isAdmin()) {  // have to be admin to cancel a non-published engagement
          this.toggleCancel = true
        }
      } else if (opt === 'attr') {
        this.toggleAttributes = true
      } else if (opt === 'export') {
        this.toggleExport = true
      } else if (opt === 'report') {
        this.toggleReport = true
      } else if (opt === 'issues') {
        this.toggleIssue = true
      }

      this.toggleSettingsOptions = false
    },
    saveAttributes() {
      this.handleUpdateEngagement('attributes', this.engagement.attributes)
      this.$refs.attributes.scrollTop = 0
    },
    isAdmin() {
      // allows superuser or pentest admin roles
      if (this.configFile !== null) {
        return (this.user.roles.includes(this.configFile.pentest_admin_role) ||
          this.user.roles.includes(this.configFile.admin_role))
      }
    },
    exportIssueImport() {
      return this.$axios
        .$get(`${this.$store.state.config.api_url}/Engagement/${this.id}/issue-import-export`, {
          headers: {
            'Content-Type': 'application/json',
          },
          responseType: 'blob',
        })
        .then((r) => {
          const a = document.createElement('a');
          a.href = window.URL.createObjectURL(r);
          a.download = "issueImport.json";
          document.body.appendChild(a);
          a.click();    
          a.remove();  
          this.toggleIssue = false;
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Getting Export Issue Import")
        })
    },
    exportEngagement() {
      return this.$axios
        .$get(`${this.$store.state.config.api_url}/Engagement/${this.id}/export`, {
          headers: {
            'Content-Type': 'application/json',
          },
          responseType: 'blob',
        })
        .then((r) => {
          const a = document.createElement('a');
          a.href = window.URL.createObjectURL(r);
          a.download = "engagement.zip";
          document.body.appendChild(a);
          a.click();    
          a.remove();  
          this.toggleExport = false;
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Getting Engagement Export")
        })
    },
    reportEngagement() {
      return this.$axios
        .$get(`${this.$store.state.config.api_url}/Engagement/${this.id}/report?template=${this.reportTemplate}`, {
            headers: {
              'Content-Type': 'application/json',
            }
        })
        .then((r) => {
          this.toggleReport = false;
          this.addAlert(["The report has been generated and placed into queue for processing."], "success", "Generate Report")
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Generating Engagement Report")
        })
    },
    formatMultiSelectValue(val, opts) {
      const newVal =
        typeof val === 'string' && val[0] === '[' && val[val.length - 1] === ']'
          ? JSON.parse(val)
          : val
      const updatedVal =
        typeof newVal === 'string'
          ? [newVal]
          : newVal === null
          ? []
          : [...newVal]
      const r = updatedVal.filter((v) => opts.includes(v))
      return r
    },

    getAttributeValue(attribute) {
      const { name } = attribute

      const attr = this.getAttributeCustomization(name)
      
      // get value to set
      const val = attr.value || attr.defaultValue || null;
      
      const attrType = this.getAttributeField(attribute.type)
      if (!(name in this.engagement.attributes)) {
        if (attrType === 'date') {
          this.engagement.attributes[name] = val || this.formattedAttributeDate
        } else if (attrType === 'select') {
          this.engagement.attributes[name] = val
        } else if (attrType === 'multiselect') {
          this.engagement.attributes[name] = val || []
        } else if (attrType === 'number') {
          this.engagement.attributes[name] = val || null
        } else {
          this.engagement.attributes[name] = val || ''
        }
      }

      if (attrType === 'multiselect') {
        const currentVal = this.engagement.attributes[name]
        const opts = attribute?.options || []
        return this.formatMultiSelectValue(currentVal, opts)
      }

      return this.engagement.attributes[name]
    },
    formatAttributeOptions(opts) {
      return [{ display: "Select", value: null },  ...opts.map((o) => {
        return {
          display: o,
          value: o
        }
      })]
    },
    formatAttributeMultiOptions(opts) {
      return opts || []
    },
    getNumType(attributeType) {
      return attributeType === 'Integer (long)' ? 'int' : 'double'
    },
    handleUpdateAttribute(attributeName, newVal) {
      this.engagement.attributes[attributeName] = newVal
    },
    getAttributeField(type) {
      const attr = this.attributeTypes.find((attrType) =>
        attrType.types.includes(type)
      )
      return attr?.field || 'unknown type'
    },

    handleSave(newIssues) {
      this['user/setFields'](newIssues)
    },
    handleInit(callback = null) {
      this.init()
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
    },
    async handleAction(action) {
      if (action === 'Publish') {
        if(this.assetDropdownOptions.length === 0){
          this.addAlert(["No  Assets have been added yet, please add Asset before Publishing."], "danger", "Error")
          return;
        }
        if (!this.allRequiredFieldsComplete()) {
          return;
        }
        
        const confirm = await this.$refs.confirmDialog.show("Are you sure you want to publish this engagement?", "Confirm Publish");
        if (confirm) {
          this.handlePublish();
        }
      }
      if (action === 'Checkout') {
        if (this.engagement.draftEngagementId) {
          const confirm = await this.$refs.confirmDialog.show("This engagement is already checked out. Would you like to view it?", "Already Checked Out");
          if (confirm) {
            this.$router.push({
              path: `/engagements/${this.engagement.draftEngagementId}`,
            })
          }
        } else {
          this.handleCheckout()
        }
      }
    },
    handleReport() {
      
    },
    handlePublish() {
      return this.$axios
        .$post(`${this.$store.state.config.api_url}/Engagement/${this.id}/queue`, null,  {
          headers: {
            'Content-Type': 'application/json',
          },
        })
        .then((r) => {
          this.$router.push({
            path: `/engagements`,
          })
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Publishing Engagement")
        })
    },
    handleCheckout() {
      return this.$axios
        .$post(`${this.$store.state.config.api_url}/Engagement/${this.id}/checkout`, null,  {
          headers: {
            'Content-Type': 'application/json',
          },
        })
        .then((r) => {
          this.refreshPage(r.data);
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Checking Out Engagement")
        })
    },
    handleResetPublish() {
      const engagementId = this.id;
      return this.$axios
        .$post(`${this.$store.state.config.api_url}/Engagement/${engagementId}/reset`, null,  {
          headers: {
            'Content-Type': 'application/json',
          },
        })
        .then((r) => {
          this.goBack();
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error resetting the published engagement")
        })
    },
    handleCancel() {
      return this.$axios
        .$post(`${this.$store.state.config.api_url}/Engagement/${this.id}/cancel`, null,  {
          headers: {
            'Content-Type': 'application/json',
          },
        })
        .then((r) => {
          this.$router.push({
            path: `/engagements`,
          })
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Canceling Engagement")
        })
    },
    formatLastModified() {
      let modeString = "";
      if(this.isPublished){
        modeString = 'Published on'
      } else if(this.isError) {
        modeString = 'Errored on'
      } else if(this.isHistorical) {
        modeString = 'Historical since'
      } else {
        modeString = 'Draft since'
      }
      const tmpTimestamp = new Date(
        this.isPublished ? this.engagement.publishDate : this.engagement.timestamp
      )
      const modeDate = tmpTimestamp.toLocaleDateString(this.dateFormat, {
        month: 'long',
        day: 'numeric',
        year: 'numeric',
      })

      return `${modeString} ${modeDate}`
    },
    handleMarkAsRemoved() {
      if (this.isDisabled) return

      const ids = this.checkedRows.map((row) => row.id)      
      if (ids?.length < 1) return

      const data = {
        ids
      }

      return this.$axios
        .$post(`${this.$store.state.config.api_url}/Issue/remove`,  JSON.stringify(data),  {
          headers: {
            'Content-Type': 'application/json',
          },
        })
        .then((r) => {
          this.checkedRows = []
          this.handleSearch(1, this.currentPerPage)
          this.handleInit()
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Marking Issues Removed")
        })
    },
    handleTemplateIssueDelete() {
      if (this.isDisabled) return

      const ids = this.checkedRows.map((row) => row.id)

      if (ids?.length < 1) return

      const data = {
        ids,
      }

      return this.$axios
      .$post(`${this.$store.state.config.api_url}/issue/template/delete`, JSON.stringify(data), {
        headers: {
          'Content-Type': 'application/json',
        }
      })
      .then((r) => {
        this.addAlert(["Success Deleting Issues"], "success", "Success")
        this.checkedRows = []
        this.handleSearch(1, this.currentPerPage)
        this.handleInit()
      })
      .catch((error) => {
        this.handleErrorResponse(error, "Error Deleting Issues")
      })

    },
    handleDropdownButtonClick(option) {
      const options = ['import', 'issue', 'asset', 'template', 'template-import', 'template-issue']
      if (options.includes(option.value)) {
        if (option.value === 'import') {
          if(this.assetDropdownOptions.length === 0){
            this.addAlert(["No Assets have been added yet, please add Asset first."], "danger", "Error")
          }
          else {
            this.$router.push({
              path: `/engagements/${this.id}/import`,
            })
          }
        } else if (option.value === 'template-import') {
          this.$router.push({
            path: `/engagements/${this.id}/template/import`,
          })
        } else if (option.value === 'template-issue') {
          this.$router.push({
            path: `/engagements/${this.id}/issues/create`,
          })
        } else if (option.value === 'issue') {
          if(this.assetDropdownOptions.length === 0){
            this.addAlert(["No  Assets have been added yet, please add Asset first."], "danger", "Error")
          }
          else {
            this.$router.push({
              path: `/engagements/${this.id}/issues/create`,
            })
          }
        } else if (option.value === 'asset') {
          this.$router.push({
            path: `/engagements/${this.id}/assets/create`,
          })
        } else if (option.value === 'template') {
          if(this.assetDropdownOptions.length === 0){
            this.addAlert(["No Assets have been added yet, please add Asset first."], "danger", "Error")
          }
          else {
            this.templateAsset = this.assetDropdownOptions[0].assetId
            this.toggleTemplateIssueAdd = true
          }
        }
      }
    },
    handleButtonSet(button) {
      this.currentButton = button
    },
    handleSlideModal(n) {
      this.slideModals[n].toggle = !this.slideModals[n].toggle
    },
    updateAttachments(attachmentList) {
      this.engagement.attachments = attachmentList.reduce((acc, curr) => {
        if (!acc.some((a) => a.fileId === curr.fileId)) acc.push(curr)
        return acc
      }, [])

      return this.$axios
        .post(`${this.$store.state.config.api_url}/Engagement/${this.id}/attachments`, JSON.stringify(this.engagement.attachments), {
          headers: {
            'Content-Type': 'application/json',
            accept: 'application/json',
          }
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Updating Engagement Attachments")
        })
    },
    handleFileAdded(filedata) {
      const info = {
        fileId: filedata.fileId,
        fileName: filedata.fileName,
      }

      const attachmentList = [info, ...this.engagement.attachments]
      this.updateAttachments(attachmentList)
    },
    handleFileNotFound(fileData) {
      this.addAlert(["File Not Found"], "danger", fileData)
    },
    handleFileRemoved(filedata) {
      const attachmentList = this.engagement.attachments.filter(
        (a) => a.fileId !== filedata.fileId
      )
      this.updateAttachments(attachmentList)
    },
    handleTemplateUpdate(option){
      this.reportTemplate = option.value
    },
    handleReportToggle() {
      this.toggleReport = !this.toggleReport
      this.reportTemplate = this.reportTemplateDropdownOptions[0].value
    },
    handleUpdateEngagement(field = '', value = '') {
      if (this.isDisabled) return

      const attributes = Object.assign({}, field === 'attributes' ? value : this.engagement.attributes)
      
      Object.keys(attributes).forEach((attr) => {
        const attrDef = this.attributeDefinitions.find(
          (def) => def.name === attr
        )
        if (attrDef && this.getAttributeField(attrDef.type) === 'multiselect') {
          if (typeof attributes[attr] !== 'string') {
            attributes[attr] = JSON.stringify(
              attributes[attr]
            )
          } else {
            attributes[attr] = JSON.parse(
              attributes[attr]
            )
          }
        }
      })

      const body = {
        id: this.id,
        name: field === 'name' ? value : this.engagement.name,
        summary: field === 'summary' ? value : this.engagement.summary,
        customer: field === 'customer' ? value : this.engagement.customer,
        subtype: field === 'subtype' ? value : this.engagement.subtype
      }

      body.attributes = attributes
      return this.$axios
        .$post(`${this.$store.state.config.api_url}/Engagement/summary/edit`, JSON.stringify(body), {
          headers: {
            'Content-Type': 'application/json',
            accept: 'application/json',
          }
        })
        .then((r) => {
          const { name, summary, customer, subtype } = r.data
          this.engagement.name = name
          this.engagement.summary = summary
          this.engagement.customer = customer
          this.engagement.subtype = subtype

          this.toggleAttributes = false
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Saving Engagement Summary")
        })
    },
    handleSeverityUpdate(option) {
      this.severityFilter = option.value.toLowerCase() === 'all' ? null : option.value
      this.handleSearch(1, this.currentPerPage)
    },
    handleAssetUpdate(option) {
      this.assetFilter =  option.value.toLowerCase() === 'all' ? null :  option.value
      this.handleSearch(1, this.currentPerPage)
    },
    handleTestedUpdate(option) {
      this.testedFilter = option.value.toLowerCase() === 'all' ? null : option.value
      this.handleSearch(1, this.currentPerPage)
    },
    handleStateUpdate(option) {
      this.stateFilter = option.value.toLowerCase() === 'all' ? null : option.value
      this.handleSearch(1, this.currentPerPage)
    },
    handleSearchUpdate(query) {
      this.search.query = query
    },
    handleSearchControl(data) {
      this.search.query = data.query
      this.currentFilter = data.facet
      this.handleSearch(1, this.currentPerPage)
    },
    handleTemplateSearchUpdate(query) {
      this.templateSearch.query = query
    },
    handleTemplateSearchControl(data) {
      this.templateSearch.query = data.query
      this.handleTemplateSearch(1, this.currentTemplatePerPage)
    },
    handleTemplatePageChange(data) {
      this.currentTemplatePerPage = data.size
      this.handleTemplateSearch(data.page, data.size)
    },
    handleTemplateAssetUpdate(option) {
      this.templateAsset = option.id
    },
    handleTemplateSearch(page, size) {
      this.currentTemplatePage = page
      const searchFilter = {
        field: "Name",
        value: this.templateSearch.query,
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
        .$post(`${this.$store.state.config.api_url}/Issue/template/search`, JSON.stringify(body), {
          headers: {
            'Content-Type': 'application/json',
            accept: 'application/json',
          }
        })
        .then((r) => {
          this.paginationTemplateTotalResults = r?.pager?.total || 0
          this.templateIssueFieldDefCustomizations = r.data || []
          

          this.templateIssues = this.templateIssueFieldDefCustomizations.map((obj) => {
            const attributes = {};

            return Object.fromEntries(
              Object.entries(obj).map(([key, value]) => {
                if (typeof value === "object" && value !== null) {
                  if (key === "attributes") {
                      value.forEach((attr) => {
                        const val = (attr.value !== null && attr.value !== '') ? attr.value : attr.defaultValue || "";
                        attributes[attr.name] = val
                      })
                      return [key, attributes]
                  } else {
                    return [key, value.value || value.defaultValue];
                  }
                }
                return [key, value];
              })
            );
          });
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Searching Template Issues")
        })
    },
    goBack() {
      this.$router.push({
        path: `/engagements`,
      })
    },
    refreshPage(id) {
      this.$router.push({
        path: `/engagements/${id}`,
        force: true
      })
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
    handleHeaderClick(header) {
      const { field } = header

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
      this.$router.push({
        path: `/engagements/${this.id}/issues/${row.id}`,
      })
    },
    handleTemplateRowClick(row) {

      return this.$axios
        .$post(`${this.$store.state.config.api_url}/Issue/${row.id}/engagement/${this.$route.params.id}/asset/${this.templateAsset}/template`, null, {
          headers: {
            'Content-Type': 'application/json',
          }
        })
        .then((r) => {
          this.$router.push({
            path: `/engagements/${this.$route.params.id}/issues/${r.data.id}?q=templateAdd`
          })
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Adding Template Issue to Engagement")
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

      if(JSON.stringify(this.sortFilters) === "{}") {
        this.sortFilters.name = true
      }

      const body = {
        searchFilters: [searchFilter],
        engagementId: `${this.id}`,
        pager: {
          size,
          page,
          sortFilters: this.sortFilters,
        },
        testStatusFilters: [],
        statusFilters: [],
        severityFilters: [],
        assetFilters: []
      }

      if (this.severityFilter) {
        body.severityFilters = [this.severityFilter]
      }

      if (this.assetFilter) {
        body.assetFilters = [this.assetFilter]
      }

      if (this.testedFilter) {
        body.testStatusFilters = [this.testedFilter]
      }

      if (this.stateFilter) {
        body.stateFilters = [this.stateFilter]
      }

      return this.$axios
        .$post(`${this.$store.state.config.api_url}/Issue/search`, JSON.stringify(body), {
          headers: {
            'Content-Type': 'application/json',
            accept: 'application/json',
          }
        })
        .then((r) => {
          this.paginationTotalResults = r?.pager?.total || 0
          this.fieldDefCustomizations = r.data || {}

          this.issues = this.fieldDefCustomizations.map((obj) => {
            return Object.fromEntries(
              Object.entries(obj).map(([key, value]) => {
                if (typeof value === "object" && value !== null) {
                  return [key, value.value || value.defaultValue];
                }
                return [key, value];
              })
            );
          });

        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Searching Issues")
        })
    },
    init() {
      return this.$axios
        .$get(`${this.$store.state.config.api_url}/Issue/primer?engagementId=${this.id}`)
        .then((r) => {
          
          this.subtypeDropdownOptions = r.data?.subtypeDropdown
          this.severityDropdownOptions = r.data?.severityDropdown
          this.stateDropdownOptions = r.data?.issueStateDropdown
          
          this.reportTemplateDropdownOptions = 
            r.data?.reportTemplateDropdown.sort((a, b) => {
              return a.display.localeCompare(b.display)
            }) ?? []
          this.reportTemplate = this.reportTemplateDropdownOptions[0]?.value ?? ""
          this.search.facets =
            r.data?.searchFilters?.map((facet) => {
              return {
                display: facet.value,
                field: facet.field.toLowerCase(),
              }
            }) || []
            
          this.assetDropdownOptions = r.data?.assetDropdown || []
          this.testedDropdownOptions = r?.data?.testedDropdown || []
          this.dropdownButtonOptions = r?.data?.addItemDropdown || []
          this.validFileExtensions = r?.data?.validFileExtensions.join(',')
          
          this.attributeDefinitions =
            r?.data?.attributeDefinitions || []

          return this.$axios
            .$get(`${this.$store.state.config.api_url}/Engagement/${this.id}/summary`)
            .then((r) => {
              const {
                totalIssues,
                critical,
                high,
                medium,
                low,
                info,
                criticalBar,
                highBar,
                mediumBar,
                lowBar,
                infoBar,
              } = r.data.issueCount

              this.actionRestrictions = r?.data?.actionRestrictions

              this.attributeCustomizations = r?.data?.attributes;

              this.attributeCustomizations.forEach((attr) => {
                const attrDef = this.attributeDefinitions.find(
                  (def) => def.name === attr.name
                )

                let val = (attr.value !== null && attr.value !== '') ? attr.value : attr.defaultValue || "";

                if (attrDef && this.getAttributeField(attrDef.type) === 'multiselect') {
                  if (val === null || val === '' || val.includes('[]')) val = '[]';
                  this.attributes[attr.name] = JSON.parse(
                    val
                  )
                }
              })

              this.engagement.attributes = this.attributes;

              this.summary = {
                totalIssues: totalIssues || 0,
                critical: critical || 0,
                high: high || 0,
                medium: medium || 0,
                low: low || 0,
                info: info || 0,
                criticalBar: criticalBar || 0,
                highBar: highBar || 0,
                mediumBar: mediumBar || 0,
                lowBar: lowBar || 0,
                infoBar: infoBar || 0,
              }
              this.engagement.name = r.data.name
              this.engagement.summary = r.data.summary
              this.engagement.customer = r.data.customer
              this.engagement.subtype = r.data.subtype
              this.engagement.attachments = 
                r.data.attachments !== null ?
                r.data.attachments.map(function(attachment) { 
                  return attachment.attachment; 
                }) : []
              this.engagement.status = r.data.status

              // need the scanid to filter log history if 'published' status.
              // If not, the comments in log  history will show for the engagement draft and published
              this.engagement.scan_id = r.data.status === "Published" ? r.data.scanId : null

              this.engagement.publishDate = r.data.publishDate
              this.engagement.timestamp = r.data.timestamp
              this.lastModified = this.formatLastModified()
              this.statusStep =
                this.engagement.status === 'Draft' ? 'Publish' : 'Checkout'
              this.isTemplate = this.engagement.subtype === "Template"
              
              if(this.isTemplate)
              {
                this.dropdownButtonOptions = this.dropdownButtonTemplateOptions
              }
            })
            .catch((error) => {
              this.handleErrorResponse(error, "Error Getting Engagement Summary")
            })
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Getting Issue Primer")
        })
    },
    deleteIssue(issueId) {
      if (this.isDisabled) return
      return this.$axios
        .$delete(`${this.$store.state.config.api_url}/Issue/${issueId}`, {
          headers: {
            accept: 'application/json',
          }
        })
        .then((r) => {
          alert(`DELETED: ${r.message}`)
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Deleting Issue")
        })
    },
    formatAsset(issue) {
      return (
        issue?.assetName ||
        this.assetDropdownOptions.find((a) => a.assetId === issue.assetId)?.name
      )
    },
    async confirmTemplateIssueDelete() {
      const confirm = await this.$refs.confirmDialog.show("Are you sure you want to delete the selected template issues?", "Confirm Delete");
        if (confirm) {
          this.handleTemplateIssueDelete();
        }
    }
  },
}
</script>

<style lang="scss" scoped>
.page-layout {
  max-width: 1872px;
  width: 100%;
  .header {
    padding: 24px;
    background-color: $brand-color-scale-1;
    display: flex;
    flex-flow: column;

    .header-row {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      padding: 0 0 0 0;
      gap: 20px;

      .header-left {
        display: flex;
        align-items: center;
        justify-content: flex-start;
        gap: 24px;
      }
      .header-right {
        display: flex;
        align-items: center;
        justify-content: flex-end;
        gap: 24px;
      }
    }
  }
  .summary-wrapper {
    display: flex;
    flex-direction: column;
    flex: 1;
    gap: 8px;
    position: relative;
    top: 0px;
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
  .last-modified {
    font-family: $font-primary;
    font-style: normal;
    font-weight: 400;
    font-size: 14px;
    line-height: 18px;
  }
}

.header-title-wrapper-self
{
  align-self: baseline;
}

.attributes-list {
  padding-left: 12px;
  display: flex;
  flex-flow: column;
  gap: 24px;
}

.attr-submit {
  align-self: flex-start;
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

    .settings-option-disabled {
      color: gray;
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
.export-engagement__wrapper {
  display: flex;
  flex-flow: column;
  justify-content: flex-start;
  gap: 24px;
  background-color: $brand-off-white-2;
  width: 100%;
  height: 100%;
  font-family: $font-primary;

  & > p {
    flex: 0 0 auto;
  }
  p {
    font-style: normal;
    font-weight: 400;
    font-size: 14px;
    line-height: 18px;
    color: $brand-color-scale-6;
  }
}
</style>
