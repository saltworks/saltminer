<template>
    <div class="wrapper">redirecting...</div>
</template>

<script>
    import isValidUser from '../../../../middleware/is-valid-user'
    
    export default {
        name: 'IssueScannerPage',
        middleware: isValidUser,
        asyncData({ params }) {
            return {
                id: params.id,
                scannerId: params.scanner,
            }
        },
        async fetch({ store: { dispatch } }) {
            await dispatch('configInit')
        },
        mounted() {
            return this.$axios
            .$get(`${this.$store.state.config.api_url}/issue/scanner/${this.scannerId}/engagement/${this.id}`, {
                headers: {
                'Content-Type': 'application/json',
                },
            })
            .then((r) => {
                this.$router.push({
                    path: `/engagements/${this.id}/issues/${r.data.issue.id}`,
                })
            })
        },
    }
</script>
  