using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Hakeem.Infrastructure.AI.Agents;

internal sealed class BoundedToolInvocationFilter(
    int maximumIterations,
    ILogger logger)
    : IAutoFunctionInvocationFilter
{
    private int _invocationCount;

    public int InvocationCount => Volatile.Read(ref _invocationCount);

    public async Task OnAutoFunctionInvocationAsync(
        AutoFunctionInvocationContext context,
        Func<AutoFunctionInvocationContext, Task> next)
    {
        var invocationCount = Interlocked.Increment(
            ref _invocationCount);

        if (invocationCount > maximumIterations)
        {
            throw new InvalidOperationException(
                $"The extraction agent exceeded the maximum of {maximumIterations} tool-call iterations.");
        }

        logger.LogInformation(
            "Extraction agent invoking tool {PluginName}.{FunctionName} in iteration {Iteration}.",
            context.Function.PluginName,
            context.Function.Name,
            invocationCount);

        await next(context);

        // Each phase exposes exactly one required extraction tool. Once that
        // tool has run, the workflow owns the next step; no arbitrary model
        // completion is needed after the tool result.
        context.Terminate = true;
    }
}
