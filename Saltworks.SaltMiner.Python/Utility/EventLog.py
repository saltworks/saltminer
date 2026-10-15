''' --[auto-generated, do not modify this block]--
*
* SaltMiner - The open source vulnerability and pen testing management platform
* Copyright (C) 2024-2026 Saltworks Security, LLC
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program. If not, see <https://www.gnu.org/licenses/>.
*
* ----
'''

from Core.DataClient import DataClient, DataClientException


class EventLogException(Exception):
    pass


class EventLog:
    '''
    Helper for sending event log entries to the SaltMiner Data API /Eventlog endpoint.

    Builds a DataItemRequest<Eventlog>-shaped payload and POSTs it via DataClient.
    '''

    def __init__(self, application, provider: str, data_set: str, config_name: str = 'DataClient'):
        '''
        :param application:  Application instance (provides .Settings for DataClient config)
        :param provider:     ECS provider field, e.g. "servicemanager"
        :param data_set:     ECS dataset field, e.g. "saltminer.servicemanager"
        :param config_name:  Config section name passed to DataClient; defaults to "DataClient"
        '''
        self._provider = provider
        self._data_set = data_set
        self._client = DataClient(application, config_name)

    # ------------------------------------------------------------------
    # Public helpers
    # ------------------------------------------------------------------

    def log(self, action: str, outcome: str, level: str, reason: str = None,
            application: str = None, service_job_id: str = None, service_job_name: str = None,
            severity: int = 0, kind: str = 'event') -> dict:
        '''
        Send a single event log entry to the Data API.

        :param action:           ECS action field, e.g. "In progress", "Complete", "Failed"
        :param outcome:          ECS outcome — must be one of "success", "failure", or "unknown"
        :param level:            Log level text, e.g. "Information", "Warning", "Critical"
        :param reason:           Optional message body
        :param application:      SaltMiner application name, e.g. "manager"
        :param service_job_id:   ID of the associated service job
        :param service_job_name: Name of the associated service job
        :param severity:         ECS numeric severity (LogSeverity enum value)
        :param kind:             ECS kind field, defaults to "event"
        :return:                 Parsed JSON response body as a dict
        :raises EventLogException: on non-202 response
        '''
        outcome = outcome.lower()
        if outcome not in ('success', 'failure', 'unknown'):
            raise EventLogException(f"outcome must be 'success', 'failure', or 'unknown', got '{outcome}'")

        import datetime
        now = datetime.datetime.now(datetime.timezone.utc).isoformat()

        saltminer = {}
        if application:
            saltminer['Application'] = application
        if service_job_id:
            saltminer['ServiceJobId'] = service_job_id
        if service_job_name:
            saltminer['ServiceJobName'] = service_job_name

        payload = {
            'Entity': {
                'Timestamp': now,
                'Saltminer': saltminer if saltminer else None,
                'Event': {
                    'Action': action,
                    'Severity': severity,
                    'Outcome': outcome,
                    'Reason': reason,
                    'DataSet': self._data_set,
                    'Provider': self._provider,
                    'Kind': kind,
                    'Created': now,
                },
                'Log': {
                    'Level': level,
                },
            }
        }

        try:
            return self._client.event_add(payload)
        except DataClientException as e:
            raise EventLogException(str(e)) from e

    def info(self, action: str, reason: str = None, outcome: str = 'success', **kwargs) -> dict:
        '''Convenience wrapper for Information-level events.'''
        return self.log(action=action, outcome=outcome, level='Information', reason=reason, **kwargs)

    def warning(self, action: str, reason: str = None, outcome: str = 'unknown', **kwargs) -> dict:
        '''Convenience wrapper for Warning-level events.'''
        return self.log(action=action, outcome=outcome, level='Warning', reason=reason, severity=4, **kwargs)

    def error(self, action: str, reason: str = None, outcome: str = 'failure', **kwargs) -> dict:
        '''Convenience wrapper for Error/Critical-level events.'''
        return self.log(action=action, outcome=outcome, level='Critical', reason=reason, severity=2, **kwargs)
