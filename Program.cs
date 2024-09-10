using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
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
var fileProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory());
GardenAgent gardenAgent = new(kernelBuilder, arguments, fileProvider);
WeatherAgent weatherAgent = new(kernelBuilder, arguments);

UserAgent userAgent = new(Console.In);
OrchestrationAgent orchestrationAgent = new(
    kernelBuilder,
    arguments,
    fileProvider, [
        gardenAgent,
        weatherAgent,
        userAgent
    ]);

kernelBuilder.Services.AddSingleton(new ChatHistory());
var kernel = kernelBuilder.Build();
var chat = new AgentGroupChat(orchestrationAgent, userAgent);
chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, "Is today's weather good to plant tomatoes?"));
await foreach(var message in chat.InvokeAsync())
{
    Console.WriteLine(message.Content);
}