import calendar
import datetime
import logging


def _utc_now() -> datetime.datetime:
    return datetime.datetime.now(tz=datetime.timezone.utc)


def month_start(year: int, month: int) -> datetime.datetime:
    return datetime.datetime(year, month, 1, tzinfo=datetime.timezone.utc)


def next_month_start(year: int, month: int) -> datetime.datetime:
    if month == 12:
        return datetime.datetime(year + 1, 1, 1, tzinfo=datetime.timezone.utc)
    return datetime.datetime(year, month + 1, 1, tzinfo=datetime.timezone.utc)


def month_end_inclusive(year: int, month: int) -> datetime.datetime:
    """Last microsecond of the month in UTC."""
    last_day = calendar.monthrange(year, month)[1]
    return datetime.datetime(year, month, last_day, 23, 59, 59, 999999, tzinfo=datetime.timezone.utc)


def snapshot_date_for_month(year: int, month: int) -> datetime.datetime:
    """Mid-month date for completed months; utcnow for the in-progress month."""
    now = _utc_now()
    end = month_end_inclusive(year, month)
    if end >= now:
        return now
    days_in_month = calendar.monthrange(year, month)[1]
    mid_day = days_in_month // 2
    return datetime.datetime(year, month, mid_day, tzinfo=datetime.timezone.utc)


def iter_months(start: datetime.datetime, end: datetime.datetime):
    """Yield (year, month) tuples from start through end (inclusive)."""
    current = month_start(start.year, start.month)
    stop = month_start(end.year, end.month)
    while current <= stop:
        yield current.year, current.month
        if current.month == 12:
            current = current.replace(year=current.year + 1, month=1)
        else:
            current = current.replace(month=current.month + 1)


def earliest_data_date(es, issue_index: str, source_type: str) -> datetime.datetime | None:
    """Return the earliest month start with issue data for this source type."""
    query = {
        "query": {"term": {"saltminer.asset.source_type": source_type}},
        "aggs": {
            "min_found":   {"min": {"field": "vulnerability.found_date"}},
            "min_removed": {"min": {"field": "vulnerability.removed_date"}},
        },
        "size": 0,
    }
    result = es.Search(issue_index, queryBody=query, size=0, navToData=False)
    if not result or "aggregations" not in result:
        return None

    dates = []
    for key in ("min_found", "min_removed"):
        raw = result["aggregations"].get(key, {}).get("value_as_string")
        if raw:
            try:
                dt = datetime.datetime.fromisoformat(raw.replace("Z", "+00:00"))
                dates.append(dt)
            except ValueError:
                logging.debug("Could not parse date '%s' from agg key '%s'", raw, key)

    if not dates:
        return None
    earliest = min(dates)
    return month_start(earliest.year, earliest.month)
