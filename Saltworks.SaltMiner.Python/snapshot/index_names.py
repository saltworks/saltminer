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


def historical_snapshot_index(asset_type: str, source_type: str) -> str:
    """Single index holding all historical monthly issue snapshots for a source type."""
    return f"snapshots_{asset_type.lower()}_{_short_source(source_type)}_historical"


def historical_scan_snapshot_index(asset_type: str, source_type: str) -> str:
    """Single index holding all historical monthly scan snapshots for a source type."""
    return f"scan_snapshots_{asset_type.lower()}_{_short_source(source_type)}_historical"


def current_snapshot_index(asset_type: str, source_type: str) -> str:
    """Index holding the live current-month issue summarization, refreshed daily."""
    return f"snapshots_{asset_type.lower()}_{_short_source(source_type)}_current"


def current_scan_snapshot_index(asset_type: str, source_type: str) -> str:
    """Index holding the live current-month scan summarization, refreshed daily."""
    return f"scan_snapshots_{asset_type.lower()}_{_short_source(source_type)}_current"
