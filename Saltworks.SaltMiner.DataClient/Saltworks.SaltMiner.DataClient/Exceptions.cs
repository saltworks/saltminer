using Saltworks.Utility.ApiHelper;
using System;

namespace Saltworks.SaltMiner.DataClient
{
    [Serializable]
    public class DataClientResponseException : DataClientException
    {
        public ApiClientResponse Response { get; }
        public DataClientResponseException(ApiClientResponse response) { Response = response; }
        public DataClientResponseException(string message, ApiClientResponse response) : base(message) { Response = response; }
        public DataClientResponseException(string message, Exception inner, ApiClientResponse response) : base(message, inner) { Response = response; }
        protected DataClientResponseException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class DataClientValidationException : DataClientException
    {
        public ApiClientResponse Response { get; }
        public DataClientValidationException(ApiClientResponse response) { Response = response; }
        public DataClientValidationException(string message, ApiClientResponse response) : base(message) { Response = response; }
        public DataClientValidationException(string message, Exception inner, ApiClientResponse response) : base(message, inner) { Response = response; }
        protected DataClientValidationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class DataClientException : Exception
    {
        public DataClientException() { }
        public DataClientException(string message) : base(message) { }
        public DataClientException(string message, Exception inner) : base(message, inner) { }
        protected DataClientException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class DataClientInitializationException : DataClientException
    {
        public DataClientInitializationException() { }
        public DataClientInitializationException(string message) : base(message) { }
        public DataClientInitializationException(string message, Exception inner) : base(message, inner) { }
        protected DataClientInitializationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
