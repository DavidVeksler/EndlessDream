public class Conversation
{
   // public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; }
    public string ServiceId { get; set; } // Track which AI service this chat uses
    public List<Message> Messages { get; set; } = new();
  //  public DateTime CreatedAt { get; set; } = DateTime.Now;
}


public class Models
{
    public string Id { get; set; } = "";

    //public string EndpointUrl { get; set; }
    //public string Object { get; set; } = "";
    //public string OwnedBy { get; set; } = "";
    //public List<object> Permission { get; set; } = new();
}

public class ModelResponse
{
    public List<Models> Data { get; set; } = new();
    //public string Object { get; set; } = "";
}


public class Message
{
    public bool IsUser { get; set; }
    public string Content { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public bool IsError { get; set; }
}


public class AIEndpoint
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string EndpointUrl { get; set; }
    public bool IsCustomService { get; set; }
    public string ModelId { get; set; } // For local models
    public string Source { get; set; } // To track model source

    // Factory method for custom services
    public static AIEndpoint CreateCustomService(string id, string name, string description, string endpointUrl)
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

    // Factory method for local models (maintaining backward compatibility)
    public static AIEndpoint CreateLocalModel(Models model, string baseUrl, string modelId)
    {
        return CreateModel(model, baseUrl, modelId);
    }

    // New factory method for any model source
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

    // Helper method to determine if model is from a specific source
    public bool IsFromSource(string source)
    {
        return Source?.Equals(source, StringComparison.OrdinalIgnoreCase) ?? false;
    }
}