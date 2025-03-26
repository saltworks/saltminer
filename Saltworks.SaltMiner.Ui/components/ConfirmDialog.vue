<template>
  <div v-if="isVisible" class="modal">
    <div class="modal-content">
        <div class="header-title">
          <div>{{ title }} <Close class="close-icon" @click="cancel" /></div>
        </div>
      <div class="message-container">{{ message }}</div>

      <div class="button-container">
        <ButtonComponent 
            label="Ok"
            :icon-only="false"
            theme="primary"
            size="small"
            :disabled="false"
            @button-clicked="confirm"
        />
        <ButtonComponent 
            label="Cancel"
            :icon-only="false"
            theme="primary"
            size="small"
            :disabled="false"
            @button-clicked="cancel"
        />
      </div>
      
    </div>
  </div>
</template>

<script>
import Close from '../assets/svg/fi_x.svg?inline'
import ButtonComponent from '../components/controls/ButtonComponent'

export default {
  components: {
   ButtonComponent,
   Close
  },
  data() {
    return {
      isVisible: false,
      message: "",
      title: "",
      resolve: null,
      reject: null,
    };
  },
  methods: {
    show(message, title) {
      this.message = message;
      this.title = title;
      this.isVisible = true;
      return new Promise((resolve, reject) => {
        this.resolve = resolve;
        this.reject = reject;
      });
    },
    confirm() {
      this.isVisible = false;
      this.resolve(true);
    },
    cancel() {
      this.isVisible = false;
      this.resolve(false);
    },
  },
};
</script>

<style>
.modal {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
}

.modal-content {
  background-color: white;
  padding: 20px;
  border-radius: 5px;
  justify-content: center;
  align-items: center;
}

.header {
  background-color: #FFF;
  padding: 20px;
  color: #000;
}

.header-title {
  font-size: 20px;
  margin: 0;
  color: 696969;
  border-bottom: 2px solid #D3D3D3;
  display: inline-block;
  width: 100%
}

.button-container {
    padding: 15px;
    float: right;
}

.message-container {
    font-size: 15px;
    padding: 30px
}

.close-icon {
  float: right;
  width: 20px;
}

.close-icon:hover {
  cursor: pointer;
}
</style>
