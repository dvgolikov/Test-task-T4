using System.Runtime.Serialization;

namespace TestTaskT4.Exceptions;

public class AppException : Exception
{
    public AppException(string title, string detail, int statusCodes)
    {
        Title = title;
        Detail = detail;
        StatusCode = statusCodes;
    }

    protected AppException(SerializationInfo info, StreamingContext context, string title, string detail, int statusCodes) : base(info, context)
    {
        Title = title;
        Detail = detail;
        StatusCode = statusCodes;
    }

    public AppException(string? message, string title, string detail, int statusCodes) : base(message)
    {
        Title = title;
        Detail = detail;
        StatusCode = statusCodes;
    }

    public AppException(string? message, Exception? innerException, string title, string detail, int statusCodes) : base(message, innerException)
    {
        Title = title;
        Detail = detail;
        StatusCode = statusCodes;
    }

    public string Title { get; set; }
    public string Detail { get; set; }
    public int StatusCode { get; set; }
}