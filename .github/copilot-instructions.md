# Workleap Azure Event Grid Emulator

The Workleap Azure Event Grid Emulator is a .NET 9.0 ASP.NET Core application that emulates Azure Event Grid for local development. It supports both push delivery (webhooks) and pull delivery (queue-based) models for EventGridEvents and CloudEvents.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

- Bootstrap, build, and test the repository:
  - Install .NET 9.0 SDK: `curl -sSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 9.0 --install-dir ~/.dotnet`
  - Add to PATH: `export PATH="$HOME/.dotnet:$PATH"`
  - Build and test: `pwsh -File Build.ps1`
  - Alternative build commands (from src/ directory):
    - `dotnet clean -c Release src/`
    - `dotnet build -c Release src/`
    - `dotnet test -c Release src/ --no-restore`

- Run the Event Grid emulator locally:
  - ALWAYS run the bootstrapping steps first.
  - Navigate to: `cd src/EventGridEmulator`
  - Start emulator: `dotnet run` -- starts on http://localhost:6500
  - Health check: `curl -X GET http://localhost:6500/health` returns "Healthy"
  - Default configuration supports both push webhooks and pull subscriptions
  - Application logs show loaded topics and subscribers on startup

- Format and lint code:
  - Format code: `dotnet format` (from src/ directory)
  - Verify formatting: `dotnet format --verify-no-changes` (from src/ directory) -- fails if formatting is needed
  - The project uses extensive .editorconfig rules for C# code analysis
  - NOTE: Some files currently have formatting issues (final newlines, encoding) that need to be fixed

## Validation

- ALWAYS manually validate changes by running the complete build and test suite.
- ALWAYS test the application by starting it and verifying the health endpoint responds.
- ALWAYS test basic event publishing functionality after making changes:
  - Start the emulator: `dotnet run` in src/EventGridEmulator/
  - Test health: `curl -X GET http://localhost:6500/health`
  - Publish test event:

    ```sh
    curl -X POST http://localhost:6500/topicfoobar/api/events \
      -H "Content-Type: application/json" \
      -d @- <<EOF
    [
      {
        "id": "test123",
        "subject": "test-subject",
        "eventType": "test.event",
        "dataVersion": "1.0",
        "data": {
          "message": "test data"
        }
      }
    ]
    EOF
    ```
  - Verify events appear in application logs for pull subscriptions
- You cannot build or test the Docker image in this environment due to certificate restrictions.
- Always run `dotnet format` before you are done or the CI (.github/workflows/ci.yml) will fail.

## Configuration and Usage

- Configuration file: The emulator requires an `appsettings.json` file to define topics and subscriptions
- Push delivery: Events are forwarded to configured webhook URLs
- Pull delivery: Events are queued and retrieved via API calls using `pull://` subscription format
- Event filtering: Supports filtering by event type through the `Filters` configuration section
- The emulator runs on port 6500 by default and exposes these endpoints:
  - `POST /{topic}/api/events` - Publish events to a topic (Custom Topics)
  - `POST /topics/{topic}:publish` - Publish events to a namespace topic
  - `POST /topics/{topic}/eventsubscriptions/{subscription}:receive` - Pull events from subscription
  - `POST /topics/{topic}/eventsubscriptions/{subscription}:acknowledge` - Acknowledge events
  - `POST /topics/{topic}/eventsubscriptions/{subscription}:release` - Release events back to queue
  - `POST /topics/{topic}/eventsubscriptions/{subscription}:reject` - Reject events
  - `GET /health` - Health check endpoint

## Common Tasks

The following are key project directories and files to understand:

### Repository Structure
```
src/
├── EventGridEmulator/              # Main application
│   ├── Program.cs                  # Application entry point and DI setup
│   ├── EventHandling/              # Event processing logic
│   ├── Configuration/              # Configuration options and validation  
│   ├── Network/                    # HTTP client and networking
│   ├── appsettings.json           # Development configuration
│   └── Dockerfile                  # Docker image definition
├── EventGridEmulator.Tests/        # Integration and unit tests
├── Samples/                        # Example publisher and subscriber apps
│   ├── Publisher/                  # Sample event publisher
│   └── Subscriber/                 # Sample webhook subscriber
└── EventGridEmulator.sln          # Solution file
```

### Key Configuration Files
- `global.json` - Specifies .NET 9.0.304 SDK requirement
- `src/Directory.Build.props` - Common MSBuild properties for all projects
- `Build.ps1` - PowerShell build script used by CI
- `.github/workflows/ci.yml` - GitHub Actions CI pipeline
- `src/.editorconfig` - Extensive C# coding standards and analyzer rules

### Build System Details
- Uses PowerShell-based build script for consistency across platforms
- CI runs on Ubuntu using the Build.ps1 script
- Docker image targets linux-musl-x64 runtime on Alpine Linux
- Test framework: xUnit with Microsoft.AspNetCore.Mvc.Testing for integration tests
- Code analysis: Workleap.DotNet.CodingStandards package provides extensive rules

### Event Formats Supported
- EventGridEvents: Traditional Azure Event Grid format
- CloudEvents: CNCF CloudEvents v1.0 specification
- Both formats support the same delivery models (push/pull)
- Namespace topics only support CloudEvents format

### Sample Applications
- Publisher (`src/Samples/Publisher/`): Demonstrates how to publish EventGridEvents to multiple topics
  - Run with: `dotnet run` (from Publisher directory)
  - Publishes to both `activities-eg` and `comments-eg` topics
  - Shows proper use of EventGridPublisherClient with dummy credentials
- Subscriber (`src/Samples/Subscriber/`): Mock webhook server that responds to different scenarios
  - Run with: `dotnet run` (from Subscriber directory)
  - Provides endpoints that return different HTTP status codes for testing retry behavior
  - Includes slow endpoint (1-minute delay) for testing timeout handling
  - Essential for testing push delivery scenarios

### Development Tips
- The emulator starts quickly (2.5 seconds) for rapid development iteration
- Use the health endpoint to verify the application is running correctly
- Sample applications in `Samples/` directory demonstrate usage patterns
- Tests provide examples of both push and pull delivery scenarios
- Configuration hot-reload works when not running in Docker containers
- Always test both EventGridEvent and CloudEvent formats when making changes to event handling
- Docker build may fail in sandboxed environments due to certificate issues - this is normal and expected