<template>
    <div class="page__wrapper">
      <div class="page__header">
        <ButtonComponent
          :label="'Engagement: ' + issueInfo.engagement?.name"
          :icon="backIcon"
          icon-position='left'
          :icon-only=false
          theme='primary'
          size='medium'
          @button-clicked="navEngagement"
        />
        <ButtonComponent
          :label="'Issue: ' + issueInfo.name"
          :icon="backIcon"
          icon-position='left'
          :icon-only=false
          theme='primary'
          size='medium'
          @button-clicked="navIssue"
        />
      </div>
      <div v-if="!markdownLoading" class="page__content">
        <MarkdownEditor
          class="md-editor"
          reff="markdownInstructions"
          type="testingInstructions"
          :config="$store.state.config"
          :dont-show="markdownLoading"
          :value="issueInfo.testingInstructions"
          :disabled="true"
        />
      </div>
    </div>
</template>

<script>
import { mapState } from 'vuex'
import MarkdownEditor from '../../../../../components/controls/MarkdownEditor.vue'
import ButtonComponent from '../../../../../components/controls/ButtonComponent'
import isValidUser from '../../../../../middleware/is-valid-user'
import BackTo from '../../../../../assets/svg/go-back.svg?inline'

export default {
  name: 'TestingInstructions',
  components: {
    MarkdownEditor,
    ButtonComponent
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
      issueInfo: {
        assetId: '',
        assetName: '',
        attributes: {},
        attachments: [],
        classification: '',
        description: '',
        details: '1',
        engagementId: this.$route.params.id,
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
      fieldDefCustomizations: {},
      mappedIssues: {},
      markdownLoading: true,
      backIcon: BackTo
    }
  },
  async fetch({ store: { dispatch } }) {
    await dispatch('configInit')
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
      pageTitle: (state) => state.config.pageTitle
    }),
  },
  mounted() {
    this.getPrimer()
  },
  methods: {
    handleErrorResponse(r, title) {
      this.errorResponse = this.$getErrorResponse(r)
      if (this.errorResponse != null) {
        this.addAlert(this.errorResponse, "danger", title)
      }
    },
    async getPrimer() {
      return await this.$axios
        .$get(`${this.$store.state.config.api_url}/Issue/${this.issueId}/edit/primer`)
        .then((r) => {
          // this.issueInfo = r.data.issue

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

          this.markdownLoading = false
        })
        .catch((error) => {
          return this.handleErrorResponse(error, "Error Getting Issue Edit Primer")
        })
    },
    navEngagement() {
      this.$router.push({
        path: `/engagements/${this.issueInfo.engagement.id}`,
      })
    },
    navIssue() {
      this.$router.push({
        path: `/engagements/${this.issueInfo.engagement.id}/issues/${this.issueInfo.id}`,
      })
    }
  }
}
</script>

<style lang="scss" scoped>
  .page__wrapper {
    height: 100%;
    max-width: 1200px;
    width: 100%;
  }
  .page__header {
    margin: 20px;
  }
  .page__content {
    margin: 20px;
  }
  .goBack {
    display: flex;
    svg {
      margin-left: 3px;
    }
  }
</style>