using Saltworks.Utility.ApiHelper;
using System.Net;

namespace Saltworks.SaltMiner.UiApiClient
{
    [Serializable]
    public class UiApiClientResponseException : UiApiClientException
    {
        public ApiClientResponse Response { get; }
        public UiApiClientResponseException(ApiClientResponse response) { Response = response; }
        public UiApiClientResponseException(string message, ApiClientResponse response) : base(message) { Response = response; }
        public UiApiClientResponseException(string message, Exception inner, ApiClientResponse response) : base(message, inner) { Response = response; }
        protected UiApiClientResponseException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class UiApiClientException : Exception
    {
        public UiApiClientException() { }
        public UiApiClientException(string message) : base(message) { }
        public UiApiClientException(string message, Exception inner) : base(message, inner) { }
        protected UiApiClientException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class UiApiClientInitializationException : UiApiClientException
    {
        public UiApiClientInitializationException() { }
        public UiApiClientInitializationException(string message) : base(message) { }
        public UiApiClientInitializationException(string message, Exception inner) : base(message, inner) { }
        protected UiApiClientInitializationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class UiApiClientAttributeDefinitionException : UiApiClientException
    {
        public UiApiClientAttributeDefinitionException() { }
        public UiApiClientAttributeDefinitionException(string message) : base(message) { }
        public UiApiClientAttributeDefinitionException(string message, Exception inner) : base(message, inner) { }
        protected UiApiClientAttributeDefinitionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class UiApiClientFieldDefinitionException : UiApiClientException
    {
        public UiApiClientFieldDefinitionException() { }
        public UiApiClientFieldDefinitionException(string message) : base(message) { }
        public UiApiClientFieldDefinitionException(string message, Exception inner) : base(message, inner) { }
        protected UiApiClientFieldDefinitionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class UiApiClientImportException : UiApiClientException
    {
        public UiApiClientImportException() { }
        public UiApiClientImportException(string message) : base(message) { }
        public UiApiClientImportException(string message, Exception inner) : base(message, inner) { }
        protected UiApiClientImportException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class UiApiClientValidationException : UiApiClientHttpException
    {
        public UiApiClientValidationException() : base((string)null, 400) { }
        public UiApiClientValidationException(List<string> messages) : base(messages, 400) { }
        public UiApiClientValidationException(string message) : base(message, 400) { }
        public UiApiClientValidationException(string message, Exception inner) : base(message, 400, inner) { }
        protected UiApiClientValidationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class UiApiClientHttpException : UiApiClientException
    {
        public List<string> HttpMessages { get; set; } = [];
        public int HttpStatus { get; set; } = 500;
        public string HttpReason { get; set; } = "Server Error";

        public UiApiClientHttpException() { }
        public UiApiClientHttpException(List<string> messages) : base(ConvertErrorsToMessage(messages))
        {
            HttpMessages = messages;
        }

        public UiApiClientHttpException(List<string> messages, int httpStatus) : base(ConvertErrorsToMessage(messages))
        {
            HttpStatus = httpStatus;
            HttpReason = Enum.GetName((HttpStatusCode)httpStatus);
            HttpMessages = messages;
        }

        public UiApiClientHttpException(List<string> messages, int httpStatus, Exception innerException) : base(ConvertErrorsToMessage(messages), innerException)
        {
            HttpStatus = httpStatus;
            HttpReason = Enum.GetName((HttpStatusCode)httpStatus);
            HttpMessages = messages;
        }

        public UiApiClientHttpException(string message) : base(message)
        {
            HttpMessages = [ message ];
        }

        public UiApiClientHttpException(string message, int httpStatus) : base(message)
        {
            HttpMessages = [ message ];
            HttpStatus = httpStatus;
            HttpReason = Enum.GetName((HttpStatusCode)httpStatus);
        }

        public UiApiClientHttpException(string message, int httpStatus, Exception innerException) : base(message, innerException)
        {
            HttpMessages = [ message ];
            HttpStatus = httpStatus;
            HttpReason = Enum.GetName((HttpStatusCode)httpStatus);
        }

        protected UiApiClientHttpException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

        private static string ConvertErrorsToMessage(List<string> errors)
        {
            if (errors == null)
            {
                return null;
            }

            return string.Join("|", errors);
        }
    }

    [Serializable]
    public class UiApiClientValidationMissingValueException : UiApiClientValidationException
    {
        public UiApiClientValidationMissingValueException() { }
        public UiApiClientValidationMissingValueException(string message) : base(message) { }
        public UiApiClientValidationMissingValueException(string message, Exception inner) : base(message, inner) { }
        protected UiApiClientValidationMissingValueException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class UiApiClientValidationQueueStateException : UiApiClientValidationException
    {
        public UiApiClientValidationQueueStateException() { }
        public UiApiClientValidationQueueStateException(string message) : base(message) { }
        public UiApiClientValidationQueueStateException(string message, Exception inner) : base(message, inner) { }
        protected UiApiClientValidationQueueStateException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    public class UiApiClientValidationMissingArgumentException : UiApiClientValidationException
    {
        public UiApiClientValidationMissingArgumentException() { }
        public UiApiClientValidationMissingArgumentException(string message) : base(message) { }
        public UiApiClientValidationMissingArgumentException(string message, Exception inner) : base(message, inner) { }
        protected UiApiClientValidationMissingArgumentException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class UiApiClientValidationReferentialIntegrityException : UiApiClientValidationException
    {
        public UiApiClientValidationReferentialIntegrityException() { }
        public UiApiClientValidationReferentialIntegrityException(string message) : base(message) { }
        public UiApiClientValidationReferentialIntegrityException(string message, Exception inner) : base(message, inner) { }
        protected UiApiClientValidationReferentialIntegrityException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class UiApiNotFoundException : UiApiClientHttpException
    {
        public UiApiNotFoundException() : base((string)null, 404) { }
        public UiApiNotFoundException(string message) : base(message, 404) { }
        public UiApiNotFoundException(string message, Exception inner) : base(message, 404, inner) { }
        protected UiApiNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class UiApiUnauthorizedException : UiApiClientHttpException
    {
        public UiApiUnauthorizedException() : base((string)null, 401) { }
        public UiApiUnauthorizedException(string message) : base(message, 401) { }
        public UiApiUnauthorizedException(string message, Exception inner) : base(message, 401, inner) { }
        protected UiApiUnauthorizedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class UiApiForbiddenException : UiApiClientHttpException
    {
        public UiApiForbiddenException() : base((string)null, 403) { }
        public UiApiForbiddenException(string message) : base(message, 403) { }
        public UiApiForbiddenException(string message, Exception inner) : base(message, 403, inner) { }
        protected UiApiForbiddenException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class UiApiConfigurationException : UiApiClientHttpException
    {
        public UiApiConfigurationException() : base() { }
        public UiApiConfigurationException(string message) : base(message) { }
        public UiApiConfigurationException(string message, Exception inner) : base(message, 500, inner) { }
        protected UiApiConfigurationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    public class UiApiAuthException : UiApiClientHttpException
    {
        public UiApiAuthException() { }
        public UiApiAuthException(string message) : base(message) { }
        public UiApiAuthException(string message, Exception inner) : base(message, 500, inner) { }
        protected UiApiAuthException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
