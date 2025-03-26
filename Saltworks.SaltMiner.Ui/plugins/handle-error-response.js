


export default (context, inject) => {
    const getErrorResponse = (r) => {
        if (r.isAxiosError) {
            r = r?.response?.data
            if (r?.errorMessages) {
                return r?.errorMessages
            } else {
                return ["Unknown error. Contact support."]
            }
        }
    }

    inject('getErrorResponse', getErrorResponse)
}
