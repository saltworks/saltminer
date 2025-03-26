<template>
  <div class="commentSection">
    <div class="newCommentForm">
      <div class="heading-underline">
        <HeadingText size="3" :label="title" />
      </div>
      <template v-if="!disabled">
        <InputTextArea
          reff="text-input-1"
          :label="'Add comment'"
          :placeholder="'Add comment'"
          :full-width="true"
          :disabled="disabled"
          :value="currentFormValue"
          @update="updateCommentFormValue"
        />
        <ButtonComponent
          :label="submitButton.label"
          :theme="submitButton.theme"
          size="small"
          @button-clicked="handleCommentCreate"
        />
      </template>
    </div>
    <div class="commentSection-content-wrapper">
      <CheckboxBox
        label="Include System Comments"
        :checked="includeSystem"
        @check-clicked="(val) => {
          includeSystem = val
          handleCommentSearch()
        }"
      />
      <CommentComponent
        v-for="(comment, idx) in sortedComments"
        :key="`comment-${idx}`"
        :author="comment.user"
        :type="comment.type"
        :allow-delete="engagementStatus === 'Draft'"
        :comment-id="comment.id"
        :timestamp="getTimestamp(comment.added)"
        :comment-timestamp="comment.added"
        :engagement-timestamp="engagementTimestamp"
        :content="comment.message"
        @delete="handleCommentDelete"
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
import { mapState } from 'vuex'
import HeadingText from './HeadingText'
import InputTextArea from './controls/InputTextArea'
import CheckboxBox from './controls/CheckboxBox.vue'
import CommentComponent from './CommentComponent'
import ButtonComponent from './controls/ButtonComponent'
import helpers from './Utility/helpers'
import AlertComponent from './AlertComponent'

export default {
  components: {
    HeadingText,
    InputTextArea,
    CheckboxBox,
    CommentComponent,
    ButtonComponent,
    AlertComponent,
  },
  props: {
    title: {
      type: String,
      default: 'Comments',
    },
    author: {
      type: String,
      default: 'Unknown',
    },
    engagementId: {
      type: String,
      default: '',
    },
    engagementStatus: {
      type: String,
      default: '',
    },
    engagementTimestamp: {
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
    scanId: {
      type: String,
      default: '',
    },
    disabled: {
      type: Boolean,
      default: false,
    },
    config: {
      type: Object,
      default: () => ({}),
    },
  },
  data() {
    return {
      submitButton: {
        label: 'Post Comment',
        theme: 'primary-outline',
        size: 'default',
        onClick: false,
      },
      includeSystem: false,
      commentFormValue: '',
      comments: [],
      alert: {
        messages: [],
        type: "",
        title: ""
      },
    }
  },
  computed: {
    ...mapState({
      dateFormat: (state) => state.modules.user.dateFormat,
      userName: (state) => state.modules.user.userName,
      userFullName: (state) => state.modules.user.fullName,
    }),
    currentFormValue() {
      return this.commentFormValue
    },
    hasComments() {
      return this.comments?.length > 0
    },
    sortedComments() {
      return [...this.comments].sort((a, b) => {
        return new Date(b.added) - new Date(a.added)
      })
    },
  },
  mounted() {
    this.handleCommentSearch()
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
    handleCommentDelete(id) {
      return this.$axios
        .$delete(`${this.$store.state.config.api_url}/Comment/${id}`,  {
          headers: {
            'Content-Type': 'application/json',
            accept: 'application/json',
          }
        })
        .then((r) => {
          this.comments = this.comments.filter(x => x.id !== id)
          this.commentFormValue = ''
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Deleting Comment")
        })
        .finally(() => {
          this.$emit('comments-loading', false)
        })
    },
    handleCommentCreate() {
      this.$emit('comments-loading', true)

      const body = {
        request: {
          message: this.commentFormValue,
          user: this.userName,
          userFullName: this.userFullName,
          engagementId: this.engagementId,
          assetId: this.assetId,
          issueId: this.issueId,
          scanId: this.scanId,
          pager: {
            size: 100,
            page: 1,
            sortFilters: {},
          },
        },
      }
      return this.$axios
        .$post(`${this.$store.state.config.api_url}/Comment/new`, JSON.stringify(body),  {
          headers: {
            'Content-Type': 'application/json',
            accept: 'application/json',
          }
        })
        .then((r) => {
          this.commentFormValue = ''
          this.handleCommentSearch(r.data)
          this.$emit('comment-added')
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Creating Comment")
        })
        .finally(() => {
          this.$emit('comments-loading', false)
        })
    },
    handleCommentSearch(newComment = null) {
      this.$emit('comments-loading', true)

      const size = 100
      const page = 1
      const sortFilters = {}

      const body = {
        engagementId: this.engagementId,
        issueId: this.issueId || null,
        scanId: null,
        assetId: this.assetId || null,
        includeSystem: this.includeSystem || false,
        pager: {
          size,
          page,
          sortFilters,
        },
      }
      return this.$axios
        .$post(`${this.$store.state.config.api_url}/Comment/Search`, JSON.stringify(body), {
          headers: {
            'Content-Type': 'application/json',
            accept: 'application/json',
          }
        })
        .then((r) => {
            const data = r.data || []
            const alreadyAdded = data.some((c) => c.id === newComment?.id)
            this.comments =
              newComment === null || alreadyAdded ? 
                data :
                 [newComment, ...data]
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Searching Comment")
        })
        .finally(() => {
          this.$emit('comments-loaded')
        })
    },
    handleErrorResponse(r, title) {
      this.errorResponse = this.$getErrorResponse(r)
      if (this.errorResponse != null) {
        this.addAlert(this.errorResponse, "danger", title)
      }
    },
    getTimestamp(timestamp = null) {
      const fdate = helpers.formatDate(
        timestamp,
        `M/D/yyyy @ h:mma`
      )

      return `${fdate}`
    },
    updateCommentFormValue(value) {
      this.commentFormValue = value
    },
  },
}
</script>

<style lang="scss" scoped>
.heading-underline {
  display: flex;
  flex-flow: column;
  align-items: flex-start;
  gap: 0;

  &::after {
    content: '';
    width: 40px;
    height: 4px;
    border-radius: 4px;
    background-color: $brand-primary-color;
    position: relative;
    flex: 0 0 auto;
    margin-bottom: 16px;
  }
}
.commentSection {
  display: flex;
  flex-direction: column;
  padding: 24px;
  max-width: 430px;
  align-items: flex-start;
  gap: 24px;
}
.newCommentForm {
  display: flex;
  flex-flow: column;
  gap: 8px;
  align-items: flex-start;
  width: 100%;
}

.commentSection-content-wrapper {
  padding: 24px 0 0 0;
  display: flex;
  flex-flow: column;
  gap: 24px;
  border-top: 1px solid $brand-color-scale-4;
  width: 100%;
}
.comment-section .commentBlock {
  background: $brand-white;
}
</style>
