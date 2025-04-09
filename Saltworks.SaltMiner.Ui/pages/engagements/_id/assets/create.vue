<template>
  <div class="asset-page_wrapper">
    <LoadingComponent :is-visible="isLoading" />
    <div v-if="submitted !== true" class="asset-main_wrapper">
      <div class="return-btn">
        <router-link :to="`/engagements/${id}`">
          <ButtonComponent
            label="Back to Engagement"
            :icon="arrowLeft"
            icon-position="left"
            :icon-only="false"
            theme="primary"
            :disabled="false"
          />
        </router-link>
      </div>
      <HeadingText label="Add an Asset" size="2" />
      <InputText v-if="!getFieldDefCustomization('name')?.isHidden"
        reff="txtAssetName"
        class="text-field_asset"
        :label="getFieldDefCustomization('name')?.label"
        placeholder="Enter an asset name"
        :value="asset.name"
        :disabled="getFieldDefCustomization('name')?.isReadOnly"
        @update="(val) => handleContentUpdate('name', val)"
        @blur="handleContentBlur('name', 'Name')"
      />
      <InputText v-if="!getFieldDefCustomization('description')?.isHidden"
        reff="txtAssetDescription"
        class="text-field_asset"
        :label="getFieldDefCustomization('description')?.label"
        placeholder="Enter an asset description"
        :value="asset.description"
        :disabled="getFieldDefCustomization('description')?.isReadOnly"
        @update="(val) => handleContentUpdate('description', val)"
        @blur="handleContentBlur('description', 'Description')"
      />
      <InputText v-if="!getFieldDefCustomization('versionId')?.isHidden"
        reff="txtAssetVersionId"
        class="text-field_asset"
        :label="getFieldDefCustomization('versionId')?.label"
        placeholder="Enter an asset version id"
        :value="asset.versionId"
        :disabled="getFieldDefCustomization('versionId')?.isReadOnly"
        @update="(val) => handleContentUpdate('versionId', val)"
        @blur="handleContentBlur('versionId', 'Version Id')"
      />
      <InputText v-if="!getFieldDefCustomization('version')?.isHidden"
        reff="txtAssetVersion"
        class="text-field_asset"
        :label="getFieldDefCustomization('version')?.label"
        placeholder="Enter an asset version"
        :value="asset.version"
        :disabled="getFieldDefCustomization('version')?.isReadOnly"
        @update="(val) => handleContentUpdate('version', val)"
        @blur="handleContentBlur('version', 'Version')"
      />
      <div class="attributes">
        <template v-if="attributeDefinitions.length > 0">
            <div class="attributes-list">
              <div
                v-for="(attribute, index) in attributeDefinitions.filter(def => this.attributeCustomizations.find(x => x.name === def.name && x.isHidden === false))"
                ref="attributes"
                :key="`attribute-${attribute.name}-${index}`"
              >
              <template
                  v-if="getAttributeField(attribute.type) === 'string'"
                >
                  <InputText
                    :reff="`txtAttribute-${attribute.name}`"
                    :label="getAttributeCustomization(attribute.name).label"
                    class="text-field_asset"
                    :placeholder="getAttributeCustomization(attribute.name).label"
                    :disabled="getAttributeCustomization(attribute.name).isReadOnly"
                    :value="getAttributeValue(attribute)"
                    @update="
                      (val) => handleUpdateAttribute(attribute, val)
                    "
                    @blur="handleBlurAttribute(attribute)"
                  />
                </template>
                <template
                  v-if="getAttributeField(attribute.type) === 'number'"
                >
                  <InputNumber
                    :reff="`txtAttribute-${attribute.name}`"
                    :label="getAttributeCustomization(attribute.name).label"
                    class="text-field_asset"
                    :type="getNumType(attribute.type)"
                    :placeholder="getAttributeCustomization(attribute.name).label"
                    :disabled="getAttributeCustomization(attribute.name).isReadOnly"
                    :value="getAttributeValue(attribute)"
                    @update="
                      (val) => handleUpdateAttribute(attribute, val)
                    "
                    @blur="handleBlurAttribute(attribute)"
                  />
                </template>
                <template
                  v-else-if="getAttributeField(attribute.type) === 'text'"
                >
                  <InputTextArea
                    :placeholder="getAttributeCustomization(attribute.name).label"
                    :label="getAttributeCustomization(attribute.name).label"
                    :resize="false"
                    :reff="`txtAttribute-${attribute.name}`"
                    class="text-field_asset"
                    :disabled="getAttributeCustomization(attribute.name).isReadOnly"
                    :value="getAttributeValue(attribute)"
                    @update="
                      (val) => handleUpdateAttribute(attribute, val)
                    "
                    @blur="handleBlurAttribute(attribute)"
                  />
                </template>
                <template
                  v-else-if="getAttributeField(attribute.type) === 'date'"
                >
                  <InputDate
                    :label="getAttributeCustomization(attribute.name).label"
                    :reff="`txtAttribute-${attribute.name}`"
                    class="text-field_asset"
                    :disabled="getAttributeCustomization(attribute.name).isReadOnly"
                    :value="getAttributeValue(attribute)"
                    @update="
                      (val) => handleUpdateAttribute(attribute, val)
                    "
                    @blur="handleBlurAttribute(attribute)"
                  />
                </template>
                <template
                  v-else-if="getAttributeField(attribute.type) === 'select'"
                >
                  <DropdownControl
                    :label="getAttributeCustomization(attribute.name).label"
                    theme="outline"
                    :options="
                      formatAttributeOptions(attribute.options || [])
                    "
                    class="assesment-dropdown "
                    :reff="`selectAttribute-${attribute.name}`"
                    :disabled="getAttributeCustomization(attribute.name).isReadOnly"
                    :value="getAttributeValue(attribute)"
                    @update="
                      (opt) =>
                        handleUpdateAttribute(attribute, opt.value)
                    "
                    @blur="handleBlurAttribute(attribute)"
                  />
                </template>
                <template
                  v-else-if="
                    getAttributeField(attribute.type) === 'multiselect'
                  "
                >
                  <MultiSelect
                    :label="getAttributeCustomization(attribute.name).label"
                    :options="
                      formatAttributeMultiOptions(attribute.options)
                    "
                    :values="getAttributeValue(attribute)"
                    :disabled="getAttributeCustomization(attribute.name).isReadOnly"
                    @update="
                      (val) => handleUpdateAttribute(attribute, val)
                    "
                    @blur="handleBlurAttribute(attribute)"
                  />
                </template>
              </div>
            </div>
        </template>
      </div>
      <div class="submit-btn_wrap" @click="reviewAsset">
        <ButtonComponent
          label="Continue to Review"
          :icon="arrowRight"
          icon-position="right"
          :icon-only="false"
          theme="primary"
          class="submit-btn"
        />
      </div>
    </div>

    <!-- review state -->
    <div v-else class="asset-submitted_wrapper">
      <div class="return-btn">
        <router-link :to="`/engagements/${id}`">
          <ButtonComponent
            label="Back to Engagement"
            :icon="arrowLeft"
            icon-position="left"
            :icon-only="false"
            theme="primary"
            :disabled="false"
          />
        </router-link>
      </div>
      <HeadingText label="Review Details" size="2" />
      <FormLabel class="review-label" label="Asset Name" />
      <span class="review-data">
          {{ asset.name }}
      </span>
      <FormLabel class="review-label" label="Description" />
      <span class="review-data">
        {{ asset.description }}
      </span>
      <FormLabel class="review-label" label="Version Id" />
      <span class="review-data">
        {{ asset.versionId }}
      </span>
      <FormLabel class="review-label" label="Versions" />
      <span class="review-data">
        {{ asset.version }}
      </span>
      <template v-for="item in attributeDefinitions">
        <FormLabel :key="item.name" class="review-label"  :label="item.display"/>
        <span :key="item.name + '_' + asset.attributes[item.name]" class="review-data" >
          {{ asset.attributes[item.name] }}
        </span>
      </template>

      <div class="submitted-btm_buttons">
        <div @click="adjustAsset">
          <ButtonComponent
            label="No, Back to Asset Details"
            :icon-only="false"
            theme="danger"
            :disabled="false"
            class="submitted-btn"
          />-
        </div>
        <div>
          <ButtonComponent
            label="Yes, Confirm Add Asset"
            icon-position="right"
            theme="primary-outline"
            class="submitted-btn"
            :icon="arrowRight"
            :icon-only="false"
            :disabled="false"
            @button-clicked="createAsset()"
          />
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
import ButtonComponent from '../../../../components/controls/ButtonComponent'
import IconRightArrow from '../../../../assets/svg/fi_arrow-right.svg?inline'
import IconLeftArrow from '../../../../assets/svg/fi_arrow-left.svg?inline'
import HeadingText from '../../../../components/HeadingText'
import InputText from '../../../../components/controls/InputText'
import AlertComponent from '../../../../components/AlertComponent'
import FormLabel from '../../../../components/controls/FormLabel'
import isValidUser from '../../../../middleware/is-valid-user'
import InputNumber from '../../../../components/controls/InputNumber'
import DropdownControl from '../../../../components/controls/DropdownControl.vue'
import MultiSelect from '../../../../components/controls/MultiSelect.vue'
import InputDate from '../../../../components/controls/InputDate'
import InputTextArea from '../../../../components/controls/InputTextArea'
import LoadingComponent from '../../../../components/LoadingComponent'

