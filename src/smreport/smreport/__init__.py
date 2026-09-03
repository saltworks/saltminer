"""SaltMiner engagement report renderer.

Standalone, file-based contract: a JSON context (the serialized report DTO) plus a
Jinja-tagged DOCX template in, DOCX/PDF files out.  Nothing in here may depend on
JobManager or any other SaltMiner component.
"""

__version__ = "3.5.1"
