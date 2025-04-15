<template>
  <div class="page-layout">
    <div class="row">
      <HeadingText label="Issue Details" size="2" />
    </div>
    
    <button @click="showRaw = !showRaw">
      {{ showRaw ? "Show Filtered Data" : "Show Raw JSON" }}
    </button>

    <div v-if="!showRaw">
        <div class="info-container">
        <div
            v-for="(value, key) in issue"
            :key="key"
            class="field-row"
        >
            <div class="field-label">
            {{ key }}:
            </div>

            <div class="field-value-wrapper">
            <div v-if="typeof value !== 'object'" class="field-value">
                {{ value }}
            </div>

            <!-- loop through object values (attributes) -->
            <div v-else>
                <div
                v-for="(val, subKey) in value"
                :key="subKey"
                class="nested-field"
                >
                <div class="nested-label">{{ subKey }}:</div>
                <div class="nested-value">{{ val }}</div>
                </div>
            </div>
            </div>
        </div>
        </div>
    </div>

    <div v-else>
      <pre>{{ formattedJson }}</pre>
    </div>

    
    <ConfirmDialog ref="confirmDialog" />
  </div>
</template>

<script>
import { mapState } from 'vuex'
import isValidUser from '../../../middleware/is-valid-user'
import HeadingText from '../../../components/HeadingText.vue'
import ConfirmDialog from '../../../components/ConfirmDialog.vue';

export default {
  name: 'InventoryAssetsIndex',
  components: {
    HeadingText,
    ConfirmDialog
  },
  layout: 'no-navbar',
  middleware: isValidUser,
  data() {
    return {
      issue: {},
      raw: {},
      showRaw: false,
      
    }
  },
  async fetch({ store: { dispatch } }) {
    await dispatch('configInit')
  },
  head() {
    return {
      title: `Issue Details | ${this.pageTitle}`,
    }
  },
  computed: {
    ...mapState({
      dateFormat: (state) => state.modules.user.dateFormat,
      user: (state) => state.modules.user,
      isLoading: (state) => state.loading.loading,
      pageTitle: (state) => state.config.pageTitle
    }),
    formattedJson() {
      return JSON.stringify(this.raw, null, 2);
    },
  },
  mounted() {
    this.getPrimer()
  },
  methods: {
    getPrimer() {
      return this.$axios
        .$get(`${this.$store.state.config.api_url}/Issue/${this.$route.params.id}/fullview`)
        .then((r) => {

          const fieldMap = {
                Name: 'vulnerability.name',
                Location: 'vulnerability.location',
                'Location Full': 'vulnerability.locationFull',
                Attributes: 'saltminer.attributes'
            }
            this.raw = r.data
            this.issue = this.flattenFields(r.data, fieldMap);
        })
    },
    getValueByPath(obj, path) {
      return path.split('.').reduce((acc, key) => acc?.[key], obj);
    },
    flattenFields(source, fieldMap) {
      const result = {};
      for (const [key, path] of Object.entries(fieldMap)) {
        result[key] = this.getValueByPath(source, path);
      }
      return result;
    },
    formatValue(value) {
        if (typeof value === "object") {
            return Object.entries(value).map(([key, value]) => ({
                label: key,
                value
            }));
        }

        return value
    }
  },
}

</script>

<style lang="scss" scoped>

.info-container {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.field-row {
  display: flex;
  align-items: flex-start;
}

.field-label {
  width: 200px; /* fixed width for alignment */
  padding-right: 1rem;
}

.field-value-wrapper {
  flex: 1;
}

.nested-field {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 0.25rem;
}

.nested-label {
  width: 200px;
}

.nested-value {
  flex: 1;
}

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