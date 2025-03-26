<template>
  <div>
    <p>{{ formattedDate }}</p>
  </div>
</template>

<script>
import moment from 'moment';

export default {
  props: {
    dateValue: {
      type: [String, Date],
      required: true,
    },
    dateFormat: {
      type: String,
      default: 'YYYY-MM-DD h:mm a',
    },
  },
  computed: {
    // Compute the formatted date based on the props
    formattedDate() {
      const ts = moment.utc(this.dateValue).format(this.dateFormat);
      return `${ts}`;
    },
  },
};
</script>
