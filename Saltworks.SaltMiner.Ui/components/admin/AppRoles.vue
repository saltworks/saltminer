<template>
  <div class="roles__wrapper">
    <FormLabel label="Roles"></FormLabel>
    <div class="button-row">
      <div class="button-left">
        <h1>This page can be used to add, edit, or delete application roles for access control</h1>
      </div>
      <div class="button-right">
          <SearchControl
            :placeholder="searchControlPlaceholder"
            reff="txtSearchIssues"
            :facets="search.facets"
            :query="search.query"
            @input="handleSearchUpdate"
            @facet-click="handleFilterChange"
            @search="handleSearchControl"
          />
      </div>
    </div>
    
    <TableData
      :headers="tableRoleHeaders"
      :disable-hover="true"
      :rows="formattedRoles"
      row-size="medium"
      :toggle-rows="true"
      :sortable="false"
      @row-click="handleRoleRowClick"
      @checked-rows-changed="handleCheckedRoleRowsChanged"
    />
    <PaginationComponent
      :current-page="currentPage"
      :total-results="paginationTotalResults"
      @page-change="handlePageChange"
      @amount-change="handlePageChange"
      @pagination-mounted="handlePageChange"
    />
    <div class="extraRoom">
      <ButtonComponent
        label="Create"
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
    </div>
    <SlideModal
      label="Create Role"
      :open="toggleAdd"
      size="medium"
      @toggle="() => (toggleAdd = !toggleAdd)"
    >
      <div id="role-name" class="input-flex">
        <FormLabel class="input-label" label="Name" />
        <InputText
          reff="txtRoleName"
          class="text-field_asset btm-input"
          placeholder="Enter Name"
          :value="selectedRole && selectedRole.name || ''"
          @update="(val) => handleInput('name', val)"
        />
      </div>

      <FormLabel label="Current Field Restrictions"></FormLabel>
      <TableData
        :headers="tablePermHeaders"
        :disable-hover="true"
        :rows="formattedPerms"
        row-size="medium"
        :sortable="false"
        @row-click="handleRestrictionClick"
      />

      <div style="display: flex; gap: 30px;" class="extraRoom">
        <FormLabel label="Add Field Restriction"></FormLabel>
        <ButtonComponent
          style="padding: 5px 10px"
          label="+"
          theme="primary"
          :disabled="false"
          size="xsmall"
          @button-clicked="restrictionsVisible = !restrictionsVisible"
        />
      </div>
      
      <template v-if="restrictionsVisible">
        <DropdownControl
          theme="outline"
          label="Field Type"
          reff="txtFieldType"
          :options="fieldTypes"
          :value="selectedFieldType"
          @update="handleFieldTypeUpdate"
        />

        <DropdownControl
          theme="outline"
          label="Available Fields"
          reff="txtField"
          :options="availableFields"
          :value="selectedField"
          @update="handleFieldUpdate"
        />

        <div style="display: flex; gap: 30px;">
          <CheckboxBox
            label="Hidden"
            :checked="hiddenCheckboxSelection"
            @check-clicked="handlePermCheckbox('hidden')"
          />
          <CheckboxBox
            label="Read-only"
            :checked="readOnlyCheckboxSelection"
            @check-clicked="handlePermCheckbox('readOnly')"
          />
          <div style="padding: 10px 10px;">
            <ButtonComponent
              style="padding: 10px 20px"
              label="Add"
              theme="primary"
              :disabled="availableFields.length === 0"
              size="xsmall"
              @button-clicked="handleAddRestriction"
            />
          </div>
        </div>
      </template>

      <div style="border-top: 5px solid lightgray;  padding: 20px; margin-top: 10px"></div>

      <FormLabel label="Action Restrictions"></FormLabel>
      <template>
        <table class="actions-table">
          <tbody>
            <tr v-for="item in sortedActions" :key="item.name" class="actions-row" @click="handleActionRowClick(item)">
              <td class="actions-cell">{{ item.description }}</td>
              <td class="actions-cell">
                <CheckboxBox
                  :checked="actionChecked(item.name)"
                />
              </td>
            </tr>
          </tbody>
        </table>
      </template>

      <div class="extraRoom">
        <ButtonComponent
          label="Save Role"
          theme="primary"
          :disabled="selectedRole.name.trim() === ''"
          size="medium"
          @button-clicked="handleNewSave"
        />
      </div>
    </SlideModal>
    
    <SlideModal
      label="Edit Role"
      :open="toggleEdit"
      size="medium"
      @toggle="() => (toggleEdit = !toggleEdit)"
    >   
      <div id="role-name" class="input-flex">
        <FormLabel class="input-label" label="Name" />
        <InputText
          reff="txtRoleName"
          class="text-field_asset btm-input"
          placeholder="Enter Name"
          :disabled="true"
          :value="selectedRole && selectedRole.name || ''"
          @update="(val) => handleInput('name', val)"
        />
      </div>

      <FormLabel label="Current Field Restrictions"></FormLabel>
      <TableData
        :headers="tablePermHeaders"
        :disable-hover="true"
        :rows="formattedPerms"
        row-size="medium"
        :sortable="false"
        @row-click="handleRestrictionClick"
      />

      <div style="display: flex; gap: 30px;" class="extraRoom">
        <FormLabel label="Add Field Restriction"></FormLabel>
        <ButtonComponent
          style="padding: 5px 10px"
          label="+"
          theme="primary"
          :disabled="false"
          size="xsmall"
          @button-clicked="restrictionsVisible = !restrictionsVisible"
        />
      </div>

      <template v-if="restrictionsVisible">
        <DropdownControl
          theme="outline"
          label="Field Type"
          reff="txtFieldType"
          :options="fieldTypes"
          :value="selectedFieldType"
          @update="handleFieldTypeUpdate"
        />

        <DropdownControl
          theme="outline"
          label="Available Fields"
          reff="txtField"
          :options="availableFields"
          :value="selectedField"
          @update="handleFieldUpdate"
        />
      
        <div style="display: flex; gap: 30px;">
          <CheckboxBox
            label="Hidden"
            :checked="hiddenCheckboxSelection"
            @check-clicked="handlePermCheckbox('hidden')"
          />
          <CheckboxBox
            label="Read-only"
            :checked="readOnlyCheckboxSelection"
            @check-clicked="handlePermCheckbox('readOnly')"
          />
          <div style="padding: 10px 10px;">
            <ButtonComponent
              style="padding: 10px 20px"
              label="Add"
              theme="primary"
              :disabled="availableFields.length === 0"
              size="xsmall"
              @button-clicked="handleAddRestriction"
            />
          </div>
        </div>
      </template>

     
      <div style="border-top: 5px solid lightgray;  padding: 20px; margin-top: 10px"></div>

      <FormLabel label="Action Restrictions"></FormLabel>
      <template>
        <table class="actions-table">
          <tbody>
            <tr v-for="item in sortedActions" :key="item.name" class="actions-row" @click="handleActionRowClick(item)">
              <td class="actions-cell">{{ item.description }}</td>
              <td class="actions-cell">
                <CheckboxBox
                  :checked="actionChecked(item.name)"
                />
              </td>
            </tr>
          </tbody>
        </table>
      </template>

      <div class="extraRoom">
        <ButtonComponent
          label="Save Role"
          theme="primary"
          :disabled="selectedRole.name === ''"
          size="medium"
          @button-clicked="handleEditSave"
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
import { mapState } from 'vuex'
import ButtonComponent from './../controls/ButtonComponent'
import SlideModal from './../controls/SlideModal.vue'
import TableData from './../TableData.vue'
import AlertComponent from './../AlertComponent'
import FormLabel from './../controls/FormLabel.vue'
import InputText from './../../components/controls/InputText'
import helpers from './../../components/Utility/helpers.js'
import PaginationComponent from './../../components/PaginationComponent.vue'
import DropdownControl from './../../components/controls/DropdownControl.vue'
import SearchControl from './../../components/controls/SearchControl.vue'
import CheckboxBox from './../../components/controls/CheckboxBox.vue'


