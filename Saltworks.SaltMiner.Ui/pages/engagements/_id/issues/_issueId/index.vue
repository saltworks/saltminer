<template>
  <div class="issue-page__wrapper">
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
              size="medium"
              :disabled="false"
            />
          </router-link>
          <div class="issue-tl_text">
            <span>{{ issueInfo.engagement?.name || "" }}</span>
            <HeadingText :label="issueInfo.name" size="3" />
            <span>{{ issueActiveText }}</span>
          </div>
        </div>

        <div v-if="!isDisabled" class="issue-top_right">
          <div @click="removeIssue">
            <ButtonComponent
              label="Mark Removed"
              :icon-only="false"
              theme="primary-outline"
              size="medium"
              :disabled="isDisabled || notSaved || actionRoleRestricted('MarkRemoved')"
            />
          </div>
          <div v-if="isTemplate" @click="deleteTemplateIssue">
            <ButtonComponent
              label="Delete Issue"
              :icon-only="false"
              theme="primary-outline"
              size="medium"
              :disabled="isDisabled"
            />
          </div>
          <div @click="cloneIssue">
            <ButtonComponent
              label="Clone Issue"
              :icon-only="false"
              theme="primary"
              size="medium"
              :disabled="isDisabled || notSaved || actionRoleRestricted('Clone')"
            />
          </div>
          <div @click="saveIssue()">
            <ButtonComponent
              label="Save Issue"
              :icon-only="false"
              theme="primary"
              size="medium"
              :disabled="isDisabled"
            />
          </div>
          <div v-if="userLocked" class="user-lock">
            Locked by {{ formattedUserLockMessage }}
          </div>
        </div>
      </div>
      <TabsComponent
        class="issue-tabs"
        :tabs="tabsMenu"
        @infoclick="handleTestingInfoClick"
      />
    </div>
    <div class="issue-btm__wrapper">
      <div id="issue-btm__left" ref="issueWrapper" class="issue-btm__left">
        <div v-show="hasLoaded" class="issue-btm__left-inner">
          <HeadingText id="basic-info" label="Basic Info" size="3" />

          <div v-if="showField('name')" id="issue-name" class="input-flex">
            <FormLabel class="input-label" :label="getFieldDefCustomization('name')?.label" />
            <InputText
              reff="txtIssueName"
              class="text-field_asset btm-input"
              placeholder="Enter Name"
              :value="issueInfo.name || ''"
              :disabled="isDisabled || getFieldDefCustomization('name')?.isReadOnly"
              @update="(val) => handleUpdateIssue('name', val)"
            />
          </div>
          <div id="asset-name" class="input-flex">
            <FormLabel class="input-label" :label="getFieldDefCustomization('assetName')?.label" />
            <DropdownControl
              theme="outline"
              :options="assetDropdown"
              class="asset-dropdown btm-input"
              reff="txtAssetName"
              :value="issueInfo.assetName || ''"
              :disabled="isDisabled || getFieldDefCustomization('assetName')?.isReadOnly"
              @update="(opt) => handleUpdateIssue('assetId', opt.value)"
            />
          </div>
          <div
            v-if="showField('description')"
            id="issue-description"
            class="input-flex"
          >
            <FormLabel class="input-label" :label="getFieldDefCustomization('description')?.label" />
            <InputTextArea
              placeholder="Add description"
              :resize="false"
              class="btm-input"
              reff="txtIssueDescription"
              :value="issueInfo.description || ''"
              :disabled="isDisabled || getFieldDefCustomization('description')?.isReadOnly"
              @update="(val) => handleUpdateIssue('description', val)"
            />
          </div>
          <div
            v-if="showField('location')"
            id="issue-location"
            class="input-flex"
          >
            <FormLabel class="input-label" :label="getFieldDefCustomization('location')?.label" />
            <InputText
              reff="txtIssueLocation"
              class="text-field_asset btm-input"
              placeholder="http://example.com"
              :value="issueInfo.location || ''"
              :disabled="isDisabled || getFieldDefCustomization('location')?.isReadOnly"
              @update="(val) => handleUpdateIssue('location', val)"
            />
          </div>
          <div
            v-if="showField('locationFull')"
            id="issue-location-full"
            class="input-flex"
          >
            <FormLabel class="input-label" :label="getFieldDefCustomization('locationFull')?.label" />
            <InputText
              reff="txtIssueLocationFull"
              class="text-field_asset btm-input"
              placeholder="http://example.com"
              :value="issueInfo.locationFull || ''"
              :disabled="isDisabled || getFieldDefCustomization('locationFull')?.isReadOnly"
              @update="(val) => handleUpdateIssue('locationFull', val)"
            />
          </div>
          <div  v-if="showField('severity')"
            id="issue-severity" class="input-flex">
            <FormLabel class="input-label" :label="getFieldDefCustomization('severity')?.label" />
            <DropdownSeverityControl
              :options="formattedSeverityDropdownOptions"
              default-display="Select Severity"
              :hide-all="true"
              theme="default"
              class="asset-dropdown btm-input"
              reff="txtIssueSeverity"
              :disabled="isDisabled || getFieldDefCustomization('severity')?.isReadOnly"
              @update="(opt) => handleUpdateIssue('severity', opt.value)"
            />
          </div>

          <div class="section-break">.</div>

          <HeadingText id="audit" label="Audit" size="3" />

          <div  v-if="showField('testStatus')"
            id="tested-name" class="input-flex">
            <FormLabel class="input-label" :label="getFieldDefCustomization('testStatus')?.label" />
            <DropdownControl
              theme="outline"
              :options="testStatusDropdown"
              class="testStatus-dropdown btm-input"
              reff="txtIssueTestStatus"
              :disabled="isDisabled || getFieldDefCustomization('testStatus')?.isReadOnly"
              @update="(opt) => handleUpdateIssue('testStatus', opt.value)"
            />
          </div>

          <div
            v-if="showField('isSuppressed')"
            id="issue-suppressed"
            class="input-flex"
          >
            <FormLabel class="input-label" :label="getFieldDefCustomization('isSuppressed')?.label" />
            <InputCheckbox
              class="remove-checkbox btm-input"
              :checked="issueInfo.isSuppressed || false"
              :size="'default'"
              :disabled="isDisabled || getFieldDefCustomization('isSuppressed')?.isReadOnly"
              @input="
                () => (issueInfo.isSuppressed = !issueInfo.isSuppressed)
              "
            />
          </div>
          <div id="issue-removed-date" class="input-flex">
            <FormLabel class="input-label input-small" :label="getFieldDefCustomization('removedDate')?.label" />
            <InputDate
              reff="txtIssueRemovedDate"
              class="text-field_asset btm-input"
              :value="issueInfo.removedDate"
              :disabled="isDisabled || getFieldDefCustomization('removedDate')?.isReadOnly"
              @update="(val) => handleUpdateIssue('removedDate', val)"
            />
          </div>
          <div
            v-if="showField('vendor')"
            id="issue-vendor"
            class="input-flex"
          >
            <FormLabel class="input-label input-small" :label="getFieldDefCustomization('vendor')?.label" />
            <InputText
              reff="txtIssueVendor"
              class="text-field_asset btm-input"
              placeholder="Enter Vendor"
              :value="issueInfo.vendor || ''"
              :disabled="isDisabled || getFieldDefCustomization('vendor')?.isReadOnly"
              @update="(val) => handleUpdateIssue('vendor', val)"
            />
          </div>
          <div
            v-if="showField('product')"
            id="issue-product"
            class="input-flex"
          >
            <FormLabel class="input-label input-small" :label="getFieldDefCustomization('product')?.label" />
            <InputText
              reff="txtIssueProduct"
              class="text-field_asset btm-input"
              placeholder="Enter Product"
              :value="issueInfo.product"
              :disabled="isDisabled || getFieldDefCustomization('product')?.isReadOnly"
              @update="(val) => handleUpdateIssue('product', val)"
            />
          </div>

          <div v-if="showDetailsHeader" class="section-break">.</div>

          <HeadingText
            v-if="nonHiddenAttributeDefinitions.length > 0"
            id="attributes"
            label="Attributes"
            size="3"
          />
          <template v-if="nonHiddenAttributeDefinitions.length > 0">
            <div
              v-for="(attribute, index) in nonHiddenAttributeDefinitions"
              :id="`issue-attributes-${index}`"
              :key="`attribute-${attribute.name}-${index}`"
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
                  :disabled="isDisabled || getAttributeCustomization(attribute.name)?.isReadOnly"
                  @update="
                    (val) => handleUpdateAttribute(attribute.name, val)
                  "
                />
              </template>
              <template v-if="getAttributeField(attribute.type) === 'number'">
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
                  :options="formatAttributeOptions(attribute.options || [])"
                  class="assesment-dropdown btm-input"
                  :reff="`attribute-${attribute.name}`"
                  :disabled="isDisabled || getAttributeCustomization(attribute.name)?.isReadOnly"
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
                  :config="$store.state.config"
                  :type="attribute.name"
                  :dont-show="markdownLoading"
                  :value="getAttributeValue(attribute)"
                  :disabled="isDisabled || getAttributeCustomization(attribute.name)?.isReadOnly"
                  @input="(val) => handleUpdateAttribute(attribute.name, val)"
                />
              </template>
              <template
                v-else-if="getAttributeField(attribute.type) === 'multiselect'"
              >
                <MultiSelect
                  :options="formatAttributeMultiOptions(attribute.options)"
                  :values="getAttributeValue(attribute)"
                  :disabled="isDisabled || getAttributeCustomization(attribute.name)?.isReadOnly"
                  @update="
                    (val) => handleUpdateAttribute(attribute.name, val)
                  "
                />
              </template>
            </div>
          </template>

          <div v-if="showDetailsHeader && nonHiddenAttributeDefinitions.length > 0" class="section-break">.</div>

          <HeadingText
            v-if="showDetailsHeader"
            id="details"
            label="Details"
            size="3"
          />
          <template v-if="!markdownLoading && showField('proof')">
            <div id="issue-proof">
              <FormLabel
                class="input-label input-small md-label"
                :label="getFieldDefCustomization('proof')?.label"
              />
              <MarkdownEditor
                class="md-editor"
                reff="markdownProof"
                type="proof"
                :config="$store.state.config"
                :dont-show="markdownLoading"
                :value="issueInfo.proof"
                :disabled="isDisabled || getFieldDefCustomization('proof')?.isReadOnly"
                @input="(data) => handleUpdateIssue('proof', data)"
              />
            </div>
          </template>
          <template v-if="!markdownLoading && showField('details')">
            <div id="issue-details">
              <FormLabel
                class="input-label input-small md-label"
                :label="getFieldDefCustomization('details')?.label"
              />
              <MarkdownEditor
                class="md-editor"
                reff="markdownDetails"
                type="details"
                :config="$store.state.config"
                :dont-show="markdownLoading"
                :value="issueInfo.details"
                :disabled="isDisabled || getFieldDefCustomization('details')?.isReadOnly"
                @input="(data) => handleUpdateIssue('details', data)"
              />
            </div>
          </template>

          <template v-if="!markdownLoading && showField('implication')">
            <div id="issue-implication">
              <FormLabel
                class="input-label input-small md-label"
                :label="getFieldDefCustomization('implication')?.label"
              />
              <MarkdownEditor
                class="md-editor"
                reff="markdownImplication"
                type="implication"
                :config="$store.state.config"
                :dont-show="markdownLoading"
                :value="issueInfo.implication"
                :disabled="isDisabled || getFieldDefCustomization('implication')?.isReadOnly"
                @input="(data) => handleUpdateIssue('implication', data)"
              />
            </div>
          </template>

          <template v-if="!markdownLoading && showField('recommendation')">
            <div id="issue-recommendation">
              <FormLabel
                class="input-label input-small md-label"
                :label="getFieldDefCustomization('recommendation')?.label"
              />
              <MarkdownEditor
                class="md-editor"
                reff="markdownRecommendation"
                type="recommendation"
                :config="$store.state.config"
                :dont-show="markdownLoading"
                :value="issueInfo.recommendation"
                :disabled="isDisabled || getFieldDefCustomization('recommendation')?.isReadOnly"
                @input="(data) => handleUpdateIssue('recommendation', data)"
              />
            </div>
          </template>

          <template v-if="!markdownLoading && showField('references')">
            <div id="issue-references">
              <FormLabel
                class="input-label input-small md-label"
                :label="getFieldDefCustomization('references')?.label"
              />
              <MarkdownEditor
                class="md-editor"
                reff="markdownReferences"
                type="references"
                :config="$store.state.config"
                :dont-show="markdownLoading"
                :value="issueInfo.references"
                :disabled="isDisabled || getFieldDefCustomization('references')?.isReadOnly"
                @input="(data) => handleUpdateIssue('references', data)"
              />
            </div>
          </template>

          <template v-if="!markdownLoading && showField('testingInstructions')">
            <div class="section-break">.</div>

            <HeadingText id="testing-instructions" label="Testing" size="3" />
            <div id="issue-testing-instructions">
              <FormLabel
                class="input-label md-label"
                :label="getFieldDefCustomization('testingInstructions')?.label"
              />
              <MarkdownEditor
                class="md-editor"
                reff="markdownInstructions"
                type="testingInstructions"
                :config="$store.state.config"
                :dont-show="markdownLoading"
                :value="issueInfo.testingInstructions"
                :disabled="isDisabled || getFieldDefCustomization('testingInstructions')?.isReadOnly"
                @input="
                  (data) => handleUpdateIssue('testingInstructions', data)
                "
              />
            </div>
          </template>

          <div class="section-break">.</div>

          <HeadingText
            id="supporting-info"
            label="Supporting Info"
            size="3"
          />

          <div id="issue-attachments" class="input-attachments">
            <AttachmentList class="attachment-input" 
              :files="issueInfo.attachments"
              :disabled="isDisabled"
              :validFileExtensions="validFileExtensions"
              @file-added="handleFileAdded"
              @file-not-found="handleFileNotFound"
              @file-removed="handleFileRemoved"
            />
          </div>
          <div v-if="!isDisabled" id="issue-save-bottom" @click="saveIssue()">
            <ButtonComponent
              label="Save Issue"
              :icon-only="false"
              theme="primary"
              size="medium"
              class="save-btm_btn"
              :disabled="isDisabled"
              @click="saveIssue()"
            />
          </div>
        </div>
      </div>
      <div class="issue-btm__right">
        <div v-if="loadComments" id="issue-comments" class="comment-section">
          <CommentSection
            title="Comments"
            :config="$store.state.config"
            :author="user.userFullName || 'Unknown'"
            :issue-id="issueId"
            :engagement-id="id"
            :engagement-status="engagement?.status"
            :engagement-timestamp="engagement?.timestamp"
            :asset-id="issueInfo.assetId"
            :scan-id="issueInfo.scanId"
            :loading="commentsLoading"
            :disabled="isDisabled"
            @comments-loading="handleCommentsLoading"
            @comments-loaded="handleCommentsLoaded"
            @comment-added="handleCommentAdded"
          />
        </div>
      </div>
    </div>

    <LoadingComponent :is-visible="isLoading" />
    <ModalComponent v-if="!loggedIn" size="small">
      <SessionTimeoutModalVue
        :login-url="loginUrl"
        title="Warning: Session Expired"
        message="You have been logged out due to inactivity. Please log in again to continue."
      />
    </ModalComponent>
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
import ButtonComponent from '../../../../../components/controls/ButtonComponent'
import IconRightArrow from '../../../../../assets/svg/fi_arrow-right.svg?inline'
import IconLeftArrow from '../../../../../assets/svg/fi_arrow-left.svg?inline'
import HeadingText from '../../../../../components/HeadingText'
import InputText from '../../../../../components/controls/InputText'
import InputNumber from '../../../../../components/controls/InputNumber'
import InputDate from '../../../../../components/controls/InputDate'
import InputTextArea from '../../../../../components/controls/InputTextArea'
import DropdownControl from '../../../../../components/controls/DropdownControl.vue'
import MultiSelect from '../../../../../components/controls/MultiSelect.vue'
import ModalComponent from '../../../../../components/controls/ModalComponent.vue'
import SessionTimeoutModalVue from '../../../../../components/SessionTimeoutModal.vue'
import InputCheckbox from '../../../../../components/controls/InputCheckbox'
import FormLabel from '../../../../../components/controls/FormLabel'
import TabsComponent from '../../../../../components/controls/TabsComponent'
import CommentSection from '../../../../../components/CommentSection.vue'
import MarkdownEditor from '../../../../../components/controls/MarkdownEditor.vue'
import AttachmentList from '../../../../../components/AttachmentList.vue'
import DropdownSeverityControl from '../../../../../components/controls/DropdownSeverityControl.vue'
import AlertComponent from '../../../../../components/AlertComponent'
import isValidUser from '../../../../../middleware/is-valid-user'
import LoadingComponent from '../../../../../components/LoadingComponent'
import ConfirmDialog from '../../../../../components/ConfirmDialog.vue';

