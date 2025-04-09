<template>
  <div class="import-page_wrapper">
    <LoadingComponent :is-visible="isLoading" />
    <div v-if="submitted !== true" class="import-main_wrapper">
      <div class="return-btn">
        <router-link :to="`/engagements/${id}`">
          <ButtonComponent
            label="Back to Engagement"
            :icon="arrowLeft"
            icon-position="left"
            :icon-only="false"
            theme="primary"
            size="medium"
            :disabled="false"
          />
        </router-link>
      </div>
      <HeadingText label="Import Template Issues" size="2" />
      <p>
        CSV and JSON file formats are supported. <br /><br />
        CSV file must be in the same format and have the same headers as the template available below. <br /><br />
        JSON file must be in the format of the Engagement Issue.<br/><br/>
        Import files larger than {{user.maxImportFileSize}} will be placed in the Job Queue and processed.
      </p>
      <div class="template-buttons">
        <span @click="csvTemplate">Click here to download the CSV Template</span>
      </div>
      <div class="template-buttons">
        <span @click="jsonTemplate">Click here to download the JSON Template</span>
      </div>

      <br />

      <div
        class="input-file__container"
        @dragover="dragover"
        @dragleave="dragleave"
        @drop="drop"
      >
        <label class="block cursor-pointer">File</label>

        <span
          v-if="file === null"
          class="browse-button no-upload"
          value="loadXml"
          @click="onPickFile"
        >
          Drop file here to attach or browse
          <UploadImg class="file-upload__svg" />
        </span>

        <div v-else class="browse-button upload-exists" value="loadXml">
          <span @click="onPickFile">
            {{ file.name }}
          </span>
          <Trash @click="removeFiles" />
        </div>

        <input
          ref="fileInput"
          accept=".csv,.json"
          type="file"
          placeholder="Browse Files"
          class="input-file__hidden"
          @change="onFilePicked"
        />
      </div>
      <div class="submit-btn_wrap" @click="importIssue">
        <ButtonComponent
          label="Import Issues"
          :icon="arrowRight"
          icon-position="right"
          :icon-only="false"
          theme="primary"
          size="medium"
          :disabled="false"
          class="submit-btn"
        />
      </div>
    </div>

    <div v-else class="import-submitted_wrapper">
      <div class="return-btn">
        <router-link :to="`/engagements/${id}/import`">
          <ButtonComponent
            label="Back to Engagement"
            :icon="arrowLeft"
            icon-position="left"
            :icon-only="false"
            theme="primary"
            size="default"
            :disabled="false"
          />
        </router-link>
      </div>
      <HeadingText label="Review Import" size="2" />

      <div class="submitted-btm_buttons">
        <router-link :to="`/engagements/${id}/import`">
          <ButtonComponent
            label="Return to Engagement"
            :icon-only="false"
            theme="primary"
            size="default"
            :disabled="false"
          />
        </router-link>
        <div @click="importIssue">
          <router-link :to="`/engagements/${id}/import`">
            <ButtonComponent
              label="Import More Issues"
              :icon-only="false"
              theme="primary-outline"
              size="default"
              :disabled="false"
            />
          </router-link>
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
import isValidUser from '../../../../middleware/is-valid-user'
import ButtonComponent from '../../../../components/controls/ButtonComponent'
import IconRightArrow from '../../../../assets/svg/fi_arrow-right.svg?inline'
import IconLeftArrow from '../../../../assets/svg/fi_arrow-left.svg?inline'
import HeadingText from '../../../../components/HeadingText'
import AlertComponent from '../../../../components/AlertComponent'
import LoadingComponent from '../../../../components/LoadingComponent'

import UploadImg from '~/assets/svg/fi_upload.svg?inline'
import Trash from '~/assets/svg/fi_trash.svg?inline'

