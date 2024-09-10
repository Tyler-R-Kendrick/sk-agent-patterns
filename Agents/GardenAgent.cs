using Microsoft.Extensions.FileProviders;
using Microsoft.SemanticKernel;

namespace Patterns.Agents;

public class GardenAgent
    : BaseAgent
{
    public GardenAgent(IKernelBuilder builder,
        KernelArguments arguments,
        IFileProvider fileProvider)
        : base(builder, arguments, fileProvider.GetFileInfo("prompts/Gardener.prompty"), autoRegisterPlugins: false)
    {
        Name = "Garden Agent";
        Description = "You are an agent that can answer garden-related questions.";
        Instructions = @"
            You are an agent that can answer garden-related questions.
            You should respond with the best time to plant a given plant.
            If information is not available to answer the question, you should respond with an error message
            and ask the user to provide the missing information.";
    }
}
