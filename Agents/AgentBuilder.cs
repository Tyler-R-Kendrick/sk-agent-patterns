using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.History;

namespace Patterns.Agents;

public class AgentBuilder(ILoggerFactory loggerFactory)
{
    private string _instructions = "You are an agent that can answer questions.";
    private string _description = "You are an agent that can answer questions.";
    private string _name = "BaseAgent";
    private IKernelBuilder _builder = Kernel.CreateBuilder();
    private KernelArguments _arguments = [];
    private IChatHistoryReducer? _historyReducer = null;
    private string _id = Guid.NewGuid().ToString();

    public AgentBuilder WithId(string id)
    {
        _id = id;
        return Clone();
    }

    public AgentBuilder WithHistoryReducer(IChatHistoryReducer reducer)
    {
        _historyReducer = reducer;
        return Clone();
    }

    public AgentBuilder WithKernel(IKernelBuilder builder)
    {
        _builder = builder;
        return Clone();
    }

    public AgentBuilder WithArguments(KernelArguments arguments)
    {
        _arguments = arguments;
        return Clone();
    }

    public AgentBuilder AddArguments(KernelArguments arguments)
    {
        foreach(var argument in arguments)
        {
            _arguments.Add(argument.Key, argument.Value);
        }
        return Clone();
    }

    public AgentBuilder WithInstructions(string instructions)
    {
        _instructions = instructions;
        return Clone();
    }

    public AgentBuilder WithDescription(string description)
    {
        _description = description;
        return Clone();
    }

    public AgentBuilder WithName(string name)
    {
        _name = name;
        return Clone();
    }

    private AgentBuilder Clone() => new(loggerFactory)
    {
        _builder = _builder,
        _arguments = _arguments,
        _description = _description,
        _name = _name,
        _instructions = _instructions,
        _historyReducer = _historyReducer,
        _id = _id
    };

    public BaseAgent Build()
        => new(_builder, _arguments, false)
        {
            Name = _name,
            Instructions = _instructions,
            HistoryReducer = _historyReducer,
            LoggerFactory = loggerFactory,
            Id = _id
        };
}
