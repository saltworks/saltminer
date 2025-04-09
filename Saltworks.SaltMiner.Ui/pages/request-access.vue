<template>
  <div class="requestAccess">
    <div class="wrapper">
      <div class="row">
        <div class="col-medium-1-3 col-medium-offset-1-3">
          <template v-if="!success">
            <div class="align-center m-b-3">
              <HeadingText label="Request Access" size="2" />
            </div>

             <AlertComponent
               v-if="alert.messages.length > 0"
              class="alert-margin"
              :alert=alert
              @close="handleAlertClose"
            />

            <InputTextArea 
              label="Add a Message to Request" 
              class="m-b-3" 
              id="message-request"
              placeholder="Enter Reason for Request"
              :resize="false"
              :value="messageRequest"
              @update="
                (val) => {
                  messageRequest = val
                }
            "/>

            <div class="align-center">
              <ButtonComponent
                label="Send Request"
                theme="primary"
                @button-clicked="sendRequest"
              />
            </div>
          </template>

          <template v-if="success">
            <div class="align-center m-b-3">
              <HeadingText label="Request Access" class="m-b-2" size="2" />

              <div class="content">
                <p>Your request has been received. Thank you!</p>
              </div>
            </div>
          </template>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { mapState } from 'vuex'
import HeadingText from '../components/HeadingText'
import InputTextArea from '../components/controls/InputTextArea'
import ButtonComponent from '../components/controls/ButtonComponent'
import { getResponseErrors } from '../components/Utility/responseErrors'
import AlertComponent from '../components/AlertComponent.vue'

export default {
  name: 'RequestAccessPage',
  components: {
    ButtonComponent,
    InputTextArea,
    HeadingText,
    AlertComponent
},
  layout: 'auth',
  async asyncData() {
    const config = await fetch('/smpgui/config.json').then((res) => res.json())
    return {
      config,
    }
  },
  data() {
    return {
      errors: [],
      success: false, 
      messageRequest: "",
      alert: {
        messages: [],
        type: "",
        title: ""
      },
    }
  },
  computed: {
    ...mapState({
      user: (state) => state.modules.user,
    }),
  },
  methods: {  
    handleAlertClose() {
      this.alert = {
        messages: [],
        type: "",
        title: ""
      }
    },
    sendRequest() {
      this.$axios
        .$post(
          `${this.$store.state.config.api_url}/Auth/request-access`,
          {
            message: this.messageRequest,
          },
          {
            headers: {
              accept: '*/*',
            },
          }
        )
        .then((response) => {
          this.success = true
        })
        .catch((e) => {
          this.alert = {
            messages: getResponseErrors(e),
            type: "danger",
            title: "Error"
          }
        })
    },
  },
}
</script>
<style lang="scss" scoped></style>
