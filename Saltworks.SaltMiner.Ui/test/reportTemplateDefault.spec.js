import reportTemplateDefault from '@/components/Utility/reportTemplateDefault'

describe('reportTemplateDefault.defaultReportTemplate', () => {
  test('Saltworks wins when it is neither first by order nor first alphabetically', () => {
    const options = [
      { display: 'Acme', value: 'Acme', order: 1 },
      { display: 'test template', value: 'test template', order: 2 },
      { display: 'Saltworks', value: 'Saltworks', order: 3 },
    ]

    expect(reportTemplateDefault.defaultReportTemplate(options)).toBe(
      'Saltworks'
    )
  })

  test('without a Saltworks option, returns the first by display in localeCompare order', () => {
    const options = [
      { display: 'test template', value: 'test template', order: 1 },
      { display: 'Beta', value: 'beta-folder', order: 2 },
      { display: 'Acme', value: 'acme-folder', order: 3 },
    ]

    expect(reportTemplateDefault.defaultReportTemplate(options)).toBe(
      'acme-folder'
    )
  })

  test('does not mutate the input array', () => {
    const options = [
      { display: 'b', value: 'b', order: 1 },
      { display: 'a', value: 'a', order: 2 },
    ]
    const original = [...options]

    reportTemplateDefault.defaultReportTemplate(options)

    expect(options).toEqual(original)
  })

  test('an empty list, null and undefined return an empty string', () => {
    expect(reportTemplateDefault.defaultReportTemplate([])).toBe('')
    expect(reportTemplateDefault.defaultReportTemplate(null)).toBe('')
    expect(reportTemplateDefault.defaultReportTemplate(undefined)).toBe('')
  })
})
