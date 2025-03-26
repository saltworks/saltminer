<template>
  <div class="issue-page__wrapper">
    <LoadingComponent :is-visible="isLoading" />
    <div class="issue-page__top">
      <div class="issue-page__topBox">
        <div class="issue-top_left">
          <router-link :to="`/engagements/${id}`">
            <ButtonComponent
              label=""
              :icon="arrowLeft"
              icon-position="left"
              :icon-only="true"
              theme="default"
              :disabled="false"
            />
          </router-link>
          <div class="issue-tl_text">
            <span>{{ issueInfo.engagementName }}</span>
            <EngagementTitle
              :label="issueInfo.name"
              placeholder="Add name..."
              changeLabel="Rename"
              @saveTitle="(val) => (issueInfo.name = val)"
            />
          </div>
        </div>

        <div class="issue-top_right">
          <ButtonComponent
            label="Create Issue"
            :icon-only="false"
            theme="primary"
            :disabled="false"
            @button-clicked="createIssue"
          />
          <ButtonComponent
            label="Cancel"
            :icon-only="false"
            theme="primary-outline"
            :disabled="false"
            @button-clicked="cancelIssue"
          />
        </div>
      </div>
      <TabsComponent class="issue-tabs" :tabs="tabsMenu" />
    </div>
    <div ref="btmWrapper" class="issue-btm__wrapper">
      <HeadingText id="basic-info" label="Basic Info" size="3" />
      <div id="issue-create-issue-name" class="input-flex-col">
        <FormLabel class="input-label" :label="getFieldDefCustomization('name')?.label" />
        <InputText
          reff="txtIssueName"
          class="text-field_asset btm-input"
          placeholder="Enter Name"
          :value="issueInfo.name"
          :disabled="getFieldDefCustomization('name')?.isReadOnly"
          @update="(val) => handleUpdateIssue('name', val)"
        />
      </div>
      <template v-if="!isTemplate">
        <div id="issue-create-asset" class="input-flex-col">
          <FormLabel class="input-label" :label="getFieldDefCustomization('assetName')?.label" />
          <DropdownControl
            theme="outline"
            :options="assetDropdown"
            class="asset-dropdown btm-input"
            reff="txtAssetName"
            :value="issueInfo.assetName"
            :disabled="getFieldDefCustomization('assetName')?.isReadOnly"
            @update="(opt) => handleUpdateIssue('assetId', opt.value)"
          />
        </div>
      </template>
      <div  v-if="showField('description')" id="issue-create-description" class="input-flex-col">
        <FormLabel class="input-label" :label="getFieldDefCustomization('description')?.label" />
        <InputTextArea
          placeholder="Add description"
          :resize="false"
          class="btm-input"
          reff="txtIssueDescription"
          :value="issueInfo.description"
          :disabled="getFieldDefCustomization('description')?.isReadOnly"
          @update="(val) => handleUpdateIssue('description', val)"
        />
      </div>
      <div v-if="showField('location')" id="issue-create-location" class="input-flex-col">
        <FormLabel class="input-label" :label="getFieldDefCustomization('location')?.label" />
        <InputText
          reff="txtIssueLocation"
          class="text-field_asset btm-input"
          placeholder="http://example.com"
          :value="issueInfo.location"
          :disabled="getFieldDefCustomization('location')?.isReadOnly"
          @update="(val) => handleUpdateIssue('location', val)"
        />
      </div>
      <div v-if="showField('locationFull')" id="issue-create-location-full" class="input-flex-col">
        <FormLabel class="input-label" :label="getFieldDefCustomization('locationFull')?.label" />
        <InputText
          reff="txtIssueLocationFull"
          class="text-field_asset btm-input"
          placeholder="http://example.com"
          :value="issueInfo.locationFull"
          :disabled="getFieldDefCustomization('locationFull')?.isReadOnly"
          @update="(val) => handleUpdateIssue('locationFull', val)"
        />
      </div>
      <div v-if="showField('severity')" id="issue-create-severity" class="input-flex-col">
        <FormLabel class="input-label" :label="getFieldDefCustomization('severity')?.label" />
        <DropdownSeverityControl
          :options="severityDropdown"
          default-display="Select Severity"
          theme="default"
          :hide-all="true"
          class="asset-dropdown btm-input"
          reff="txtIssueSeverity"
          :value="issueInfo.severity"
          :disabled="getFieldDefCustomization('severity')?.isReadOnly"
          @update="(opt) => handleUpdateIssue('severity', opt.value)"
        />
      </div>

      <div class="section-break">.</div>

      <HeadingText id="audit" label="Audit" size="3" />

      <div v-if="showField('vendor')" class="input-flex-col">
        <FormLabel class="input-label input-small" :label="getFieldDefCustomization('vendor')?.label" />
        <InputText
          reff="txtIssueVendor"
          class="text-field_asset btm-input"
          placeholder="Enter Vendor"
          :value="issueInfo.vendor"
          :disabled="getFieldDefCustomization('vendor')?.isReadOnly"
          @update="(val) => handleUpdateIssue('vendor', val)"
        />
      </div>
      <div v-if="showField('product')" id="issue-create-product" class="input-flex-col">
        <FormLabel class="input-label input-small" :label="getFieldDefCustomization('product')?.label" />
        <InputText
          reff="txtIssueProduct"
          class="text-field_asset btm-input"
          placeholder="Enter Product"
          :value="issueInfo.product"
          :disabled="getFieldDefCustomization('product')?.isReadOnly"
          @update="(val) => handleUpdateIssue('product', val)"
        />
      </div>

      <div class="section-break">.</div>

      <HeadingText
        v-if="nonHiddenAttributeDefinitions.length > 0"
        id="attributes"
        label="Attributes"
        size="3"
      />
      <template v-if="nonHiddenAttributeDefinitions.length > 0">
        <div
          v-for="(attribute, index) in nonHiddenAttributeDefinitions"
          :id="`issue-create-attributes-${index}`"
          :key="`attribute-${attribute.name}-${index}`"
          class="input-flex-col"
        >
          <FormLabel
            class="input-label input-small md-label"
            :label="getAttributeCustomization(attribute.name)?.label"
          />

          <template v-if="getAttributeField(attribute.type) === 'string'">
            <InputText
              :reff="`attribute-${attribute.name}`"
              class="text-field_asset btm-input"
              :placeholder="attribute.display"
              :value="getAttributeValue(attribute)"
              :disabled="getAttributeCustomization(attribute.name).isReadOnly"
              @update="(val) => handleUpdateAttribute(attribute.name, val)"
            />
          </template>
          <template v-if="getAttributeField(attribute.type) === 'number'">
            <InputNumber
              :reff="`attribute-${attribute.name}`"
              class="text-field_asset btm-input"
              :type="getNumType(attribute.type)"
              :placeholder="attribute.display"
              :value="getAttributeValue(attribute)"
              :disabled="getAttributeCustomization(attribute.name).isReadOnly"
              @update="(val) => handleUpdateAttribute(attribute.name, val)"
            />
          </template>
          <template v-else-if="getAttributeField(attribute.type) === 'text'">
            <InputTextArea
              :placeholder="attribute.display"
              :resize="false"
              :reff="`attribute-${attribute.name}`"
              class="btm-input"
              :value="getAttributeValue(attribute)"
              :disabled="getAttributeCustomization(attribute.name).isReadOnly"
              @update="(val) => handleUpdateAttribute(attribute.name, val)"
            />
          </template>
          <template v-else-if="getAttributeField(attribute.type) === 'date'">
            <InputDate
              :reff="`attribute-${attribute.name}`"
              class="text-field_asset btm-input"
              :value="getAttributeValue(attribute)"
              :disabled="getAttributeCustomization(attribute.name).isReadOnly"
              @update="(val) => handleUpdateAttribute(attribute.name, val)"
            />
          </template>
          <template v-else-if="getAttributeField(attribute.type) === 'select'">
            <DropdownControl
              theme="outline"
              :options="formatAttributeOptions(attribute.options || [])"
              class="assesment-dropdown btm-input"
              :reff="`attribute-${attribute.name}`"
              :disabled="getAttributeCustomization(attribute.name).isReadOnly"
              :value="getAttributeValue(attribute)"
              @update="
                (opt) => handleUpdateAttribute(attribute.name, opt.value)
              "
            />
          </template>
          <template
            v-else-if="getAttributeField(attribute.type) === 'markdown'"
          >
            <MarkdownEditor
              class="md-editor"
              :reff="`markdownAttribute-${attribute.name}`"
              :type="attribute.name"
              :dont-show="markdownLoading"
              :value="getAttributeValue(attribute)"
              :disabled="getAttributeCustomization(attribute.name).isReadOnly"
              @input="(val) => handleUpdateAttribute(attribute.name, val)"
            />
          </template>
          <template
            v-else-if="getAttributeField(attribute.type) === 'multiselect'"
          >
            <MultiSelect
              :options="formatAttributeMultiOptions(attribute.options)"
              :values="getAttributeValue(attribute)"
              :disabled="getAttributeCustomization(attribute.name).isReadOnly"
              @update="(val) => handleUpdateAttribute(attribute.name, val)"
            />
          </template>
        </div>
      </template>

      <div class="section-break" v-if="nonHiddenAttributeDefinitions.length > 0">.</div>

      <HeadingText id="details" label="Details" size="3" />

      <template v-if="!markdownLoading && showField('proof')">
        <div id="issue-create-proof">
          <FormLabel class="input-label input-small md-label" :label="getFieldDefCustomization('proof')?.label" />
          <MarkdownEditor
            class="md-editor"
            reff="markdownProof"
            type="proof"
            :dont-show="markdownLoading"
            :value="issueInfo.proof"
            :disabled="getFieldDefCustomization('proof')?.isReadOnly"
            @input="(data) => handleUpdateIssue('proof', data)"
          />
        </div>
      </template>
      <template v-if="!markdownLoading && showField('details')">
        <div id="issue-create-details">
          <FormLabel class="input-label input-small md-label" :label="getFieldDefCustomization('details')?.label" />
          <MarkdownEditor
            class="md-editor"
            reff="markdownDetails"
            type="details"
            :dont-show="markdownLoading"
            :value="issueInfo.details"
            :disabled="getFieldDefCustomization('details')?.isReadOnly"
            @input="(data) => handleUpdateIssue('details', data)"
          />
        </div>
      </template>
      <template v-if="!markdownLoading && showField('implication')">
        <div id="issue-create-implication">
          <FormLabel
            class="input-label input-small md-label"
            :label="getFieldDefCustomization('implication')?.label"
          />
          <MarkdownEditor
            class="md-editor"
            reff="markdownImplication"
            type="implication"
            :dont-show="markdownLoading"
            :value="issueInfo.implication"
            :disabled="getFieldDefCustomization('implication')?.isReadOnly"
            @input="(data) => handleUpdateIssue('implication', data)"
          />
        </div>
      </template>
      <template v-if="!markdownLoading && showField('recommendation')">
        <div id="issue-create-recommendation">
          <FormLabel
            class="input-label input-small md-label"
            :label="getFieldDefCustomization('recommendation')?.label"
          />
          <MarkdownEditor
            class="md-editor"
            reff="markdownRecommendation"
            type="recommendation"
            :dont-show="markdownLoading"
            :value="issueInfo.recommendation"
            :disabled="getFieldDefCustomization('recommendation')?.isReadOnly"
            @input="(data) => handleUpdateIssue('recommendation', data)"
          />
        </div>
      </template>
      <template v-if="!markdownLoading && showField('references')">
        <div id="issue-create-references">
          <FormLabel
            class="input-label input-small md-label"
            :label="getFieldDefCustomization('references')?.label"
          />
          <MarkdownEditor
            class="md-editor"
            reff="markdownReferences"
            type="references"
            :dont-show="markdownLoading"
            :value="issueInfo.references"
            :disabled="getFieldDefCustomization('references')?.isReadOnly"
            @input="(data) => handleUpdateIssue('references', data)"
          />
        </div>
      </template>

      <template v-if="!markdownLoading && showField('testingInstructions')">

        <div class="section-break">.</div>
        <HeadingText id="testing-instructions" label="Testing" size="3" />

        <div id="issue-create-testing-instructions">
          <FormLabel class="input-label md-label" :label="getFieldDefCustomization('testingInstructions')?.label" />
          <MarkdownEditor
            class="md-editor"
            reff="markdownInstructions"
            type="testingInstructions"
            :dont-show="markdownLoading"
            :value="issueInfo.testingInstructions"
            :disabled="getFieldDefCustomization('testingInstructions')?.isReadOnly"
            @input="(data) => handleUpdateIssue('testingInstructions', data)"
          />
        </div>
      </template>
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
import ButtonComponent from '../../../../components/controls/ButtonComponent'
import IconRightArrow from '../../../../assets/svg/fi_arrow-right.svg?inline'
import IconLeftArrow from '../../../../assets/svg/fi_arrow-left.svg?inline'
import HeadingText from '../../../../components/HeadingText'
import InputText from '../../../../components/controls/InputText'
import InputDate from '../../../../components/controls/InputDate'
import InputNumber from '../../../../components/controls/InputNumber'
import InputTextArea from '../../../../components/controls/InputTextArea'
import DropdownControl from '../../../../components/controls/DropdownControl.vue'
import MultiSelect from '../../../../components/controls/MultiSelect.vue'
import FormLabel from '../../../../components/controls/FormLabel'
import TabsComponent from '../../../../components/controls/TabsComponent'
import MarkdownEditor from '../../../../components/controls/MarkdownEditor.vue'
import DropdownSeverityControl from '../../../../components/controls/DropdownSeverityControl.vue'
import AlertComponent from '../../../../components/AlertComponent'
import isValidUser from '../../../../middleware/is-valid-user'
import LoadingComponent from '../../../../components/LoadingComponent'

