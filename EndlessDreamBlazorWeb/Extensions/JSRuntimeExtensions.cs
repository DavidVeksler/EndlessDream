using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

public static class JSRuntimeExtensions
{
    public static ValueTask ScrollToBottomAsync(this IJSRuntime jsRuntime, ElementReference element) =>
        jsRuntime.InvokeVoidAsync("scrollToBottom", element);

    public static ValueTask OpenModalAsync(this IJSRuntime jsRuntime, string modalId) =>
        jsRuntime.InvokeVoidAsync("openModal", modalId);

    public static ValueTask CloseModalAsync(this IJSRuntime jsRuntime, string modalId) =>
        jsRuntime.InvokeVoidAsync("closeModal", modalId);
}