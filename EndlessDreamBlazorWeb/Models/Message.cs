public record Message(bool IsUser, string Content, DateTime Timestamp, bool IsError = false);