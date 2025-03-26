<template>
  <div class="flex w-full h-screen items-center justify-center text-center">
    <div
      ref="attachment_wrapper"
      class="attachment-list__container"
    >
      <div v-if="controls" class="attachment-list_top">
        <FormLabel label="Attachments" />
        <div class="attachment-control" @click="handleClickAttach">
          <span>Add Attachment</span><Attachment />
        </div>
      </div>

      <div
        v-if="!hasFiles"
        class="fileless-button"
        value="loadXml"
        @click="handleClickAttach"
      >
        <span>No Files Attached Currently</span>
      </div>

      <!-- eslint-disable-next-line vue/no-v-html -->
      <ul v-else v-cloak class="files-list mt-4" value="loadXml" :style="`height:${height}px`">
        <li
          v-for="(file, i) in files"
          :key="`attachment-file-${i}`"
          class="list-item"
        >
          <Trash class="icon-righht-margin" @click="remove(file)"  />
          <!-- eslint-disable-next-line vue/no-v-html -->
          <span @click="handleClickFile(file)">
            {{ file.fileName }}
          </span>
          
        </li>
      </ul>

      <input
        ref="file"
        :accept="validFileExtensions"
        type="file"
        multiple
        name="fields[assetsFieldHandle_list][]"
        class="input-file__hidden"
        @change="onChange"
        @click="resetImageUploader"
      />
    </div>

    <AlertComponent
      v-if="alert.messages.length > 0"
      class="alert-margin"
      :alert = alert
      @close="handleAlertClose"
    />
  </div>
</template>

<script>
import FormLabel from './controls/FormLabel.vue'
import AlertComponent from './AlertComponent'
import Trash from '~/assets/svg/fi_trash.svg?inline'
import Attachment from '~/assets/svg/attachment.svg?inline'

export default {
  components: {
    Trash,
    FormLabel,
    Attachment,
    AlertComponent
  },
  data() {
    return {
      alert: {
        messages: [],
        type: "",
        title: ""
      }
    }
  },
  props: {
    controls: {
      type: Boolean,
      default: true,
    },
    files: {
      type: Array,
      default: () => [],
    },
    disabled: {
      type: Boolean,
      default: false,
    },
    config: {
      type: Object,
      default: () => ({}),
    },
    height: {
      type: Number,
      default: 152
    },
    validFileExtensions: {
      type: String,
      default: '',
    }
  },
  computed: {
    hasFiles() {
      return this.files.length > 0
    }
  },
  methods: {
    resetImageUploader() {
        this.$refs.file.value = '';
    },
    handleClickAttach() {
      this.$refs.file.click()
    },
    handleClickFile(file) {
      this.$axios
      .$get(`${this.$store.state.config.api_url}/File/Check/${file.fileId}`)
        .then((r) => {
          if(r === ''){
          this.$emit('file-not-found', "File " + file.fileName)
          }
          else {
            const link = document.createElement("a")
            link.href = `${this.$store.state.config.api_url}/File/${file.fileId}`
            document.body.appendChild(link)
            link.click()
            document.body.removeChild(link)
          }
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
    handleAddFile(file) {
      const formData = new FormData()

      formData.append('file', file)

      const tmpname = file?.name ?? null

      if (tmpname === null) return

      this.$axios
        .$post(`${this.$store.state.config.api_url}/File/upload`, formData, {
          headers: {
            'Content-Type': 'multipart/form-data',
          },
        })
        .then((r) => {
          const fdata = {
            fileName: tmpname,
            fileId: r,
            size: file.size,
            name: file.name,
          }
          /* this.filelist = [fdata] */
          this.$emit('file-added', fdata)
        }).catch((error) => {
          this.handleErrorResponse(error, "Error attaching file")
        })
    },
    onChange() {
      this.handleAddFile(this.$refs.file.files[0])
    },
    remove(f) {
      this.$emit('file-removed', f)
    },
    formatFileSize(file) {
      return file?.size ? `${Math.round(file.size / 1000)}kb` : ''
    },
  },
}
</script>

<style lang="scss" scoped>
.files-list,
.fileless-button {
  cursor: pointer;

  position: relative;
  box-sizing: border-box;

  padding: 16px;
  margin-top: 0px;

  border: 1px solid $brand-color-scale-4;
  border-radius: 8px;

  width: 343px;
  min-height: 152px;

  font-family: $font-primary;
  font-style: normal;
  font-weight: 400;
  font-size: 14px;
  line-height: 18px;

  color: $brand-color-scale-6;

  overflow: scroll;
}

.icon-righht-margin{
  margin: 0 5px 0 0;
}

.files-list {
  background: $brand-white;
  display: flex;
  flex-flow: column;
  justify-content: flex-start;
  gap: 16px;

  .list-item {
    width: 100%;
    padding: 8px 0;
    flex: 0 0 auto;
    display: flex;
    justify-content: space-between;
    align-items: center;
    border-bottom: 1px solid $brand-color-scale-2;

    svg {
      flex: 0 0 auto;
      width: 15px;
      height: 15px;
    }
  }
  .list-item:last-of-type {
    margin-bottom: 0px;
  }
}

.fileless-button {
  position: relative;
  font-family: $font-primary;
  font-style: normal;
  font-weight: 700;
  font-size: 16px;
  line-height: 24px;
  color: $brand-primary-light-color;
  display: flex;
  justify-content: center;
  align-items: center;
}

.disable-upload {
  .files-list {
    display: block;
  }
  .fileless-button {
    pointer-events: none !important;
    display: none;
  }
}

.browse-button {
  svg {
    width: fit-content;
    margin-left: auto;
  }
}

.input-file__hidden {
  display: none;
}

.attachment-list_top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  font-family: $font-primary;
  font-style: normal;
  font-weight: 600;
  font-size: 14px;
  line-height: 18px;
  color: $brand-color-scale-6;
  margin-bottom: 8px;
}

.attachment-control {
  display: flex;
  white-space: pre;
  gap: 5px;
  cursor: pointer;
  color: $brand-primary-light-color;
  font-size: 14px;
  font-weight: 700;
  line-height: 16px;
  font-family: $font-primary;
  height: 16px;
}
</style>
