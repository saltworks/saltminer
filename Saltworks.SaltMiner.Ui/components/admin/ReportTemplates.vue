<template>
  <div class="custom_issue__wrapper">
    <FormLabel label="Report Templates">
    </FormLabel>
    <h1>Manage report templates used to generate engagement reports</h1>
    <TableData
      :headers="tableHeaders"
      :disable-hover="true"
      :rows="reportTemplate"
      row-size="medium"
      :toggle-rows="false"
      :sortable="true"
      @row-click="handleRowClick"
      @checked-rows-changed="handleCheckedRowsChanged"
    />
    <div class="extraRoom">
      <ButtonComponent
        label="Add"
        theme="primary"
        size="medium"
        @button-clicked="handleAdd"
      />
      <ButtonComponent style="display: none"
        label="Delete"
        theme="danger"
        size="medium"
        :disabled="checkedRows.length < 1"
        @button-clicked="handleDeleteChecked"
      />
    </div>
    <SlideModal
      label="Add Report Template"
      :open="toggleAdd"
      size="small"
      @toggle="() => (toggleAdd = !toggleAdd)"
    >   
        <InputText
        reff="txtNewTemplateName"
        label="Name"
        id="newTemplateNameValue"
        placeholder="Name"
        :value="newTemplateName"
        @update="
            (val) => {
            newTemplateName = val
            }
        "
        />

        <div
            class="input-file__container"
        >
            <label class="block cursor-pointer"> Upload File </label>

            <span
            v-if="file === null"
            class="browse-button no-upload"
            value="loadXml"
            @click="onPickFile"
            >
            Browse for file
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
            accept=".docx"
            type="file"
            placeholder="Browse Files"
            class="input-file__hidden"
            @change="onFilePicked"
            />
        </div>

        <div class="new-template__buttons">
        <ButtonComponent
          label="Save"
          theme="save"
          :disabled="(newTemplateName === null || newTemplateName.length === 0)"
          size="small"
          @button-clicked="handleSave"
        />
        <ButtonComponent
          label="Cancel"
          theme="cancel"
          size="small"
          @button-clicked="toggleAdd = !toggleAdd"
        />
      </div>
    </SlideModal>
    
    <SlideModal
      label="Edit Report Template"
      :open="toggleEdit" 
      size="small"
      @toggle="() => (toggleEdit = !toggleEdit)"
    >   
        <InputText
            reff="txtNewTemplateName"
            label="Name"
            id="newTemplateNameValue"
            placeholder="Name"
            :value="newTemplateName"
            @update="
              (val) => {
                newTemplateName = val
              }
            "
          />

        <div
            class="input-file__container"
        >
            <label class="block cursor-pointer"> Upload File </label>

            <span
            v-if="file === null"
            class="browse-button no-upload"
            value="loadXml"
            @click="onPickFile"
            >
            Browse for file
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
            accept=".docx"
            type="file"
            placeholder="Browse Files"
            class="input-file__hidden"
            @change="onFilePicked"
            />
        </div>

        <div class="new-template__buttons">
        <ButtonComponent
          label="Save"
          theme="save"
          size="small"
          :disabled="(newTemplateName === null || newTemplateName.length === 0)"
          @button-clicked="handleSave"
          />
        <ButtonComponent
          label="Delete"
          theme="danger"
          size="small"
          :disabled="(newTemplateName === null || newTemplateName.length === 0)"
          @button-clicked="handleDelete"
          />
        <ButtonComponent
          label="Cancel"
          theme="cancel"
          size="small"
          @button-clicked="toggleEdit = !toggleEdit"
        />
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
import InputText from './../controls/InputText.vue'
import ButtonComponent from './../controls/ButtonComponent'
import SlideModal from './../controls/SlideModal.vue'
import TableData from './../TableData.vue'
import AlertComponent from './../AlertComponent'
import FormLabel from './../controls/FormLabel.vue'

import UploadImg from '~/assets/svg/fi_upload.svg?inline'
import Trash from '~/assets/svg/fi_trash.svg?inline'

