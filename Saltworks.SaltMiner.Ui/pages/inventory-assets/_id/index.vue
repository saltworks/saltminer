<template>
  <div class="asset-page_wrapper">
    <LoadingComponent :is-visible="isLoading" />
    <div v-if="submitted !== true" class="asset-main_wrapper">
      <div class="return-btn">
        <router-link :to="`/inventory-assets`">
          <ButtonComponent
            label="Back to Inventory Assets"
            :icon="arrowLeft"
            icon-position="left"
            :icon-only="false"
            theme="primary"
            :disabled="false"
          />
        </router-link>
      </div>
      <HeadingText label="Edit Inventory Asset" size="2" />
      <InputText v-if="!getFieldDefCustomization('key')?.isHidden"
        reff="txtInventoryKey"
        class="text-field_asset"
        :label="getFieldDefCustomization('key')?.label"
        :placeholder="getFieldDefCustomization('key')?.label"
        :disabled="getFieldDefCustomization('key')?.isReadOnly"
        :value="inventoryAsset.key || ''"
        @update="(val) => handleContentUpdate('key', val)"
      />
      <InputText v-if="!getFieldDefCustomization('name')?.isHidden"
        reff="txtInventoryAssetName"
        class="text-field_asset"
        :label="getFieldDefCustomization('name')?.label"
        :placeholder="getFieldDefCustomization('name')?.label"
        :disabled="getFieldDefCustomization('name')?.isReadOnly"
        :value="inventoryAsset.name || ''"
        @update="(val) => handleContentUpdate('name', val)"
      />
      <InputText v-if="!getFieldDefCustomization('description')?.isHidden"
        reff="txtInventoryAssetDescription"
        class="text-field_asset"
        :label="getFieldDefCustomization('description')?.label"
        :placeholder="getFieldDefCustomization('description')?.label"
        :disabled="getFieldDefCustomization('description')?.isReadOnly"
        :value="inventoryAsset.description || ''"
        @update="(val) => handleContentUpdate('description', val)"
      />
      <template v-if="!getFieldDefCustomization('isProduction')?.isHidden">
      <FormLabel class="input-label" :label="getFieldDefCustomization('isProduction')?.label" />
      <InputCheckbox
        class="btm-input"
        :disabled="getFieldDefCustomization('isProduction')?.isReadOnly"
        :checked="inventoryAsset.isProduction"
        :size="'default'"
        @input="
          () => (inventoryAsset.isProduction = !inventoryAsset.isProduction)
        "
      />
      </template>
      <InputText v-if="!getFieldDefCustomization('version')?.isHidden"
        reff="txtInventoryAssetVersion"
        class="text-field_asset"
        :label="getFieldDefCustomization('version')?.label"
        :placeholder="getFieldDefCustomization('version')?.label"
        :disabled="getFieldDefCustomization('version')?.isReadOnly"
        :value="inventoryAsset.version || ''"
        @update="(val) => handleContentUpdate('version', val)"
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
                    :value="getAttributeValue(attribute)"
                    :disabled="getAttributeCustomization(attribute.name).isReadOnly"
                    @update="
                      (val) => handleUpdateAttribute(attribute.section, attribute.name, val)
                    "
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
                    :value="getAttributeValue(attribute)"
                    :disabled="getAttributeCustomization(attribute.name).isReadOnly"
                    @update="
                      (val) => handleUpdateAttribute(attribute.section, attribute.name, val)
                    "
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
                    :value="getAttributeValue(attribute)"
                    :disabled="getAttributeCustomization(attribute.name).isReadOnly"
                    @update="
                      (val) => handleUpdateAttribute(attribute.section, attribute.name, val)
                    "
                  />
                </template>
                <template
                  v-else-if="getAttributeField(attribute.type) === 'date'"
                >
                  <InputDate
                    :label="getAttributeCustomization(attribute.name).label"
                    :reff="`txtAttribute-${attribute.name}`"
                    class="text-field_asset"
                    :value="getAttributeValue(attribute)"
                    :disabled="getAttributeCustomization(attribute.name).isReadOnly"
                    @update="
                      (val) => handleUpdateAttribute(attribute.section, attribute.name, val)
                    "
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
                    :value="getAttributeValue(attribute)"
                    :disabled="getAttributeCustomization(attribute.name).isReadOnly"
                    @update="
                      (opt) =>
                        handleUpdateAttribute(attribute.section, attribute.name, opt.value)
                    "
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
                      (val) => handleUpdateAttribute(attribute.section, attribute.name, val)
                    "
                  />
                </template>
              </div>
            </div>
        </template>

        <template>
          <div>
            <div v-for="(item, index) in additionalAttributes" :key="index">
              <div v-for="(propertyValue, propertyName) in item" :key="propertyName" >
                <FormLabel class="review-label uppercase-text" :label="propertyName"/>
                <div v-for="(value, key) in propertyValue" :key="key">
                  <div class="indent">
                    <FormLabel class="review-label uppercase-text" :label="key"/>
                      {{ value }}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </template>

      </div>
      <div class="submit-btn_wrap" @click="saveInventoryAsset">
        <ButtonComponent
          label="Save"
          theme="primary"
          class="submit-btn"
        />
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
import ButtonComponent from '../../../components/controls/ButtonComponent'
import IconLeftArrow from '../../../assets/svg/fi_arrow-left.svg?inline'
import HeadingText from '../../../components/HeadingText'
import InputText from '../../../components/controls/InputText'
import FormLabel from '../../../components/controls/FormLabel'
import isValidUser from '../../../middleware/is-valid-user'
import InputNumber from '../../../components/controls/InputNumber'
import DropdownControl from '../../../components/controls/DropdownControl.vue'
import MultiSelect from '../../../components/controls/MultiSelect.vue'
import InputDate from '../../../components/controls/InputDate'
import InputTextArea from '../../../components/controls/InputTextArea'
import LoadingComponent from '../../../components/LoadingComponent'
import InputCheckbox from '../../../components/controls/InputCheckbox'
import AlertComponent from '../../../components/AlertComponent'

