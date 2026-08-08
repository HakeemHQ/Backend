using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Hakeem.Infrastructure.AI.Agents;

internal sealed class SingleToolInvocationFilter(ILogger logger)
    : IAutoFunctionInvocationFilter
{
    private int _invocationCount;

    public int InvocationCount => Volatile.Read(ref _invocationCount);

    public async Task OnAutoFunctionInvocationAsync(
        AutoFunctionInvocationContext context,
        Func<AutoFunctionInvocationContext, Task> next)
    {
        var count = Interlocked.Increment(ref _invocationCount);

        if (count != 1)
        {
            throw new InvalidOperationException(
                "The medical-intelligence agent attempted more than one tool invocation.");
        }

        logger.LogInformation(
            "Medical-intelligence agent invoking {PluginName}.{FunctionName}.",
            context.Function.PluginName,
            context.Function.Name);

        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Medical-intelligence tool {PluginName}.{FunctionName} failed.",
                context.Function.PluginName,
                context.Function.Name);
            context.Terminate = true;
            throw;
        }

        // The application owns final-response generation after the single
        // required tool result has been captured. Terminate only after the
        // function has actually executed.
        context.Terminate = true;
    }
}
