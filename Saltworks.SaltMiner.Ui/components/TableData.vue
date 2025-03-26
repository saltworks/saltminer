<template>
  <table :class="tableClasses">
    <thead :class="getHeaderClass()">
      <template v-if="!disabled">
        <th v-if="toggleRows" class="theader-check">
          <CheckboxBox
            :checked="headerChecked"
            @check-clicked="handleCheckAll"
          />
        </th>
      </template>
      <template v-for="(header, idx) in headers">
        <th
          v-if="header.hide === false"
          :key="`theader-${idx}`"
          :style="{ 'white-space': header.nowrap && header.nowrap == true ? 'nowrap' : ''}"
          @click="handleHeaderClick(header)"
        >
          <div :class="`theader-layout ${isIssueCount(header.field)} ${activeFilterCheck(header.field)}`">
            <span>{{ header.display }}</span
            ><IconChevronUp v-if="header.sortable && sortable" />
          </div>
        </th>
      </template>
    </thead>
    <tbody>
      <tr
        v-for="(row, idx) in rows"
        :key="`row-${idx}`"
        :class="getRowClass(row)"
      >
        <template v-if="!disabled">
          <td v-if="toggleRows" class="td-check">
            <CheckboxBox
              :checked="checkIfChecked(row)"
              @check-clicked="handleCheckboxClick(row)"
            />
          </td>
        </template>
        <template v-for="(header, headerIndex) in headers">
          <td
            v-if="header.hide === false"
            :key="`header-${headerIndex}`"
            :style="{ 'white-space': header.nowrap && header.nowrap == true ? 'nowrap' : ''}"
            :class="`${isIssueCount(header.field)} ${checkColumnType(header.field.toLowerCase(), row)}`"
            @click="handleRowClick(row)"
          >

            <div v-if="header.field === 'hidden' || header.field === 'readOnly' || header.field === 'required' || header.field === 'system'" class="cell-contents">
              <span v-if="getCellContents(row, header) === true">
                <CheckboxBox
                  :checked="true"
                  :handlesClicks="false">
                </CheckboxBox>
              </span>

              <span v-else>
                <CheckboxBox
                :checked="false"
                :handlesClicks="false"
                :disabled="true">
                </CheckboxBox>
              </span>
            </div>
            <div v-else-if="header.field === 'disabled'" class="cell-contents">
              <span v-if="getCellContents(row, header) === true">
                <ButtonComponent
                  label="Disabled"
                  theme="danger"
                  size="xsmall"
                />
              </span>

              <span v-else>
                <ButtonComponent
                  label="Enabled"
                  theme="primary"
                  size="xsmall"
                />
              </span>
            </div>
            <div v-else-if="header.field === 'targetId'" class="cell-contents">
              <span v-if="getCellContents(row, header) !== ''">
                <router-link :to="'/engagements/' + row.targetId" class="custom-link">{{ getCellContents(row, header) }}</router-link>
              </span>
            </div>
            <div v-else-if="header.field === 'deleteColumn'" class="cell-contents">
              <div class="delete-icon"></div>
            </div>
            <div v-else class="cell-contents">
              {{ getCellContents(row, header) }}
            </div>
          </td>
        </template>

        <template v-if="showOpenLinks">
          <td class="open-links-icon" v-if="linkType === 'engagementDetails'">
            <OpenLinkComponent
              iconSrc="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAoAAAAKCAYAAACNMs+9AAAAQElEQVR42qXKwQkAIAxDUUdxtO6/RBQkQZvSi8I/pL4BoGw/XPkh4XigPmsUgh0626AjRsgxHTkUThsG2T/sIlzdTsp52kSS1wAAAABJRU5ErkJggg=="
              :linkUrl="`/smpgui/engagements/${row.id}`"
              :windowWidth="1800"
              :windowHeight="900"
            />
          </td>
          <td class="open-links-icon" v-if="linkType === 'issueDetails'">
            <OpenLinkComponent
              iconSrc="data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAoAAAAKCAYAAACNMs+9AAAAQElEQVR42qXKwQkAIAxDUUdxtO6/RBQkQZvSi8I/pL4BoGw/XPkh4XigPmsUgh0626AjRsgxHTkUThsG2T/sIlzdTsp52kSS1wAAAABJRU5ErkJggg=="
              :linkUrl="`/smpgui/engagements/${engagementId}/issues/${row.id}`"
              :windowWidth="1800"
              :windowHeight="900"
            />
          </td>
        </template>

        <template v-if="rowIsNotDraft(row)">
          <IconLock class="lockedRow" />
        </template>
      </tr>
    </tbody>
  </table>
