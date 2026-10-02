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

# Pictures embedded in issue markdown (PBI-083): resolve each markdown image to bytes, embed it in
# the Word output, and resize every picture the way the .NET report did.
#
# Matches `ReportProcessor.cs` on 77b1f86: `ToInternalFileUrl` and `UiApiClient.DownloadFile`
# (the app's own `/File/<guid>` URLs go to the internal UI API with the reporting key), the
# `data:image/` decode, and the resize at lines 294-335.
#
# Every network read goes through a `FileTransport`, `(url, headers) -> (status, body)`. The
# reporting key exists in exactly one place, `UiApiFileClient`, which hands it to its transport.
# A foreign URL goes to the resolver's own `foreign` transport with an empty header dictionary,
# so the key is never sent to a host other than the UI API base. Nothing here logs a header.

from __future__ import annotations

import base64
import binascii
import io
import logging
import re
from typing import Callable

import requests
from docx.image.exceptions import UnrecognizedImageError
from docx.oxml import OxmlElement
from docx.oxml.ns import qn

from Reports.WordFields import make_run

FileTransport = Callable[[str, dict], "tuple[int, bytes]"]

EMU_PER_POINT = 12700

# .NET's `OwnFileUrlRegex` (ReportProcessor.cs 1201-1203 on 77b1f86) with one inner capture group
# added around the file id; .NET's own group 1 captures `File/...(/attachment)?` whole.
_OWN_FILE_URL = re.compile(
    r"^https?://[^/]+(?:/[^/]+)*?/File/"
    r"([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}[^/]*)(?:/attachment)?$",
    re.IGNORECASE)
_HTTP_SCHEME = re.compile(r"^https?://", re.IGNORECASE)


class ImageResolveError(Exception):
    """An image could not be turned into bytes. The message is the reason, safe to log."""


def http_transport(verify_ssl: bool, timeout_sec: float) -> FileTransport:
    """The production transport: one `requests.Session`, never following a redirect.

    .NET's API client sets `AllowAutoRedirect = false` for every request, and a redirect here
    would carry the key header to another host (requests deletes only `Authorization` on a host
    change). A redirected image fails with `HTTP 3xx` and falls back to `[alt]`.
    """
    session = requests.Session()

    def get(url: str, headers: dict) -> tuple[int, bytes]:
        response = session.get(url, headers=headers, verify=verify_ssl, timeout=timeout_sec,
                               allow_redirects=False)
        return response.status_code, response.content

    return get


class UiApiFileClient:
    """Reads one file from the UI API, `GET <base_url>/File/<id>`, with the reporting key header."""

    def __init__(self, base_url: str, key_header: str, key: str, transport: FileTransport) -> None:
        self._base_url = base_url.rstrip("/")
        self._key_header = key_header
        self._key = key
        self._transport = transport

    def fetch(self, file_id: str) -> bytes:
        return _checked(self._transport, f"{self._base_url}/File/{file_id}",
                        {self._key_header: self._key})


def _checked(transport: FileTransport, url: str, headers: dict) -> bytes:
    try:
        status, body = transport(url, headers)
    except Exception as exc:  # any transport failure is a fallback to [alt], never a failed job
        raise ImageResolveError(f"[{type(exc).__name__}] {exc}") from None
    if status != 200:
        raise ImageResolveError(f"HTTP {status}")
    if not body:
        raise ImageResolveError("empty response")
    return body


class ImageResolver:
    """Turns a markdown image `src` into bytes. Results and failures are cached by `src`."""

    def __init__(self, client: UiApiFileClient | None, foreign: FileTransport) -> None:
        self._client = client
        self._foreign = foreign
        self._cache: dict[str, bytes | ImageResolveError] = {}

    def resolve(self, src: str) -> bytes:
        if src not in self._cache:
            try:
                self._cache[src] = self._resolve(src)
            except ImageResolveError as exc:
                self._cache[src] = exc
        cached = self._cache[src]
        if isinstance(cached, ImageResolveError):
            raise cached
        return cached

    def _resolve(self, src: str) -> bytes:
        match = _OWN_FILE_URL.match(src)
        if match:
            if self._client is None:
                raise ImageResolveError("no UI API address")
            return self._client.fetch(match.group(1))
        if src.lower().startswith("data:image/"):
            payload = src[src.index(",") + 1:] if "," in src else ""
            try:
                data = base64.b64decode(payload, validate=False)
            except (binascii.Error, ValueError) as exc:
                raise ImageResolveError(f"bad base64: {exc}") from None
            if not data:
                raise ImageResolveError("empty response")
            return data
        if _HTTP_SCHEME.match(src):
            return _checked(self._foreign, src, {})
        raise ImageResolveError("not an http(s) URL or data:image URI")


