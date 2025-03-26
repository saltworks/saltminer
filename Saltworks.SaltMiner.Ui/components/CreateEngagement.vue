<template>
  <div class="new-engagement__wrapper">
    <InputText
      reff="txtEngagementName"
      label="Name"
      id="newEngagementName"
      placeholder="Enter Engagement Name"
      :value="name"
      @update="
        (val) => {
          name = val
        }
      "
    />
    <InputText
      reff="txtEngagementCustomer"
      label="Customer"
      id="newEngagementCustomer"
      placeholder="Enter Customer"
      :value="customer"
      @update="
        (val) => {
          customer = val
        }
      "
    />
    <InputTextArea
      reff="txtEngagementSummary"
      label="Summary"
      id="newEngagementSummary"
      placeholder="Enter Summary"
      :resize="false"
      :value="summary"
      @update="
        (val) => {
          summary = val
        }
      "
    />
    <DropdownControl
      theme="outline"
      label="Subtype"
      :options="subtypes"
      @update="handleSubtypeUpdate"
    />
    <div class="new-engagement__buttons">
      <span class="ci-button_L" id="newEngagementSaveButton" tabIndex="0" @click="createEngagement">Save</span>
      <span class="ci-button_R" id="newEngagementCancelButton" tabIndex="0" @click="closeSlideModal">Cancel</span>
    </div>
  </div>
</template>

<script>
import DropdownControl from '../components/controls/DropdownControl.vue'
import InputText from './controls/InputText.vue'
import InputTextArea from './controls/InputTextArea.vue'

export default {
  components: {
    InputText,
    InputTextArea,
    DropdownControl
  },
  props: {
    subtypes: {
      type: Array,
      default: [],
    }
  },
  data() {
    return {
      name: '',
      summary: '',
      customer: '',
      subtype: '',
    }
  },
  methods: {
    handleSubtypeUpdate(option) {
      const formattedOption =
        option.value.charAt(0).toUpperCase() + option.value.slice(1)
      this.subtype = formattedOption === 'All' ? '' : formattedOption
    },
    createEngagement() {
      this.$emit('save-engagement', {
        name: this.name,
        summary: this.summary,
        customer: this.customer,
        subtype: this.subtype.length > 0 ? this.subtype : "PenTest"
      })
    },
    closeSlideModal() {
      this.$emit('close-slide-modal')
    },
  },
}
</script>

<style lang="scss" scoped>
.new-engagement__wrapper {
  display: flex;
  flex-direction: column;
  gap: 32px;
  background: $brand-white;
  width: 100%;
  height: 100%;
  font-family: $font-primary;
  padding: 20px 18px 40px 18px;
}
.new-engagement__wrapper h4 {
  font-style: normal;
  font-weight: 700;
  font-size: 24px;
  line-height: 32px;
  display: flex;
  align-items: center;
  justify-content: center;
  letter-spacing: -0.25px;
  color: $brand-color-scale-6;
  margin-bottom: 24px;
  padding-left: 18px;
  padding-right: 18px;
}
.new-engagement__wrapper svg {
  width: fit-content;
  margin-left: auto;
  cursor: pointer;
}
.new-engagement__wrapper p {
  font-style: normal;
  font-weight: 400;
  font-size: 14px;
  line-height: 18px;
  color: $brand-color-scale-6;
}
.checkbox-wrapper {
  margin-top: 26px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.new-engagement__buttons {
  margin-top: 30px;
  display: flex;
  gap: 20px;
}
.ci-button_L,
.ci-button_R {
  padding: 12px;
  font-style: normal;
  font-weight: 700;
  font-size: 16px;
  line-height: 24px;
  border-radius: 40px;
  cursor: pointer;
  transition: 400ms cubic-bezier(0.075, 0.82, 0.165, 1);
  width: 50%;
  text-align: center;
}
.ci-button_L {
  color: $brand-white;
  background: $brand-primary-color;
  border: 1px solid $brand-primary-color;
}
.ci-button_L:hover {
  color: $brand-primary-color;
  background: $brand-white;
  border: 1px solid $brand-primary-color;
}
.ci-button_R {
  color: $brand-color-scale-6;
  background: $brand-color-scale-1;
  border: 1px solid $brand-color-scale-1;
}
.ci-button_R:hover {
  color: $brand-color-scale-1;
  background: $brand-color-scale-6;
  border: 1px solid $brand-color-scale-6;
}
</style>
