<template>
  <div class="commentBlock">
    <div class="commentBlock--header">
      <div class="commentBlock--author">
        {{ author }}
      </div>
      <div class="commentBlock--timestamp">
        {{ timestamp }}
      </div>
      <div v-if="type === 'User' && allowDelete === true && CommentNewToEngagement()" class="delete-icon" @click="deleteComment(commentId)" />
    </div>
    <div class="commentBlock--body">
      {{ content }}
    </div>
    
  </div>
  
</template>

<script>
export default {
  props: {
    author: {
      type: String,
      default: '',
    },
    type: {
      type: String,
      default: '',
    },
    allowDelete: {
      type: Boolean,
      default: false,
    },
    commentId: {
      type: String,
      default: '',
    },
    timestamp: {
      type: String,
      default: '',
    },
    commentTimestamp: {
      type: String,
      default: '',
    },
    engagementTimestamp: {
      type: String,
      default: '',
    },
    content: {
      type: String,
      default: '',
    },
  },
  methods: {
    deleteComment(id) {
      this.$emit('delete', id)
    },
    CommentNewToEngagement() {
      // the idea here is comment can only be deleted if it was added
      // to engagement since the engagement was checked out or first created

      const eDate = new Date(this.engagementTimestamp)
      const cDate = new Date(this.commentTimestamp)

      return eDate.getTime() < cDate.getTime()
    }
  }
}
</script>

<style lang="scss" scoped>
.commentBlock {
  padding: 16px;
  border: 1px solid $brand-color-scale-2;
  border-radius: 8px;
  display: flex;
  flex-flow: column;
  gap: 16px;
}

.commentBlock--header {
  display: flex;
  flex-flow: row;
  justify-content: space-between;
  gap: 8px;
}

.commentBlock--author {
  font-family: 'Open Sans', sans-serif;
  color: $brand-color-scale-6;
  font-style: normal;
  font-weight: 600;
  font-size: 14px;
  line-height: 18px;
}
.commentBlock--timestamp {
  font-family: 'Open Sans', sans-serif;
  color: $brand-color-scale-5;
  font-style: normal;
  font-weight: 400;
  font-size: 14px;
  line-height: 18px;
}
.commentBlock--body {
  font-family: 'Open Sans', sans-serif;
  color: $brand-color-scale-6;
  font-style: normal;
  font-weight: 400;
  font-size: 14px;
  line-height: 18px;
}

.delete-icon {
  position: relative;
  width: 16px; /* Size of the X */
  height: 16px;
  cursor: pointer;
  display: inline-block;
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

.delete-icon:hover::before,
.delete-icon:hover::after {
  background-color: darkred;
}

</style>
