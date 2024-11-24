public record Conversation
{
    public string Title { get; init; } = "";
    public List<Message> Messages { get; init; } = new();
}