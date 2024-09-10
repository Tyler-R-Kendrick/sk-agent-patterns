using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Patterns.Agents;

public class UserAgent : ChatHistoryKernelAgent
{
    private readonly TextReader reader;
    public UserAgent(TextReader reader)
    {
        this.reader = reader;
        Name = "User Agent";
        Description = "You are an agent that represents a user. You can ask a question and provide information.";
    }

    public async override IAsyncEnumerable<ChatMessageContent> InvokeAsync(
        ChatHistory history,
        KernelArguments? arguments = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new ChatMessageContent(AuthorRole.User, Environment.NewLine);
        var result = await reader.ReadLineAsync(cancellationToken);
        yield return new ChatMessageContent(AuthorRole.User, result);
    }

    public async override IAsyncEnumerable<StreamingChatMessageContent> InvokeStreamingAsync(
        ChatHistory history,
        KernelArguments? arguments = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new StreamingChatMessageContent(AuthorRole.User, Environment.NewLine);
        var result = await reader.ReadLineAsync(cancellationToken) ?? string.Empty;
        foreach(var @char in result)
            yield return new StreamingChatMessageContent(AuthorRole.User, @char.ToString());
    }
}
