<template>
  <div class="custom_field_def__wrapper">
    <FormLabel label="Field Definitions">
    </FormLabel>
    <h1>This page can be used to customize standard fields for all access levels.</h1>
    <h1>Set the default field values. Modify field labels. Control field access with required, hidden, or read-only options.</h1>
    <h1>Only labels can be updated for fields marked as "System".</h1>

    <div class="extraRoom">
      <DropdownControl
          theme="outline"
          label="Select Field Type"
          :options="entityTypes"
          class="btm-input"
          @update="(val) => handleSelectType(val)"
      />
    </div>

    <TableData
      :headers="tableHeaders"
      :disable-hover="true"
      :rows="fieldDefinitions"
      row-size="medium"
      :sortable="true"
      @row-click="handleRowClick"
    />
    
    <SlideModal
      label="Modify Field Definition"
      :open="toggleEdit"
      size="small"
      @toggle="() => (toggleEdit = !toggleEdit)"
    >   
        <InputText
            reff="txtFieldDisplay"
            label="Display"
            id="fieldDisplayValue"
            placeholder="Display"
            :value="editCustomField.label"
            @update="
              (val) => {
                editCustomField.label = val
              }
            "
          />

        <div style="display: flex; gap: 30px;">
          <CheckboxBox
            label="Hidden"
            :disabled="hiddenDisabled"
            :checked="editCustomField.hidden"
            @check-clicked="handleCheckboxClick('hidden')"
          />
          <CheckboxBox
            label="Read-only"
            :disabled="readOnlyDisabled"
            :checked="editCustomField.readOnly"
            @check-clicked="handleCheckboxClick('readOnly')"
          />
          <CheckboxBox
            label="Required"
            :disabled="requiredDisabled"
            :checked="editCustomField.required"
            @check-clicked="handleCheckboxClick('required')"
          />
          <CheckboxBox
            label="System"
            :checked="editCustomField.system"
            disabled="true"
          />
        </div>

        <template v-if="!['severity','testStatus','isSuppressed','isProduction','assetName','removedDate'].includes(editCustomField.name)">
          <InputText
            reff="txtDefault"
            label="Default Value"
            id="txtDefault"
            placeholder="Default Value"
            :value="editCustomField.default"
            @update="
              (val) => {
                editCustomField.default = val
              }
            "
          />
        </template>

        <template v-if="editCustomField.name==='severity'">
          <DropdownSeverityControl
            :options="severityDropdown"
            label="Default Value"
            :default-display="setSeverityValue()"
            :hide-all="false"
            :rounded="true"
            theme="solid"
            class=" btm-input"
            reff="dropDownSeverity"
            @update="handleDefaultDropdown"
          />
        </template>

        <template v-if="editCustomField.name==='testStatus'">
          <DropdownControl
              theme="outline"
              label="Default Value"
              :options="testedDropdown"
              class="btm-input"
              reff="dropDownTested"
              :value="(editCustomField.default === '') ? null : editCustomField.default"
              @update="handleDefaultDropdown"
          />
        </template>

        <template v-if="editCustomField.name==='removedDate'">
          <InputDate
              reff="txtDefault"
              label="Default Value"
              class="text-field_asset btm-input"
              :value="editCustomField.default"
              @update="
                (val) => {
                  editCustomField.default = val
                }
              "
            />
        </template>

        <template v-if="editCustomField.name==='isSuppressed' || editCustomField.name==='isProduction'">
        <CheckboxBox
            label="Default Value"
            :checked="defaultCheckboxValue || false"
            @check-clicked="() => { defaultCheckboxValue = !defaultCheckboxValue }"
          />
        </template>

        <div class="new-definition__buttons">
        <ButtonComponent
          label="Save"
          theme="save"
          size="small"
          @button-clicked="handleEditSave"
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
import InputDate from './../controls/InputDate.vue'
import CheckboxBox from './../controls/CheckboxBox.vue'
import DropdownControl from './../controls/DropdownControl.vue'
import ButtonComponent from './../controls/ButtonComponent'
import SlideModal from './../controls/SlideModal.vue'
import TableData from './../TableData.vue'
import AlertComponent from './../AlertComponent'
import DropdownSeverityControl from './../controls/DropdownSeverityControl.vue'
import FormLabel from './../controls/FormLabel.vue'

