using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.History;
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


public class GardenAgent
    : BaseAgent
{
    public GardenAgent(IKernelBuilder builder,
        KernelArguments arguments)
        : base(builder, arguments, autoRegisterPlugins: false)
    {
        Name = "Garden Agent";
        Description = @"You are an agent that can answer garden-related questions.
            You should respond with the best time to plant a given plant.
            If information is not available to answer the question, you should respond with an error message
            and ask the user to provide the missing information.";
        // Instructions = @"
        //     Description: {Description}                

        //     Scenario: Garden Agent answers a garden-related question
        //         Given a task related to gardening
        //         When the task can be completed
        //         Then the agent should respond with a completion message in the following JSON schema:
        //         {
        //             ""isComplete"": true,
        //             ""result"": {
        //                 ""bestTimeToPlant"": ""Spring""
        //             }
        //         }

        //     Scenario: Garden Agent cannot answer a garden-related question because the plant is unknown
        //         Given a task related to gardening
        //             And the plant is unknown
        //         When the task cannot be completed
        //         Then the agent should respond with a completion message in the following JSON schema:
        //         {
        //             ""isComplete"": false,
        //             ""result"": {
        //                 ""errors"": [
        //                     ""Plant is unknown""
        //                 ]
        //             }
        //         }
        //         ";
        Description = "You are an agent that can answer garden-related questions.";
        Instructions = @"
            You are an agent that can answer garden-related questions.
            You should respond with the best time to plant a given plant.
            If information is not available to answer the question, you should respond with an error message
            and ask the user to provide the missing information.";
    }
}

public class WeatherAgent
    : BaseAgent
{
    public WeatherAgent(IKernelBuilder builder,
        KernelArguments arguments)
        : base(builder, arguments)
    {
        Name = "Weather Agent";
        Description = @"You are an agent that can answer weather-related questions.
            You should respond with the current temperature and conditions for the given location.
            If information is not available to answer the question, you should respond with an error message
            and ask the user to provide the missing information.";
        Instructions = @"
            Description: {Description}

            Scenario: Weather Agent answers a weather-related question
                Given a task related to weather
                When the task can be completed
                Then the agent should respond with a completion message in the following JSON schema:
                {
                    ""isComplete"": true,
                    ""result"": {
                        ""temperature"": 72,
                        ""conditions"": ""sunny""
                    }
                }

            Scenario: Weather Agent cannot answer a weather-related question because the location is unknown
                Given a task related to weather
                    And the location is unknown
                When the task cannot be completed
                Then the agent should respond with a completion message in the following JSON schema:
                {
                    ""isComplete"": false,
                    ""result"": {
                        ""errors"": [
                            ""Location is unknown""
                        ]
                    }
                }
                ";
    }

    [KernelFunction("GetWeather")]
    [Description("Get the current temperature and conditions for the given location.")]
    public string GetWeather(
        [Description("The location to get the weather for.")] string location)
    {
        Console.WriteLine(@"processed ""GetWeather""");
        return @$"
            {{
                ""isComplete"": true,
                ""result"": {{
                    ""temperature"": 72,
                    ""conditions"": ""sunny""
                }}
            }}
        ";
    }
}

