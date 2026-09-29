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

# The entry point ReportGenerator.cs (Saltworks.SaltMiner.JobManager) invokes as a subprocess, one
# call per engagement report job, plus a one-shot startup check. See
# Designs/DESIGN-PBI-051-jobmanager-report-cutover.md section 1 for the request/result JSON shapes
# and the exit code contract this module implements:
#
#   python3 -m Reports.Generate report --request <tmp>/request.json
#   python3 -m Reports.Generate check  --attachment-type <ReportAttachmentType>
#
#   env SM_REPORT_DATA_API_KEY=<JobManagerConfig.DataApiKey>   (never on argv, never in a file)
#   exit 0 success or check passed | 1 report failed | 2 usage error (argparse) | 3 check failed
#
# Logging goes to stderr only: .NET reads stdout for nothing and relays stderr into the job's
# error message on a non-zero exit.

from __future__ import annotations

import argparse
import json
import logging
import os
import sys
import traceback
from pathlib import Path

from Core.DataClient import DataClient
from Reports.EngagementData import DataApiSource, assemble_engagement, load_report_settings
from Reports.PdfConverter import check_pdf_converter, write_attachments
from Reports.ValueRenderer import make_value_renderer
from Reports.WordMerge import fill_template

DATA_API_KEY_ENV_VAR = "SM_REPORT_DATA_API_KEY"

EXIT_OK = 0
EXIT_REPORT_FAILED = 1
EXIT_USAGE_ERROR = 2
EXIT_CHECK_FAILED = 3


class _SettingsShim:
    """Serves `app.Settings.Get(section, key, default)` from a plain dict of sections.

    `Core.DataClient.DataClient.__init__` and `Reports.EngagementData.load_report_settings` touch
    `app` only through this method (design section 1), so this is the whole of what a request-driven
    run needs in place of a real `Core.Application.Application`.
    """

    def __init__(self, sections: dict) -> None:
        self._sections = sections

    def Get(self, section, key, default=None):
        return self._sections.get(section, {}).get(key, default)


class _AppShim:
    def __init__(self, sections: dict) -> None:
        self.Settings = _SettingsShim(sections)


def _build_app_shim(request: dict, data_api_key: str) -> _AppShim:
    data_api = request.get("data_api") or {}
    return _AppShim({
        "DataClient": {
            "ApiUrl": data_api.get("url"),
            "ApiKey": data_api_key,
            "ManagerApiKey": data_api_key,
            "SslVerify": data_api.get("verify_ssl", True),
            "TimeoutSec": data_api.get("timeout_sec", 240),
        },
        "Reports": request.get("settings") or {},
    })


def _run_report(request_path: Path) -> int:
    request = json.loads(request_path.read_text(encoding="utf-8"))
    result_path = Path(request["result_path"])
    data_api_key = os.environ.get(DATA_API_KEY_ENV_VAR, "")

    app = _build_app_shim(request, data_api_key)
    client = DataClient(app)
    try:
        source = DataApiSource(client)
        settings = load_report_settings(app)
        record, markdown_fields = assemble_engagement(request["engagement_id"], source, settings)

        output_dir = Path(request["output_dir"])
        work_dir = output_dir / "work"
        work_dir.mkdir(parents=True, exist_ok=True)
        report_name = request["report_name"]
        work_docx = work_dir / f"{report_name}.docx"

        renderer, render_result = make_value_renderer(markdown_fields,
                                                       request.get("field_value_colors") or {})
        merge_result = fill_template(request["template_path"], work_docx, record,
                                     keep_unmatched=False, renderer=renderer)

        written = write_attachments(work_docx, output_dir, request.get("attachment_type"))

        result_path.write_text(json.dumps({
            "files": [str(path) for path in written],
            "template_path": str(request["template_path"]),
            "fields_merged": merge_result.fields_merged,
            "unmatched": merge_result.unmatched,
            "unsupported_markers": merge_result.unsupported_markers,
            "unsupported_fields": merge_result.unsupported_fields,
            "groups_missing": merge_result.groups_missing,
            "unknown_colors": render_result.unknown_colors,
        }), encoding="utf-8")
        return EXIT_OK
    finally:
        client.close()


def _handle_report(args: argparse.Namespace) -> int:
    try:
        return _run_report(Path(args.request))
    except Exception as exc:
        print(f"report generation failed: [{type(exc).__name__}] {exc}", file=sys.stderr)
        traceback.print_exc()
        return EXIT_REPORT_FAILED


def _handle_check(args: argparse.Namespace) -> int:
    return EXIT_OK if check_pdf_converter(args.attachment_type) else EXIT_CHECK_FAILED


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(prog="python3 -m Reports.Generate")
    subparsers = parser.add_subparsers(dest="mode", required=True)

    report_parser = subparsers.add_parser(
        "report", help="Generate one engagement report from a request file.")
    report_parser.add_argument("--request", required=True, help="Path to the request JSON file.")
    report_parser.set_defaults(handler=_handle_report)

    check_parser = subparsers.add_parser(
        "check", help="Check whether this image can satisfy an attachment type.")
    check_parser.add_argument("--attachment-type", dest="attachment_type", required=True,
                              help="The configured ReportAttachmentType: Word, Pdf or All.")
    check_parser.set_defaults(handler=_handle_check)

    return parser


def main(argv: list[str] | None = None) -> int:
    logging.basicConfig(stream=sys.stderr, level=logging.INFO)
    parser = build_parser()
    args = parser.parse_args(argv)
    return args.handler(args)


if __name__ == "__main__":
    sys.exit(main())
