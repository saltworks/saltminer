using Microsoft.AspNetCore.Razor.Hosting;
using Saltworks.SaltMiner.Core.Data;
using Saltworks.SaltMiner.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Saltworks.SaltMiner.DataClient;

public static class DataGenerator
{
    public static IEnumerable<T> Generate<T>(SearchRequest searchRequest, Func<SearchRequest, DataResponse<T>> dataClientCall) where T: SaltMinerEntity
    {
        Validate(searchRequest, dataClientCall);
        string curFirstId = null;
        while (true)
        {
            var rsp = dataClientCall.Invoke(searchRequest);
            if (!rsp.Success)
                throw new DataClientException("Search failed in DataGenerator");
            var first = rsp.Data.FirstOrDefault();
            if (first == null)
                break;
            if (first.Id == curFirstId)
                throw new DataClientException("Repeating first ID, pagination error.");
            curFirstId = first.Id;
            foreach (var item in rsp.Data)
                yield return item;
            if (rsp.PagingInfo.IsLastPage())
                break;
            searchRequest.PagingInfo = rsp.PagingInfo.NextPage();
        }
    }

    private static void Validate<T>(SearchRequest searchRequest, Func<SearchRequest, DataResponse<T>> dataClientCall) where T : SaltMinerEntity
    {
        ArgumentNullException.ThrowIfNull(searchRequest);
        ArgumentNullException.ThrowIfNull(dataClientCall);
    }
}
