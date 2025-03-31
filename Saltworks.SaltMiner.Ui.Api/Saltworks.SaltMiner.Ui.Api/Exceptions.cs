namespace Saltworks.SaltMiner.Ui.Api
{


    [Serializable]
    public class UiApiException : Exception
    {
        public UiApiException() { }
        public UiApiException(string message) : base(message) { }
        public UiApiException(string message, Exception inner) : base(message, inner) { }
        protected UiApiException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class UiApiSslException : UiApiException
    {
        public UiApiSslException() { }
        public UiApiSslException(string message) : base(message) { }
        public UiApiSslException(string message, Exception inner) : base(message, inner) { }
        protected UiApiSslException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
