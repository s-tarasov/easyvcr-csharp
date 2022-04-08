# EasyVCR

#### Disclaimer
This is a distant fork of the original [Scotch by Martin Leech](https://github.com/mleech/scotch), completely rewritten in C# and enhanced with several new features and performance improvements.

## What is EasyVCR?
EasyVCR is a library for recording and replaying HTTP interactions in your test suite.

This can be useful for speeding up your test suite, or for running your tests on a CI server which doesn't have connectivity to the HTTP endpoints you need to interact with.

## How to use EasyVCR
#### Step 1.
Run your test suite locally against a real HTTP endpoint in recording mode

```csharp
using EasyPost.EasyVCR;

// Create a cassette to handle HTTP interactions
var cassette = new Cassette("path/to/cassettes", "my_cassette");

// create an HttpClient using the cassette
var recordingHttpClient = HttpClients.NewHttpClient(cassette, Mode.Record);

// Use this HttpClient in any class making HTTP calls
// For example, RestSharp v107+ supports custom HTTP clients
RestClient restClient = new RestClient(recordingHttpClient, new RestClientOptions()));

// Or make HTTP calls directly
var response = await recordingHttpClient.GetAsync("https://api.example.com/v1/users");
```
Real HTTP calls will be made and recorded to the cassette file.

#### Step 2.
Switch to replay mode:
```csharp
using EasyPost.EasyVCR;

// Create a cassette to handle HTTP interactions
var cassette = new Cassette("path/to/cassettes", "my_cassette");

// create an HttpClient using the cassette
var replayingHttpClient = HttpClients.NewHttpClient(cassette, Mode.Replay);
```
Now when tests are run, no real HTTP calls will be made. Instead, the HTTP responses will be replayed from the cassette file.

### Available modes
- `Mode.Record`: Record a request, including overwriting any existing matching recording.
- `Mode.Replay`: Replay a request. Throws an exception if no matching recording is found.
- `Mode.Auto`:  Play back a request if it has been recorded before, or record a new one if not.
- `Mode.Bypass`:  Do not record or replay any requests (client will behave like a normal HttpClient).

## Features
`EasyVCR` comes with a number of features, many of which can be customized via the `AdvancedOptions` class.

### Censoring
Censor sensitive data in the request and response bodies and headers, such as API keys and auth tokens.

**Default**: *Disabled*
```csharp
using EasyPost.EasyVCR;

var cassette = new Cassette("path/to/cassettes", "my_cassette");
var advancedOptions = new AdvancedOptions()
{
    Censors = new Censors().HideHeader("Authorization") // Hide the Authorization header
};

var httpClient = HttpClients.NewHttpClient(cassette, Mode.Record, advancedSettings);
```
### Delay
Simulate a delay when replaying a recorded request, either using a specified delay or the original request duration.

**Default**: *No delay*
```csharp
using EasyPost.EasyVCR;

var cassette = new Cassette("path/to/cassettes", "my_cassette");
var advancedOptions = new AdvancedOptions()
{
    SimulateDelay = true, // Simulate a delay of the original request duration when replaying (overrides ManualDelay)
    ManualDelay = 1000 // Simulate a delay of 1000 milliseconds when replaying
};

var httpClient = HttpClients.NewHttpClient(cassette, Mode.Replay, advancedSettings);
```
### Matching
Customize how a recorded request is determined to be a match to the current request.

**Default**: *Method and full URL must match*
```csharp
using EasyPost.EasyVCR;

var cassette = new Cassette("path/to/cassettes", "my_cassette");
var advancedOptions = new AdvancedOptions()
{
    MatchRules = new MatchRules().ByBody().ByHeader("X-My-Header"), // Match recorded requests by body and a specific header
};

var httpClient = HttpClients.NewHttpClient(cassette, Mode.Replay, advancedSettings);
```

### Ordering
Customize how elements of a recorded request are organized in the cassette file. 
Helpful to avoid unnecessary git differences between cassette file versions. 

**Default**: *Elements are stored alphabetically*

**NOTE:** This setting must be used when creating the cassette.
```csharp
using EasyPost.EasyVCR;

var order = new CassetteOrder.None(); // elements of each request in a cassette won't be ordered in any particular way
var cassette = new Cassette("path/to/cassettes", "my_cassette", order);

var httpClient = HttpClients.NewHttpClient(cassette, Mode.Replay, advancedSettings);
```

### HttpClient Conversion
Override how HttpClient request and response objects are converted into `EasyVCR` request and response objects, and vice versa.
Useful if `HttpClient` suffers breaking changes in future .NET versions.
```csharp
using EasyPost.EasyVCR;

var cassette = new Cassette("path/to/cassettes", "my_cassette");
var advancedOptions = new AdvancedOptions()
{
    InteractionConverter = new MyInteractionConverter(), // use a custom interaction converter by implementing IInteractionConverter
};

var httpClient = HttpClients.NewHttpClient(cassette, Mode.Replay, advancedSettings);
```

## VCR
`EasyVCR` also offers a built-in VCR, which can be used to easily switch between multiple cassettes. Any advanced settings applied to the VCR will be applied on every request made using the VCR's HttpClient.

- Prevent sensitive headers from being recorded to cassette:
```csharp
using EasyPost.Scotch;

// Censor default sensitive headers (i.e. `Authorization`)
var vcrHttpClient = HttpClients.NewHttpClient(pathToCassetteFile, ScotchMode.Recording, hideCredentials: true);

// Indicate specific headers to censor
var vcrHttpClient = HttpClients.NewHttpClient(pathToCassetteFile, ScotchMode.Recording, hideCredentials: true, headersToHide: new string[] { "MyCustomHeader" });
```