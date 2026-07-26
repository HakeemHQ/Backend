using Hakeem.Application.Constants;

namespace Hakeem.Application.Common.ResponseModel;

public class BaseResponseModel
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public ICollection<ErrorResponseModel> ErrorList { get; set; }
    public string? GlobalErrorCode { get; set; }
    public BaseResponseModel(string message)
    {
        Success = true;
        Message = message;
        ErrorList = new List<ErrorResponseModel>();
    }

    public BaseResponseModel(string message, IList<ErrorResponseModel> errorList, string? errorcode = null)
    {
        Success = false;
        Message = message;
        ErrorList = errorList;
        GlobalErrorCode = errorcode;
    }
}

public class ErrorResponseModel
{
    public string PropertyName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public static ErrorResponseModel Create(string propertyName, string message, string code = "") =>
        new() { PropertyName = propertyName, Message = message, Code = code };
}