def _shown(src: str) -> str:
    if src.lower().startswith("data:"):
        return f"{src[:40]}...({len(src)} chars)"
    return src


class ImageEmbedder:
    """Builds the `w:r` that carries one picture, and records a warning for each failure."""

    def __init__(self, resolver: ImageResolver) -> None:
        self._resolver = resolver
        self._part = None
        self._next_id = 1000
        self._warned: set[str] = set()
        self.warnings: list[str] = []

    def bind(self, document) -> None:
        """Keep the document part pictures are added to, and seed the drawing id counter."""
        self._part = document.part
        self._next_id = self._part.next_id + 1000

    def run_for(self, src: str, alt: str, rpr_source):
        """A `w:r` holding the picture, or None on any failure (a warning is recorded)."""
        try:
            if self._part is None:
                raise ImageResolveError("no document bound")
            data = self._resolver.resolve(src)
            try:
                inline = self._part.new_pic_inline(io.BytesIO(data))
            except UnrecognizedImageError:
                raise ImageResolveError("unrecognised image format") from None
            except Exception as exc:
                raise ImageResolveError(f"[{type(exc).__name__}] {exc}") from None
        except ImageResolveError as exc:
            self._warn(src, str(exc))
            return None
        shape_id = str(self._next_id)
        self._next_id += 1
        doc_pr = inline.find(qn("wp:docPr"))
        doc_pr.set("id", shape_id)
        doc_pr.set("descr", alt)
        for c_nv_pr in inline.iter(qn("pic:cNvPr")):
            c_nv_pr.set("id", shape_id)
        run = make_run("", rpr_source)
        drawing = OxmlElement("w:drawing")
        drawing.append(inline)
        run.append(drawing)
        return run

    def _warn(self, src: str, reason: str) -> None:
        if src in self._warned:
            return
        self._warned.add(src)
        message = f"[Images] image '{_shown(src)}' not embedded: {reason}"
        logging.warning(message)
        self.warnings.append(message)


def _story_roots(document) -> list:
    roots = [document.element.body]
    for section in document.sections:
        for part in (section.header, section.footer, section.first_page_header,
                     section.first_page_footer, section.even_page_header,
                     section.even_page_footer):
            if not part.is_linked_to_previous:
                roots.append(part._element)
    return roots


def resize_pictures(document, max_width_pt: float = 216, max_height_pt: float = 288,
                    static_alt: str = "StaticImage") -> int:
    """Apply .NET's resize to every picture in the body, headers and footers; return the count."""
    max_w, max_h = int(max_width_pt), int(max_height_pt)
    resized = 0
    ids: list[str] = []
    for root in _story_roots(document):
        for doc_pr in root.iter(qn("wp:docPr")):
            ids.append(doc_pr.get("id"))
        for shape in list(root.iter(qn("wp:inline"))) + list(root.iter(qn("wp:anchor"))):
            doc_pr = shape.find(qn("wp:docPr"))
            if doc_pr is not None and doc_pr.get("descr") == static_alt:
                continue
            extent = shape.find(qn("wp:extent"))
            if extent is None:
                continue
            w, h = int(extent.get("cx")) / EMU_PER_POINT, int(extent.get("cy")) / EMU_PER_POINT
            if h <= 0:
                continue
            delta_w, delta_h = w - max_w, h - max_h
            if delta_w > 0 or delta_h > 0:
                ratio = w / h
                if delta_w > delta_h:
                    new_w, new_h = max_w, int(max_w / ratio)
                else:
                    new_w, new_h = int(max_h * ratio), max_h
                cx, cy = int(new_w * EMU_PER_POINT), int(new_h * EMU_PER_POINT)
                extent.set("cx", str(cx))
                extent.set("cy", str(cy))
                for ext in shape.iter(qn("a:ext")):
                    ext.set("cx", str(cx))
                    ext.set("cy", str(cy))
                resized += 1
    if len(ids) != len(set(ids)):
        logging.warning("[Images] duplicate wp:docPr ids in the generated document")
    return resized