export default {
  name: 'EngagementImport',
  components: {
    ButtonComponent,
    HeadingText,
    AlertComponent,
    UploadImg,
    Trash,
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
      arrowRight: IconRightArrow,
      arrowLeft: IconLeftArrow,
      submitted: false,
      file: null,
      passed: false,
      issueMessage: this.$store.state.modules.issues.importMessage,
      assetDropdown: [
        {
          label: 'None',
          value: '0',
        },
      ],
      defaultAsset: null,
      issues: null,
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
      title: `Import Issues | ${this.pageTitle}`,
    }
  },
  computed: {
    ...mapState({
      user: (state) => state.modules.user,
      isLoading: (state) => state.loading.loading,
      pageTitle: (state) => state.config.pageTitle
    }),
    defaultAssetName() {
      return this.defaultAsset?.display || null
    },
    fileRefName() {
      return this.file?.name || this.$refs.fileInput?.files[0]?.name
    },
  },
  mounted() {
    this.getAssets()
  },
  methods: {
    getAssets(callback) {
      return this.$axios
        .$get(`${this.$store.state.config.api_url}/Asset/engagement/${this.id}`)
        .then((r) => {
          this.assetDropdown =
            r.data
              ?.map((option) => {
                return {
                  ...option,
                  display: option.name,
                  value: option.assetId,
                  name: option.name,
                  id: option.assetId,
                }
              })
              .sort((a, b) => {
                return a.name.localeCompare(b.name)
              }) || []
          this.defaultAsset = this.assetDropdown[0] || null
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Getting Asset")
        })
    },
    handleErrorResponse(r, title) {
      this.errorResponse = this.$getErrorResponse(r)
      if (this.errorResponse != null) {
        this.addAlert(this.errorResponse, "danger", title)
      }
    },
    setDefaultValue(opt) {
      this.defaultAsset = opt
    },
    removeFiles() {
      this.$refs.fileInput.value = null
      this.file = null
    },
    onFilePicked() {
      this.file = this.$refs.fileInput.files[0]
    },
    onPickFile() {
      this.$refs.fileInput.click()
    },
    importIssue() {
      const csvfile = this.$refs.fileInput.files[0]

      const fd = new FormData()

      fd.append('file', csvfile)

      let url = `${this.$store.state.config.api_url}/issue/import/${this.id}?isTemplate=true`

      const defaultAssetId = this.defaultAsset?.value || null

      if (defaultAssetId != null) {
        url = url + `&defaultQueueAssetId=${defaultAssetId}`
      }

      return this.$axios
        .$post(url, fd, {
          headers: {
            accept: '*/*',
          }
        })
      .then((r) => {
        if (r.data?.isQueued === true) {
          this.addAlert(["The template issue import has been placed into queue for processing."], "success", "In Queue")
        }
        else {
          this.$router.push({
            path: `/engagements/${this.id}`,
          })
        }
      })
      .catch((error) => {
        this.handleErrorResponse(error, "Error Importing Issues")
      })
    },
    onFileUpload(file) {
      this.file = file
    },
    csvTemplate() {
      this.$store.dispatch('modules/issues/downloadCsvTemplate', null, {
        root: true,
      })
    },
    jsonTemplate() {
      this.$store.dispatch('modules/issues/downloadTemplateTemplate', null, {
        root: true,
      })
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
    dragover(event) {
      event.preventDefault()
      // Add some visual fluff to show the user can drop its files
      if (!event.currentTarget.classList.contains('bg-green-300')) {
        event.currentTarget.classList.remove('bg-gray-100')
        event.currentTarget.classList.add('bg-green-300')
      }
    },
    dragleave(event) {
      // Clean up
      event.currentTarget.classList.add('bg-gray-100')
      event.currentTarget.classList.remove('bg-green-300')
    },
    drop(event) {
      event.preventDefault()
      this.$refs.file.files = event.dataTransfer.files
      this.onFilePicked() // Trigger the onChange event manually
      // Clean up
      event.currentTarget.classList.add('bg-gray-100')
      event.currentTarget.classList.remove('bg-green-300')
    },
  },
}
</script>

<style lang="scss" scoped>
.import-page_wrapper {
  width: 472px;
  margin: auto;
  margin-top: 48px;
  margin-bottom: 154px;
  display: flex;
  flex-direction: column;
  justify-content: center;
}
.import-main_wrapper {
  display: flex;
  flex-direction: column;
  justify-content: center;
}
.return-btn {
  display: flex;
  align-items: center;
  justify-content: center;
}
.import-page_wrapper .heading {
  width: fit-content;
  margin: auto;
  margin-top: 48px;
}
.import-page_wrapper p {
  font-family: $font-primary;
  font-style: normal;
  font-weight: 400;
  font-size: 14px;
  line-height: 18px;
  text-align: center;
  color: $brand-color-scale-6;
  margin-top: 24px;
}
.import-page_wrapper p:last-of-type {
  text-align: left;
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
.input-upload {
  margin-top: 24px;
}
.asset-dropdown {
  margin-top: 24px;
}
.submit-btn_wrap {
  display: flex;
  justify-content: center;
}
.submit-btn {
  margin: auto;
  margin-top: 24px;
  max-width: 175px;
  white-space: nowrap;
}
.alert-margin {
  margin-top: 24px;
  margin-bottom: 24px;
}
.submitted-btm_buttons {
  display: flex;
  gap: 30px;
  justify-content: center;
  margin-top: 24px;
}

[v-cloak] {
  display: none;
}
.browse-button {
  padding: 16px;
  border: 1px solid $brand-color-scale-4;
  box-sizing: border-box;
  border-radius: 8px;
  cursor: pointer;
  display: flex;
  width: 472px;
  font-family: $font-primary;
  font-style: normal;
  font-weight: 400;
  font-size: 14px;
  line-height: 18px;
  color: $brand-color-scale-6;
  margin-top: 8px;
}
.browse-button svg {
  width: fit-content;
  margin-left: auto;
}
.input-file__hidden {
  display: none;
}
.input-file__container label {
  font-family: $font-primary;
  font-style: normal;
  text-align: left;
  cursor: pointer;
  display: block;
  font-weight: 600;
  font-size: 16px;
  line-height: 22px;
  color: $brand-color-scale-6;
  margin-bottom: 8px;
}

.disable-upload_2 .no-upload {
  pointer-events: none !important;
  display: none;
}
.disable-upload_2 .upload-exists {
  display: flex;
}
</style>
