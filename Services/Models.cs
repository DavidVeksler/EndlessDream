/// <summary>
/// Represents a conversation session with multiple messages.
/// </summary>
public class Conversation
{
    /// <summary>Gets or sets the conversation title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the AI service used for this conversation.</summary>
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>Gets or sets the messages in this conversation.</summary>
    public List<Message> Messages { get; set; } = new();
}

/// <summary>
/// Represents a single message in a conversation.
/// </summary>
public class Message
{
    /// <summary>Gets or sets whether this message is from the user (true) or assistant (false).</summary>
    public bool IsUser { get; set; }

    /// <summary>Gets or sets the message content.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Gets or sets the timestamp when the message was created.</summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>Gets or sets whether this message represents an error.</summary>
    public bool IsError { get; set; }
}

/// <summary>
/// Represents an AI model from the LLM API.
/// </summary>
public class Models
{
    /// <summary>Gets or sets the unique model identifier.</summary>
    public string Id { get; set; } = string.Empty;
}

/// <summary>
/// Response from LLM endpoint containing available models.
/// </summary>
public class ModelResponse
{
    /// <summary>Gets or sets the list of available models.</summary>
    public List<Models> Data { get; set; } = new();
}

/// <summary>
/// Represents an AI endpoint configuration for the LLM service.
/// Supports local models, remote models, and custom services.
/// </summary>
public class AIEndpoint
{
    /// <summary>Gets or sets the unique endpoint identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable endpoint name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the endpoint description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the endpoint base URL.</summary>
    public string EndpointUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets whether this is a custom service.</summary>
    public bool IsCustomService { get; set; }

    /// <summary>Gets or sets the model ID for non-custom services.</summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>Gets or sets the source of the endpoint (local, remote, custom).</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Creates a custom service endpoint.
    /// </summary>
    public static AIEndpoint CreateCustomService(
        string id,
        string name,
        string description,
        string endpointUrl)
    {
        return new AIEndpoint
        {
            Id = id,
            Name = name,
            Description = description,
            EndpointUrl = endpointUrl,
            IsCustomService = true,
            Source = "custom"
        };
    }

    /// <summary>
    /// Creates a model endpoint from an API response.
    /// </summary>
    public static AIEndpoint CreateModel(Models model, string baseUrl, string source)
    {
        return new AIEndpoint
        {
            Id = $"{source}-{model.Id}",
            Name = model.Id,
            Description = $"{source} model: {model.Id}",
            EndpointUrl = baseUrl,
            IsCustomService = false,
            ModelId = model.Id,
            Source = source
        };
    }

    /// <summary>
    /// Creates a local model endpoint (backward compatibility).
    /// </summary>
    public static AIEndpoint CreateLocalModel(Models model, string baseUrl, string modelId) =>
        CreateModel(model, baseUrl, modelId);

    /// <summary>
    /// Checks if this endpoint is from a specific source.
    /// </summary>
    public bool IsFromSource(string source) =>
        Source?.Equals(source, StringComparison.OrdinalIgnoreCase) ?? false;
}