export default{
  name: 'IssueShow',
  components: {
    ButtonComponent,
    HeadingText,
    InputText,
    InputNumber,
    InputDate,
    DropdownControl,
    MultiSelect,
    ModalComponent,
    SessionTimeoutModalVue,
    InputCheckbox,
    FormLabel,
    TabsComponent,
    CommentSection,
    InputTextArea,
    // Percent,
    MarkdownEditor,
    AttachmentList,
    DropdownSeverityControl,
    AlertComponent,
    LoadingComponent,
    ConfirmDialog
  },
  middleware: isValidUser,
  asyncData({ params }) {
    return {
      id: params.id,
      issueId: params.issueId,
    }
  },
  data() {
    return {
      configFile: '',
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
      leavePage: false,
      commentsLoading: false,
      removeCheck: false,
      activeCheck: false,
      inputActive: true,
      isNew: false,
      isActivePrim: false,
      IsSuppressedPrim: false,
      loginUrl: "",
      hasLoaded: false, // Change to true after we finish loading in data for first time.
      primer: {},
      defaultSeverityOptions: [
        {
          display: 'Critical',
          value: 'critical',
        },
        {
          display: 'High',
          value: 'high',
        },
        {
          display: 'Medium',
          value: 'medium',
        },
        {
          display: 'Low',
          value: 'low',
        },
        {
          display: 'Info',
          value: 'info',
        },
      ],
      updatedIssueInfo: {},
      issueInfoCopy: {},
      issueInfo: {
        assetId: '',
        assetName: '',
        attributes: [],
        attachments: [],
        classification: '',
        description: '',
        details: '1',
        engagementId: '',
        engagementName: '',
        enumeration: '',
        foundDate: '',
        implication: '1',
        isActive: false,
        isSuppressed: false,
        id: this.$route.params.issue,
        location: '',
        locationFull: '',
        name: '',
        product: '',
        proof: '1',
        recommendation: '1',
        reference: '',
        references: '1',
        removedDate: '',
        reportId: '',
        scanId: '',
        severity: '',
        sourceSeverity: '',
        testingInstructions: '# Testing Instructions',
        testStatus: '',
        vendor: '',
      },
      issueActiveText: '',
      assetOptions: [
        {
          label: 'Select Asset',
          value: '0',
        },
      ],
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
      tabsMenu: [],
      severityDropdown: [],
      assetDropdown: [],
      testStatusDropdown: [],
      lockInfo: {},
      markdownLoading: true,
      engagement: {
        status: 'Published',
      },
      alert: {
        messages: [],
        type: "",
        title: ""
      },
      heartbeat: null,
      attributeDefinitions: [],
      attributeCustomizations: [],
      fieldDefCustomizations: {},
      actionRestrictions: [],
      mappedIssues: {},
      nonHiddenAttributeDefinitions: [],
      userLocked: false,
      userLockMessage: '',
      loggedIn: true,
      validFileExtensions: "",
      issuesThatRequireComments: [],
      commentAdded: false
    }
  },
  async fetch({ store: { dispatch } }) {
    await dispatch('configInit')
  },
  async beforeRouteLeave(to, from, next) {
    if (this.notSaved) {
      const confirmed = await this.$refs.confirmDialog.show("Do you want to leave this page and lose the unsaved changes?", "Unsaved Changes");
      if (confirmed) {
        next();
      }
    } else {
      next();
    }
  },
  head() {
    return {
      title: `${this.issueInfo.name} | ${this.pageTitle}`,
    }
  },
  computed: {
    ...mapState({
      user: (state) => state.modules.user,
      isLoading: (state) => state.loading.loading,
      pageTitle: (state) => state.config.pageTitle,
    }),
    referrer() {
      return this.$store.state.referrer;
    },
    formattedAttributeDate() {
      const today = new Date();
      return today.toISOString().split('T')[0];
    },
    formattedSeverityDropdownOptions() {
      const result = [
        ...(this.severityDropdown.map((option) => {
          return {
            display: option.display,
            value: option.value,
            selected: option.value.toLowerCase() === this.issueInfo.severity.toLowerCase(),
          }
        }) || []),
      ]

      return result
    },
    formattedUserLockMessage() {
      const words = this.userLockMessage.split(' ')
      const lastword = words[words.length - 1].replace('.', '')
      return lastword
    },
    showDetailsHeader() {
      return (
        this.showField('details') ||
        this.showField('implication') ||
        this.showField('recommendation') ||
        this.showField('references') ||
        this.showField('proof') ||
        this.showField('testingInstructions')
      ) 
    },
    loadComments() {
      return (
        this.issueId &&
        this.id &&
        this.issueInfo.scanId &&
        this.issueInfo.assetId
      )
    },
    isDisabled() {
      return (
        this.isDraft || this.isLocked || !this.loggedIn
      )
    },
    notSaved() {
      return (this.issueInfo?.engagement?.id === '' || this.issueInfo?.engagement?.id === null);
    },
    isDraft() {
      return this.engagement?.status !== 'Draft';
    },
    isLocked() {
      if(this.lockInfo == null){
        return false
      }
      if(this.lockInfo.user == null)
      {
        return false;
      }

      if(this.lockInfo.user === this.user.userName){
        return false;
      }
      else if(this.lockInfo.expires < new Date())
      {
        return false;
      }
      else {
        return true;
      }
    },
    isTemplate() {
      return (
        this.engagement.subtype === "Template"
      ) 
    },
  },
  async mounted() {
    this.configFile = await fetch('/smpgui/config.json').then((res) => res.json())
    this.loginUrl = this.$store.state.config.login_url
    if(this.$store.state.config.login_url.startsWith('/')){
      this.loginUrl = window.location.origin + this.$store.state.config.login_url
    }

    this.getEngagement(() => {
      this.getPrimer()
      this.handleHeartbeat()
    })

    if (this.heartbeat) {
      clearInterval(this.heartbeat)
    }

    this.heartbeat = setInterval(() => {
      if(window.location.href.includes(this.issueId))
      {
        this.handleHeartbeat()
      } else {
        clearInterval(this.heartbeat)
      }
    }, 30000) 
  },
  methods: {
    async handleHeartbeat() {
      if(this.engagement?.status === 'Draft')
      {
        const locked = this.isLocked;
        return await this.$axios
        .$get(`${this.$store.state.config.api_url}/Auth/cookie`)
        .then((r) => {
          this.loggedIn = window.location.href.includes("localhost") || r?.data?.length > 3
          this.$axios
          .$get( `${this.$store.state.config.api_url}/Issue/${this.issueId}/edit/refresh`)
            .then((r) => {
              if (r?.data) {
                this.lockInfo = r.data
              }

              if(locked && !this.isLocked){
                this.addAlert(["This issue has been unlocked and now is editable."], "warning", "Issue Unlocked")
              }
            })
            .catch((error) => {
              this.handleErrorResponse(error, "Error Getting Heartbeat")
            })
        })
      }
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
    actionRoleRestricted(val) {
      return this.actionRestrictions.includes(val)
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
    checkRequiredValue(val) {
      return val == null || (typeof val === 'string' ? val.replace(/<br\s*\/?>/gi, "").replace(/\s+/g, "").trim() === '' : val === '' || JSON.stringify(val) === "[]") || val === []
    },
    fieldNeedsComment(val) {
      let oldValue = '';
      let newValue = '';
      
      if (val in this.issueInfo) {
        oldValue = this.issueInfoCopy[val]
        newValue = this.issueInfo[val] === '' ? null : this.issueInfo[val]

        // clearing a previous removed date does not need comment
        if (val === "removedDate" && (newValue === null || newValue === '')) return false

        return oldValue !== newValue
      }
      return false
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

      if (this.issuesThatRequireComments && this.issuesThatRequireComments.length > 0 && !this.commentAdded) {
        const needsCommentList = this.issuesThatRequireComments.filter((x) => this.fieldNeedsComment(x)).map(name => `${this.getFieldDefCustomization(name)?.label}`).join(", ")
        if (needsCommentList.length > 0) {
          this.addAlert([`Please add a comment when modifying these fields: ${needsCommentList}`], "danger", "Save Error")
          return false;
        }
      }

      return true;
    },

    getAttributeCustomization(name) {
      return this.attributeCustomizations.find(x => x.name === name)
    },
    getFieldDefCustomization(name) {
      return this.fieldDefCustomizations[name];
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
    checkActiveIssue() {
      return this.issueInfo.isActive
    },
    showField(name) {
      if (this.getFieldDefCustomization(name)?.isHidden) {
        return false;
      }

      return true
    },
    handleTestingInfoClick() {
      if (this.notSaved) return;
      window.open(`/smpgui/engagements/${this.issueInfo.engagement.id}/issues/${this.issueInfo.id}/testingInstructions`, '_blank')
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
    handleUpdateIssue(field, value) {
      if (this.isDisabled) return

      this.issueInfo[field] = value
    },
    updateAttachments(attachmentList) {
      if (this.isDisabled) return

      this.issueInfo.attachments = attachmentList.reduce((acc, curr) => {
        if (!acc.some((a) => a.fileId === curr.fileId)) acc.push(curr)
        return acc
      }, [])
      
      this.$forceUpdate()
      
      return this.$axios
        .post(`${this.$store.state.config.api_url}/issue/${this.issueId}/engagement/${this.id}/attachments`, JSON.stringify(this.issueInfo.attachments), {
          headers: {
            'Content-Type': 'application/json',
            accept: 'application/json',
          }
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Updating Issue Attachments")
        })
    },
    handleFileAdded(filedata) {
      if (this.isDisabled) return

      const info = {
        fileId: filedata.fileId,
        fileName: filedata.fileName,
      }
      const attachmentList = [info, ...this.issueInfo.attachments]
      this.updateAttachments(attachmentList)
    }, 
    handleFileNotFound(fileData) {
      this.addAlert(["File Not Found"], "danger", fileData)
    },
    handleFileRemoved(filedata) {
      const attachmentList = this.issueInfo.attachments.filter(
        (a) => a.fileId !== filedata.fileId
      )
      this.updateAttachments(attachmentList)
    },
    handleCommentsLoading(loading = false) {
      this.commentsLoading = loading
    },
    handleCommentsLoaded() {
      this.commentsLoading = false
    },
    handleCommentAdded() {
      this.commentAdded = true
    },
    handleErrorResponse(r, title) {
      this.errorResponse = this.$getErrorResponse(r)

      // redirect back to engagements with this error
      // it means nothing will load on the page anyway (fatal)

      if (this.errorResponse.some(i => i.includes("Id not present"))) {
        this.$router.push({
          path: `/engagements/${this.$route.params.id}`,
        })
      }

      if (this.errorResponse != null) {
        this.addAlert(this.errorResponse, "danger", title)
      }
    },
   async getEngagement(callback) {
      return await this.$axios
        .$get(`${this.$store.state.config.api_url}/Engagement/${this.id}/summary`)
        .then((r) => {
          if (r?.data) {
            this.engagement = r.data
          }
          if (typeof callback === 'function') callback()
        })
        .catch((error) => {
          return this.handleErrorResponse(error, "Error Getting Engagement Summary")
        })
    },
    async getPrimer() {
      return await this.$axios
        .$get(`${this.$store.state.config.api_url}/Issue/${this.issueId}/edit/primer`)
        .then((r) => {
          this.lockInfo = r.data?.lockInfo
          if(this.isLocked){
            this.addAlert(["This issue is currently being viewed by user '" + this.lockInfo.user + "' and is locked."], "warning", "Issue Currently Locked")
          }

          this.assetDropdown =
            r.data?.assetDropdown
              .map((option) => {
                return {
                  ...option,
                  display: option.name,
                  value: option.assetId,
                  name: option.name,
                  id: option.assetId,
                  selected: option.assetId === r.data?.issue.assetId
                }
              })
              .sort((a, b) => {
                return a.name.localeCompare(b.name)
              }) || []

          this.actionRestrictions = r?.data?.actionRestrictions

          const attributes = {}

          this.fieldDefCustomizations = r?.data?.issue || {}

          this.mappedIssues = Object.fromEntries(
            Object.entries(this.fieldDefCustomizations).map(([key, value]) => {
              if (typeof value === "object" && value !== null && key !== "engagement") {
                return [key, value.value || value.defaultValue];
              }
              return [key, value];
            })
          );

          this.issueInfo =
            r?.data?.issue?.attributes === null
              ? { ...this.mappedIssues, attributes }
              : this.mappedIssues

          this.attributeDefinitions = r.data?.attributeDefinitions || []
          this.attributeCustomizations = r.data?.issue?.attributes || []
          this.validFileExtensions = r?.data?.validFileExtensions.join(',')
          this.issuesThatRequireComments = r?.data?.issueFieldsThatRequireComments || []

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
            } else {
              attributes[attr.name] = val;
            }
          })

          this.issueInfo.attributes = attributes;

          this.nonHiddenAttributeDefinitions =
              this.attributeDefinitions.filter(def => this.attributeCustomizations.find(x => x.name === def.name && x.isHidden === false))

          this.issueInfo.attachments = r?.data?.attachments !== null ?
            r.data.attachments.map(function(attachment) { 
              return attachment.attachment; 
            }) : []

          this.issueInfo.assetName = this.assetDropdown.find(
            (x) => x.id === this.issueInfo.assetId
          )?.name
          if (this.issueInfo.attributes === null)
            this.issueInfo.attributes = {}
          this.severityDropdown =
            r.data?.severityDropdown || this.defaultSeverityOptions
          this.testStatusDropdown =
            r.data?.testedDropdowns
              ?.map((option) => {
                return {
                  ...option,
                  display: option.display,
                  value: option.value,
                  order: option.order,
                  selected: this.issueInfo.testStatus === option.value
                }
              })
              .sort((a, b) => {
                return a.order - b.order
              }) || []
          this.issueActiveText = this.issueInfo.isActive
            ? 'Active'
            : 'Not Active'

          this.tabsMenu = [
            { label: 'Basic Info' },
            { label: 'Audit' },
            { label: 'Details' },
            { label: 'Supporting Info' },
            { label: 'Attributes' }
          ]
          if (this.showField('testingInstructions')) {
            this.tabsMenu.push({ label: 'Testing Instructions', openInNewTab: true, linkUrl: `/smpgui/engagements/${this.issueInfo.engagement.id}/issues/${this.issueInfo.id}/testingInstructions` });
          }

          this.hasLoaded = true
          this.markdownLoading = false

          // shallow copy to have access to original values loaded
          this.issueInfoCopy = {...this.issueInfo}

          // when editing a newly added template issue, remove the engagement id for the issue and save
          // this will disassociate from engagement until the issue can be validated and saved
          // On save and after validation, the engagement id will be added back to the update, finalizing adding the template
          // This was done to allow the template issue to be copied and created into a new issue
          // but still allow it to be edited and validated with required fields before permanent save
          // If it doesn't get saved, the issue will be orphaned with no engagement
          // and removed by a separate clean up process.

          if (this.referrer === "engagements-id" && this.$route.query.q === "templateAdd") {
            if (this.issueInfo.engagement.customer === "Template" && !this.isTemplate ) {
              this.issueInfo.engagement.id = null;
              this.saveIssue(true)
            }
          }
        })
        .catch((error) => {
          return this.handleErrorResponse(error, "Error Getting Issue Edit Primer");
        })
    },
    async saveIssue(skipValidation=false) {
      if (!skipValidation) {
        if (!this.allRequiredFieldsComplete()) return;
        this.issueInfo.engagement.id = this.$route.params.id;
      }
      
      if (this.isDisabled) return

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
        engagementId: this.issueInfo?.engagement.id || null,
        name: this.issueInfo?.name || null,
        severity: this.issueInfo?.severity || null,
        assetId: this.issueInfo?.assetId || null,
        attributes: attributes || null,
        attachments: this.issueInfo?.attachments || null,
        foundDate: this.issueInfo?.foundDate || null,
        testStatus: this.issueInfo?.testStatus || null,
        isActive: this.issueInfo?.isActive || false,
        isSuppressed: this.issueInfo?.isSuppressed || false,
        isRemoved: this.issueInfo?.isRemoved || false,
        id: this.issueInfo?.id || null,
        sourceSeverity: this.issueInfo?.severity || null,
        removedDate: this.issueInfo?.removedDate || null,
        location: this.issueInfo?.location || null,
        locationFull: this.issueInfo?.locationFull || null,
        reportId: this.issueInfo?.reportId || null,
        classification: this.issueInfo?.classification || null,
        description: this.issueInfo?.description || null,
        enumeration: this.issueInfo?.enumeration || null,
        proof: this.issueInfo?.proof || null,
        details: this.issueInfo?.details || null,
        implication: this.issueInfo?.implication || null,
        recommendation: this.issueInfo?.recommendation || null,
        references: this.issueInfo?.references || null,
        reference: this.issueInfo?.reference || null,
        vendor: this.issueInfo?.vendor || null,
        product: this.issueInfo?.product || null,
        testingInstructions: this.issueInfo?.testingInstructions || null,
      }
      return await this.$axios
        .$post(`${this.$store.state.config.api_url}/Issue/edit`,  JSON.stringify(body), {
          headers: {
            'Content-Type': 'application/json',
          }
        })
        .then((r) => {
          if (!skipValidation) {
            this.addAlert(["Success Saving Issue"], "success", "Success")
          }
          
          this.issueActiveText = r.data.isActive ? 'Active' : 'Not Active'
          // this.$refs.issueWrapper.scrollTop = 0
        })
        .catch((error) => {
          return this.handleErrorResponse(error, "Error Saving Issue")
        })
    },
    async cloneIssue() {
      if (this.notSaved || this.actionRoleRestricted('Clone')) return;

      if (!this.allRequiredFieldsComplete()) return;

      const confirm = await this.$refs.confirmDialog.show("Are you sure you want to clone this issue?", "Confirm Clone");
      if (confirm) {
        return await this.$axios
          .$post( `${this.$store.state.config.api_url}/Issue/clone/${this.issueInfo.id}`, null, {
            headers: {
              'Content-Type': 'application/json',
            }
          })
          .then((r) => {
            this.addAlert(["Success Cloning Issue"], "success", "Success")
            this.$router.push({
              path: `/engagements/${this.$route.params.id}/issues/${r.data.id}`,
            })
          })
          .catch((error) => {
            return this.handleErrorResponse(error, "Error Cloning Issue")
          })
      }
    },
    async removeIssue() {
      if (this.notSaved || this.actionRoleRestricted('MarkRemoved')) return;

      const post = {
        engagementId: this.issueInfo.engagement.id,
        ids: [this.issueInfo.id],
      }
      return await this.$axios
        .$post(`${this.$store.state.config.api_url}/Issue/remove`, JSON.stringify(post), {
          headers: {
            'Content-Type': 'application/json',
          }
        })
        .then((r) => {
          this.addAlert(["Success Removing Issue"], "success", "Success")
          this.$router.push({
            path: `/engagements/${this.issueInfo.engagement.id}`,
          })
        })
        .catch((error) => {
          return this.handleErrorResponse(error, "Error Removing Issue")
        })
    },
    async deleteTemplateIssue() {
      if (this.isDisabled) return

      const confirm = await this.$refs.confirmDialog.show("Are you sure you want to delete this template issue?", "Confirm Delete");
      if (confirm) {
        return await this.$axios
        .$delete(`${this.$store.state.config.api_url}/Issue/template/${this.issueInfo.id}`, {
          headers: {
          accept: 'application/json',
          }
        })
        .then((r) => {
          this.addAlert(["Success Deleting Issue"], "success", "Success")
          this.$router.push({
          path: `/engagements/${this.issueInfo.engagement.id}`,
          })
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Deleting Issue")
        })
      }
    }
  },
}
</script>

