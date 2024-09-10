using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Prompty;

namespace Patterns.Agents;

public static class PromptyHelper
{
    public static KernelFunction FromFile(Kernel kernel, string path,
        IPromptTemplateFactory? promptTemplateFactory = null)
    {
        var text = File.ReadAllText(path);
        return kernel.CreateFunctionFromPrompty(text, promptTemplateFactory);
    }
    public static KernelFunction FromFile(IKernelBuilder builder, string path,
        IPromptTemplateFactory? promptTemplateFactory = null)
    {
        var kernel = builder.Build();
        return FromFile(kernel, path, promptTemplateFactory);
    }
    
    public static AgentBuilder FromPrompty(this AgentBuilder agentBuilder, string path)
    {
        var prompty = KernelFunctionPrompty.ToPromptTemplateConfig(path)!;
        return agentBuilder
            .WithName(prompty.Name ?? "BaseAgent")
            .WithDescription(prompty.Description ?? "You are an agent that can answer questions.")
            .WithInstructions(prompty.Template);
    }
}
