using System.Reflection;
using Microsoft.Azure.Functions.Worker;
using MKTestAzureFunction;

namespace MKTestAzureFunction.Tests;

public class ExtractIsdocFunctionTests
{
    [Fact]
    public void Run_HttpTrigger_ContainsOptionsMethodForCorsPreflight()
    {
        var runMethod = typeof(ExtractIsdocFunction).GetMethod(nameof(ExtractIsdocFunction.Run));

        Assert.NotNull(runMethod);

        var requestParameter = runMethod!.GetParameters().Single();
        var triggerAttribute = requestParameter.GetCustomAttribute<HttpTriggerAttribute>();

        Assert.NotNull(triggerAttribute);
        Assert.NotNull(triggerAttribute!.Methods);
        Assert.Contains("options", triggerAttribute.Methods, StringComparer.OrdinalIgnoreCase);
    }
}