</template>

<script>
import IconChevronUp from '../assets/svg/fi_chevron-up.svg?inline'
import IconLock from '../assets/svg/fi_lock.svg?inline'
import CheckboxBox from './controls/CheckboxBox.vue'
import ButtonComponent from './controls/ButtonComponent'
import OpenLinkComponent from './controls/OpenLinkComponent'

export default {
  name: 'TableData',
  components: {
    IconChevronUp,
    IconLock,
    CheckboxBox,
    ButtonComponent,
    OpenLinkComponent
  },
  props: {
    toggleRows: {
      type: Boolean,
      default: false,
    },
    showOpenLinks: {
      type: Boolean,
      default: false,
    },
    linkType: {
      type: String,
      default: '',
    },
    engagementId: {
      type: String,
      default: '',
    },
    headers: {
      type: Array,
      required: true,
      default: () => [],
    },
    rows: {
      type: Array,
      required: false,
      default: () => [],
    },
    currentFilter: {
      type: String,
      required: false,
      default: '',
    },
    disableHover: {
      type: Boolean,
      default: false,
    },
    sortable: {
      type: Boolean,
      default: true,
    },
    disabled: {
      type: Boolean,
      default: false,
    },
    rowSize: {
      type: String,
      default: 'large',
    },
    checkStatus: {
      type: Boolean,
      default: false,
    },
    isEngagement: {
      type: Boolean,
      default: false,
    },
  },

  data() {
    return {
      headerChecked: false,
      checkedRows: []
    }
  },

  computed: {
    tableClasses() {
      const classes = ['table-data', `row-size-${this.rowSize}`]

      if (this.disableHover) classes.push('show-hover')

      if (this.disabled) classes.push('disabled')

      return classes.join(' ')
    },
  },
  methods: {
    rowIsNotDraft(row) {
      if(!this.isEngagement){
        return false;
      }
      if (!this.checkStatus) 
      {
        return false
      }
      
      return row?.status !== 'Draft'
    },
    getRowBackground(row) {
      if (!this.checkStatus || row?.status === 'Draft' || !this.isEngagement) {
        return ''
      }
        
      if(row?.status === 'Historical'){
        return 'historical'
      } 
      
      return 'published'
    },
    truncateString(value) {
      if (!value) return '';
      return value.length > 40 ? value.substring(0, 40) + '...' : value;
    },
    handleCheckAll() {
      if (this.disabled) return

      this.headerChecked = !this.headerChecked
      this.checkedRows = this.headerChecked
        ? this.rows.map((row) => JSON.stringify(row))
        : []

      this.handleRowsChanged()
    },
    handleCheckboxClick(row) {
      if (this.disabled) return

      const rowString = JSON.stringify(row)
      if (this.checkedRows.includes(rowString)) {
        this.checkedRows.splice(this.checkedRows.indexOf(rowString), 1)
      } else {
        this.checkedRows.push(rowString)
      }

      this.handleRowsChanged()
    },
    handleRowsChanged() {
      if (this.disabled) return

      const actualCheckedRows = this.rows.filter((row) =>
        this.checkedRows.includes(JSON.stringify(row))
      )
      this.$emit('checked-rows-changed', actualCheckedRows)
    },
    activeFilterCheck(field) {
      return this.currentFilter === field ? 'active' : ''
    },
    handleHeaderClick(header) {
      if (!header.sortable || !this.sortable) return
      this.$emit('header-click', header)
    },
    handleRowClick(row) {
      this.$emit('row-click', row)
    },
    checkColumnType(field, row) {
      const { severity } = row
      return severity  && this.isEngagement && field === 'severity'
        ? `severity-col severity-${severity.toLowerCase()}`
        : ''
    },
    isIssueCount(field) {
      if(field.includes("issueCount")){
        return 'issueCountWidth'
      } else {
        return ''
      }
    },
    getCellContents(row, header) {
      let columnCell = ''
      if (header.field === 'status' && this.isEngagement) {
        if (['Published', 'Draft', 'Error', 'Processing', 'Queued', 'Historical'].includes(row[header.field])) {
          return row[header.field]
        }
      } else if(header.field === 'state' && !this.isEngagement) {
        if (row.isActive && !row.isSuppressed && !row.isRemoved) {
          return 'Active'
        }

        if (!row.isActive && row.isSuppressed && !row.isRemoved) {
          return 'Suppressed'
        }

        if (!row.isActive && !row.isSuppressed && row.isRemoved) {
          return 'Removed'
        }

        if (!row.isActive && !row.isSuppressed && !row.isRemoved) {
          return 'Not Active'
        }

        if (row.isActive) {
          columnCell = columnCell + ' A '
        }
        if (row.isSuppressed) {
          columnCell = columnCell + ' S '
        }
        if (row.isRemoved) {
          columnCell = columnCell + ' R '
        }
        else {
          return ''
        }
      } else if(header.field.includes("issueCount") && this.isEngagement){
        const split = header.field.split(".");
        columnCell = row[split[0]][split[1]]
      } else {
        columnCell = row[header.field] // this.truncateString(row[header.field]) || ''
      }

      return columnCell
    },
    checkIfChecked(row) {
      return this.checkedRows.includes(JSON.stringify(row))
    },
    getRowClass(row) {
      const classes = []
      if (this.checkIfChecked(row)) classes.push('checked-row')
      const rowBackground = this.getRowBackground(row)
      if (rowBackground !== '') classes.push(rowBackground)
      if (this.checkStatus) classes.push('pad-left')
      return classes.join(' ')
    },
    getHeaderClass() {
      const classes = []
      if (this.checkStatus) classes.push('pad-left')
      return classes.join(' ')
    },
  },
}
</script>

