import sqlite3

# Function to get a database connection
def get_db_connection():
    return sqlite3.connect("chat_app.db", check_same_thread=False)

# Initialize database and create tables
def initialize_database():
    with get_db_connection() as conn:
        c = conn.cursor()

        # Create Messages Table
        c.execute('''CREATE TABLE IF NOT EXISTS messages (
            id INTEGER PRIMARY KEY, 
            sender TEXT NOT NULL, 
            receiver TEXT NOT NULL, 
            message TEXT NOT NULL, 
            timestamp TEXT NOT NULL
        )''')

        # Create Users Table
        c.execute('''CREATE TABLE IF NOT EXISTS users (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            username TEXT UNIQUE NOT NULL,
            password TEXT NOT NULL
        )''')

        conn.commit()

# Function to Add a Message
def add_message(sender, receiver, message, timestamp):
    try:
        with get_db_connection() as conn:
            c = conn.cursor()
            c.execute("INSERT INTO messages (sender, receiver, message, timestamp) VALUES (?, ?, ?, ?)", 
                      (sender, receiver, message, timestamp))
            conn.commit()
    except Exception as e:
        print(f"Error adding message: {e}")

# Function to Get All Messages
def get_all_messages():
    with get_db_connection() as conn:
        c = conn.cursor()
        c.execute("SELECT * FROM messages ORDER BY timestamp DESC")
        return c.fetchall()

# Function to Get Messages by User
def get_messages_by_user(username):
    with get_db_connection() as conn:
        c = conn.cursor()
        c.execute("SELECT * FROM messages WHERE sender=? OR receiver=? ORDER BY timestamp DESC", 
                  (username, username))
        return c.fetchall()

# Function to Delete a Message by ID
def delete_message(message_id):
    try:
        with get_db_connection() as conn:
            c = conn.cursor()
            c.execute("DELETE FROM messages WHERE id=?", (message_id,))
            conn.commit()
            return c.rowcount > 0
    except Exception as e:
        print(f"Error deleting message: {e}")
        return False

# Function to Add a New User
def add_user(username, password):
    try:
        with get_db_connection() as conn:
            c = conn.cursor()
            c.execute("INSERT INTO users (username, password) VALUES (?, ?)", (username, password))
            conn.commit()
    except sqlite3.IntegrityError:
        print("Error: Username already exists.")
    except Exception as e:
        print(f"Error adding user: {e}")

# Function to Get User by Username
def get_user(username):
    with get_db_connection() as conn:
        c = conn.cursor()
        c.execute("SELECT * FROM users WHERE username = ?", (username,))
        return c.fetchone()

# Initialize the database when the script is run
if __name__ == "__main__":
    initialize_database()
    print("Database initialized successfully.")