export default {
  name: 'AssetCreate',
  components: {
    ButtonComponent,
    HeadingText,
    InputText,
    AlertComponent,
    FormLabel,
    InputTextArea,
    InputDate,
    InputNumber,
    MultiSelect,
    DropdownControl,
    LoadingComponent
  },
  middleware: isValidUser,
  asyncData({ params, store }) {
    return {
      id: params.id,
    }
  },
  data() {
    return {
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
      submitted: false,
      attributeDefinitions: [],
      attributeCustomizations: [],
      fieldDefCustomizations: {},
      asset: {
        engagementId: this.$route.params.id,
        name: null,
        description: null,
        versionId: null,
        version: null,
        subtype: null,
        attributes: [],
      },

      productionCheck: false,
      alert: {
        messages: [],
        type: "",
        title: ""
      },
    }
  },
  async fetch({ store: { dispatch } }) {
    await dispatch('configInit')
  },
  head() {
    return {
      title: `Add Asset | ${this.pageTitle}`,
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
    this.handleInit()
  },
  methods: {
    handleInit() {
      this.getPrimer()
    },
    handleAlertClose() {
      this.alert = {
        messages: [],
        type: "",
        title: ""
      }
    },
    getPrimer() {
      return this.$axios
        .$get( `${this.$store.state.config.api_url}/asset/new/primer?engagementId=${this.id}`)
        .then((r) => {
          this.attributeDefinitions =
            r?.data?.attributeDefinitions || []
            this.fieldDefCustomizations = r?.data?.asset || {}
            this.attributeCustomizations = r?.data?.asset?.attributes || []
            this.asset.attributes = {};
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Getting Asset Report")
        })
    },
    getAttributeField(type) {
      const attr = this.attributeTypes.find((attrType) =>
        attrType.types.includes(type)
      )
      return attr?.field || 'unknown type'
    },
    formatAttributeMultiOptions(opts) {
      return opts || []
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
      const fieldKeysWithNoValue = Object.keys(this.asset).filter((key) => this.checkRequiredValue(this.asset[key]))
      const requiredFields = fieldKeysWithNoValue.filter((key) => this.fieldDefCustomizations[key]?.isRequired === true).map((key) => this.fieldDefCustomizations[key])
      requiredList.push(...requiredFields)

      // get required attributes
      const requiredAttributes = this.attributeCustomizations.filter((v) => v.isRequired && Object.entries(this.asset.attributes).find(([key, value]) => key === v.name && this.checkRequiredValue(value)))
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
      if (!(name in this.asset.attributes)) {
        if (attrType === 'date') {
          this.asset.attributes[name] = val || this.formattedAttributeDate
        } else if (attrType === 'select') {
          this.asset.attributes[name] = val
        } else if (attrType === 'multiselect') {
          this.asset.attributes[name] = val || []
        } else if (attrType === 'number') {
          this.asset.attributes[name] = val || null
        } else {
          this.asset.attributes[name] = val || ''
        }
      }

      if (attrType === 'multiselect') {
        const currentVal = this.asset.attributes[name]
        const opts = attribute?.options || []
        return this.formatMultiSelectValue(currentVal, opts)
      }

      return this.asset.attributes[name]
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
    handleUpdateAttribute(attribute, newVal) {
      this.asset.attributes[attribute.name] = newVal
    },
    handleBlurAttribute(attribute) {
      const attributes = Object.assign({}, this.asset.attributes)

      Object.keys(this.asset.attributes).forEach((attr) => {
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
        "AssetAttributes": attributes
      }
      return this.$axios
        .$post( `${this.$store.state.config.api_url}/Utility/TextValidation`, body)
        .catch((error) => {
          this.handleErrorResponse(error, `Validation Error`)
        })
    },
    getNumType(attributeType) {
      return attributeType === 'Integer (long)' ? 'int' : 'double'
    },
    formatAttributeOptions(opts) {
      return [{ display: "Select", value: null },  ...opts.map((o) => {
        return {
          display: o,
          value: o
        }
      })]
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
    handleContentUpdate(field, val) {
      this.asset[field] = val
    },
    handleContentBlur(field, display) {
      if(this.asset[field] !== ""){
        const body = {
          "Input": this.asset[field]
        }
        return this.$axios
          .$post( `${this.$store.state.config.api_url}/Utility/TextValidation`, body)
          .catch((error) => {
            this.handleErrorResponse(error, `Validation Error`)
          })
      }
    },
    handleAttributeUpdate(type, val, index) {
      this.asset.attributes[index][type] = val
    },
    reviewAsset() {

      if (!this.allRequiredFieldsComplete()) return

      this.submitted = !this.submitted
    },
    adjustAsset() {
      this.submitted = !this.submitted
    },
    onResetDropdown(e) {
      this.asset.assetType = e
    },
    isJsonString(str) {
      try {
        JSON.parse(str)
      } catch (e) {
        return false
      }
      return true
    },
    createAsset() {
      const attributes = Object.assign({}, this.asset.attributes)

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
      const newAsset = {
        ...this.asset
      }
      
      newAsset.version = newAsset.version ?? ""
      newAsset.versionId = newAsset.version ?? ""

      newAsset.attributes = attributes;

      return this.$axios
        .post(`${this.$store.state.config.api_url}/Asset/new`, JSON.stringify(newAsset), {
        headers: {
          'Content-Type': 'application/json',
        },
      })
      .then((r) => {
        this.$router.push({
          path: `/engagements/${this.id}`,
        })
      })
      .catch((error) => {
        this.handleErrorResponse(error, "Error Creating Asset")
      })
    },
  },
}
</script>

<style lang="scss" scoped>
.attributes {
  display: flex;
  flex-direction: column;
  .assesment-dropdown {
    margin-top: 24px;
  }
  .multiselectControl {
    margin-top: 24px;
  }
  .attributes-header {
    display: flex;
    align-items: center;
    gap: 20px;
    position: relative;
  }
  .attributes-add-button,
  .attributes-remove-button {
    position: relative;
    color: $brand-white;
    background: #ccc;
    width: 20px;
    height: 20px;
    font-size: 16px;
    line-height: 16px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 0 0 0 0;
    border: 0px;
    box-sizing: border-box;
    cursor: pointer;
    transition: background-color 0.1s ease-in-out;

    &:hover {
      background: $brand-primary-light-color;
    }
  }
  .attributes-add-button {
    margin-top: 24px;
    margin-bottom: 10px;
  }
  .attributes-remove-button {
    flex: 0 0 auto;
  }
  .attributes-row {
    width: 100%;
    flex: 0 0 auto;
    margin-bottom: 10px;
    display: flex;
    align-items: center;
    position: relative;
    gap: 10px;
    .attributes-remove-button {
      margin-right: 10px;
    }

    .text-field_asset {
      flex: 1;
    }
    &:last-child {
      margin-bottom: 0;
    }
  }
}
.asset-page_wrapper {
  width: 472px;
  margin: auto;
  margin-top: 48px;
  margin-bottom: 154px;
  display: flex;
  flex-direction: column;
  justify-content: center;
}
.asset-main_wrapper {
  display: flex;
  flex-direction: column;
  justify-content: center;
}
.return-btn {
  display: flex;
  align-items: center;
  justify-content: center;
}
.asset-page_wrapper .heading {
  width: fit-content;
  margin: auto;
  margin-top: 48px;
}
.asset-page_wrapper p {
  font-family: $font-primary;
  font-style: normal;
  font-weight: 400;
  font-size: 14px;
  line-height: 18px;
  text-align: center;
  color: $brand-color-scale-6;
  margin-top: 24px;
}
.asset-page_wrapper p:last-of-type {
  text-align: left;
}
.text-field_asset {
  margin-top: 24px;
}
.asset-dropdown {
  margin-top: 24px;
}
.checkbox-label {
  margin-top: 24px;
  margin-bottom: 10px;
}
.prod-checkbox {
  position: relative;
  left: 5px;
}
.submit-btn_wrap {
  display: flex;
  justify-content: center;
}
.submit-btn {
  margin: auto;
  margin-top: 24px;
  white-space: nowrap;
}
.alert-margin {
  margin-top: 24px;
  margin-bottom: 24px;
}
.asset-page_wrapper .error-message {
  margin-top: 8px;
}

.submitted-btm_buttons {
  display: flex;
  gap: 30px;
  justify-content: center;
  margin-top: 24px;
}
.submitted-btn {
  white-space: nowrap;
}
.review-label {
  margin-top: 24px;
}
.review-label {
  margin-top: 24px;
}
.review-data {
  margin-top: 8px;
  font-style: normal;
  font-weight: 400;
  font-size: 14px;
  line-height: 18px;
}
</style>