export default {
  components: {
    InputText,
    InputDate,
    CheckboxBox,
    ButtonComponent,
    DropdownControl,
    TableData,
    SlideModal,
    AlertComponent,
    DropdownSeverityControl,
    FormLabel
},
  props: {
  },
  data() {
    return {
      tableHeaders: [
        {
          display: 'Display',
          field: 'label',
          sortable: true,
          hide: false
        },
        {
          display: 'Hidden',
          field: 'hidden',
          sortable: false,
          hide: false
        },
        {
          display: 'Read-only',
          field: 'readOnly',
          sortable: false,
          hide: false
        },
        {
          display: 'Required',
          field: 'required',
          sortable: false,
          hide: false
        },
        {
          display: 'System',
          field: 'system',
          sortable: false,
          hide: false
        }
      ],
      fieldDefinitions: [],
      editCustomField: {},
      entityTypes: [],
      selectedEntityType: '',
      toggleAdd: false,
      toggleEdit: false,
      hiddenDisabled: false,
      readOnlyDisabled: false,
      requiredDisabled: false,
      newField: '',
      newHidden: false,
      newDefault: '',
      severityDropdown: [],
      testedDropdown: [],
      defaultCheckboxValue: false,
      alert: {
        messages: [],
        type: "",
        title: ""
      }
    }
  },
  async mounted() {
    await this.getPrimer()
    await this.getCustomFields()
  },
  methods: { 
    handleRowClick(row) {
      this.hiddenDisabled = false
      this.readOnlyDisabled = false
      this.requiredDisabled = false
      
      this.editCustomField = JSON.parse(JSON.stringify(row));
      if (this.editCustomField.system) {
        this.hiddenDisabled = true
        this.readOnlyDisabled = true
        this.requiredDisabled = true
      }
      this.defaultCheckboxValue = this.editCustomField.default === "true"
      
      this.toggleEdit = true;
    },
    handleCheckboxClick(name) {
      if (name === 'hidden') {
        this.editCustomField.readOnly = false
        this.editCustomField.hidden = !this.editCustomField.hidden
      }
      if (name === 'readOnly') {
        this.editCustomField.hidden = false
        this.editCustomField.readOnly = !this.editCustomField.readOnly
      }
      if (name === 'required') {
        this.editCustomField.required = !this.editCustomField.required
      }
    },
    handleDefaultDropdown(option) {
      if (option.value === 'all' || option.value === null) {
        this.editCustomField.default = null
      } else {
        this.editCustomField.default = option.value
      }
    },
    setSeverityValue() {
      if (this.editCustomField.default !== null && this.editCustomField.default !== '') {
        return this.editCustomField.default
      } else {
        return 'All'
      }
    },
    getPrimer() {
      return this.$axios
        .$get(`${this.$store.state.config.api_url}/admin/fielddefinition/primer`)
        .then((r) => {
          this.entityTypes = r?.data?.entityTypes
          this.selectedEntityType = this.entityTypes[0].value
          this.severityDropdown = r.data?.severityDropdown
          this.testedDropdown = r.data?.testedDropdown
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Getting Field Definition Primer")
        })
    },
    getCustomFields() {
      return this.$axios
        .$get(`${this.$store.state.config.api_url}/admin/fielddefinition/entity/${this.selectedEntityType}`)
        .then((r) => {
          this.fieldDefinitions = r?.data
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Getting Field Definitions")
        })
    },
    handleAlertClose() {
      this.alert = {
        messages: [],
        type: "",
        title: ""
      }
    },
    handleSelectType(field) {
      this.handleAlertClose();
      this.selectedEntityType = field.value
      this.getCustomFields()
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
    handleFieldUpdate(field) {
      this.newField = field.value
    },
    handleEditSave() {
      if (this.editCustomField.required && (this.editCustomField.default === '' || this.editCustomField.default === null) && (this.editCustomField.hidden || this.editCustomField.readOnly)) {
        this.addAlert(["If a field is required and either hidden or read-only, a default value must be selected"], "danger", "Cannot Save")
        return
      }

      delete this.editCustomField.timestamp
      delete this.editCustomField.lastUpdated
      
      const updatedField = this.editCustomField;
      if (updatedField.name === "isSuppressed" || updatedField.name === "isProduction") {
        updatedField.default = String(this.defaultCheckboxValue)
      } 
      
      return this.$axios
        .$post(`${this.$store.state.config.api_url}/admin/fielddefinition`, JSON.stringify(this.editCustomField), {
          headers: {
            'Content-Type': 'application/json',
          }
        })
        .then((r) => {
          this.addAlert(["Success Editing Field Definition"], "success", "Success")
          this.toggleEdit = false;
          this.editCustomField = r?.data
          const itemIndex = this.fieldDefinitions.findIndex(item => item.id === this.editCustomField.id);
  
          if (itemIndex !== -1) {
            this.fieldDefinitions.splice(itemIndex, 1, this.editCustomField);
          }
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Editing Field Definition")
      })
    }
  }
}
</script>

<style lang="scss" scoped>
.new-definition__buttons {
  margin-top: 30px;
  display: flex;
  gap: 20px;
}

.extraRoom {
  margin-top: 30px;
  display: flex;
  gap: 20px;
}

</style>
