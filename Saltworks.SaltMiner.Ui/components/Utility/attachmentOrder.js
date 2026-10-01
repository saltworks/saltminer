// A record's `timestamp` is the attachment's creation time from the Elasticsearch document.
// A report attachment's `fileName` also carries a trailing `_<unixSeconds>` (PBI-077), used
// only when `timestamp` is missing or unparsable.
function attachmentSortTimestamp(attachment) {
  const recordTimestamp = attachment && attachment.timestamp
    ? Date.parse(attachment.timestamp)
    : NaN

  if (!Number.isNaN(recordTimestamp)) {
    return recordTimestamp
  }

  const fileName = (attachment && attachment.fileName) || ''
  const match = /_([0-9]+)(?:\.[^./]+)?$/.exec(fileName)

  if (match) {
    return Number(match[1]) * 1000
  }

  return 0
}

function sortAttachmentsNewestFirst(attachments) {
  return [...(attachments || [])].sort(
    (a, b) => attachmentSortTimestamp(b) - attachmentSortTimestamp(a)
  )
}

export default {
  sortAttachmentsNewestFirst,
}
