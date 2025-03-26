<template>
    <div class="attribute_wrapper">
      <FormLabel label="Search Filter Definitions">
      </FormLabel>
      <h1>This page can be used to add, edit, or delete Search Filters.</h1>
      <div class="extraRoom">
        <DropdownControl
            theme="outline"
            label="Select Search Filter Type"
            :options="searchFilterTypes"
            class="btm-input"
            @update="(val) => handleSelectType(val)"
        />
      </div>
  
      <div class="extraRoom">
        <InputTextArea
          placeholder="Add Search Filter Json"
          :resize="true"
          class="btm-input"
          reff="txtsearchFilterValueJson"
          :value="JSON.stringify(selectedSearchFilter.filters, null, 2) || ''"
          :disabled=false
          @update="(val) => handleUpdateSearchfilter(val)"
        />
      </div>
      
      <div class="extraRoom">
        <ButtonComponent
          label="Save"
          theme="primary"
          size="medium"
          :disabled="false"
          @button-clicked="handleSave"
        />
        <ButtonComponent
            label="Add"
            theme="primary"
            size="medium"
            @button-clicked="handleAdd"
        />
      </div>
      
      <AlertComponent
        v-if="alert.messages.length > 0"
        class="alert-margin"
        :alert="alert"
        @close="handleAlertClose"
      />
      <SlideModal
        label="Add Search Filter"
        :open="toggleAdd"
        size="small"
        @toggle="() => (toggleAdd = !toggleAdd)"
        >   
            <InputText
            reff="txtNewFieldDefault"
            label="Type"
            id="newFieldTypeValue"
            placeholder="Type"
            :value="newType"
            @update="
                (val) => {
                    newType = val
                }
            "
            />
            <div class="extraRoom">
                <InputTextArea
                placeholder="Add Search Filter Json"
                :resize="true"
                class="btm-input"
                reff="txtSearchfilterValueJson"
                :value="JSON.stringify(newJson, null, 2) || ''"
                :disabled=false
                @update="
                    (val) => {
                        newJson = val
                    }
                "
                />
            </div>
             
            <div class="new-lookUp__buttons">
                <ButtonComponent
                label="Save"
                theme="save"
                :disabled="newType.length !== 0 && newJson.length !== 0"
                size="small"
                @button-clicked="handleNewSave"
                />
                <ButtonComponent
                label="Cancel"
                theme="cancel"
                size="small"
                @button-clicked="toggleAdd = !toggleAdd"
                />
            </div>
        </SlideModal>
  </div>
  </template>
  
  <script>
  import DropdownControl from './../controls/DropdownControl.vue'
  import ButtonComponent from './../controls/ButtonComponent'
  import AlertComponent from './../AlertComponent'
  import FormLabel from './../controls/FormLabel.vue'
  import InputTextArea from './../controls/InputTextArea'
  import InputText from './../controls/InputText'
  import SlideModal from './../controls/SlideModal.vue'
  
  export default {
    components: {
      ButtonComponent,
      DropdownControl,
      AlertComponent,
      FormLabel,
      InputText,
      InputTextArea,
      SlideModal
  },
    props: {
    },
    data() {
      return {
        searchFilterDefinitions: [],
        searchFilterTypes: [],
        selectedSearchFilter: {},
        selectedTypeId: '',
        isValidJson: true,
        toggleAdd: false,
        newJson: {},
        newType: '',
        alert: {
          messages: [],
          type: "",
          title: ""
        }
      }
    },
    mounted() {
        this.getSearchfilters()
    },
    methods: { 
        getSearchfilters() {
            return this.$axios
            .$post(`${this.$store.state.config.api_url}/Admin/searchfilters/search`, {})
            .then((r) => {
                this.searchFilterDefinitions = r?.data ?? []
                this.searchFilterDefinitions.forEach(lookUp => {
                    this.searchFilterTypes.push({ display: lookUp.type, value: lookUp.id, order: 0 })
                })
                this.selectedTypeId = this.searchFilterTypes[0].value;
                this.selectedSearchFilter = this.getSelectedSearchfilter(this.selectedTypeId);
            })
            .catch((error) => {
                this.handleErrorResponse(error, "Error getting Search Filters")
            })
        },
        handleAlertClose() {
            this.alert = {
                messages: [],
                type: "",
                title: ""
            }
        },
        handleAdd() {
            this.newType = ''
            this.newJson = ''
            this.toggleAdd = !this.toggleAdd
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
      handleSelectType(field) {
        this.handleAlertClose();
        this.selectedSearchFilter = this.getSelectedSearchfilter(field.value);
        this.validateJson(JSON.stringify(this.selectedSearchFilter.filters));
      },
      getSelectedSearchfilter(id) {
        return this.searchFilterDefinitions.find((lookUp) => lookUp.id === id)
      },
      validateJson(value) {
        if (value === "") {
          this.addAlert(["The JSON doc cannot be empty"], "danger", "JSON error")
          this.isValidJson = false;
          return;
        }
  
        try {
          this.selectedSearchFilter.filters = JSON.parse(value);
          this.isValidJson = true;
        } catch (error) {
          this.addAlert([error], "danger", "JSON error")
          this.isValidJson = false;
        }
      },
        handleUpdateSearchfilter(value) {
        this.validateJson(value);
      },
      handleSave() {
        if (!this.isValidJson) {
          this.addAlert(["The JSON doc is invalid and cannot be saved"], "danger", "Cannot Save");
          return;
        }
  
        this.selectedSearchFilter.timestamp = new Date().toISOString();
      
        return this.$axios
          .$post(`${this.$store.state.config.api_url}/admin/searchfilters`, JSON.stringify(this.selectedSearchFilter), {
            headers: {
              'Content-Type': 'application/json',
            }
          })
          .then((r) => {
            this.addAlert(["Success Saving Search Filter"], "success", "Success")
          })
          .catch((error) => {
            this.handleErrorResponse(error, "Error Saving Look  Up")
          })
      },
      handleNewSave() {
        this.validateJson(this.newJson);
        if (!this.isValidJson) {
          this.addAlert(["The JSON doc is invalid and cannot be saved"], "danger", "Cannot Save");
          return;
        }

        const newSearchfilter = {
            type: this.newType,
            filters: this.newJson,
            timestamp: new Date().toISOString()
        }

        return this.$axios
          .$post(`${this.$store.state.config.api_url}/admin/searchfilters`, JSON.stringify(newSearchfilter), {
            headers: {
              'Content-Type': 'application/json',
            }
          })
          .then((r) => {
            this.addAlert(["Success Saving Search Filter"], "success", "Success")
            this.toggleAdd = false;
            this.getSearchfilters();
          })
          .catch((error) => {
            this.handleErrorResponse(error, "Error Saving Look  Up")
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
  