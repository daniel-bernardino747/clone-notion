using Microsoft.JSInterop;

namespace CloneNotion.Services;

/// <summary>
/// Serviço para interação com notificações do navegador.
/// Centraliza os nomes das funções JavaScript para evitar erros de digitação.
/// </summary>
public class NotificationService
{
    private readonly IJSRuntime _jsRuntime;

    public NotificationService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Verifica se a permissão para notificações foi concedida.
    /// </summary>
    /// <returns>True se a permissão foi concedida, caso contrário False.</returns>
    public async Task<bool> CheckPermissionAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<bool>("checkPermission");
        }
        catch (JSException)
        {
            // Log do erro se necessário
            return false;
        }
    }

    /// <summary>
    /// Solicita permissão para exibir notificações.
    /// </summary>
    /// <returns>
    /// "granted" se a permissão foi concedida,
    /// "denied" se foi negada,
    /// "default" se o usuário não respondeu,
    /// "unsupported" se o navegador não suporta notificações,
    /// "error" se ocorreu um erro.
    /// </returns>
    public async Task<string> RequestPermissionAsync()
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string>("requestNotificationPermission");
        }
        catch (JSException)
        {
            return "error";
        }
    }

    /// <summary>
    /// Exibe uma notificação no navegador.
    /// </summary>
    /// <param name="title">Título da notificação.</param>
    /// <param name="body">Corpo da notificação.</param>
    public async Task ShowNotificationAsync(string title, string body)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("showNotification", title, body);
        }
        catch (JSException)
        {
            // Log do erro se necessário
            throw;
        }
    }
}