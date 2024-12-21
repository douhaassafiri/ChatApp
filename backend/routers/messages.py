from fastapi import APIRouter, HTTPException
from pydantic import BaseModel
from typing import List
import sqlite3
from datetime import datetime, timezone

# Router Initialization
router = APIRouter()

# Database File
DATABASE = "chat_app.db"

# Helper Function to Connect to the Database
def get_db_connection():
    """
    Establish a connection to the SQLite database.
    """
    conn = sqlite3.connect(DATABASE)
    conn.row_factory = sqlite3.Row
    return conn

# Pydantic Models
class Message(BaseModel):
    sender: str
    receiver: str
    message: str
    timestamp: str = datetime.now(timezone.utc).isoformat()

class MessageDelete(BaseModel):
    id: int

# Endpoint to Send a Message
@router.post("/send/", response_model=dict)
def send_message(msg: Message):
    """
    Send a message from one user to another.
    """
    conn = get_db_connection()
    cursor = conn.cursor()
    try:
        cursor.execute(
            "INSERT INTO messages (sender, receiver, message, timestamp) VALUES (?, ?, ?, ?)",
            (msg.sender, msg.receiver, msg.message, msg.timestamp),
        )
        message_id = cursor.lastrowid

        response_message = "Server: Message received!"
        cursor.execute(
            "INSERT INTO messages (sender, receiver, message, timestamp) VALUES (?, ?, ?, ?)",
            ("Server", msg.sender, response_message, msg.timestamp),
        )
        conn.commit()

        return {
            "status": "Message sent successfully",
            "id": message_id,
            "response": response_message,
        }
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Database Error: {str(e)}")
    finally:
        conn.close()

# Endpoint to Retrieve Message History Between Two Users
@router.get("/history/", response_model=List[dict])
def get_message_history(sender: str, receiver: str):
    """
    Retrieve all messages exchanged between two users, ordered by timestamp.
    """
    conn = get_db_connection()
    cursor = conn.cursor()
    try:
        cursor.execute(
            """
            SELECT * FROM messages 
            WHERE (sender=? AND receiver=?) OR (sender=? AND receiver=?) 
            ORDER BY timestamp
            """,
            (sender, receiver, receiver, sender),
        )
        messages = cursor.fetchall()
        if not messages:
            raise HTTPException(status_code=404, detail="No messages found")
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Database Error: {str(e)}")
    finally:
        conn.close()
    return [dict(msg) for msg in messages]

# Endpoint to Delete a Message by ID
@router.delete("/delete/{message_id}", response_model=dict)
def delete_message(message_id: int):
    """
    Delete a specific message by its ID.
    """
    conn = get_db_connection()
    cursor = conn.cursor()
    try:
        cursor.execute("DELETE FROM messages WHERE id=?", (message_id,))
        conn.commit()
        if cursor.rowcount == 0:
            raise HTTPException(status_code=404, detail="Message ID not found")
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Database Error: {str(e)}")
    finally:
        conn.close()
    return {"status": "Message deleted successfully"}
