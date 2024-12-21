from pydantic import BaseModel
from typing import Optional
from datetime import datetime, timezone

# Data Model for a New Message
class Message(BaseModel):
    sender: str
    receiver: str
    message: str
    timestamp: Optional[str] = datetime.now(timezone.utc).isoformat()

# Model for Deleting a Message
class MessageDelete(BaseModel):
    id: int
