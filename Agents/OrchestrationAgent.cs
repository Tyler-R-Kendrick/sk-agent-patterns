using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.Extensions.FileProviders;
using Microsoft.SemanticKernel.Prompty;
using Microsoft.SemanticKernel.Agents.Chat;

namespace Patterns.Agents;

public class OrchestrationAgent
    : BaseAgent
{
    private readonly Lazy<PromptTemplateConfig> _lazyAgentPrompty;
    private readonly IFileProvider _fileProvider;

    public List<Agent> Agents { get; set; } = [];

    public static string GetHistoryString(IEnumerable<ChatMessageContent> history)
        => string.Join(Environment.NewLine, history.Select(x => x.ToString()));
    public static string GetAgentString(Agent agent) => $"- {agent.Name}: {agent.Description}";
    public static string GetAgentsString(IEnumerable<Agent> agents)
        => string.Join(Environment.NewLine, agents.Select(GetAgentString));

    private PromptTemplateConfig AgentPrompty => _lazyAgentPrompty.Value;
    public new string Name => AgentPrompty.Name ?? "Orchestration Agent";
    public new string Description => AgentPrompty.Description ?? "Orchestrates the agents to complete tasks.";
    public new string Instructions => AgentPrompty.Template;

    public OrchestrationAgent(
        IKernelBuilder builder,
        KernelArguments arguments,
        IFileProvider fileProvider,
        params Agent[] agents)
        : base(builder, arguments)
    {
        Agents = [.. agents];
        _fileProvider = fileProvider;
        _lazyAgentPrompty = new(() => {
            var promptyContents = GetPromptyContents("prompts/Orchestrator.prompty");
            return KernelFunctionPrompty.ToPromptTemplateConfig(promptyContents);
        });
    }

    protected override KernelArguments InternalArguments(KernelArguments? arguments)
    {
        arguments ??= [];
        KernelArguments args = [];
        args["agents"] = Agents;
        args = new(args.Concat(arguments).ToDictionary());
        return base.InternalArguments(args);
    }

    private string GetPromptyContents(string filePath)
    {
        var promptyFile = _fileProvider.GetFileInfo(filePath);
        using var promptyReader = new StreamReader(promptyFile.CreateReadStream());
        return promptyReader.ReadToEnd();
    }

    [KernelFunction("CreateTasks")]
    [Description("Create a list of tasks from the most recent chat messages.")]
    public async Task<string[]> CreateTasks(
        [Description("The most recent chat messages.")] IEnumerable<ChatMessageContent> history)
    {
        Console.WriteLine(@"processed ""CreateTasks""");
        var tasksPrompty = GetPromptyContents("prompts/Orchestrator.tasks.prompty");
        var func = Kernel.CreateFunctionFromPrompty(tasksPrompty);
        var result = await func.InvokeAsync(Kernel, InternalArguments(
            new(new Dictionary<string, object?> { ["history"] = history })));
        return result.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    }

    [KernelFunction("CreateGroupChatResponse")]
    [Description("Create a group chat with the selected agents and output the response.")]
    public async Task<string> CreateGroupChatResponse(
        [Description("The agents to include in the group chat.")] string[] selectedAgents,
        [Description("The task for the agents to complete")] string task,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine(@"processed ""CreateGroupChatResponse""");
        var history = Kernel.GetRequiredService<ChatHistory>() ?? [];
        var agents = Agents.Where(x => selectedAgents.Contains(x.Name)).ToArray();
        Console.WriteLine($"Selected agents: {GetAgentsString(agents)}");
        if(Agents.Any(x => x.Name == "User Agent") && !agents.Any(x => x.Name == "User Agent"))
            agents = [.. agents, Agents.First(x => x.Name == "User Agent")];
        IEnumerable<ChatMessageContent> localHistory = history;
        if(HistoryReducer != null)
        {
            var summarizedHistory = await HistoryReducer.ReduceAsync(history, cancellationToken);
            localHistory = summarizedHistory ?? history;
        }
        
        var selectionPrompty = GetPromptyContents("prompts/Orchestrator.selection.prompty");
        var terminationPrompty = GetPromptyContents("prompts/Orchestrator.termination.prompty");
        var chat = new AgentGroupChat(agents)
        {
            ExecutionSettings = new AgentGroupChatSettings
            {
                SelectionStrategy = new KernelFunctionSelectionStrategy(
                    Kernel.CreateFunctionFromPrompty(selectionPrompty),
                    Kernel
                ),
                TerminationStrategy = new KernelFunctionTerminationStrategy(
                    Kernel.CreateFunctionFromPrompty(terminationPrompty),
                    Kernel
                )
            }
        };
        chat.AddChatMessages(localHistory.ToArray());
        List<string> responses = [];
        Console.WriteLine("Starting Group Chat");
        await foreach (var message in chat.InvokeAsync(cancellationToken).Where(x => x.Role != AuthorRole.User))
        {
            Console.WriteLine("Message Generated: {0}", message);
            responses.Add(message.ToString());
        }
        Console.WriteLine("Group Chat Complete");
        Console.WriteLine("Responses: {0}", string.Join(Environment.NewLine, responses));
        return string.Join(Environment.NewLine, responses);
    }
}
