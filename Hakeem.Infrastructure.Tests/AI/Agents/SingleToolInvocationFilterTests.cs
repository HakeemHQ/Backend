using Hakeem.Infrastructure.AI.Agents;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Hakeem.Infrastructure.Tests.AI.Agents;

public sealed class SingleToolInvocationFilterTests
{
    [Fact]
    public async Task OnAutoFunctionInvocationAsync_InvokesToolBeforeTerminating()
    {
        var context = await CreateContextAsync();
        var filter = new SingleToolInvocationFilter(NullLogger.Instance);
        var nextWasCalled = false;

        await filter.OnAutoFunctionInvocationAsync(
            context,
            invocationContext =>
            {
                Assert.False(invocationContext.Terminate);
                nextWasCalled = true;
                return Task.CompletedTask;
            });

        Assert.True(nextWasCalled);
        Assert.True(context.Terminate);
        Assert.Equal(1, filter.InvocationCount);
    }

    [Fact]
    public async Task OnAutoFunctionInvocationAsync_WhenToolFails_PreservesException()
    {
        var context = await CreateContextAsync();
        var filter = new SingleToolInvocationFilter(NullLogger.Instance);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => filter.OnAutoFunctionInvocationAsync(
                context,
                _ => throw new InvalidOperationException("original failure")));

        Assert.Equal("original failure", exception.Message);
        Assert.True(context.Terminate);
        Assert.Equal(1, filter.InvocationCount);
    }

    private static async Task<AutoFunctionInvocationContext>
        CreateContextAsync()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var function = KernelFunctionFactory.CreateFromMethod(
            () => "result",
            "medical_intelligence_tool");
        var result = await function.InvokeAsync(kernel);

        return new AutoFunctionInvocationContext(
            kernel,
            function,
            result,
            new ChatHistory(),
            new ChatMessageContent(
                AuthorRole.Assistant,
                content: (string?)null));
    }
}
