<template>
  <div class="attribute_wrapper">
    <FormLabel label="Attribute Definitions">
    </FormLabel>
    <h1>This page can be used to add, edit, or delete attributes.</h1>
    <div class="extraRoom">
      <DropdownControl
          theme="outline"
          label="Select Attribute Type"
          :options="attributeTypes"
          class="btm-input"
          @update="(val) => handleSelectType(val)"
      />
    </div>

    <TableData
      :headers="tableHeaders"
      :disable-hover="true"
      :rows="this.selectedAttribute.values"
      row-size="medium"
      :toggle-rows="true"
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
      <ButtonComponent
        label="Delete"
        theme="danger"
        size="medium"
        :disabled="checkedRows.length < 1"
        @button-clicked="handleDeleteChecked"
      />
      <!--<ButtonComponent
        label="Save"
        theme="primary"
        size="medium"
        :disabled="false"
        @button-clicked="handleSave"
      />-->
    </div>
    
    <SlideModal
      label="Add Attribute Definition"
      :open="toggleAdd"
      size="small"
      @toggle="() => (toggleAdd = !toggleAdd)"
    > 

      <InputText
        reff="txtNewFieldDisplay"
        label="Name"
        id="newFieldDisplayValue"
        placeholder="Name"
        :value="selectedAttributeValue.display"
        @update="
          (val) => {
            selectedAttributeValue.display = val
          }
        "
      />

      <DropdownControl
          theme="outline"
          label="Select Attribute Field Type"
          :options="attributeValueTypes"
          class="btm-input"
          @update="(val) => handleSelectValueType(val)"
      />

      <template v-if="selectedAttributeValue.type.startsWith('Single select') || selectedAttributeValue.type.startsWith('Multi select')">
        <div >
          <div style="margin-top: 20px;">
            <div>Drop down Items</div>
            <input v-model="newOptionItem" placeholder="Enter new item" @keyup.enter="handleOptionAdd" /> <span @click="handleOptionAdd" style="cursor: pointer;">➕</span>
          </div>
          <div v-for="(item, index) in this.selectedAttributeValue.options" :key="index" style="display: flex; align-items: center; gap: 10px;background-color: #f0f0f0;">
            <span>{{ item }} <span @click="handleOptionDelete(item)" style="cursor: pointer;"> 🗑 </span></span>
          </div>
        </div>
      </template>
     
      <template v-if="selectedAttributeValue.type.includes('text')">
        <InputText
          reff="txtNewFieldDefault"
          label="Default Value"
          id="newFieldDefaultValue"
          placeholder="Default Value"
          :value="selectedAttributeValue.default"
          @update="
            (val) => {
              selectedAttributeValue.default = val
            }
          "
        />
      </template>

      <template v-if="selectedAttributeValue.type.includes('long') || selectedAttributeValue.type.includes('double')">
        <InputNumber
          reff="txtNewFieldDefault"
          label="Default Value"
          id="newFieldDefaultValue"
          placeholder="Default Value"
          :value="selectedAttributeValue.default"
          @update="
            (val) => {
              selectedAttributeValue.default = val
            }
          "
        />
      </template>

      <template v-if="selectedAttributeValue.type.includes('date')">
        <InputDate
            reff="txtNewFieldDefault"
            label="Default Value"
            class="text-field_asset btm-input"
            :value="selectedAttributeValue.default"
            @update="
              (val) => {
              selectedAttributeValue.default = val
            }
            "
          />
      </template>

      <template v-if="selectedAttributeValue.type.startsWith('Single select')">
        <DropdownControl
          reff="txtNewFieldDefault"
          label="Default Value"
          class="assesment-dropdown btm-input"
          :options="formatAttributeOptions(selectedAttributeValue.options || [])"
          :value=selectedAttributeValue.default
          @update="
              (val) => {
              selectedAttributeValue.default = val.value
            }
            "
        />
      </template>

      <template v-if="selectedAttributeValue.type.includes('Multi select')">
        <MultiSelect
          reff="txtNewFieldDefault"
          label="Default Value"
          class="assesment-dropdown btm-input"
          :options="formatAttributeMultiOptions(selectedAttributeValue.options || [])"
          :values="getMultiSelectValue(selectedAttributeValue.default)"
          @update="
              (val) => {
              selectedAttributeValue.default = val
            }
            "
        />
      </template>

      <InputText v-if="false"
        reff="txtNewFieldSection"
        label="Section"
        id="newFieldSectionValue"
        placeholder="Section"
        :value="selectedAttributeValue.section"
        @update="
          (val) => {
            selectedAttributeValue.section = val
          }
        "
      />

      <InputNumber
        reff="txtNewFieldOrder"
        label="Order"
        id="newFieldOrderValue"
        placeholder="Order"
        :value="String(selectedAttributeValue.order)"
        @update="
          (val) => {
            selectedAttributeValue.order = val
          }
        "
      />

      <div style="display: flex; gap: 30px;">
      <CheckboxBox
        label="Hidden"
        :checked="selectedAttributeValue.hidden"
        @check-clicked="handleCheckboxClick('hidden')"
      />
      <CheckboxBox
        label="Read-only"
        :checked="selectedAttributeValue.readOnly"
        @check-clicked="handleCheckboxClick('readOnly')"
      />
      <CheckboxBox
        label="Required"
        :checked="selectedAttributeValue.required"
        @check-clicked="handleCheckboxClick('required')"
      />
      </div>
      
      <div class="new-definition__buttons">
      <ButtonComponent
        label="Save"
        theme="save"
        :disabled="(selectedAttributeValue.display === null || selectedAttributeValue.display.length === 0)"
        size="small"
        @button-clicked="handleNewSave"
      />
      <ButtonComponent
        label="Cancel"
        theme="cancel"
        size="small"
        @button-clicked=closeAdd()
      />
      </div>

    </SlideModal>


    <SlideModal
      label="Edit Attribute Definition"
      :open="toggleEdit"
      size="small"
      @toggle="() => (toggleEdit = !toggleEdit)"
    >   
      <InputText
        reff="txtEditFieldDisplay"
        label="Name"
        id="editFieldDisplayValue"
        placeholder="Name"
        :value="selectedAttributeValue.display"
        @update="
          (val) => {
            selectedAttributeValue.display = val
          }
        "
      />

      <DropdownControl
          theme="outline"
          label="Select Attribute Field Type"
          :options="attributeValueTypes"
          class="btm-input"
          :value="selectedAttributeValue.type"
          @update="(val) => handleSelectValueType(val)"
      />

      <template v-if="selectedAttributeValue.type.startsWith('Single select') || selectedAttributeValue.type.startsWith('Multi select')">
        <div >
          <div style="margin-top: 20px;">
            <div>Drop down Items</div>
            <input v-model="newOptionItem" placeholder="Enter new item" @keyup.enter="handleOptionAdd" /> <span @click="handleOptionAdd" style="cursor: pointer;">➕</span>
          </div>
          <div v-for="(item, index) in this.selectedAttributeValue.options" :key="index" style="display: flex; align-items: center; gap: 10px;background-color: #f0f0f0;">
            <span>{{ item }} <span @click="handleOptionDelete(item)" style="cursor: pointer;"> 🗑 </span></span>
          </div>
        </div>
      </template>
     
      <template v-if="selectedAttributeValue.type.includes('text')">
        <InputText
          reff="txtEditFieldDefault"
          label="Default Value"
          id="editFieldDefaultValue"
          placeholder="Default Value"
          :value="selectedAttributeValue.default"
          @update="
            (val) => {
              selectedAttributeValue.default = val
            }
          "
        />
      </template>

      <template v-if="selectedAttributeValue.type.includes('long') || selectedAttributeValue.type.includes('double')">
        <InputNumber
          reff="txtEditFieldDefault"
          label="Default Value"
          id="editFieldDefaultValue"
          placeholder="Default Value"
          :value="selectedAttributeValue.default"
          @update="
            (val) => {
              selectedAttributeValue.default = val
            }
          "
        />
      </template>

      <template v-if="selectedAttributeValue.type.includes('date')">
        <InputDate
            reff="txtEditFieldDefault"
            label="Default Value"
            class="text-field_asset btm-input"
            :value="selectedAttributeValue.default"
            @update="
              (val) => {
              selectedAttributeValue.default = val
            }
            "
          />
      </template>

      <template v-if="selectedAttributeValue.type.startsWith('Single select')">
        <DropdownControl
          reff="txtNewFieldDefault"
          label="Default Value"
          class="assesment-dropdown btm-input"
          :options="formatAttributeOptions(selectedAttributeValue.options || [])"
          :value=selectedAttributeValue.default
          @update="
              (val) => {
              selectedAttributeValue.default = val.value
            }
            "
        />
      </template>

      <template v-if="selectedAttributeValue.type.includes('Multi select')">
        <MultiSelect
          reff="txtNewFieldDefault"
          label="Default Value"
          class="assesment-dropdown btm-input"
          :options="formatAttributeMultiOptions(selectedAttributeValue.options || [])"
          :values="getMultiSelectValue(selectedAttributeValue.default)"
          @update="
              (val) => {
              selectedAttributeValue.default = val
            }
            "
        />
      </template>

      <InputText v-if="false"
        reff="txtEditFieldSection"
        label="Section"
        id="editFieldSectionValue"
        placeholder="Section"
        :value="selectedAttributeValue.section"
        @update="
          (val) => {
            selectedAttributeValue.section = val
          }
        "
      />

      <InputNumber
        reff="txtEditFieldOrder"
        label="Order"
        id="editFieldOrderValue"
        placeholder="Order"
        :value="String(selectedAttributeValue.order)"
        @update="
          (val) => {
            selectedAttributeValue.order = val
          }
        "
      />

      <div style="display: flex; gap: 30px;">
      <CheckboxBox
        label="Hidden"
        :checked="selectedAttributeValue.hidden"
        @check-clicked="handleCheckboxClick('hidden')"
      />
      <CheckboxBox
        label="Read-only"
        :checked="selectedAttributeValue.readOnly"
        @check-clicked="handleCheckboxClick('readOnly')"
      />
      <CheckboxBox
        label="Required"
        :checked="selectedAttributeValue.required"
        @check-clicked="handleCheckboxClick('required')"
      />
      </div>

      <div class="new-definition__buttons">
        <ButtonComponent
          label="Save"
          theme="save"
          :disabled="(selectedAttributeValue.display === null || selectedAttributeValue.display.length === 0)"
          size="small"
          @button-clicked="handleEditSave"
        />
        <ButtonComponent
          label="Cancel"
          theme="cancel"
          size="small"
          @button-clicked=closeEdit()
        />
      </div>

    </SlideModal>
    
    <AlertComponent
      v-if="alert.messages.length > 0"
      class="alert-margin"
      :alert="alert"
      @close="handleAlertClose"
    />
