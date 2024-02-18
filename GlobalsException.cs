using System;
using System.Runtime.Serialization;

namespace Globals.NET.RabbitMQ
{
    public class GlobalsException : Exception, ISerializable
    {
        public GlobalsException() : base() { }

        public GlobalsException(string message, Exception innerException) : base(message + " See inner exception for details.", innerException) { }
    }

    public class GlobalsException<T> : Exception, ISerializable
    {
        public GlobalsException() : base() { }

        public GlobalsException(string message, Exception innerException, string globalName, T data) : base(message + " See inner exception for details.", innerException)
        {
            Data.Add("GlobalName", globalName);
            Data.Add("GlobalData", data);
        }
    }
}