<style lang="scss" scoped>
.issue-page__wrapper {
  font-family: $font-primary;
  height: 100%;
  max-width: 1872px;
  width: 100%;
  position: fixed;
  top: 56px;
  overflow: hidden;
  border: 1px solid $brand-color-scale-2;
}
.issue-page__top {
  padding: 24px;
  padding-bottom: 0px;
  background: $brand-color-scale-1;
  margin-left: auto;
  width: 100%;
  position: sticky;
}
.issue-page__topBox {
  display: flex;
  gap: 30px;
  align-items: center;
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
  margin-left: auto;
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
  display: flex;
}
.issue-btm__left {
  padding: 24px;
  height: calc(100vh - 206px);
  width: 67%;
  overflow: scroll;
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
.input-attachments {
  display: flex;
  flex-flow: column;
  align-items: flex-start;
  margin-top: 24px;
}
.attachment-input {
  width: 344px;
}
.btm-input {
  width: 344px;
  margin-left: auto;
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
.issue-btm__right {
  margin-left: auto;
  width: 33%;
  height: calc(100vh - 206px);
  background: $brand-color-scale-1;
  border: 1px solid $brand-color-scale-2;
  padding: 24px;
  overflow: scroll;
}
.issue-page__wrapper .dropdownSeverityControl {
  width: 344px;
  min-width: unset;
  margin-left: 100px;
}
.issue-page__wrapper .selectedOption {
  border: 1px solid $brand-color-scale-2;
}
</style>
