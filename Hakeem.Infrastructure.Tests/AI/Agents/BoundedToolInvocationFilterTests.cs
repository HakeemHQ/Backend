using Hakeem.Infrastructure.AI.Agents;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Hakeem.Infrastructure.Tests.AI.Agents;

public sealed class BoundedToolInvocationFilterTests
{
    [Fact]
    public async Task OnAutoFunctionInvocationAsync_FifthInvocation_Throws()
    {
        var context = await CreateContextAsync();
        var filter = new BoundedToolInvocationFilter(
            4,
            NullLogger.Instance);
        var nextWasCalled = false;

        for (var invocation = 0; invocation < 4; invocation++)
        {
            await filter.OnAutoFunctionInvocationAsync(
                context,
                _ => Task.CompletedTask);
        }

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => filter.OnAutoFunctionInvocationAsync(
                context,
                _ =>
                {
                    nextWasCalled = true;
                    return Task.CompletedTask;
                }));

        Assert.Contains("maximum of 4", exception.Message);
        Assert.False(nextWasCalled);
        Assert.Equal(5, filter.InvocationCount);
    }

    [Fact]
    public async Task OnAutoFunctionInvocationAsync_FourthIteration_Continues()
    {
        var context = await CreateContextAsync();
        var filter = new BoundedToolInvocationFilter(
            4,
            NullLogger.Instance);
        var nextWasCalled = false;

        for (var invocation = 0; invocation < 4; invocation++)
        {
            await filter.OnAutoFunctionInvocationAsync(
                context,
                _ =>
                {
                    nextWasCalled = true;
                    return Task.CompletedTask;
                });
        }

        Assert.True(nextWasCalled);
        Assert.Equal(4, filter.InvocationCount);
    }

    private static async Task<AutoFunctionInvocationContext>
        CreateContextAsync()
    {
        var kernel = Kernel.CreateBuilder().Build();
        var function = KernelFunctionFactory.CreateFromMethod(
            () => "result",
            "read_document_ocr");
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
