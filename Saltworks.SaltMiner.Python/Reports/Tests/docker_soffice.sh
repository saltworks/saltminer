#!/bin/sh
# --[auto-generated, do not modify this block]--
#
# SaltMiner - The open source vulnerability and pen testing management platform
# Copyright (C) 2024-2026 Saltworks Security, LLC
#
# This program is free software: you can redistribute it and/or modify
# it under the terms of the GNU General Public License as published by
# the Free Software Foundation, either version 3 of the License.
#
# This program is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
# GNU General Public License for more details.
#
# You should have received a copy of the GNU General Public License
# along with this program. If not, see <https://www.gnu.org/licenses/>.
#
# ----

# A `soffice` stand-in for test_pdf_converter_live.py: placed first on PATH, it runs the real
# converter inside the built `-pdf` image via `docker run`, so `PdfConverter.convert_to_pdf`
# exercises the actual LibreOffice install rather than a stub. `$SM_SOFFICE_IMAGE` names the
# image (for example jobmanager-3.6-pdf, or a local `docker build --target os-pdf` tag). The
# test's own temp root is mounted at the identical path inside the container, because every URI
# PdfConverter builds (profile, output, error file) is an absolute path on the host.

set -eu

if [ -z "${SM_SOFFICE_IMAGE:-}" ]; then
    echo "docker_soffice.sh: SM_SOFFICE_IMAGE is not set" >&2
    exit 1
fi

if [ -z "${SM_SOFFICE_MOUNT_ROOT:-}" ]; then
    echo "docker_soffice.sh: SM_SOFFICE_MOUNT_ROOT is not set" >&2
    exit 1
fi

exec docker run --rm \
    --user "$(id -u):$(id -g)" \
    -e "HOME=${SM_SOFFICE_MOUNT_ROOT}" \
    -v "${SM_SOFFICE_MOUNT_ROOT}:${SM_SOFFICE_MOUNT_ROOT}" \
    "${SM_SOFFICE_IMAGE}" \
    soffice "$@"
