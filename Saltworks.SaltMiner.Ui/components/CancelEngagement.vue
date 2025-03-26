<template>
  <div class="custom-issue__wrapper">
    <div class="cancel-engagement-section">
      <InputText
        :label="inputLabel()"
        reff="txtCancelEngagement"
        placeholder="Engagement Name"
        :value="cancelEngagementName"
        @update="
          (val) => {
            cancelEngagementName = val
          }
        "
      />
      <ButtonComponent
        :label="buttonLabel()"
        theme="primary"
        size="medium"
        :disabled="canCancelEngagement"
        @button-clicked="handleCancelEngagement"
      /> 
    </div>
  </div>
</template>

<script>
import { mapState } from 'vuex'
import { mapActions } from 'vuex'
import InputText from './controls/InputText'
import ButtonComponent from './controls/ButtonComponent'

export default {
  components: {
    InputText,
    ButtonComponent,
  },
  props: {
    engagementName: {
      type: String,
      default: null,
    },
    markHistorical: {
      type: Boolean,
      default: false
    }
  },
  data() {
    return {
      cancelEngagementName: '',
    }
  },
  computed: {
    ...mapState({
    }),
    canCancelEngagement() {
      return this.cancelEngagementName !== this.engagementName
    },
  },
  methods: {
    ...mapActions(['user/setFields']),
    handleSave() {
      this.$emit('save', this.issuesList)
      this.closeSlideModal()
    },
    handleCancelEngagement() {
      this.$emit('cancel-engagement')
    },
    closeSlideModal() {
      this.$emit('toggle')
    },
    inputLabel() {
      if(this.markHistorical){
        return "Confirm Name of Engagement to Mark Historical"
      } 
      return "Confirm Name of Engagement to Cancel"
    },
    buttonLabel() {
      if(this.markHistorical){
        return "Mark Historical"
      } 
      return "Cancel"
    }
  },
}
</script>

<style lang="scss" scoped>
.custom-issue__wrapper {
  display: flex;
  flex-flow: column;
  justify-content: flex-start;
  gap: 24px;
  background-color: $brand-white;
  width: 100%;
  height: 100%;
  font-family: $font-primary;

  & > .cancel-engagement-section,
  & > .checkbox-wrapper,
  & > p {
    flex: 0 0 auto;
  }
  .cancel-engagement-section {
    display: flex;
    width: 100%;
    gap: 24px;
    flex-flow: column;
    justify-self: flex-end;
    padding: 24px 0 40px 0;
  }

  h4 {
    font-style: normal;
    font-weight: 700;
    font-size: 24px;
    line-height: 32px;
    display: flex;
    align-items: center;
    justify-content: center;
    letter-spacing: -0.25px;
    color: $brand-color-scale-6;
  }
  svg {
    width: fit-content;
    margin-left: auto;
    cursor: pointer;
  }
  p {
    font-style: normal;
    font-weight: 400;
    font-size: 14px;
    line-height: 18px;
    color: $brand-color-scale-6;
  }
  .checkbox-wrapper {
    display: flex;
    flex-direction: column;
    gap: 10px;
  }
  .custom-issue__buttons {
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
}
</style>