<style lang="scss" scoped>
table.table-data {
  border-radius: 8px;
  table-layout: auto;
  width: 100%;

  thead {
    &.pad-left {
      & > th:first-child {
        padding-left: 16px;
      }
    }

    th {
      padding: 0 0 0 0;
      cursor: pointer;
      transition: background-color 0.2s ease-in-out;

      &.theader-check {
        padding: 19.5px 19.5px 19.5px 19.5px;
        display: flex;
        align-items: center;
        justify-content: center;
      }
      &:active {
        background-color: $brand-table-row-active;
      }

      .theader-layout {
        display: flex;
        align-items: center;
        justify-content: flex-start;
        gap: 8px;
        width: 100%;
        height: 100%;
        padding: 19.5px 93px 19.5px 16px;

        span {
          flex: 0 0 auto;
          font-family: 'Open Sans', sans-serif;
          font-style: bold;
          font-weight: 700;
          font-size: 14px;
          line-height: 18px;
          color: $brand-color-scale-6;
          text-align: left;
        }
        svg {
          flex: 0 0 auto;
          width: 12px;
          height: 12px;
          transform: rotate(180deg);
        }

        &.active {
          span {
            color: $brand-primary-lighter-color;
          }

          svg {
            path {
              stroke: $brand-primary-lighter-color;
            }
          }
        }
      }
    }
  }
  tbody {
    tr {
      cursor: pointer;
      transition: background-color 0.2s ease-in-out;

      &.pad-left {
        position: relative;
        .lockedRow {
          position: absolute;
          top: 50%;
          left: 12px;
          transform: translateY(-50%);

          width: 12px;
          height: 100%;

          display: flex;
          align-items: center;
          justify-content: center;

          object-fit: contain;
          object-position: center;
        }
        & > td:first-child {
          padding-left: 32px;
        }
      }

      &:nth-child(odd) {
        background: $brand-white;
        box-shadow: inset 0px -1px 0px rgba(0, 0, 0, 0.12);
      }

      &:nth-child(even) {
        background: $brand-off-white-2;
        box-shadow: inset 0px -1px 0px rgba(0, 0, 0, 0.12);
      }

      &.checked-row {
        background-color: $brand-table-row-active;
        box-shadow: inset 0px -1px 0px rgba(0, 0, 0, 0.12);
      }

      &.published {
        background-color: $brand-table-row-published;
        box-shadow: inset 0px -1px 0px rgba(0, 0, 0, 0.12);
      }

      &.historical {
        background-color: $brand-table-row-historical;
        box-shadow: inset 0px -1px 0px rgba(0, 0, 0, 0.12);
      }

      td {
        text-align: left;
        font-family: 'Open Sans', sans-serif;
        font-style: normal;
        font-weight: 400;
        position: relative;

        .cell-contents {
          display: flex;
          align-items: center;
          justify-content: flex-start;
          font-size: 14px;
          line-height: 14px;
          gap: 10px;
        }

        &.td-check {
          display: flex;
          align-items: center;
          justify-content: center;
        }

        &.td-check + td,
        &:first-child {
          font-weight: 600;
        }

        font-size: 14px;
        line-height: 14px;
        color: $brand-color-scale-6;

        &.severity-col {
          .cell-contents {
            &::before {
              content: '';
              display: block;
              width: 12px;
              height: 12px;
              position: relative;
              border-radius: 12px;
              background-color: $issue-severity-best-practice;
            }
          }

          &.severity-critical {
            .cell-contents {
              &::before {
                background-color: $issue-severity-critical;
              }
            }
          }

          &.severity-high {
            .cell-contents {
              &::before {
                background-color: $issue-severity-high;
              }
            }
          }

          &.severity-medium {
            .cell-contents {
              &::before {
                background-color: $issue-severity-medium;
              }
            }
          }

          &.severity-low {
            .cell-contents {
              &::before {
                background-color: $issue-severity-low;
              }
            }
          }

          &.severity-info {
            .cell-contents {
              &::before {
                background-color: $issue-severity-info;
              }
            }
          }
        }
      }
    }
  }
}

