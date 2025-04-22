# ipvcr API documentation

## Authentication

The ipvcr Web API uses token-based authentication. To use the API endpoints, you must first authenticate and obtain a token.

| Method | Route | Data Type | Description |
|--------|-------|-----------|-------------|
| POST | /api/login | [LoginRequest](#loginrequest) | Authenticates a user and returns a token |
| POST | /api/login/changepassword | [LoginRequest](#loginrequest) | Updates the admin password (requires authentication) |

**Data Types:**

<a name="loginrequest"></a>
```typescript
interface LoginRequest {
  username: string;
  password: string;
}
```

**Response for successful login:**
```typescript
{
  token: string  // JWT token for authentication
}
```

## Recordings

| Method | Route | Data Type | Description |
|--------|-------|-----------|-------------|
| GET | /api/recordings | [ScheduledRecording](#scheduledrecording)[] | Retrieves a list of all scheduled recordings |
| GET | /api/recordings/{id} | [ScheduledRecording](#scheduledrecording) | Retrieves a specific recording by ID |
| POST | /api/recordings | [ScheduledRecording](#scheduledrecording) | Creates a new scheduled recording |
| PUT | /api/recordings/{id} | [ScheduledRecording](#scheduledrecording) | Updates an existing scheduled recording |
| DELETE | /api/recordings/{id} | - | Deletes a scheduled recording |
| GET | /api/recordings/task/{id} | [TaskDefinitionModel](#taskdefinitionmodel) | Retrieves the task definition for a recording |
| PUT | /api/recordings/task/{id} | [TaskEditModel](#taskeditmodel) | Updates the task definition for a recording |
| GET | /api/recordings/channelcount | number | Returns the total count of available channels |
| GET | /api/recordings/channels/search?query={query} | [ChannelInfo](#channelinfo)[] | Searches for channels matching the query string |

**Data Types:**

<a name="scheduledrecording"></a>
```typescript
interface ScheduledRecording {
  id: string; // GUID
  name: string;
  startTime: string; // ISO 8601 date format
  duration: number; // in seconds
  channelName?: string;
  channelUrl?: string;
  isEnabled: boolean;
  isRecurring: boolean;
  recurringDays?: number[]; // Days of week (0 = Sunday, 1 = Monday, etc.)
}
```

<a name="taskdefinitionmodel"></a>
```typescript
interface TaskDefinitionModel {
  id: string; // GUID
  name: string;
  content: string; // Task definition file content
}
```

<a name="taskeditmodel"></a>
```typescript
interface TaskEditModel {
  id: string; // GUID
  taskFile: string; // Updated task definition content
}
```

<a name="channelinfo"></a>
```typescript
interface ChannelInfo {
  name: string;
  url: string;
  group?: string;
}
```

### Settings

| Method | Route | Data Type | Description |
|--------|-------|-----------|-------------|
| GET | /api/settings/scheduler | [SchedulerSettings](#schedulersettings) | Retrieves scheduler settings |
| PUT | /api/settings/scheduler | [SchedulerSettings](#schedulersettings) | Updates scheduler settings |
| GET | /api/settings/playlist | [PlaylistSettings](#playlistsettings) | Retrieves playlist settings |
| PUT | /api/settings/playlist | [PlaylistSettings](#playlistsettings) | Updates playlist settings |
| GET | /api/settings/ssl | [SslSettings](#sslsettings) | Retrieves SSL settings |
| PUT | /api/settings/ssl | [SslSettings](#sslsettings) | Updates SSL settings |
| GET | /api/settings/ffmpeg | [FfmpegSettings](#ffmpegsettings) | Retrieves FFmpeg settings |
| PUT | /api/settings/ffmpeg | [FfmpegSettings](#ffmpegsettings) | Updates FFmpeg settings |
| POST | /api/settings/upload-m3u | FormData (file) | Uploads an M3U playlist file |
| GET | /api/settings/adminsettings | [AdminPasswordSettings](#adminpasswordsettings) | Retrieves admin settings |
| PUT | /api/settings/adminsettings | [AdminPasswordSettings](#adminpasswordsettings) | Updates admin settings |

**Data Types:**

<a name="schedulersettings"></a>
```typescript
interface SchedulerSettings {
  mediaPath: string;
  dataPath: string;
  removeTaskAfterExecution: boolean;
}
```

<a name="playlistsettings"></a>
```typescript
interface PlaylistSettings {
  m3uPlaylistPath: string;
  playlistAutoUpdateInterval: number; // hours
  autoReloadPlaylist: boolean;
  filterEmptyGroups: boolean;
}
```

<a name="sslsettings"></a>
```typescript
interface SslSettings {
  certificatePath: string;
  certificatePassword: string;
  useSsl: boolean;
}
```

<a name="ffmpegsettings"></a>
```typescript
interface FfmpegSettings {
  fileType: string;
  codec: string;
  audioCodec: string;
  videoBitrate: string;
  audioBitrate: string;
  resolution: string;
  frameRate: string;
  aspectRatio: string;
  outputFormat: string;
}
```

<a name="adminpasswordsettings"></a>
```typescript
interface AdminPasswordSettings {
  adminUsername: string;
  allowUserRegistration?: boolean;
  maxUsersAllowed?: number;
}
```

## Python Examples

### Basic Authentication and Fetching Recordings

```python
import requests

# Configuration
base_url = "http://localhost:5000"  # Change to your server URL
username = "admin"                  # Your username
password = "your_password"          # Your password

# Login to get authentication token
login_response = requests.post(
    f"{base_url}/api/login",
    json={"username": username, "password": password}
)

# Extract token from response
token = login_response.json()["token"]

# Use token for authenticated requests
headers = {"Authorization": f"Bearer {token}"}

# Fetch list of recordings
recordings_response = requests.get(
    f"{base_url}/api/recordings", 
    headers=headers
)

# Display results
recordings = recordings_response.json()
print(f"Found {len(recordings)} recordings:")
for recording in recordings:
    print(f"- {recording['name']} (Channel: {recording['channelName']})")
```

This example demonstrates:
1. Authenticating with username/password
2. Extracting the JWT token
3. Using the token to make an authenticated request
4. Retrieving and displaying the list of recordings

### JavaScript Example: Scheduling a New Recording

```javascript
// Using fetch API with async/await
async function scheduleNewRecording() {
  // Configuration
  const baseUrl = 'http://localhost:5000'; // Change to your server URL
  const username = 'admin';               // Your username
  const password = 'your_password';       // Your password

  try {
    // Login to get authentication token
    const loginResponse = await fetch(`${baseUrl}/api/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password })
    });
    
    const { token } = await loginResponse.json();
    
    // Create a new recording object
    const newRecording = {
      id: '', // Leave empty
      name: 'Evening News',
      startTime: new Date(Date.now() + 3600000).toISOString(), // Start 1 hour from now
      duration: 1800, // 30 minutes in seconds
      channelName: 'BBC News',
      channelUrl: 'http://example.com/bbc-news-stream',
      isEnabled: true,
      isRecurring: false
    };
    
    // Schedule the recording
    const recordingResponse = await fetch(`${baseUrl}/api/recordings`, {
      method: 'POST',
      headers: { 
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify(newRecording)
    });
    
    if (recordingResponse.ok) {
      const result = await recordingResponse.json();
      console.log('Recording scheduled successfully:', result);
    } else {
      console.error('Failed to schedule recording:', await recordingResponse.text());
    }
  } catch (error) {
    console.error('Error:', error);
  }
}

// Call the function to schedule a recording
scheduleNewRecording();
```

This example demonstrates:
1. Authenticating with username/password using fetch API
2. Creating a new recording object with appropriate properties
3. Submitting the recording to the API with proper authorization
4. Handling the response

### Zig Example: Deleting a Scheduled Recording

```zig
const std = @import("std");
const http = @import("std").http;
const json = @import("std").json;

pub fn main() !void {
    // Configuration
    const base_url = "http://localhost:5000"; // Change to your server URL
    const username = "admin";                 // Your username
    const password = "your_password";         // Your password
    const recording_id = "12345678-1234-1234-1234-123456789abc"; // ID of recording to delete

    var gpa = std.heap.GeneralPurposeAllocator(.{}){};
    defer _ = gpa.deinit();
    const allocator = gpa.allocator();

    // Initialize HTTP client
    var client = http.Client{ .allocator = allocator };
    defer client.deinit();

    // Prepare login credentials
    const login_payload = try std.fmt.allocPrint(
        allocator,
        "{{\"username\":\"{s}\",\"password\":\"{s}\"}}",
        .{ username, password }
    );
    defer allocator.free(login_payload);

    // Login to get authentication token
    var login_headers = http.Headers.init(allocator);
    try login_headers.append("Content-Type", "application/json");

    const login_response = try client.post(
        base_url ++ "/api/login",
        login_headers,
        login_payload
    );
    defer login_response.deinit();

    // Extract token from JSON response
    var token_buf: [1024]u8 = undefined;
    const token_bytes = try login_response.reader().readAll(&token_buf);
    const token_json = try json.parse(token_buf[0..token_bytes]);
    const token = token_json.object.get("token").?.string;

    // Prepare DELETE request with authentication
    var delete_headers = http.Headers.init(allocator);
    try delete_headers.append("Authorization", try std.fmt.allocPrint(allocator, "Bearer {s}", .{token}));

    // Send DELETE request
    const delete_url = try std.fmt.allocPrint(
        allocator, 
        "{s}/api/recordings/{s}", 
        .{ base_url, recording_id }
    );
    defer allocator.free(delete_url);

    const delete_response = try client.delete(delete_url, delete_headers, "");
    defer delete_response.deinit();

    // Check response status
    if (delete_response.status_code == 204) {
        std.debug.print("Recording deleted successfully\n", .{});
    } else {
        std.debug.print("Failed to delete recording: {d}\n", .{delete_response.status_code});
    }
}
```

This example demonstrates:
1. Authenticating with username/password in Zig
2. Working with HTTP headers and JSON in Zig's standard library
3. Sending a DELETE request with proper authentication
4. Handling the response to confirm successful deletion