export default {
  components: {
    ButtonComponent,
    TableData,
    SlideModal,
    AlertComponent,
    FormLabel,
    InputText,
    PaginationComponent,
    DropdownControl,
    SearchControl,
    CheckboxBox
},
  props: {
  },
  data() {
    return {
      role: [],
      perms: [],
      newPerm: {
        "scope": '',
        "fieldName": '',
        "permission": 'h'
      },
      selectedRole: {
        "id": '',
        "name": '',
        "permissions": [],
        "actions": []
      },
      fieldTypes: [],
      selectedFieldType: '',
      selectedField: '',
      roleSourceFields: [],
      roleActions: [],
      roleFields: ["name", "lastUpdated"],
      availableFields: [],
      restrictionsVisible: false,
      hiddenCheckboxSelection: true,
      readOnlyCheckboxSelection: false,
      permFields: ["fieldName", "permission"],
      toggleAdd: false,
      toggleEdit: false,
      checkedRows: [],
      searchControlPlaceholder: 'search roles...',
      alert: {
        messages: [],
        type: "",
        title: ""
      },
      defaultDateFormat: 'en-US',
      defaultFilter: {
        display: 'All Fields',
        field: 'all',
      },
      currentFilter: {
        display: 'All Fields',
        field: 'all',
      },
      search: {
        facets: [
          {
            label: 'All Fields',
            value: 'all',
          },
        ],
        query: '',
      },
      paginationTotalResults: 0,
      currentPerPage: 10,
      currentPage: 1
    }
  },
  computed: {
    ...mapState({
      dateFormat: (state) => state.modules.user.dateFormat,
      user: (state) => state.modules.user,
      isLoading: (state) => state.loading.loading,
      pageTitle: (state) => state.config.pageTitle
    }),
    formattedFilters() {
      return this.search.facets.map((facet) => {
        return {
          display: facet.display,
          field: facet.field,
          sortable: true,
        }
      })
    },
    sortedActions() {
      return this.roleActions?.slice()?.sort((a, b) => a.name - b.name);
    },
    tableRoleHeaders() {
      return [
        {
          display: 'Role',
          field: 'name',
          sortable: true,
          hide: false,
          nowrap: true
        },
        {
          display: 'Last Updated',
          field: 'lastUpdated',
          sortable: false,
          hide: false,
          nowrap: true
        }
      ]
    },
    tablePermHeaders() {
      return [
        {
          display: 'Field',
          field: 'fieldName',
          sortable: false,
          hide: false,
          nowrap: true
        },
        {
          display: 'Scope',
          field: 'scope',
          sortable: false,
          hide: false,
          nowrap: true
        },
        {
          display: 'Restriction',
          field: 'permission',
          sortable: false,
          hide: false,
          nowrap: true
        },
        {
          display: '',
          field: 'deleteColumn',
          sortable: false,
          hide: false
        },
        {
          display: 'FieldId',
          field: 'fieldId',
          sortable: false,
          hide: true,
          nowrap: true
        }
      ]
    },
    formattedRoles() {
      return this.role.map((role) => {
        return {
          ...role,
          lastUpdated: helpers.formatDate(role.lastUpdated, 'M/D/yyyy h:mm:ss a')
        }
      })
    },
    formattedPerms() {
      return this.selectedRole.permissions.map((perm) => {
        return {
          ...perm,
          fieldId: perm.fieldName,
          fieldName: this.getFieldDisplay(perm.fieldName, perm.scope),
          permission: this.formatPermission(perm.permission)
        }
      })
    }
  },
  async mounted() {
    await this.getRolesPrimer()
  },
  methods: { 
    handleInput(field, value) {
      this.selectedRole[field] = value
    },
    handleCheckedRoleRowsChanged(checkedRows) {
      this.checkedRows = checkedRows
    },
    handleFilterChange(newFilter) {
      this.currentFilter =
        this.currentFilter.field.toLowerCase() === newFilter.field.toLowerCase()
          ? this.defaultFilter
          : newFilter
    },
    handleRoleRowClick(row) {
      const selectedRow = JSON.parse(JSON.stringify(row));
      this.initNewRole();
      this.selectedRole.id = selectedRow.id
      this.selectedRole.name = selectedRow.name
      this.selectedRole.permissions = selectedRow.permissions
      this.selectedRole.actions = selectedRow.actions
      this.setAvailableFields(this.fieldTypes[0].value)
      this.handlePermCheckbox('hidden')
      this.restrictionsVisible = false
      this.toggleEdit = true;
    },
    handleAddRestriction() {
      let permissionValue = 'h'
      if (this.readOnlyCheckboxSelection) {
        permissionValue = 'r'
      }
      this.newPerm = {
        "fieldName": this.selectedField,
        "scope": this.selectedFieldType,
        "permission": permissionValue
      }
      
      this.selectedRole.permissions.push(this.newPerm)
      this.setAvailableFields(this.selectedFieldType)
    },
    handleRestrictionClick(row) {
      const index = this.selectedRole.permissions.findIndex(x => x.fieldName === row.fieldId && x.scope === row.scope);
      if (index !== -1) {
        this.selectedRole.permissions.splice(index, 1);
      }
      this.setAvailableFields(this.selectedFieldType);
    },
    handlePermCheckbox(value) {
      if (value === 'hidden') {
        this.hiddenCheckboxSelection = true
        this.readOnlyCheckboxSelection = false
      }
      if (value === 'readOnly') {
        this.hiddenCheckboxSelection = false
        this.readOnlyCheckboxSelection = true
      }
    },
    getFieldDisplay(val, scope) {
      return this.roleSourceFields.filter(f => f.name === val && f.type === scope).map(d => d.display).join(",")
    },
    handleActionRowClick(item) {
      const selected = this.actionChecked(item.name)
      if (selected) {
        this.selectedRole.actions = this.selectedRole.actions.filter(x => x.name !== item.name)
      } else {
        this.selectedRole.actions.push(
          {
            name: item.name,
            label: item.description,
            disable: true
          }
        )
      }
    },
    actionChecked(val) {
      return this.selectedRole?.actions?.some(x => x.name === val)
    },
    formatPermission(val) {
      if (val === 'r') {
          return 'Read-only'
      }
      if (val === 'h') {
          return 'Hidden'
      }
    },
    setAvailableFields(fieldType){
      this.selectedFieldType = fieldType;
      this.availableFields = 
      this.roleSourceFields.filter(
        (source) => !this.selectedRole.permissions.some(perm => perm.fieldName === source.name && source.type === perm.scope) && source.type === fieldType).map(function(field) {
           return {
              ...field,
              display: field.display,
              value: field.name,
              order: field.order
            }
        })

        if (this.availableFields.length > 0) {
          this.selectedField = this.availableFields[0].name
        }
        
    },
    getRolesPrimer() {
      return this.$axios
        .$get(`${this.$store.state.config.api_url}/admin/role/primer`)
        .then((r) => {
          this.fieldTypes = r?.data.fieldPermissionScopes
          // this.selectedFieldType = this.fieldTypes[0].value
          this.roleSourceFields = r?.data.fields
          this.roleActions = r?.data.actions
          this.setAvailableFields(this.fieldTypes[0].value)

          this.search.facets =
            r.data?.searchFilters?.map((facet) => {
              return {
                display: facet.value,
                field: facet.field.toLowerCase(),
              }
            }) || []
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Getting Roles")
        })
    },
    handleAlertClose() {
      this.alert = {
        messages: [],
        type: "",
        title: ""
      }
    },
    validateRole() {
      if (this.selectedRole && this.selectedRole.name.trim() === "") {
        this.addAlert(["The Name field is required to save"], "danger", "Required");
        return false;
      }

      return true;
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
      this.selectedField = field.name
    },
    handleNewSave() {
      if (!this.validateRole()) {
        return;
      }

      return this.$axios
        .$post(`${this.$store.state.config.api_url}/admin/role`, JSON.stringify(this.selectedRole), {
          headers: {
            'Content-Type': 'application/json',
          }
        })
        .then((r) => {
          this.toggleAdd = false;
          this.addAlert(["Success Adding Role"], "success", "Success")
          this.role.push(r.data);
          this.paginationTotalResults = this.role.length
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Adding Role")
        })
    },
    handleEditSave() {
      if (!this.validateRole()) {
        return;
      }

      return this.$axios
        .$post(`${this.$store.state.config.api_url}/admin/role`, JSON.stringify(this.selectedRole), {
          headers: {
            'Content-Type': 'application/json',
          }
        })
        .then((r) => {
          this.toggleEdit = false;
          this.addAlert(["Success Editing Role"], "success", "Success")
          const itemIndex = this.role.findIndex(item => item.id === this.selectedRole.id);
  
          if (itemIndex !== -1) {
            this.role.splice(itemIndex, 1, this.selectedRole);
          }
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Editing Role")
        })
    },
    handleDeleteChecked() {
      const Ids = this.checkedRows.map((row) => row.id)

      if (Ids?.length < 1) return

      const data = {
        Ids,
      }

      return this.$axios
        .$post(`${this.$store.state.config.api_url}/admin/role/delete`, JSON.stringify(data), {
          headers: {
            'Content-Type': 'application/json',
          }
        })
        .then((r) => {
          this.role = this.role.filter(item => !Ids.includes(item.id))
          this.addAlert(["Success Removed Role(s)"], "success", "Success")
          // this.role = r?.data
          this.checkedRows = []
          this.paginationTotalResults = this.role.length
        })
        .catch((error) => {
          this.handleErrorResponse(error, "Error Removing Role(s)")
        })
    },
    initNewRole() {
      this.perms = [];
      this.selectedRole = {
        "id": "",
        "name": "",
        "permissions": [],
        "actions": []
      };
      this.hiddenCheckboxSelection = true
      this.readOnlyCheckboxSelection = false
      this.restrictionsVisible = false
      this.setAvailableFields(this.fieldTypes[0].value)
    },
    handleAdd() {
      this.toggleAdd = !this.toggleAdd
      this.initNewRole()
    },
    handleSearchUpdate(query) {
      this.search.query = query
    },
    handleSearchControl(data) {
      this.search.query = data.query
      this.currentFilter = data.facet
      this.handleSearch(1, this.currentPerPage)
    },
    handlePageChange(data) {
      this.handleSearch(data.page, data.size)
    },
    handleFieldTypeUpdate(option) {
      this.setAvailableFields(option.value)
    },
    handleSearch(page, size) {
      this.currentPage = page
      this.currentPerPage = size

      const searchFilter = {
        field: this.currentFilter.field.toLowerCase(),
        value: this.search.query,
      }
      
      const body = {
        searchFilters: [searchFilter],
        pager: {
          size,
          page
        },
      }

      return this.$axios
        .$post(`${this.$store.state.config.api_url}/admin/role/search`, JSON.stringify(body), {
          headers: {
            'Content-Type': 'application/json',
            accept: 'application/json',
          }
        })
        .then((r) => {
          this.paginationTotalResults = r?.pager?.total || 0
          this.role = r.data || []
        })
        .catch((e) => {
          this.handleErrorResponse(e, "Error searching Roles")
          return e
        })
    }
  }
}
</script>

<style lang="scss" scoped>


.actions-table .actions-cell {
  text-align: left;
  padding: 8px 4px;
  cursor: pointer;
  border-bottom: 1px solid lightgray;
}

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

</style>
