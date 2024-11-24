
public class AIService
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string EndpointUrl { get; set; }
}

// Models/Conversation.cs
public class Conversation
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; }
    public string ServiceId { get; set; }  // Track which AI service this chat uses
    public List<Message> Messages { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}


public class Model
{
    public string Id { get; set; } = "";

    public string EndpointUrl { get; set; }
    public string Object { get; set; } = "";
    public string OwnedBy { get; set; } = "";
    public List<object> Permission { get; set; } = new();
}

public class ModelResponse
{
    public List<Model> Data { get; set; } = new();
    public string Object { get; set; } = "";
}

public class EmbeddingResponse
{
    public List<Embedding> Data { get; set; } = new();
    public string Model { get; set; } = "";
    public Usage Usage { get; set; } = new();
}

public class Embedding
{
    public List<float> embedding { get; set; } = new();
    public int Index { get; set; }
}

public class Usage
{
    public int PromptTokens { get; set; }
    public int TotalTokens { get; set; }
}

public class CompletionRequest
{
    public string Model { get; set; } = "";
    public string Prompt { get; set; } = "";
    public float Temperature { get; set; } = 0.7f;
    public int MaxTokens { get; set; } = 16;
    public float TopP { get; set; } = 1;
    public float FrequencyPenalty { get; set; } = 0;
    public float PresencePenalty { get; set; } = 0;
}

public class CompletionResponse
{
    public string Id { get; set; } = "";
    public string Object { get; set; } = "";
    public long Created { get; set; }
    public string Model { get; set; } = "";
    public List<Choice> Choices { get; set; } = new();
    public Usage Usage { get; set; } = new();
}




public class Message
{
    public bool IsUser { get; set; }
    public string Content { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public bool IsError { get; set; }
}

// Models/CompletionChunk.cs
public class CompletionChunk
{
    public List<Choice> Choices { get; set; } = new();
}

public class Choice
{
    public Delta Delta { get; set; }
}

public class Delta
{
    public string Content { get; set; }
}

public class AIEndpoint
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string EndpointUrl { get; set; }
    public bool IsLocalService { get; set; }
    public string ModelId { get; set; }  // For remote models

    // Factory method for local services
    public static AIEndpoint CreateLocalService(string id, string name, string description, string endpointUrl)
    {
        return new AIEndpoint
        {
            Id = id,
            Name = name,
            Description = description,
            EndpointUrl = endpointUrl,
            IsLocalService = true
        };
    }

    // Factory method for remote models
    public static AIEndpoint CreateRemoteModel(Model model, string baseUrl)
    {
        return new AIEndpoint
        {
            Id = $"remote-{model.Id}",
            Name = model.Id,
            Description = $"Remote model: {model.Id}",
            EndpointUrl = baseUrl,
            IsLocalService = false,
            ModelId = model.Id
        };
    }
}