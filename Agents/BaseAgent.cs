using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.FileProviders;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Microsoft.SemanticKernel.PromptTemplates.Liquid;
using Microsoft.SemanticKernel.Prompty;
using YamlDotNet.Core.Tokens;

namespace Patterns.Agents;

public partial class BaseAgent
    : ChatHistoryKernelAgent
{
    private const string
        DefaultDescription = "You are an agent that can answer questions.",
        DefaultInstructions = "You are an agent that can answer questions.";
        
    internal static readonly AggregatorPromptTemplateFactory s_defaultTemplateFactory =
        new(new LiquidPromptTemplateFactory(), new HandlebarsPromptTemplateFactory());

    private readonly IPromptTemplateFactory _templateFactory;
    private readonly PromptTemplateConfig _templateConfig;

    public BaseAgent(
        IKernelBuilder builder,
        KernelArguments arguments,
        IPromptTemplateFactory? templateFactory = null,
        bool autoRegisterPlugins = true)
    {
        this.builder = builder;
        Kernel = builder.Build();
        Arguments = arguments;
        if(autoRegisterPlugins)
            Kernel.Plugins.AddFromObject(this);
        _templateFactory = templateFactory ?? s_defaultTemplateFactory;
        var type = GetType();
        Name ??= type.Name;
        Description ??= type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? DefaultDescription;
        Instructions ??= DefaultInstructions;
        _templateConfig = KernelFunctionPrompty.ToPromptTemplateConfig(Instructions);
    }

    public BaseAgent(IKernelBuilder builder,
        KernelArguments arguments,
        IFileInfo promptyFile,
        IPromptTemplateFactory? templateFactory = null,
        bool autoRegisterPlugins = true)
        : this(builder, arguments, templateFactory, autoRegisterPlugins)
    {
        var promptyFileContents = GetPromptyContents(promptyFile);
        var promptyConfig = KernelFunctionPrompty.ToPromptTemplateConfig(promptyFileContents);
        _templateConfig = promptyConfig;
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

    private async Task<string> GetSystemPromptAsync(KernelArguments? arguments = null)
    {
        var args = InternalArguments(arguments);
        return await _templateFactory.Create(_templateConfig).RenderAsync(Kernel, args);
    }

    protected virtual KernelArguments InternalArguments(KernelArguments? arguments)
    {
        var internalArguments = Arguments ?? [];
        arguments ??= [];
        return new(internalArguments.Concat(arguments).ToDictionary());
    }

    public async override IAsyncEnumerable<ChatMessageContent> InvokeAsync(
        ChatHistory history,
        KernelArguments? arguments = null,
        Kernel? kernel = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        kernel ??= builder.Build();
        var historyString = await GetSystemPromptAsync(arguments);
        var result = await kernel.InvokePromptAsync(
            historyString, InternalArguments(arguments),
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
        var systemPrompt = await GetSystemPromptAsync(arguments);
        await foreach(var result in kernel.InvokePromptStreamingAsync(
            systemPrompt, InternalArguments(arguments),
            cancellationToken: cancellationToken))
            yield return new StreamingChatMessageContent(AuthorRole.Assistant, result.ToString());
    }
}
