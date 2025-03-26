using Saltworks.SaltMiner.Core.Entities;
using System;
using System.Collections.Generic;

namespace Saltworks.SaltMiner.Core.Data
{
    public abstract class Response
    {
        public Response() { }
        public string Message { get; init; } = string.Empty;
        public bool Success => ErrorMessages == null || ErrorMessages.Count == 0;
        public long Affected { get; init; } = 0;
        public int StatusCode { get; set; }
        public string ErrorType { get; set; } = null;
        public List<string> ErrorMessages { get; set; } = null;
    }

    public class NoDataResponse : Response
    {
        public NoDataResponse() { }
        public NoDataResponse(long affected, string message = "")
        {
            Affected = affected;
            Message = message;
        }

        public NoDataResponse(int statusCode, string errorType, List<string> messages)
        {
            StatusCode = statusCode;
            ErrorType = errorType;
            Affected = 0;
            ErrorMessages = messages;
        }
        public NoDataResponse(int statusCode, string errorType, string message)
        {
            StatusCode = statusCode;
            ErrorType = errorType;
            Affected = 0;
            ErrorMessages = [message];
        }
    }

    public class DataDictionaryResponse<T1, T2> : Response
    {
        public Dictionary<T1, T2> Results { get; set; }
        public PitPagingInfo PitPagingInfo { get; set; }
        public UIPagingInfo UIPagingInfo { get; set; }
        public DataDictionaryResponse(Dictionary<T1, T2> results, PitPagingInfo pagingInfo = null)
        {
            Results = results;
            PitPagingInfo = pagingInfo;
            UIPagingInfo = null;
        }
        public DataDictionaryResponse(Dictionary<T1, T2> results, UIPagingInfo pagingInfo = null)
        {
            Results = results;
            UIPagingInfo = pagingInfo;
            PitPagingInfo = null;
        }
        public DataDictionaryResponse() { }

        public DataDictionaryResponse(int statusCode, string errorType, List<string> messages)
        {
            StatusCode = statusCode;
            ErrorType = errorType;
            ErrorMessages = messages;
        }
        public DataDictionaryResponse(int statusCode, string errorType, string message)
        {
            StatusCode = statusCode;
            ErrorType = errorType;
            ErrorMessages = [message];
        }
    }

    public class DataDtoResponse<T>: DataResponse<T> where T: SaltMinerEntity
    {
        public DataDtoResponse(IEnumerable<DataDto<T>> data, UIPagingInfo pagingInfo = null)
        {
            Data = data ?? [];
            PitPagingInfo = null;
            UIPagingInfo = pagingInfo;
        }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Disabling base ctor")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1806:Do not ignore method results", Justification = "Disabling base ctor")]
        public DataDtoResponse(IEnumerable<T> data, UIPagingInfo pagingInfo = null) => new NotImplementedException();
        public new IEnumerable<DataDto<T>> Data { get; set; }
    }

    public class DataDto<T> where T : SaltMinerEntity
    {
        public long? SequenceNumber { get; set; }
        public long? PrimaryTerm { get; set; }
        public string Index { get; set; }
        public T DataItem { get; set; }
    }

    public class DataResponse<T> : Response where T : class
    {
        public virtual IEnumerable<T> Data { get; set; }
        public IList<object> AfterKeys { get; set; }
        public PitPagingInfo PitPagingInfo { get; set; }
        public UIPagingInfo UIPagingInfo { get; set; }
        public DataResponse() { }

        public DataResponse(IEnumerable<T> data, UIPagingInfo pagingInfo = null)
        {
            Data = data ?? [];
            PitPagingInfo = null;
            UIPagingInfo = pagingInfo;
        }

        public DataResponse(int statusCode, string errorType, List<string> messages)
        {
            StatusCode = statusCode;
            ErrorType = errorType;
            ErrorMessages = messages;
        }
        public DataResponse(int statusCode, string errorType, string message)
        {
            StatusCode = statusCode;
            ErrorType = errorType;
            ErrorMessages = [message];
        }
    }

    public class BulkResponse : Response
    {
        public Dictionary<string, string> BulkErrors { get; set; }
        public BulkResponse() { }
        public BulkResponse(long affected, string message = null)
        {
            Affected = affected;
            Message = message;
        }

        public BulkResponse(long affected, int statusCode, string errorType, Dictionary<string, string> messages)
        {

            StatusCode = statusCode;
            Affected = affected;
            ErrorType = errorType;
            BulkErrors = messages;
            ErrorMessages = ["Please see Bulk Errors"];
        }
    }

    public class DataItemResponse<T> : Response where T : class
    {
        public T Data { get; set; }
        public long? Primary { get; set; }
        public long? SeqNum { get; set; }
        public DataItemResponse() { }
        public DataItemResponse(T data)
        {
            if (data == null)
            {
                Affected = 0;
            }
            else
            {
                Data = data;
                Affected = 1;
            }
        }

        public DataItemResponse(int statusCode, string errorType, List<string> messages)
        {
            StatusCode = statusCode;
            ErrorType = errorType;
            Affected = 0;
            ErrorMessages = messages;
        }

        public DataItemResponse(int statusCode, string errorType, string message)
        {
            StatusCode = statusCode;
            ErrorType = errorType;
            Affected = 0;
            ErrorMessages = [message];
        }
    }

    public class ErrorResponse : Response
    {
        public ErrorResponse() { }

        public ErrorResponse(int statusCode, string errorType, List<string> messages)
        {
            StatusCode = statusCode;
            ErrorType = errorType;
            Affected = 0;
            ErrorMessages = messages;
        }
        public ErrorResponse(int statusCode, string errorType, string message)
        {
            StatusCode = statusCode;
            ErrorType = errorType;
            Affected = 0;
            ErrorMessages = [message];
        }
    }
}
