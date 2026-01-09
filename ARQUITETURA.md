# Arquitetura do Sistema de Notificações

## 📋 Visão Geral

Este documento descreve a arquitetura seguida para implementação de notificações do navegador no projeto CloneNotion. A solução utiliza o padrão **JSInterop** do Blazor, seguindo as melhores práticas recomendadas pela comunidade Microsoft.

## 🏗️ Arquitetura em Camadas

A arquitetura implementada segue uma separação clara de responsabilidades em três camadas principais:

```
┌─────────────────────────────────────────────────────────────┐
│                    Camada de Apresentação                    │
│              (NotificationButton.razor)                      │
│  - Componente Blazor responsável pela UI                    │
│  - Gerencia estado local (PermissionGranted, Loading)       │
│  - Usa o serviço C# via injeção de dependência              │
└──────────────────────┬──────────────────────────────────────┘
                       │ Usa
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                   Camada de Abstração C#                     │
│              (NotificationService.cs)                        │
│  - Encapsula chamadas JavaScript                             │
│  - Centraliza nomes de funções JS (evita erros de digitação)│
│  - Fornece API tipada e assíncrona em C#                    │
│  - Trata erros e exceções JS                                │
└──────────────────────┬──────────────────────────────────────┘
                       │ Chama via IJSRuntime
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                   Camada de Integração JS                    │
│          (notificationInterop.js)                           │
│  - Funções JavaScript globais no window                     │
│  - Interage diretamente com APIs do navegador               │
│  - Implementa lógica específica do navegador                │
└──────────────────────┬──────────────────────────────────────┘
                       │ Usa
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                   API do Navegador                           │
│          (Notification API)                                 │
│  - Notification.permission                                  │
│  - Notification.requestPermission()                         │
│  - new Notification()                                       │
└─────────────────────────────────────────────────────────────┘
```

## 📁 Estrutura de Arquivos

```
CloneNotion/
├── Services/
│   └── NotificationService.cs          # Serviço C# (camada de abstração)
├── Components/
│   └── NotificationButton.razor        # Componente Blazor (UI)
├── wwwroot/js/
│   └── notificationInterop.js          # Funções JavaScript (integração)
└── Program.cs                          # Registro do serviço na DI
```

## 🔄 Fluxo de Dados

### Exemplo: Verificar Permissão

```
1. Usuário carrega a página
   ↓
2. NotificationButton.razor → OnAfterRenderAsync() detecta firstRender
   ↓
3. NotificationButton.razor → LoadPermission()
   ↓
4. NotificationService → CheckPermissionAsync()
   ↓
5. IJSRuntime.InvokeAsync<bool>("checkPermission")
   ↓
6. notificationInterop.js → window.checkPermission()
   ↓
7. Navegador → Notification.permission
   ↓
8. Retorno via cadeia inversa
   ↓
9. UI atualiza → PermissionGranted = true/false
```

### Exemplo: Exibir Notificação

```
1. Usuário clica no botão
   ↓
2. NotificationButton.razor → ShowNotification()
   ↓
3. NotificationService → ShowNotificationAsync("Clone Notion", "Mensagem")
   ↓
4. IJSRuntime.InvokeVoidAsync("showNotification", title, body)
   ↓
5. notificationInterop.js → window.showNotification(title, body)
   ↓
6. Navegador → new Notification(title, { body: body })
   ↓
7. Notificação exibida ao usuário
```

## 💻 Detalhamento das Camadas

### 1. Camada JavaScript (notificationInterop.js)

**Responsabilidades:**
- Definir funções globais acessíveis via `window`
- Interagir diretamente com APIs do navegador
- Implementar lógica específica do JavaScript (verificações, validações)
- Tratar erros no contexto JavaScript

**Funções Implementadas:**

```javascript
// Verifica se permissão foi concedida
window.checkPermission = async () => { ... }

// Solicita permissão ao usuário
window.requestNotificationPermission = async () => { ... }

// Exibe uma notificação
window.showNotification = (title, body) => { ... }
```

**Características:**
- Funções são globais no `window` (padrão mais comum na comunidade Blazor)
- Todas as funções incluem verificações de suporte do navegador
- Tratamento de erros implementado em cada função
- Lógica de negócios específica do JavaScript (timeout, eventos)

### 2. Camada C# Service (NotificationService.cs)

**Responsabilidades:**
- Encapsular chamadas JavaScript via `IJSRuntime`
- Fornecer API tipada e assíncrona em C#
- Centralizar nomes de funções JavaScript (evita erros de digitação)
- Tratar exceções JavaScript (`JSException`)
- Abstrair complexidade do JSInterop

**Métodos Públicos:**

```csharp
// Verifica se a permissão foi concedida
Task<bool> CheckPermissionAsync()

// Solicita permissão ao usuário
Task<string> RequestPermissionAsync()

// Exibe uma notificação
Task ShowNotificationAsync(string title, string body)
```

**Características:**
- Injeta `IJSRuntime` via construtor (padrão DI)
- Todos os métodos são assíncronos (`async/await`)
- Tratamento de exceções com `JSException`
- Retornos tipados em C# (bool, string, void)

**Exemplo de Implementação:**

```csharp
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
```

### 3. Camada de Apresentação (NotificationButton.razor)

**Responsabilidades:**
- Renderizar a UI do botão de notificação
- Gerenciar estado local (permissões, loading, erros)
- Responder a eventos do usuário (cliques)
- Usar o serviço C# via injeção de dependência

