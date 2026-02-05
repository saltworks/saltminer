/* --[auto-generated, do not modify this block]--
*
* Copyright (c) 2025 Saltworks Security, LLC
*
* Use of this software is governed by the Business Source License included
* in the LICENSE file.
*
* Change Date: 2029-12-09
*
* On the date above, in accordance with the Business Source License, use
* of this software will be governed by version 2 or later of the General
* Public License.
*
* ----
*/

using Saltworks.SaltMiner.Core.Entities;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace Saltworks.SaltMiner.Core.Data;
public abstract class Response
{
    protected Response() { }
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
    public PagingInfo PagingInfo { get; set; } = null;
    public DataDictionaryResponse(Dictionary<T1, T2> results, PagingInfo pagingInfo = null)
    {
        Results = results;
        PagingInfo = pagingInfo;
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

public class DataDto<T> where T : SaltMinerEntity
{
    public long? SequenceNumber { get; set; }
    public long? PrimaryTerm { get; set; }
    public string Index { get; set; }
    public T DataItem { get; set; }
}

public class JsonDataResponse : Response
{
    public string TypeName { get; set; }
    public IEnumerable<JsonObject> Data { get; set; }
    public PagingInfo PagingInfo { get; set; }
    public JsonDataResponse() { }
    public JsonDataResponse(IEnumerable<JsonObject> data, PagingInfo pagingInfo = null)
    {
        Data = data ?? [];
        PagingInfo = pagingInfo;
    }
    public JsonDataResponse(int statusCode, string errorType, List<string> messages)
    {
        StatusCode = statusCode;
        ErrorType = errorType;
        ErrorMessages = messages;
    }
    public JsonDataResponse(int statusCode, string errorType, string message)
    {
        StatusCode = statusCode;
        ErrorType = errorType;
        ErrorMessages = [message];
    }
}

public class DataResponse<T> : Response where T : class
{
    public virtual IEnumerable<T> Data { get; set; }
    public PagingInfo PagingInfo { get; set; }
    public DataResponse() { }

    public DataResponse(IEnumerable<T> data, PagingInfo pagingInfo = null)
    {
        Data = data ?? [];
        PagingInfo = pagingInfo ?? new();
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