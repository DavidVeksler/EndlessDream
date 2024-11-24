using Microsoft.JSInterop;
using System.Text.Json;

public class ChatStateService : IChatStateService
{
    private readonly IJSRuntime _jsRuntime;
    private List<Conversation> _chats = new();
    private Conversation _currentChat = new();
    private Model _selectedModel = new();

    public List<Conversation> Chats => _chats;
    public Conversation CurrentChat => _currentChat;
    public Model SelectedModel => _selectedModel;

    public ChatStateService(IJSRuntime jsRuntime) => _jsRuntime = jsRuntime;

    public async Task LoadConversationsAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "chatHistory");
            if (!string.IsNullOrEmpty(json))
            {
                _chats = JsonSerializer.Deserialize<List<Conversation>>(json) ?? new();
                _currentChat = _chats.FirstOrDefault() ?? new();
            }
        }
        catch
        {
            _chats = new();
            await NewChatAsync();
        }
    }

    public async Task SaveConversationsAsync()
    {
        var json = JsonSerializer.Serialize(_chats, new JsonSerializerOptions { WriteIndented = true });
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "chatHistory", json);
    }

    public async Task NewChatAsync()
    {
        var chat = new Conversation { Title = $"Chat {_chats.Count + 1}" };
        _chats.Add(chat);
        _currentChat = chat;
        await SaveConversationsAsync();
    }

    public async Task SelectChatAsync(Conversation chat)
    {
        _currentChat = chat;
        await SaveConversationsAsync();
    }

    public async Task SelectModelAsync(Model model)
    {
        _selectedModel = model;
        await SaveConversationsAsync();
    }
}