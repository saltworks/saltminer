class AssessmentType:
    """Enumeration for different types of security assessments that can be performed.
    This is used to avoid magic strings, not to validate assessment types, so custom types are allowed.
    """
    DAST = "DAST"
    OPEN = "Open"
    NET = "Net"
    SAST = "SAST"
    PEN = "Pen"
    CONTAINER = "Container"
    MOBILE = "Mobile"
    SECRET = "Secret"
    IAST = "IAST"
    LICENSE = "License"
    IAC = "IAC"
    CLOUD = "Cloud",
    CUSTOM = "Custom"