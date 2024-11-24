public interface IChatStateService
{
    List<Conversation> Chats { get; }
    Conversation CurrentChat { get; }
    Model SelectedModel { get; }
    Task LoadConversationsAsync();
    Task SaveConversationsAsync();
    Task NewChatAsync();
    Task SelectChatAsync(Conversation chat);
    Task SelectModelAsync(Model model);
}