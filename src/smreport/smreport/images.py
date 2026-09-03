"""Image source resolution for markdown images.

Accepts ``data:image/...;base64,`` URIs, local paths (absolute, or relative to the
context file), and — as a best-effort fallback — plain http(s) URLs.  The shim is
expected to have pre-fetched anything that needs authentication and rewritten the
markdown to a local path.
"""

from __future__ import annotations

import base64
import os
import urllib.request

_MAGIC = {
    b"\x89PNG": "png",
    b"\xff\xd8\xff": "jpg",
    b"GIF8": "gif",
    b"BM": "bmp",
}


class ImageError(Exception):
    pass


def load_image(src: str, base_dir: str | None, allow_remote: bool = True, timeout: int = 20) -> bytes:
    if not src:
        raise ImageError("empty image source")
    if src.startswith("data:"):
        comma = src.find(",")
        if comma < 0:
            raise ImageError("malformed data URI")
        try:
            return base64.b64decode(src[comma + 1:], validate=False)
        except Exception as ex:  # noqa: BLE001
            raise ImageError(f"bad base64 image data: {ex}") from ex
    if src.startswith(("http://", "https://")):
        if not allow_remote:
            raise ImageError(f"remote images disabled: {src}")
        req = urllib.request.Request(src, headers={"User-Agent": "smreport/3.5.1"})
        try:
            with urllib.request.urlopen(req, timeout=timeout) as rsp:  # noqa: S310
                return rsp.read()
        except Exception as ex:  # noqa: BLE001
            raise ImageError(f"download failed for {src}: {ex}") from ex
    if src.startswith("file://"):
        src = src[7:]
    path = src if os.path.isabs(src) or not base_dir else os.path.join(base_dir, src)
    try:
        with open(path, "rb") as fh:
            return fh.read()
    except OSError as ex:
        raise ImageError(f"cannot read image {path}: {ex}") from ex


def sniff_format(data: bytes) -> str | None:
    for magic, fmt in _MAGIC.items():
        if data.startswith(magic):
            return fmt
    return None
