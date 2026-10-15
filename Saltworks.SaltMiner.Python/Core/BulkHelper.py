''' --[auto-generated, do not modify this block]--
*
* Copyright (c) 2026 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-12-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
'''

# Bulk batching for elasticsearch writes: the accumulating list, the document envelope, and the flush.
# Pulled out of ElasticClient so the buffer belongs to whoever is filling it.

import logging
import uuid


class BulkHelperException(Exception):
    pass


class BulkHelper(object):
    '''
    Accumulates documents and sends them to elasticsearch in batches.

    **Own one of these per unit of work.**  The buffer is instance state, so a helper shared between
    threads interleaves their documents: one thread's flush ships another's half-built batch, and the
    batch size set by the last caller applies to everyone.  ElasticClient's convenience instance is
    thread-local for exactly this reason (see ElasticClient.BulkSendBatch) - anything that wants a
    predictable lifetime should hold its own.

    Documents may target different indices in one batch; each carries its own _index.
    '''

    VALID_ACTIONS = ['create', 'delete', 'index', 'update']

    def __init__(self, es, batch_size:int=10000, logger=None):
        '''
        :es: ElasticClient used to send the batches (BulkInsert).
        :batch_size: documents to accumulate before an automatic send.
        '''
        self._es = es
        self._batch = []
        self._logger = logger or logging.getLogger(__name__)
        self.batch_size = batch_size

    @property
    def count(self) -> int:
        '''Documents currently queued, awaiting a flush.'''
        return len(self._batch)

    @staticmethod
    def build_document(index, source, id=None, action=None) -> dict:
        '''
        Wraps a source document in the envelope the bulk API expects.

        :source: the document itself.  Must be a dict - a JSON string would be indexed as a scalar
        rather than a document, which is the trap when converting a caller from ElasticClient.Index().
        :id: document id; one is generated when omitted, matching Index()'s behaviour of letting the
        id be assigned rather than derived.
        '''
        if action and action not in BulkHelper.VALID_ACTIONS:
            raise BulkHelperException(f"Bulk action '{action}' invalid/unknown.")
        if action in ['delete', 'update'] and not id:
            raise BulkHelperException(f"Bulk action '{action}' requires an id.")
        if isinstance(source, str):
            raise BulkHelperException("Bulk source must be a dict, not a JSON string - a string is indexed as a scalar, not a document.")
        doc = { '_index': index, '_id': id or uuid.uuid4() }
        if not action or action != 'delete':
            doc['_source'] = source
        if action:
            doc['_op_type'] = action
        return doc

    def add(self, index, doc, doc_id=None, action="index", refresh=None):
        '''
        Queues a document, sending the batch automatically once it reaches batch_size.

        :refresh: applied only to an automatic send triggered by this call - pass it to flush() for the
        final batch, which is the one a subsequent reader depends on.
        '''
        self._batch.append(BulkHelper.build_document(index, doc, doc_id, action))
        if len(self._batch) >= self.batch_size:
            self.flush(refresh)

    def flush(self, refresh=None):
        '''
        Sends whatever is queued.  No-op when empty.

        :refresh: 'wait_for' makes the send return only once its documents are searchable - use it on
        the final flush when something reads them straight back.  Note it covers only the shards this
        request wrote to, so on a multi-shard index it is not a whole-index barrier.
        '''
        if not self._batch:
            return 0
        sent = len(self._batch)
        self._logger.debug("Bulk sending %s document(s)%s", sent, " (waiting for visibility)" if refresh else "")
        batch, self._batch = self._batch, []
        self._es.BulkInsert(batch, refresh=refresh)
        return sent
