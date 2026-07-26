namespace Hakeem.Application.Common.ResponseModel;

public class GenericResponseModel<TResponse> : BaseResponseModel
{
    public TResponse Data { get; set; } = default!;

    public GenericResponseModel(string message, IList<ErrorResponseModel> errorList, string? errorCode = null) : base(message, errorList, errorCode)
    {
        if (errorList.Count == 0)
        {
            Type t = typeof(TResponse);
            if (t.GetConstructor(Type.EmptyTypes) != null)
            {
                Data = Activator.CreateInstance<TResponse>();
            }
        }
    }

    public GenericResponseModel(string message, TResponse data) : base(message) => Data = data;

    public new static GenericResponseModel<TResponse> Success(TResponse data, string message = "")
        => new GenericResponseModel<TResponse>(message, data);

    public static GenericResponseModel<TResponse> Failure(string message, IList<ErrorResponseModel> errorList)
        => new GenericResponseModel<TResponse>(message, errorList);

    public static GenericResponseModel<TResponse> Failure(string message)
        => new GenericResponseModel<TResponse>(message, new List<ErrorResponseModel>());
    public static GenericResponseModel<TResponse> Failure(string message, string errorCode)
    => new GenericResponseModel<TResponse>(message, new List<ErrorResponseModel>(), errorCode);
}
