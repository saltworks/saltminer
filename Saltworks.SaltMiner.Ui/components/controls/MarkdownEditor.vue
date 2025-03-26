<template>
  <div :class="`${viewPosition} markdown-editor`">
    <div ref="editor" :class="editorClasses" :id="reff === '' ? labelId : reff"></div>
  </div>
</template>

<script>
// eslint-disable-next-line import/no-named-as-default
import uniqueId from 'lodash/uniqueId'
import Editor from '@toast-ui/editor'
import { serialize } from 'object-to-formdata'
import '@toast-ui/editor/dist/toastui-editor-viewer.css'
import '@toast-ui/editor/dist/toastui-editor.css'

export default {
  name: 'MarkdownDisplay',
  components: {  },
  props: {
    value: {
      type: String,
      default: '',
      required: false,
    },
    disabled: {
      type: Boolean,
      default: false,
    },
    reff: {
      type: String,
      default: null,
    },
    type: {
      type: String,
      default: '',
      required: false,
    },
    viewPosition: {
      type: String,
      default: 'right',
      required: false,
    },
    dontShow: {
      type: Boolean,
      default: false,
      required: false,
    },
    config: {
      type: Object,
      default: () => ({}),
    },
  },
  data() {
    return {
      editor: null,
      editorOptions: {
        hideModeSwitch: false,
      },
      configFile: '',
    }
  },
  computed: {
    editorClasses() {
      const classes = ['md-editor-el']
      if (this.disabled) classes.push('disabled')
      return classes.join(' ')
    },
    labelId() {
      if (this.inputId) {
        return this.inputId
      }
      return uniqueId('text-input-')
    }
  },
  async mounted() {
    this.configFile = await fetch('/smpgui/config.json').then((res) => res.json())
    if (this.disabled) {
      this.createViewer()
    } else {
      this.createEditor()
    }
  },
  methods: {
    onChange() {
      if (this.disabled) return

      this.$emit('input', this.editor.getMarkdown())
    },

    setEditorMode() {
      if (this.configFile.markdown_editor_wysiwyg_default === true) {
        return 'wysiwyg'
      } else {
        return 'markdown'
      }
    },

    createEditor() {
      const value = this.value?.length > 0 ? this.value : ''

      const opts = {
        el: this.$refs.editor,
        height: '500px',
        initialEditType: this.setEditorMode(),
        initialValue: value,
        hooks: {
          addImageBlobHook: (file, cb) => this.handleImageUpload(file, cb),
        },
        events: {
          change: this.onChange,
        },
        autofocus: false,
        toolbarItems: [        
          ['code', 'codeblock'],
          ['heading', 'bold', 'italic', 'strike'],
          ['hr', 'quote'],
          ['ul', 'ol', 'task', 'indent', 'outdent'],
          ['table', 'image', 'link']
        ]
      }

      this.editor = new Editor(opts)
    },



    createViewer() {
      const value = this.value?.length > 0 ? this.value : '> No content'

      const opts = {
        el: this.$refs.editor,
        viewer: true,
        initialValue: value,
      }

      this.editor = Editor.factory(opts)
    },

    async handleImageUpload(blob, cb) {
      if (this.disabled) return

      const uploadedImageUrl = await this.uploadImage(blob)

      // Skip second argument of cb to use the alt text provided in dialog box
      cb(uploadedImageUrl)
      return false
    },

    async uploadImage(file) {
      if (this.disabled) return

      return await this.$axios
        .$post(
          `${this.$store.state.config.api_url}/File/upload`,
          serialize({
            file,
          })
        )
        .then((response) => {
          return `${this.$store.state.config.api_url}/File/${response}`
        })
        .catch((e) => {
          alert('There was a problem uploading this file.')
        })
    },

    handleUpdate(e) {
      if (this.disabled) return

      const content = this.$refs.editor.invoke('getValue')

      if (content === '' || content === null) return

      const data = {
        content,
        type: this.type,
      }

      this.$emit('updated', data)
    },
  },
}
</script>

<style lang="scss" scoped>
.markdown-editor::v-deep {
  display: flex;
  margin: 0 0 100px 0;
  box-sizing: border-box;
  font-family: 'Helvetica Neue', Arial, sans-serif;
  color: $brand-color-scale-6;
  max-width: 900px;
}

.md-editor-el::v-deep {
  margin: 0;
  display: flex;
  box-sizing: border-box;
  width: 100%;

  .toastui-editor-pseudo-clipboard {
    display:none !important;
  }

  .toastui-editor-mode-switch {
    border-top: 1px solid $brand-color-scale-4 !important;
  }

  .toastui-editor-defaultUI {
    border: 1px solid $brand-color-scale-4 !important;
  }

  &.disabled {
    padding: 4px 8px;
    border: 1px solid rgb(218 221 230);
    border-radius: 3px;
  }

  .toastui-editor-contents {
    width: 100%;
  }
}
</style>