**Características:**
- Injeção de dependência via `[Inject]`
- Estado gerenciado com propriedades C#
- Métodos de ciclo de vida (`OnAfterRenderAsync`)
- Tratamento de erros na UI

**Exemplo de Uso:**

```csharp
[Inject] public required NotificationService NotificationService { get; set; }

private async Task ShowNotification()
{
    if (!PermissionGranted) {
        var permission = await NotificationService.RequestPermissionAsync();
        PermissionGranted = permission == "granted";
    }
    
    if (PermissionGranted) {
        await NotificationService.ShowNotificationAsync(
            "Clone Notion",
            "Está funcionando!"
        );
    }
}
```

## ⚙️ Configuração e Registro

### Registro do Serviço (Program.cs)

O serviço é registrado no contêiner de Injeção de Dependência:

```csharp
builder.Services.AddScoped<CloneNotion.Services.NotificationService>();
```

**Escolha de Lifetime:**
- `Scoped`: Uma instância por requisição (recomendado para Blazor Server)
- Compatível com o ciclo de vida dos componentes Blazor
- Permite compartilhamento entre componentes na mesma requisição

### Carregamento do JavaScript (_Layout.cshtml)

O arquivo JavaScript é carregado no layout principal:

```html
<script src="~/js/notificationInterop.js"></script>
```

**Posicionamento:**
- Carregado antes do Blazor (`blazor.server.js`)
- Garante que as funções estejam disponíveis quando o Blazor inicializar

## 🎯 Padrão Utilizado

### JSInterop com Funções Globais + Serviço C# Wrapper

Este é o padrão **mais comum na comunidade Blazor** (~70-80% dos projetos) por ser:

1. **Simples**: Fácil de entender e implementar
2. **Compatível**: Funciona com Blazor Server e Blazor WebAssembly
3. **Depurável**: Fácil de debugar em DevTools
4. **Documentado**: Padrão oficial da Microsoft
5. **Estável**: Funciona em todos os navegadores

### Alternativas Consideradas (não utilizadas)

- **Módulos ES6 + IJSObjectReference**: Mais moderno, mas menos comum (~20-30%)
- **JavaScript inline**: Menos organizado e difícil de manter
- **Bibliotecas NuGet**: Dependência externa desnecessária neste caso

## ✅ Boas Práticas Seguidas

### 1. Separação de Responsabilidades
- ✅ JavaScript: Lógica específica do navegador
- ✅ C# Service: Abstração e organização
- ✅ Componente: UI e estado local

### 2. Injeção de Dependência
- ✅ Serviço registrado no contêiner DI
- ✅ Injeção via construtor no serviço
- ✅ Injeção via `[Inject]` no componente

### 3. Tratamento de Erros
- ✅ Try-catch em todas as camadas
- ✅ Tratamento específico de `JSException`
- ✅ Feedback ao usuário em caso de erro

### 4. Assíncrono
- ✅ Todos os métodos são `async/await`
- ✅ Operações não bloqueiam a UI
- ✅ Uso correto de `Task` e `Task<T>`

### 5. Nomenclatura
- ✅ Convenções C# (PascalCase para métodos públicos)
- ✅ Convenções JavaScript (camelCase para funções)
- ✅ Sufixo `Async` para métodos assíncronos

### 6. Tipagem Forte
- ✅ Retornos tipados (`bool`, `string`, `void`)
- ✅ Parâmetros tipados
- ✅ Sem uso de `dynamic` ou `object`

## 🔍 Como Adicionar Novas Funcionalidades

Para adicionar uma nova funcionalidade de notificação:

### 1. Adicionar função JavaScript (`notificationInterop.js`)

```javascript
window.novaFuncionalidade = async (param1, param2) => {
    try {
        // Lógica JavaScript
        return resultado;
    } catch (error) {
        console.error('Erro:', error);
        throw error;
    }
}
```

### 2. Adicionar método no serviço C# (`NotificationService.cs`)

```csharp
public async Task<TipoRetorno> NovaFuncionalidadeAsync(string param1, int param2)
{
    try
    {
        return await _jsRuntime.InvokeAsync<TipoRetorno>(
            "novaFuncionalidade", 
            param1, 
            param2
        );
    }
    catch (JSException)
    {
        // Tratamento de erro
        throw;
    }
}
```

### 3. Usar no componente (`NotificationButton.razor`)

```csharp
private async Task MinhaAcao()
{
    var resultado = await NotificationService.NovaFuncionalidadeAsync("valor1", 123);
    // Usar resultado
}
```

## 📚 Referências

- [Microsoft Docs - JSInterop](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/)
- [Blazor JavaScript Interop Best Practices](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/call-javascript-from-dotnet)
- [Browser Notifications API](https://developer.mozilla.org/en-US/docs/Web/API/Notifications_API)

## 🤝 Manutenção

### Quando Modificar

- **notificationInterop.js**: Ao alterar lógica do navegador ou adicionar novas funcionalidades JS
- **NotificationService.cs**: Ao adicionar novos métodos públicos ou alterar assinaturas
- **NotificationButton.razor**: Ao alterar UI ou lógica de apresentação

### Testes Recomendados

- ✅ Testar em diferentes navegadores (Chrome, Firefox, Edge, Safari)
- ✅ Testar permissões (granted, denied, default)
- ✅ Testar em dispositivos móveis
- ✅ Testar tratamento de erros

---

**Última atualização:** Janeiro 2026
**Versão da arquitetura:** 1.0