export default {
  name: 'IssueCreate',
  components: {
    ButtonComponent,
    HeadingText,
    InputText,
    InputDate,
    InputNumber,
    DropdownControl,
    MultiSelect,
    FormLabel,
    TabsComponent,
    InputTextArea,
    MarkdownEditor,
    DropdownSeverityControl,
    AlertComponent,
    LoadingComponent,
  },
  middleware: isValidUser,
  asyncData({ params }) {
    return {
      id: params.id,
    }
  },
  data() {
    return {
      isTemplate: false,
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
      arrowRight: IconRightArrow,
      arrowLeft: IconLeftArrow,
      commentsLoading: false,
      removeCheck: false,
      activeCheck: false,
      inputActive: true,
      isNew: false,
      isActivePrim: false,
      IsSuppressedPrim: false,
      hasLoaded: false,
      markdownLoading: true,
      primer: {},
      defaultSeverityOptions: [
        {
          display: 'Critical',
          value: 'Critical',
          order: 1,
        },
        {
          display: 'High',
          value: 'High',
          order: 2,
        },
        {
          display: 'Medium',
          value: 'Medium',
          order: 3,
        },
        {
          display: 'Low',
          value: 'Low',
          order: 4,
        },
        {
          display: 'Information',
          value: 'Info',
          order: 5,
        },
      ],
      updatedIssueInfo: {},
      issueInfo: {
        assetId: null,
        assetName: null,
        attributes: {},
        classification: null,
        description: null,
        details: null,
        engagementId: this.$route.params.id,
        engagementName: null,
        enumeration: null,
        foundDate: null,
        implication: null,
        location: null,
        locationFull: null,
        name: null,
        product: null,
        testingInstructions: null,
        proof: null,
        recommendation: null,
        reference: null,
        references: null,
        removedDate: null,
        reportId: null,
        scanId: null,
        severity: null,
        sourceSeverity: null,
        testStatus: null,
        vendor: null,
      },
      radios: [
        {
          label: 'Yes',
          value: 'yes',
          selected: true,
        },
        {
          label: 'No',
          value: 'no',
          selected: false,
        },
      ],
      tabsMenu: [
        { label: 'Basic Info' },
        { label: 'Audit' },
        { label: 'Details' },
        { label: 'Supporting Info' },
        { label: 'Attributes' },
        { label: 'Testing Instructions', openInNewTab: false },
      ],
      severityDropdown: [
        {
          display: 'Critical',
          value: 'Critical',
          order: 1,
        },
        {
          display: 'High',
          value: 'High',
          order: 2,
        },
        {
          display: 'Medium',
          value: 'Medium',
          order: 3,
        },
        {
          display: 'Low',
          value: 'Low',
          order: 4,
        },
        {
          display: 'Information',
          value: 'Info',
          order: 5,
        },
      ],
      pentestDropdown: [
        {
          display: 'PenTest',
          value: 'PenTest',
          order: 1,
        },
      ],
      testedDropdown: [
        {
          display: 'Not Tested',
          value: 'Not Tested',
          order: 1,
        },
        {
          display: 'Not Found',
          value: 'Not Found',
          order: 2,
        },
        {
          display: 'Found',
          value: 'Found',
          order: 3,
        },
        {
          display: 'Out of Scope',
          value: 'Out of Scope',
          order: 4,
        },
      ],
      assetDropdown: [],
      alert: {
        messages: [],
        type: "",
        title: ""
      },
      attributeDefinitions: [],
      nonHiddenAttributeDefinitions: [],
      customIssueFields: [],
      attributeCustomizations: [],
      fieldDefCustomizations: {}
    }
  },
  async fetch({ store: { dispatch } }) {
    await dispatch('configInit')
  },
  head() {
    return {
      title: `Create Issue | ${this.pageTitle}`,
    }
  },
  computed: {
    ...mapState({
      user: (state) => state.modules.user,
      isLoading: (state) => state.loading.loading,
      pageTitle: (state) => state.config.pageTitle
    }),
    formattedAttributeDate() {
      const today = new Date();
      return today.toISOString().split('T')[0];
    }
  },
  mounted() {
    this.getPrimer()
    const today = new Date()
    this.issueInfo.foundDate = today.toISOString().substring(0, 10)
    this.$refs.btmWrapper.scrollTo(0, 0)
  },
  methods: {
    showField(name) {
      if (this.getFieldDefCustomization(name)?.isHidden) {
        return false;
      }

      return true
    },
    getIsDisabled(attr) {
      return attr?.readOnly
    },

    getAttributeCustomization(name) {
      return this.attributeCustomizations.find(x => x.name === name)
    },
    getFieldDefCustomization(name) {
      return this.fieldDefCustomizations[name];
    },
    checkRequiredValue(val) {
      return val == null || (typeof val === 'string' ? val.replace(/<br\s*\/?>/gi, "").replace(/\s+/g, "").trim() === '' : val === '' || JSON.stringify(val) === "[]") || val === []
    },
    allRequiredFieldsComplete() {
      const requiredList = []
      
      // get required fields
      const fieldKeysWithNoValue = Object.keys(this.issueInfo).filter((key) => this.checkRequiredValue(this.issueInfo[key]))
      const requiredFields = fieldKeysWithNoValue.filter((key) => this.fieldDefCustomizations[key]?.isRequired === true).map((key) => this.fieldDefCustomizations[key])
      requiredList.push(...requiredFields)

      // get required attributes
      const requiredAttributes = this.attributeCustomizations.filter((v) => v.isRequired && Object.entries(this.issueInfo.attributes).find(([key, value]) => key === v.name && this.checkRequiredValue(value)))
      requiredList.push(...requiredAttributes)

      if (requiredList.length > 0) {
        const attrNames = requiredList.map(field => `${field.label}`).join(", ");
        const msg = (requiredList.length === 1) ? "This field is required to save" : "These fields are required to save";
        this.addAlert([`${msg}: ${attrNames}`], "danger", "Save Error")
        return false;
      }

      return true;
    },

    getAttributeValue(attribute) {
      const { name } = attribute

      const attr = this.getAttributeCustomization(name)
      
      // get value to set
      const val = attr.value || attr.defaultValue || null;

      const attrType = this.getAttributeField(attribute.type)

      if (!(name in this.issueInfo.attributes)) {
        if (attrType === 'date') {
          this.issueInfo.attributes[name] = val || this.formattedAttributeDate
        } else if (attrType === 'select') {
          this.issueInfo.attributes[name] = val
        } else if (attrType === 'multiselect') {
          this.issueInfo.attributes[name] = val || []
        } else if (attrType === 'number') {
          this.issueInfo.attributes[name] = val || null
        } else {
          this.issueInfo.attributes[name] = val || ''
        }
      }

      if (attrType === 'multiselect') {
        const currentVal = this.issueInfo.attributes[name]
        const opts = attribute?.options || []
        return this.formatMultiSelectValue(currentVal, opts)
      }

      return this.issueInfo.attributes[name]
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
    formatAttributeMultiOptions(opts) {
      return opts || []
    },
    formatAttributeOptions(opts) {
      return [{ display: "Select", value: null },  ...opts.map((o) => {
        return {
          display: o,
          value: o
        }
      })]
    },
    getNumType(attributeType) {
      return attributeType === 'Integer (long)' ? 'int' : 'double'
    },
    handleUpdateAttribute(attributeName, newVal) {
      this.issueInfo.attributes[attributeName] = newVal
    },
    getAttributeField(type) {
      const attr = this.attributeTypes.find((attrType) =>
        attrType.types.includes(type)
      )
      return attr?.field || 'unknown type'
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
    getPrimer() {
      return this.$axios
        .$get(`${this.$store.state.config.api_url}/Issue/new/primer?engagementId=${this.id}`)
        .then((r) => {
          this.issueInfo.assetId = r.data?.assetDropdown[0].assetId
          this.severityDropdown =
            r.data?.severityDropdown || this.defaultSeverityOptions
          this.assetDropdown =
            r.data?.assetDropdown
              .map((option) => {
                return {
                  ...option,
                  display: option.name,
                  value: option.assetId,
                  name: option.name,
                  id: option.assetId
                }
              })
              .sort((a, b) => {
                return a.name.localeCompare(b.name)
              }) || []

          this.attributeDefinitions = r.data?.attributeDefinitions || []
          this.attributeCustomizations = r.data?.issue?.attributes || []
          this.fieldDefCustomizations = r?.data?.issue || {}

          this.issueInfo = Object.fromEntries(
            Object.entries(this.fieldDefCustomizations).map(([key, value]) => {
              if (typeof value === "object" && value !== null && key !== "engagement") {
                const val = value.value || value.defaultValue;
                return [key, val];
              }
              return [key, value];
            })
          );

          const attributes = {}

          this.attributeCustomizations.forEach((attr) => {
            const attrDef = this.attributeDefinitions.find(
              (def) => def.name === attr.name
            )

            let val = (attr.value !== null && attr.value !== '') ? attr.value : attr.defaultValue || "";

            if (attrDef && this.getAttributeField(attrDef.type) === 'multiselect') {
              if (val === null || val === '') val = '[]';
              attributes[attr.name] = JSON.parse(
                val
              )
            }
          })

          this.issueInfo.attributes = attributes;
          
          this.nonHiddenAttributeDefinitions =
             this.attributeDefinitions.filter(def => this.attributeCustomizations.find(x => x.name === def.name && x.isHidden === false))

          this.isTemplate = r.data?.isTemplate ?? false

          if (this.assetDropdown.length > 0) {
            this.issueInfo.assetId = this.assetDropdown[0].value
            this.issueInfo.assetName = this.assetDropdown[0].display
          }

          this.tabsMenu = [
            { label: 'Basic Info' },
            { label: 'Audit' },
            { label: 'Details' },
            { label: 'Supporting Info' },
            { label: 'Attributes' }
          ]
          if (this.showField('testingInstructions')) {
            this.tabsMenu.push({ label: 'Testing Instructions', openInNewTab: false });
          }

          this.markdownLoading = false
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Getting New Issue Primer")
        })
    },
    handleUpdateIssue(field, value) {
      this.issueInfo[field] = value
    },
    handleCommentsLoading(loading = false) {
      this.commentsLoading = loading
    },
    handleCommentsLoaded() {
      this.commentsLoading = false
    },
    createIssue() {

      if (!this.allRequiredFieldsComplete()) return;

      const attributes = Object.assign({}, this.issueInfo.attributes)

      Object.keys(attributes).forEach((attr) => {
        const attrDef = this.attributeDefinitions.find(
          (def) => def.name === attr
        )
        if (attrDef && this.getAttributeField(attrDef.type) === 'multiselect') {
          attributes[attr] = JSON.stringify(
            attributes[attr]
          )
        }
      })

      const body = {
        engagementId: this.$route.params.id,
        name: this.issueInfo?.name || null,
        severity: this.issueInfo?.severity || null,
        assetId: this.issueInfo?.assetId || null,
        attributes: attributes || null,
        foundDate: this.issueInfo?.foundDate || new Date().toISOString().substring(0, 10),
        testStatus: this.issueInfo?.testStatus || 'Found',
        sourceSeverity: this.issueInfo?.severity || null,
        removedDate: null,
        location: this.issueInfo?.location || null,
        locationFull: this.issueInfo?.locationFull || null,
        reportId: `${Date.now()}`,
        classification: this.issueInfo?.classification || null,
        description: this.issueInfo?.description || null,
        enumeration: this.issueInfo?.enumeration || null,
        proof: this.issueInfo?.proof || null,
        details: this.issueInfo?.details || null,
        implication: this.issueInfo?.implication || null,
        recommendation: this.issueInfo?.recommendation || null,
        references: this.issueInfo?.references || null,
        reference: this.issueInfo?.reference || null,
        testingInstructions: this.issueInfo?.testingInstructions || null,
        vendor: this.issueInfo?.vendor || null,
        product: this.issueInfo?.product || null,
      }

      return this.$axios
        .$post(`${this.$store.state.config.api_url}/Issue/new`, JSON.stringify(body), {
          headers: {
            'Content-Type': 'application/json',
          }
        })
        .then((r) => {
          this.addAlert(["Success Creating Issue"], "success", "Success")
          this.$router.push({
            path: `/engagements/${this.$route.params.id}/issues/${r.data.id}`,
          })
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Creating Issue")
        })
    },
    cancelIssue() {
      this.$router.push({
        path: `/engagements/${this.$route.params.id}`,
      })
    },
  },
}
</script>

<style lang="scss" scoped>
.issue-page__wrapper {
  position: fixed;
  top: 56px;
  font-family: $font-primary;
  height: calc(100% - 56px);
  max-width: 1872px;
  width: 100%;

  overflow: hidden;
  border: 1px solid $brand-color-scale-2;

  display: flex;
  flex-flow: column;
}
.issue-page__top {
  padding: 24px;
  padding-bottom: 0px;
  background: $brand-color-scale-1;
  width: 100%;
  position: relative;
}
.issue-page__topBox {
  display: flex;
  gap: 30px;
  align-items: center;
  justify-content: space-between;
}
.issue-top_left {
  display: flex;
  gap: 16px;
  align-items: center;
}
.issue-top_right {
  display: flex;
  align-items: center;
  gap: 30px;
}
.issue-top_right span {
  font-style: normal;
  font-weight: 400;
  font-size: 14px;
  line-height: 18px;
  color: $brand-color-scale-6;
}
.issue-tabs {
  padding-top: 36px;
}
.issue-tl_text {
  font-style: normal;
  font-weight: 400;
  font-size: 14px;
  line-height: 18px;
  color: $brand-color-scale-6;
}
.issue-btm__wrapper {
  position: relative;
  display: flex;
  flex-flow: column;
  justify-content: flex-start;

  padding: 24px 24px 200px 24px;

  width: 100%;
  overflow-x: hidden;
  overflow-y: scroll;
  scroll-behavior: smooth;
}
.issue-btm__left-inner {
  width: 508px;
  margin-right: auto;
  position: relative;
  transition: 5s ease;
  scroll-behavior: smooth;
}
.input-flex {
  display: flex;
  align-items: flex-start;
  margin-top: 24px;
}
.input-flex-col {
  display: flex;
  flex-flow: column;
  flex: 0 0 auto;
  justify-content: flex-start;
  align-items: flex-start;
  margin-top: 24px;
  gap: 12px;
}
.btm-input {
  width: 344px;
}
.btm-input-small {
  left: 40px;
}
.input-small {
  max-width: 124px;
}
.percent {
  position: relative;
  width: fit-content;
  top: 56px;
  left: 62%;
  z-index: 1;
}
.section-break {
  margin-top: 64px;
  margin-bottom: 64px;
  border-bottom: 2px solid $brand-color-scale-2;
  width: 119px;
  height: 1px;
  font-size: 0px;
}
.md-label {
  margin-top: 24px;
  margin-bottom: 8px;
}
.md-editor {
  width: 200%;
}
.md-label:first-of-type {
  margin-top: 32px;
}
.save-btm_btn {
  margin-top: 64px;
}
.issue-page__wrapper::v-deep .dropdownSeverityControl {
  width: 344px;
  max-width: unset;
}
.issue-page__wrapper::v-deep .selectedOption {
  border: 1px solid $brand-color-scale-2;
  width: 344px;
}
</style>