export default {
  components: {
    InputText,
    ButtonComponent,
    TableData,
    SlideModal,
    AlertComponent,
    FormLabel,
    UploadImg,
    Trash
},
  props: {
  },
  data() {
    return {
      tableHeaders: [
        {
          display: 'Template',
          field: 'display',
          sortable: false,
          hide: false
        }
      ],
      currentTemplate: null,
      reportTemplate: [],
      editReportTemplate: {},
      toggleAdd: false,
      toggleEdit: false,
      newField: '',
      newTemplateName: '',
      checkedRows: [],
      file: null,
      alert: {
        messages: [],
        type: "",
        title: ""
      }
    }
  },
  mounted() {
    this.getReportTemplates()
  },
  methods: { 
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
    handleCheckedRowsChanged(checkedRows) {
      this.checkedRows = checkedRows
    },
    handleRowClick(row) {
      this.newTemplateName = JSON.parse(JSON.stringify(row.value));
      this.toggleEdit = true;
    },
    getReportTemplates() {
      return this.$axios
        .$get(`${this.$store.state.config.api_url}/admin/reportTemplate/primer`)
        .then((r) => {
            this.reportTemplate = r?.data?.reportTemplateDropdown ?? []
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Getting Report Template")
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
    handleErrorResponse(r, title) {
      this.errorResponse = this.$getErrorResponse(r)
      if (this.errorResponse != null) {
        this.addAlert(this.errorResponse, "danger", title)
      }
    },
    handleSave() {
      const docFile = this.$refs.fileInput.files[0]

      if (docFile) {
        const fileName = docFile.name;
        const fileExt = fileName.split('.').pop().toLowerCase();
        if (fileExt !== 'docx') {
            this.addAlert(["Must use a .docx file type for templates."], "danger", "Invalid file")
            return;
        }
      }

      const fd = new FormData()

      fd.append('file', docFile)

      return this.$axios
        .$post(`${this.$store.state.config.api_url}/admin/reportTemplate/${this.newTemplateName}`, fd, {
          headers: {
            accept: '*/*',
          }
        })
        .then((r) => {
          this.addAlert([`The report template "${this.newTemplateName}" has been added to the job queue for importing.`], "success", "In Job Queue")
          this.toggleEdit = false;
          this.toggleAdd = false;
          this.removeFiles()
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Editing Report Template")
        })
    },
    handleDelete() {
      // this.reportTemplate.fields = this.reportTemplate.fields
      //  .filter(field => field.field !== this.editReportTemplate.field)

      return this.$axios
        .$delete(`${this.$store.state.config.api_url}/admin/reportTemplate/${this.newTemplateName}`, {
          headers: {
            'Content-Type': 'application/json',
          }
        })
        .then((r) => {
          this.addAlert([`The report template "${this.newTemplateName}" has been added to the job queue to be deleted.`], "success", "In Job Queue")
          this.toggleEdit = false;
          this.removeFiles()
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Removing Report Template ")
        })
    },
    
    handleDeleteChecked() {
      const fields = this.checkedRows.map((row) => row.field)

      this.reportTemplate.fields = this.reportTemplate.fields
      .filter(field => !fields.includes(field.field))

      return this.$axios
        .$post(`${this.$store.state.config.api_url}/admin/customissue`, JSON.stringify(this.reportTemplate), {
          headers: {
            'Content-Type': 'application/json',
          }
        })
        .then((r) => {
          this.addAlert(["Success Removed Report Templates"], "success", "Success")
          // this.reportTemplate = r?.data
          this.checkedRows = []
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Removing Report Templates")
        })
    },
    handleAdd() {
      this.newField = ''
      this.newTemplateName = ''
      this.toggleAdd = !this.toggleAdd
    }
  }
}
</script>

<style lang="scss" scoped>
.new-template__buttons {
  margin-top: 30px;
  display: flex;
  gap: 20px;
}

.extraRoom {
  margin-top: 30px;
  display: flex;
  gap: 20px;
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
  width: 406px;
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
