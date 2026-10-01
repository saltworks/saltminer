import attachmentOrder from '@/components/Utility/attachmentOrder'

describe('attachmentOrder.sortAttachmentsNewestFirst', () => {
  test('a shuffled list of three runs comes back ordered newest first', () => {
    const oldest = { fileId: '1', fileName: 'Report_1000.pdf', timestamp: '2026-01-01T00:00:00Z' }
    const middle = { fileId: '2', fileName: 'Report_2000.pdf', timestamp: '2026-02-01T00:00:00Z' }
    const newest = { fileId: '3', fileName: 'Report_3000.pdf', timestamp: '2026-03-01T00:00:00Z' }

    const shuffled = [middle, newest, oldest]

    expect(attachmentOrder.sortAttachmentsNewestFirst(shuffled)).toEqual([
      newest,
      middle,
      oldest,
    ])
  })

  test('a run\'s .docx and .pdf, generated at the same time, both stay in the list', () => {
    const older = { fileId: '1', fileName: 'Report_1000.pdf', timestamp: '2026-01-01T00:00:00Z' }
    const docx = { fileId: '2', fileName: 'Report_2000.docx', timestamp: '2026-02-01T00:00:00Z' }
    const pdf = { fileId: '3', fileName: 'Report_2000.pdf', timestamp: '2026-02-01T00:00:00Z' }

    const result = attachmentOrder.sortAttachmentsNewestFirst([older, docx, pdf])

    expect(result).toHaveLength(3)
    expect(result[0].fileId).not.toBe(older.fileId)
    expect(result[2]).toBe(older)
    expect(result.map((a) => a.fileId).sort()).toEqual(['1', '2', '3'])
  })

  test('falls back to the Unix time stamp in the file name when timestamp is absent', () => {
    const withTimestamp = { fileId: '1', fileName: 'Report_1000.pdf', timestamp: '2026-01-01T00:00:00Z' }
    const noTimestamp = { fileId: '2', fileName: 'Report_99999999999.pdf' }

    const result = attachmentOrder.sortAttachmentsNewestFirst([withTimestamp, noTimestamp])

    expect(result).toEqual([noTimestamp, withTimestamp])
  })

  test('a hand-uploaded attachment with neither field stays in the list', () => {
    const generated = { fileId: '1', fileName: 'Report_1000.pdf', timestamp: '2026-01-01T00:00:00Z' }
    const handUploaded = { fileId: '2', fileName: 'notes.txt' }

    const result = attachmentOrder.sortAttachmentsNewestFirst([handUploaded, generated])

    expect(result).toEqual([generated, handUploaded])
  })

  test('does not mutate the input array', () => {
    const list = [
      { fileId: '1', fileName: 'Report_1000.pdf', timestamp: '2026-01-01T00:00:00Z' },
      { fileId: '2', fileName: 'Report_2000.pdf', timestamp: '2026-02-01T00:00:00Z' },
    ]
    const original = [...list]

    attachmentOrder.sortAttachmentsNewestFirst(list)

    expect(list).toEqual(original)
  })
})