export default {
  name: 'InventoryAssetCreate',
  components: {
    ButtonComponent,
    HeadingText,
    InputText,
    FormLabel,
    InputTextArea,
    InputDate,
    InputNumber,
    MultiSelect,
    DropdownControl,
    LoadingComponent,
    InputCheckbox,
    AlertComponent
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
      arrowLeft: IconLeftArrow,
      submitted: false,
      attributeDefinitions: [],
      additionalAttributes: [],
      attributeCustomizations: [],
      fieldDefCustomizations: {},
      inventoryAsset: {
        id: this.$route.params.id,
        key: '',
        isProduction: false,
        name: '',
        description: '',
        version: '',
        attributes: [],
      },
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
      title: `Edit Inventory Asset | ${this.pageTitle}`,
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
      this.getPrimer();
    },
    createAttributeLabel(attribute) {
      return this.getAttributeCustomization(attribute.name).label;
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
            .$get(`${this.$store.state.config.api_url}/InventoryAsset/${this.$route.params.id}/edit/primer`, {
              headers: {
                  'Content-Type': 'application/json',
                  accept: 'application/json',
              }
            })
            .then((r) => {
                this.handleResponseAlertCheck(r, () => {
                  this.fieldDefCustomizations = r?.data?.inventoryAsset || {};
                  const mappedInventoryAsset = Object.fromEntries(
                    Object.entries(this.fieldDefCustomizations).map(([key, value]) => {
                      if (typeof value === "object" && value !== null) {
                        return [key, value.value || value.defaultValue];
                      }
                      return [key, value];
                    })
                  );
                  this.inventoryAsset = mappedInventoryAsset;

                  this.attributeDefinitions =
                    r?.data?.attributeDefinitions || []

                  const attributes = {}
                  const attrNameValue = {}
                  // inventory asset attributes have a 'section' level with 'saltminer'
                  // to show just the saltminer defined attr
                  // attributes from other systems can exist in with different section names
                  // those are handled with 'getAdditionalAttributes' below
                  const allAttributes = r?.data?.inventoryAsset?.attributes;
                  this.attributeCustomizations = allAttributes.saltminer || []
                  
                  Object.entries(allAttributes).forEach(([section, list]) => {
                    list.forEach((attr, index) => {
                      const attrDef = this.attributeDefinitions.find(
                        (def) => def.name === attr.name
                      )

                      let val = (attr.value !== null && attr.value !== '') ? attr.value : attr.defaultValue || "";

                      if (attrDef && this.getAttributeField(attrDef.type) === 'multiselect') {
                        if (val === null || val === '') val = '[]';
                        attrNameValue[attr.name] = JSON.parse(
                          val
                        )
                      }
                    })

                    attributes[section] = attrNameValue;
                  });

                  this.inventoryAsset.attributes = attributes;
                  this.getAdditionalAttributes();
                })
            })
                .catch((e) => {
                return e
            })
    },
    getAttributeField(type) {
      const attr = this.attributeTypes.find((attrType) =>
        attrType.types.includes(type)
      )
      return attr?.field || 'unknown type'
    },
    getAttributeCustomization(name) {
      return this.attributeCustomizations.find(x => x.name === name)
    },
    getFieldDefCustomization(name) {
      return this.fieldDefCustomizations[name];
    },
    formatAttributeMultiOptions(opts) {
      return opts || []
    },
    getAdditionalAttributes() {
      Object.keys(this.inventoryAsset.attributes).forEach((attrKey) => {
        const prop = {}
        const nonAttributeDef = this.attributeDefinitions.find(def => def.section === attrKey);
        if (!nonAttributeDef) {
          prop[attrKey] = this.inventoryAsset.attributes[attrKey]
          this.additionalAttributes.push(prop);
        }
      })
    },
    getAttributeValue(attribute) {
      const { name } = attribute
      const { section } = attribute

      const attr = this.getAttributeCustomization(name)
      const attributes = this.inventoryAsset.attributes

      // get value to set
      const val = attr.value || attr.defaultValue || null;

      const attrType = this.getAttributeField(attribute.type)

      if (!(name in attributes[section])) {
        if (attrType === 'date') {
          attributes[section][name] = val || val || this.formattedAttributeDate
        } else if (attrType === 'select') {
          attributes[section][name] = val
        } else if (attrType === 'multiselect') {
          attributes[section][name] = val || []
        } else if (attrType === 'number') {
          attributes[section][name] = val || null
        } else {
          attributes[section][name] = val || ''
        }
      }

      if (attrType === 'multiselect') {
        const currentVal = attributes[section][name]
        const opts = attribute?.options || []
        return this.formatMultiSelectValue(currentVal, opts)
      }
      
      return attributes[section][name]
    },
    checkRequiredValue(val) {
      return val == null || (typeof val === 'string' ? val.replace(/<br\s*\/?>/gi, "").replace(/\s+/g, "").trim() === '' : val === '' || JSON.stringify(val) === "[]") || val === []
    },
    allRequiredFieldsComplete() {
      const requiredList = []
      
      // get required fields
      const fieldKeysWithNoValue = Object.keys(this.inventoryAsset).filter((key) => this.checkRequiredValue(this.inventoryAsset[key]))
      const requiredFields = fieldKeysWithNoValue.filter((key) => this.fieldDefCustomizations[key]?.isRequired === true).map((key) => this.fieldDefCustomizations[key])
      requiredList.push(...requiredFields)

      // get required attributes
      const requiredAttributes = this.attributeCustomizations.filter((v) => v.isRequired && Object.entries(this.inventoryAsset.attributes.saltminer).find(([key, value]) => key === v.name && this.checkRequiredValue(value)))
      requiredList.push(...requiredAttributes)

      if (requiredList.length > 0) {
        const attrNames = requiredList.map(field => `${field.label}`).join(", ");
		    const msg = (requiredList.length === 1) ? "This field is required to save" : "These fields are required to save";
        this.addAlert([`${msg}: ${attrNames}`], "danger", "Save Error")
        return false;
      }

      return true;
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
    handleUpdateAttribute(section, attributeName, newVal) {
      this.inventoryAsset.attributes[section][attributeName] = newVal
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
    handleResponseAlertCheck(r, callback) {
      if (r?.success || r?.ok || r?.status === 200) {
        if (typeof callback === 'function') callback()
      } else if (r?.message) {
        this.addAlert([`${r?.status}: ${r?.message}`], "danger", "Error")
      } else if (r?.errorMessages) {
        this.addAlert(r?.errorMessages, "danger", "Error")
      }
    },
    handleErrorResponse(r, title) {
      this.errorResponse = this.$getErrorResponse(r)
      if (this.errorResponse != null) {
        this.addAlert(this.errorResponse, "danger", title)
      }
    },
    handleContentUpdate(field, val) {
      this.inventoryAsset[field] = val
    },
    handleAttributeUpdate(type, val, index) {
      this.inventoryAsset.attributes[index][type] = val
    },
    adjustAsset() {
      this.submitted = !this.submitted
    },
    onResetDropdown(e) {
      this.inventoryAsset.assetType = e
    },
    isJsonString(str) {
      try {
        JSON.parse(str)
      } catch (e) {
        return false
      }
      return true
    },
    saveInventoryAsset() {
      if (!this.allRequiredFieldsComplete()) return;

      const attributes = Object.assign({}, this.inventoryAsset.attributes)

      Object.keys(attributes).forEach((sectionKey) => {
        Object.keys(attributes[sectionKey]).forEach((attr) => {
        const attrDef = this.attributeDefinitions.find(
          (def) => def.name === attr
        )
        if (attrDef && this.getAttributeField(attrDef.type) === 'multiselect') {
          attributes[sectionKey][attr] = JSON.stringify(
            attributes[sectionKey][attr]
          )
        }
        })
      })
      
      const newAsset = {
        ...this.inventoryAsset
      }
      
      newAsset.isProduction = newAsset.isProduction ?? false;
      newAsset.version = newAsset.version ?? ""
      newAsset.attributes = attributes;

      return this.$axios
        .post(`${this.$store.state.config.api_url}/InventoryAsset`, JSON.stringify(newAsset), {
        headers: {
          'Content-Type': 'application/json',
        },
      })
      .then((r) => {
        this.$router.push({
          path: `/inventory-assets`,
        })
      })
      .catch((error) => {
        this.handleErrorResponse(error, "Error Creating Inventory Asset")
      })
    },
  },
}
</script>

<style lang="scss" scoped>
.indent {
  margin-left: 20px;
}
.uppercase-text {
  text-transform: uppercase;
}
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
