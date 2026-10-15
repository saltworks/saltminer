// The shipped report template's lookup value (a folder name under report-templates/).
const SHIPPED_TEMPLATE = 'Saltworks'

// Returns the value the Generate Engagement Report dropdown should hold when nobody has picked
// one: the shipped Saltworks template when listed, otherwise the first option by display name,
// otherwise an empty string. Does not depend on the order the API returned the options in.
function defaultReportTemplate(options) {
  if (!Array.isArray(options) || options.length === 0) {
    return ''
  }

  if (options.some((option) => option && option.value === SHIPPED_TEMPLATE)) {
    return SHIPPED_TEMPLATE
  }

  const sorted = [...options].sort((a, b) =>
    String((a && a.display) || '').localeCompare(String((b && b.display) || ''))
  )

  return (sorted[0] && sorted[0].value) || ''
}

export default {
  defaultReportTemplate,
}
