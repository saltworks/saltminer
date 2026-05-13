import datetime


def _short_source(source_type: str) -> str:
    """Return the lowercase suffix after the last '.' in the source type string.

    Examples:
        'Saltworks.FOD'    -> 'fod'
        'Saltworks.Tenable'-> 'tenable'
        'FOD'              -> 'fod'
    """
    return source_type.split(".")[-1].lower()


def issue_index_pattern(asset_type: str) -> str:
    """Wildcard pattern that matches all issue indices for an asset type."""
    return f"issues_{asset_type.lower()}_*"


def scan_index_pattern(asset_type: str) -> str:
    """Wildcard pattern that matches all scan indices for an asset type."""
    return f"scans_{asset_type.lower()}_*"


def snapshot_index(asset_type: str, source_type: str, year: int, month: int) -> str:
    return f"snapshots_{asset_type.lower()}_{_short_source(source_type)}_monthly_{year:04d}_{month:02d}"


def scan_snapshot_index(asset_type: str, source_type: str, year: int, month: int) -> str:
    return f"scan_snapshots_{asset_type.lower()}_{_short_source(source_type)}_monthly_{year:04d}_{month:02d}"


def snapshot_index_for_date(asset_type: str, source_type: str, dt: datetime.datetime) -> str:
    return snapshot_index(asset_type, source_type, dt.year, dt.month)


def scan_snapshot_index_for_date(asset_type: str, source_type: str, dt: datetime.datetime) -> str:
    return scan_snapshot_index(asset_type, source_type, dt.year, dt.month)
