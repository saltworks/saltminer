using System;

namespace Saltworks.SaltMiner.ElasticClient
{
    [Serializable]
    public class EsClientException : Exception
    {
        public EsClientException() { }
        public EsClientException(string message) : base(message) { }
        public EsClientException(string message, Exception inner) : base(message, inner) { }
    }

    [Serializable]
    public class EsInvalidResponseException : NestClientException
    {
        public int StatusCode { get; set; }
        public EsInvalidResponseException() { }
        public EsInvalidResponseException(string message, int statusCode) : base(message) { }
        public EsInvalidResponseException(string message, int statusCode, Exception inner) : base(message, inner) { }
    }


    [Serializable]
    public class EsInvalidCastException : NestClientException
    {
        public EsInvalidCastException() { }
        public EsInvalidCastException(string message) : base(message) { }
        public EsInvalidCastException(string message, Exception inner) : base(message, inner) { }
    }
}
