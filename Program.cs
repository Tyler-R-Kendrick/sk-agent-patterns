using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Patterns.Agents;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();
const string keyName = "OPENAI_API_KEY";
var secret = config[keyName]!;
var kernelBuilder = Kernel.CreateBuilder()
    .AddOpenAIChatCompletion(
        "gpt-4o",
        secret
    );

OpenAIPromptExecutionSettings executionSettings = new() { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions };
KernelArguments arguments = new(executionSettings);
GardenAgent gardenAgent = new(kernelBuilder, arguments);
WeatherAgent weatherAgent = new(kernelBuilder, arguments);

// ChatMessageContent UserMessage(string message) => new(AuthorRole.User, message);
// ChatMessageContent[] UserMessages(params string[] messages) => messages.Select(UserMessage).ToArray();
// StringReader UserReader(params string[] messages)
//     => new(string.Join(Environment.NewLine,
//         UserMessages(messages).Select(m => m.ToString())));
// var textReader = UserReader(
//     "Is today's weather good to plant tomatoes?",
//     "exit");
UserAgent userAgent = new(Console.In);
//UserAgent userAgent = new(textReader);
OrchestrationAgent orchestrationAgent = new(kernelBuilder, arguments, [
    gardenAgent,
    weatherAgent,
    userAgent
]);

kernelBuilder.Services.AddSingleton(new ChatHistory());

kernelBuilder.Plugins.AddFromObject(orchestrationAgent);
var kernel = kernelBuilder.Build();

// var chat = new AgentGroupChat(userAgent, orchestrationAgent)
// {
//     ExecutionSettings = new AgentGroupChatSettings()
//     {
//         SelectionStrategy = new KernelFunctionSelectionStrategy(
//             KernelFunctionFactory.CreateFromPrompt(@$"
//             Determine which participant takes the next turn in a conversation based on the the most recent participant.
//                 State only the name of the participant to take the next turn.
//                 No participant should take more than one turn in a row.
                
//                 Choose only from these participants:
//                 - {{{userAgent.Name}}}
//                 - {{{orchestrationAgent.Name}}}
                
//                 Always follow these rules when selecting the next participant:
//                 - After {{{userAgent.Name}}}, it is {{{orchestrationAgent.Name}}}'s turn.
//                 - After {{{orchestrationAgent.Name}}}, it is {{{userAgent.Name}}}'s turn.

//                 History:
//                 {{$history}}
//                 "),
//             kernel),
//         TerminationStrategy = new KernelFunctionTerminationStrategy(
//             KernelFunctionFactory.CreateFromPrompt(@"
//                 When the user wants to end the chat, they will type the message ""exit"" in the history.
                
//                 If the chat is supposed to end because of the user, respond with ""true"".
//                 Otherwise, respond with ""false"".

//                 Don't return ""true"" until the user has typed ""exit"" in the history.
                
//                 History:
//                 {{$_history_}}
//                 "),
//             kernel
//         ),
//     }
// };

var chat = new AgentGroupChat(orchestrationAgent);
chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, "Is today's weather good to plant tomatoes?"));
await foreach(var message in chat.InvokeAsync())
{
    Console.WriteLine(message.Content);
}