table.table-data.show-hover {
  tbody {
    tr {
      &:nth-child(odd) {
        &:hover {
          background-color: $brand-table-row-hover;
        }
        &:active {
          background-color: $brand-table-row-active;
        }
      }

      &:nth-child(even) {
        &:hover {
          background-color: $brand-table-row-hover;
        }
        &:active {
          background-color: $brand-table-row-active;
        }
      }

      &.checked-row {
        &:hover {
          background-color: $brand-table-row-checked;
        }
        &:active {
          background-color: $brand-table-row-checked;
        }
      }

      &.published {
        &:hover {
          background-color: $brand-table-row-published;
        }
        &:active {
          background-color: $brand-table-row-published;
        }
      }

      &.historical {
        &:hover {
          background-color: $brand-table-row-historical;
        }
        &:active {
          background-color: $brand-table-row-historical;
        }
      }
    }
  }
}

table.row-size-large {
  tbody {
    tr {
      td {
        padding: 19.5px 93px 19.5px 16px;

        &.td-check {
          padding: 19.5px 19.5px 19.5px 19.5px;
        }

        &.severity-col {
          padding: 19.5px 93px 19.5px 16px;
        }
      }
    }
  }
}

table.row-size-medium {
  tbody {
    tr {
      td {
        padding: 12px 93px 12px 16px;

        &.td-check {
          padding: 12px 19.5px 12px 19.5px;
        }
        &.severity-col {
          padding: 12px 93px 12px 16px;
        }
      }
    }
  }
}

table.disabled {
  tbody {
    tr {
      td {
        padding: 20px 93px 20px 16px;
      }
    }
  }
}

.issueCountWidth{
  max-width: 50px !important;
}

.custom-link {
  color: blue;
  cursor: pointer;
}

.custom-link:hover {
  color: darkblue;
}

.open-links-icon {
  width: 130px;
  height: 13px;
}

.delete-icon {
  position: relative;
  width: 16px; /* Size of the X */
  height: 16px;
  cursor: pointer;
}

.delete-icon::before,
.delete-icon::after {
  content: '';
  position: absolute;
  top: 0;
  left: 50%;
  width: 2px; /* Thickness of the lines */
  height: 100%;
  background-color: red;
  transform-origin: center;
}

.delete-icon::before {
  transform: translateX(-50%) rotate(45deg);
}

.delete-icon::after {
  transform: translateX(-50%) rotate(-45deg);
}


</style>
