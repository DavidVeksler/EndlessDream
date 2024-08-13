public class Model
{
    public string Id { get; set; } = "";
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

public class Choice
{
    public string Text { get; set; } = "";
    public int Index { get; set; }
    public object Logprobs { get; set; } = new();
    public string FinishReason { get; set; } = "";
}