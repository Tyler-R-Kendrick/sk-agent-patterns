using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.History;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Patterns.Agents;

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
