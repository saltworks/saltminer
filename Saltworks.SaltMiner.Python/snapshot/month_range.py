import calendar
import datetime


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
    return datetime.datetime(year, month, 15, tzinfo=datetime.timezone.utc)


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


