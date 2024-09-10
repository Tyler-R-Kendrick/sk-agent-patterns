using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.FileProviders;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Prompty;

namespace Patterns.Agents;

public partial class BaseAgent
    : ChatHistoryKernelAgent
{
    private const string
        DefaultDescription = "You are an agent that can answer questions.",
        DefaultInstructions = "You are an agent that can answer questions.";
    public BaseAgent(
        IKernelBuilder builder,
        KernelArguments arguments,
        bool autoRegisterPlugins = true)
    {
        this.builder = builder;
        Kernel = builder.Build();
        Arguments = arguments;
        if(autoRegisterPlugins)
            Kernel.Plugins.AddFromObject(this);

        var type = GetType();
        Name ??= type.Name;
        Description ??= type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? DefaultDescription;
        Instructions ??= DefaultInstructions;
    }

    public BaseAgent(IKernelBuilder builder,
        KernelArguments arguments,
        IFileInfo promptyFile,
        bool autoRegisterPlugins = true)
        : this(builder, arguments, autoRegisterPlugins)
    {
        var promptyFileContents = GetPromptyContents(promptyFile);
        var promptyConfig = KernelFunctionPrompty.ToPromptTemplateConfig(promptyFileContents);
        Name = promptyConfig.Name;
        Description = promptyConfig.Description;
        Instructions = promptyConfig.Template;
    }
}

public partial class BaseAgent
{
    private readonly IKernelBuilder builder;

    private static string GetPromptyContents(IFileInfo promptyFile)
    {
        var exists = promptyFile.Exists;
        Console.WriteLine($"Prompty file exists: {exists}; {promptyFile.PhysicalPath}");        
        using StreamReader promptyReader = new(promptyFile.CreateReadStream());
        return promptyReader.ReadToEnd();
    }

    private string GetHistoryString(ChatHistory history) =>
        new ChatMessageContent(AuthorRole.System, Instructions).ToString() + Environment.NewLine +
        string.Join(Environment.NewLine, history.Select(x => x.ToString()));

    public async override IAsyncEnumerable<ChatMessageContent> InvokeAsync(
        ChatHistory history,
        KernelArguments? arguments = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        kernel ??= builder.Build();
        var historyString = GetHistoryString(history);
        var result = await kernel.InvokePromptAsync(
            historyString, arguments,
            cancellationToken: cancellationToken);
        yield return new ChatMessageContent(AuthorRole.Assistant, result.ToString());
    }

    public async override IAsyncEnumerable<StreamingChatMessageContent> InvokeStreamingAsync(
        ChatHistory history,
        KernelArguments? arguments = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        kernel ??= builder.Build();
        var historyString = GetHistoryString(history);
        await foreach(var result in kernel.InvokePromptStreamingAsync(
            historyString, arguments,
            cancellationToken: cancellationToken))
            yield return new StreamingChatMessageContent(AuthorRole.Assistant, result.ToString());
    }
}
