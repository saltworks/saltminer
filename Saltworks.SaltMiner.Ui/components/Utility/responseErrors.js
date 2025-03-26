export const getResponseErrors = (e) => {
  const errors = []

  // eslint-disable-next-line no-prototype-builtins
  if (e.hasOwnProperty('response')) {
    // eslint-disable-next-line no-prototype-builtins
    if (e.response.hasOwnProperty('data')) {
      // eslint-disable-next-line no-prototype-builtins
      if (e.response.data.hasOwnProperty('errors')) {
        if (typeof e.response.data.errors === 'object') {
          for (const error in e.response.data.errors) {
            // Usually an error gets returned as an array
            if (Array.isArray(e.response.data.errors[error])) {
              errors.push(e.response.data.errors[error][0])
            }

            // Allow for string errors too
            if (typeof e.response.data.errors[error] === 'string') {
              errors.push(e.response.data.errors[error])
            }
          }
        }
        // eslint-disable-next-line no-prototype-builtins
      } else if (e.response.data.hasOwnProperty('message')) {
        errors.push(e.response.data.message)
      }
    }
  }

  if (!errors.length) {
    errors.push(
      'An unknown problem occurred. If this issue persists, please contact customer support.'
    )
  }

  return errors
}

export default getResponseErrors