</div>
</template>

<script>
import InputText from './../controls/InputText.vue'
import InputDate from './../controls/InputDate.vue'
import InputNumber from './../controls/InputNumber.vue'
import CheckboxBox from './../controls/CheckboxBox.vue'
import DropdownControl from './../controls/DropdownControl.vue'
import MultiSelect from './../controls/MultiSelect.vue'
import ButtonComponent from './../controls/ButtonComponent'
import AlertComponent from './../AlertComponent'
import FormLabel from './../controls/FormLabel.vue'
import TableData from './../TableData.vue'
import SlideModal from './../controls/SlideModal.vue'

export default {
  components: {
    ButtonComponent,
    InputText,
    InputDate,
    InputNumber,
    CheckboxBox,
    DropdownControl,
    MultiSelect,
    AlertComponent,
    FormLabel,
    TableData,
    SlideModal
},
  props: {
  },
  data() {
    return {
      attributedefinitions: [],
      attributeTypes: [],
      selectedAttribute: {},
      selectedAttributeValue: {
        isInternal: false,
        default: "",
        hidden: false,
        readOnly: false,
        display: "",
        name: "",
        options: [],
        section: null,
        type: "Single line text (text)",
        required: false,
        order: 0
      },
      attributeValueTypes: [
        { "display": "Single line text (text)", "value": "Single line text (text)", "order": 1},
        { "display": "Multi line text (text)", "value": "Multi line text (text)", "order": 2},
        { "display": "Markdown (text)", "value": "Markdown (text)", "order": 3},
        { "display": "Integer (long)", "value": "Integer (long)", "order": 4},
        { "display": "Number (double)", "value": "Number (double)", "order": 5},
        { "display": "Date (date)", "value": "Date (date)", "order": 6},
        { "display": "Single select drop down", "value": "Single select drop down", "order": 7},
        { "display": "Multi select drop down", "value": "Multi select drop down", "order": 8}
      ],
      checkedRows: [],
      newOptionItem: "",
      toggleAdd: false,
      toggleEdit: false,
      selectedTypeId: '',
      isValidJson: true,
      alert: {
        messages: [],
        type: "",
        title: ""
      }
    }
  },
  computed: {
    tableHeaders() {
      return [
        {
          display: 'Attribute',
          field: 'display',
          sortable: true,
          hide: false,
          nowrap: true
        },
        {
          display: 'Hidden',
          field: 'hidden',
          sortable: false,
          hide: false,
          nowrap: true
        },
        {
          display: 'Read-only',
          field: 'readOnly',
          sortable: false,
          hide: false,
          nowrap: true
        },
        {
          display: 'Required',
          field: 'required',
          sortable: false,
          hide: false,
          nowrap: true
        }
      ]
    },
  },
  mounted() {
    this.getAttributes()
  },
  methods: { 
    handleCheckedRowsChanged(checkedRows) {
      this.checkedRows = checkedRows
    },
    handleOptionAdd(val) {

      if (this.newOptionItem === "") return;
      const exists = this.selectedAttributeValue.options.some(
        item => item.toLowerCase() === this.newOptionItem.toLowerCase()
      );

      if (!exists) {
        this.selectedAttributeValue.options.push(this.newOptionItem)  
      }
      this.newOptionItem = ""
    },
    handleOptionDelete(val) {
      const index = this.selectedAttributeValue.options.findIndex(x => x === val);
      if (index !== -1) {
        this.selectedAttributeValue.options.splice(index, 1);
      }
    },
    handleRowClick(row) {
      this.selectedAttributeValue = JSON.parse(JSON.stringify(row));
      if (this.selectedAttributeValue.options === null) {
        this.selectedAttributeValue.options = [];
      }
      this.toggleEdit = true;
    },
    handleCheckboxClick(name) {
      if (name === 'hidden') {
        this.selectedAttributeValue.readOnly = false
        this.selectedAttributeValue.hidden = !this.selectedAttributeValue.hidden
      }
      if (name === 'readOnly') {
        this.selectedAttributeValue.hidden = false
        this.selectedAttributeValue.readOnly = !this.selectedAttributeValue.readOnly
      }
      if (name === 'required') {
        this.selectedAttributeValue.required = !this.selectedAttributeValue.required
      }
    },
    handleAdd() {
      this.resetSelectedAttributeValue();
      this.toggleAdd = !this.toggleAdd
    },
    handleSelectType(field) {
      this.handleAlertClose();
      this.selectedTypeId = field.value;
      this.selectedAttribute = this.getSelectedAttribute(field.value); 
    },
    handleSelectValueType(val) {
      this.handleAlertClose();
      this.selectedAttributeValue.type = val.display
    },
    formatAttributeOptions(opts) {
      return [{ display: "Select", value: null },  ...opts.map((o) => {
        return {
          display: o,
          value: o
        }
      })]
    },
    formatAttributeMultiOptions(opts) {
      return opts || []
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
    getMultiSelectValue(val) {
      const currentVal = val
      const opts = this.selectedAttributeValue.options || []
      return this.formatMultiSelectValue(currentVal, opts)
    },
    getAttributes() {
      return this.$axios
        .$get(`${this.$store.state.config.api_url}/admin/attributes/primer`)
        .then((r) => {
          this.attributedefinitions = r?.data?.attributeDefinitions ?? []
          this.attributedefinitions.forEach(attribute => {
            this.attributeTypes.push({ display: attribute.type, value: attribute.id, order: 0 })
          })
          this.selectedTypeId = this.attributeTypes[0].value;
          this.selectedAttribute = this.getSelectedAttribute(this.selectedTypeId);
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Getting Attribute")
        })
    },
    handleAlertClose() {
      this.alert = {
        messages: [],
        type: "",
        title: ""
      }
    },
    closeAdd() {
      this.toggleAdd = !this.toggleAdd
    },
    closeEdit() {
      this.toggleEdit = !this.toggleEdit
    },
    resetSelectedAttributeValue() {
      this.selectedAttributeValue = {
        isInternal: false,
        default: "",
        hidden: false,
        readOnly: false,
        display: "",
        name: "",
        options: [],
        section: null,
        type: "Single line text (text)",
        required: false,
        order: 0
      };
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
    getSelectedAttribute(id) {
      return this.attributedefinitions.find((attribute) => attribute.id === id)
    },
    attributeExists() {
      if (this.selectedAttribute.values.filter(x => x.display.toLowerCase() === this.selectedAttributeValue.display.toLowerCase()).length > 0) {
        this.addAlert(["That attribute name already exists. Please enter a unique name."], "danger", "Name Already Exists")
        return true;
      }
      return false;
    },
    handleNewSave() {
      if (this.valid()) {
        if (this.attributeExists()) {
          return;
        }

        if (this.selectedAttributeValue.type.includes('Multi select') && this.selectedAttributeValue.default) {
          this.selectedAttributeValue.default = JSON.stringify(this.selectedAttributeValue.default)
        }

        this.selectedAttributeValue.name = this.selectedAttributeValue.display.toLowerCase().replace(/ /g, "_")
        this.selectedAttribute.values.push(this.selectedAttributeValue);
        this.handleSave();
        this.closeAdd();
      }
    },
    handleEditSave() {
      if (this.valid()) {
        const targetIndex = this.selectedAttribute.values.findIndex(i => i.name === this.selectedAttributeValue.name);
        const prevName = this.selectedAttribute.values[targetIndex].display;
        if (prevName.toLowerCase() !== this.selectedAttributeValue.display.toLowerCase()) {
          if (this.attributeExists()) {
            return;
          }
        }

        if (this.selectedAttributeValue.type.includes('Multi select') && this.selectedAttributeValue.default) {
          this.selectedAttributeValue.default = JSON.stringify(this.selectedAttributeValue.default)
        }

        this.selectedAttribute.values[targetIndex] = this.selectedAttributeValue
        this.selectedAttribute.values = [...this.selectedAttribute.values]
        this.handleSave();
        this.closeEdit();
      }
    },
    handleDeleteChecked() {
      const checkedNames = this.checkedRows.map((row) => row.name).join(",");
      const namesToRemove = checkedNames.split(",");
      this.selectedAttribute.values = this.selectedAttribute.values.filter(row => !namesToRemove.includes(row.name));
      this.handleSave();
      this.checkedRows = [];
    },
    valid() {
      if (this.selectedAttributeValue.required && (this.selectedAttributeValue.default === '' || this.selectedAttributeValue.default === null) && (this.selectedAttributeValue.hidden || this.selectedAttributeValue.readOnly)) {
        this.addAlert(["If a field is required and either hidden or read-only, a default value must be selected"], "danger", "Cannot Save")
        return false;
      }
      return true;
    },
    handleSave() {
      this.selectedAttribute.timestamp = new Date().toISOString();
      
      return this.$axios
        .$post(`${this.$store.state.config.api_url}/admin/attributes`, JSON.stringify(this.selectedAttribute), {
          headers: {
            'Content-Type': 'application/json',
          }
        })
        .then((r) => {
          this.addAlert(["Success Saving Attribute Definition"], "success", "Success")
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Saving Attribute Definition")
        })
    },
    
    
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
