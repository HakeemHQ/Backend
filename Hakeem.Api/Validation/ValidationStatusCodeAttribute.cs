namespace Hakeem.Api.Validation;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ValidationStatusCodeAttribute(int statusCode) : Attribute
{
    public int StatusCode { get; } = statusCode;
}
