using System;

namespace Saltworks.SaltMiner.Core.Common
{

    [Serializable]
    public class SaltminerException : Exception
    {
        public SaltminerException() { }
        public SaltminerException(string message) : base(message) { }
        public SaltminerException(string message, Exception inner) : base(message, inner) { }
        protected SaltminerException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }


    [Serializable]
    
    public class SaltminerDataException : SaltminerException
    {
        public SaltminerDataException() { }
        public SaltminerDataException(string message) : base(message) { }
        public SaltminerDataException(string message, Exception inner) : base(message, inner) { }
        protected SaltminerDataException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