public class OrchestrationAgent
    : BaseAgent
{
    public List<Agent> Agents { get; set; }

    public static string GetHistoryString(IEnumerable<ChatMessageContent> history)
        => string.Join(Environment.NewLine, history.Select(x => x.ToString()));
    public static string GetAgentString(Agent agent) => $"- {agent.Name}: {agent.Description}";
    public static string GetAgentsString(IEnumerable<Agent> agents)
        => string.Join(Environment.NewLine, agents.Select(GetAgentString));

    public OrchestrationAgent(IKernelBuilder builder,
        KernelArguments arguments,
        params Agent[] agents)
        : base(builder, arguments)
    {
        Agents = [.. agents];
        Name = "Orchestration Agent";
        Description = "You are an agent that can orchestrate other agents to complete tasks.";
        // Description = @"You are an agent that can orchestrate other agents to complete tasks.
        //     You should infer the task from the most recent chat messages and delegate the task to the appropriate agent(s).
        //     If no agent can complete the task, you should respond with an error message.
        //     You should continue to execute the chat until the task is complete.
        //     If no history is provided, you should prompt the user to ask questions related to the agents you have available.";
        // Instructions = @$"
        //     Description: {Description}
                
        //     Scenario: Orchestration Agent delegates a task to another agent
        //         Given a chat history
        //             And a list of agents
        //         When a task can be inferred from the most recent chat messages
        //         Then a new agent chat should be created (using {{CreateGroupChat(selectedAgents, task, history)}})
        //             And the agents that can fulfill the task should be included
        //             And the chat should be execute until completion.

        //     Scenario: Orchestration Agent cannot delegate a task to another agent
        //         Given a chat history
        //             And a list of agents
        //         When a task cannot be inferred from the most recent chat messages
        //         Then the agent should respond with an error message

        //     Agents:
        //         {GetAgentsString(agents)}

        //     History:
        //     ";
        Instructions = @$"
            You are an agent that can orchestrate other agents to complete tasks.

            You should infer the task from the most recent chat messages and delegate the task to the appropriate agent(s).
            If no agent can complete the task, you should respond with an error message.

            Output the response from the group chat.
            Don't output anything other than the group chat response.

            Agents:
            {GetAgentsString(Agents)}

            History:
        ";
    }

    [KernelFunction("CreateTasks")]
    [Description("Create a list of tasks from the most recent chat messages.")]
    public async Task<string[]> CreateTasks(
        [Description("The most recent chat messages.")] IEnumerable<ChatMessageContent> history)
    {
        Console.WriteLine(@"processed ""CreateTasks""");
        var func = Kernel.CreateFunctionFromPrompt(@$"
            Create a list of tasks from the most recent chat messages.

            Only output the tasks.
            Don't output anything other than the tasks.

            Output the tasks on separate lines.
            For example:
                Input: Is today's weather good to plant tomatoes?
                Output:
                    Task 1: Get weather
                    Task 2: Answer garden question for current weather.

            History:
            {GetHistoryString(history)}
        ");
        var result = await func.InvokeAsync(Kernel, Arguments);
        return result.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    }

    [KernelFunction("SelectAgents")]
    [Description("Select the agents that can complete the task.")]
    public async Task<string[]> SelectAgents(
        [Description("The task to complete.")] string task)
    {
        Console.WriteLine(@"processed ""SelectAgents""");
        var func = Kernel.CreateFunctionFromPrompt(@$"
            Select the agents that can complete the following task: {task}

            Select one or more agents from the following list:
            {GetAgentsString(Agents)}

            If more information is required, select the user agent.
            If no agent can complete the task, return an error.

            Only output the names of the selected agents.
            Don't output anything other than the agent names.

            Output the names of the agents on separate lines.
            For example:
            Agent 1
            Agent 2
        ");
        var result = await func.InvokeAsync(Kernel, Arguments);
        return result.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    }

    [KernelFunction("CreateGroupChatResponse")]
    [Description("Create a group chat with the selected agents and output the response.")]
    public async Task<string> CreateGroupChatResponse(
        [Description("The agents to include in the group chat.")] string[] selectedAgents,
        [Description("The task for the agents to complete")] string task,
        Kernel kernel,
        CancellationToken cancellationToken = default)
    {
        Console.WriteLine(@"processed ""CreateGroupChatResponse""");
        var history = kernel.GetRequiredService<ChatHistory>() ?? [];
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
        var chat = new AgentGroupChat(agents)
        {
            ExecutionSettings = new Microsoft.SemanticKernel.Agents.Chat.AgentGroupChatSettings
            {
                SelectionStrategy = new Microsoft.SemanticKernel.Agents.Chat.KernelFunctionSelectionStrategy(
                    KernelFunctionFactory.CreateFromPrompt(@$"
                        Select the agent that can complete the following task: {task}

                        Select a single agent from the following list:
                        {GetAgentsString(agents)}

                        If more information is required and the user agent is available, select the user agent.
                        If more information is required and the user agent is not available, return an error.
                        
                        Only output the agent name that can complete the task.
                        Don't output anything other than the agent name.
                        "),
                    kernel
                ),
                TerminationStrategy = new Microsoft.SemanticKernel.Agents.Chat.KernelFunctionTerminationStrategy(
                    KernelFunctionFactory.CreateFromPrompt(@$"
                        Ensure the following task is complete in the most recent history: {task}
                        
                        If more information is required, the task is not complete.

                        If the task is complete, output ""true""; otherwise, output ""false"".

                        History: {GetHistoryString(localHistory)}
                        "),
                    kernel
                )
            }
        };
        chat.AddChatMessages(localHistory.ToArray());
        List<string> responses = [];
        Console.WriteLine("Starting Group Chat");
        chat.AddChatMessage(new ChatMessageContent(AuthorRole.System, "Ask some questions about gardening or weather to get started!"));
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

public class BaseAgent
    : ChatHistoryKernelAgent
{
    private readonly IKernelBuilder builder;

    public BaseAgent(IKernelBuilder builder,
        KernelArguments arguments,
        string? description = null,
        bool autoRegisterPlugins = true)
    {
        this.builder = builder;
        Kernel = builder.Build();
        var type = GetType();

        if(autoRegisterPlugins)
            Kernel.Plugins.AddFromObject(this);

        Name = type.Name;
        Description = description ?? type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty;
        HistoryReducer = new ChatHistorySummarizationReducer(
            Kernel.GetRequiredService<IChatCompletionService>(), 5, 5);
        Arguments = arguments;
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
