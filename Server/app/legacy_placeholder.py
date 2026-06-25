def save_session(payload: dict) -> dict:
    """Temporary in-memory persistence used during API prototyping."""
    return {"accepted": True, "payload": payload}
