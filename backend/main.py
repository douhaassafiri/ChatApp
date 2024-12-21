from fastapi import FastAPI
from routers.messages import router as messages_router

# Create the FastAPI application instance
app = FastAPI()

# Include the message routes (from messages.py)
app.include_router(messages_router, prefix="/api", tags=["Messages"])

# Root Endpoint - For Basic Health Check or Greeting
@app.get("/")
async def root():
    """
    Root endpoint to test if the server is running.
    """
    return {"message": "Welcome to the Chat App API!"}

# Server Status Endpoint
@app.get("/status", tags=["System"])
async def status():
    """
    Endpoint to check server status.
    """
    return {"status": "online"}
