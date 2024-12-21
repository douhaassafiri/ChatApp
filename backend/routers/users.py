from fastapi import APIRouter, HTTPException
from pydantic import BaseModel
from database import get_db_connection
import bcrypt

router = APIRouter()

# User model
class User(BaseModel):
    username: str
    password: str

# Response model for user registration/login
class UserResponse(BaseModel):
    username: str
    status: str

# Helper Function to Hash Password
def hash_password(password: str) -> str:
    return bcrypt.hashpw(password.encode('utf-8'), bcrypt.gensalt()).decode('utf-8')

# Helper Function to Verify Password
def verify_password(password: str, hashed: str) -> bool:
    return bcrypt.checkpw(password.encode('utf-8'), hashed.encode('utf-8'))

# Register a new user
@router.post("/register", response_model=UserResponse)
def register_user(user: User):
    conn = get_db_connection()
    cursor = conn.cursor()

    try:
        # Check if the user already exists
        cursor.execute("SELECT * FROM users WHERE username = ?", (user.username,))
        if cursor.fetchone():
            conn.close()
            raise HTTPException(status_code=400, detail="Username already exists")

        # Hash the password before storing it
        hashed_password = hash_password(user.password)

        # Insert the new user
        cursor.execute("INSERT INTO users (username, password) VALUES (?, ?)", (user.username, hashed_password))
        conn.commit()
    except Exception as e:
        conn.close()
        raise HTTPException(status_code=500, detail=f"Database error: {e}")
    
    conn.close()
    return {"username": user.username, "status": "User registered successfully"}

# User login endpoint
@router.post("/login", response_model=UserResponse)
def login_user(user: User):
    conn = get_db_connection()
    cursor = conn.cursor()

    try:
        # Fetch user details
        cursor.execute("SELECT * FROM users WHERE username = ?", (user.username,))
        db_user = cursor.fetchone()
        if not db_user:
            raise HTTPException(status_code=404, detail="User not found")

        # Verify password
        stored_hashed_password = db_user["password"]
        if not verify_password(user.password, stored_hashed_password):
            raise HTTPException(status_code=401, detail="Invalid credentials")

    except Exception as e:
        conn.close()
        raise HTTPException(status_code=500, detail=f"Database error: {e}")

    conn.close()
    return {"username": user.username, "status": "Login successful"}

# Fetch user details
@router.get("/users/{username}", response_model=UserResponse)
def get_user(username: str):
    conn = get_db_connection()
    cursor = conn.cursor()

    try:
        cursor.execute("SELECT username FROM users WHERE username = ?", (username,))
        user = cursor.fetchone()
    except Exception as e:
        conn.close()
        raise HTTPException(status_code=500, detail=f"Database error: {e}")

    conn.close()

    if not user:
        raise HTTPException(status_code=404, detail="User not found")
    
    return {"username": user["username"], "status": "User details fetched"}
