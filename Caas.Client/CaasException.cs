using System;

namespace Caas.Client
{
    /// <summary>
    /// Handling of Caas Client errors
    /// </summary>
    public class CaasException : Exception
    {
        /// <summary>
        /// Error code if initialization has not occurred
        /// </summary>
        public const int NOT_INITIALIZED = -1;

        /// <summary>
        /// Error code if client has invalid URI
        /// </summary>
        public const int INVALID_URI = 1;

        /// <summary>
        /// Error code if response was server was okay, but in a different format 
        /// than expected
        /// </summary>
        public const int INVALID_SERVER_RESPONSE = 2;

        /// <summary>
        /// Error code if 500 error from server on request
        /// </summary>
        public const int SERVER_ERROR = 3;

        /// <summary>
        /// Error code if unknown response from server
        /// </summary>
        public const int UNKNOWN_SERVER_RESPONSE = 4;

        /// <summary>
        /// Error code if 404 error from server
        /// </summary>
        public const int SERVER_NOT_FOUND = 5;

        /// <summary>
        /// Get the error code
        /// </summary>
        public int ErrorCode { get; private set; }

        private string extraMessage;

        public override string Message
        {
            get
            {
                switch(ErrorCode)
                {
                    case NOT_INITIALIZED:
                        return "You must first call CaasManager.Init() before making any calls";
                    case INVALID_URI:
                        return "The endpoint must be a valid URI";
                    case INVALID_SERVER_RESPONSE:
                        return $"The response from the server was in a invalid format: {extraMessage}";
                    case SERVER_ERROR:
                        return $"The server returned an error: {extraMessage}";
                    case UNKNOWN_SERVER_RESPONSE:
                        return $"The server returned an unknown error: {extraMessage}";
                    default:
                        return "Unknown Error";
                }
            }
        }

        /// <summary>
        /// Ctor for <see cref="CaasException"/> defining the <see cref="ErrorCode"/>
        /// </summary>
        /// <param name="errorCode"></param>
        public CaasException(int errorCode) => ErrorCode = errorCode;

        /// <summary>
        /// Ctor for <see cref="CaasException"/> defining the <see cref="ErrorCode"/>
        /// and any extra data
        /// </summary>
        /// <param name="errorCode"></param>
        /// <param name="extra"></param>
        public CaasException(int errorCode, string extra)
        {
            ErrorCode = errorCode;
            extraMessage = extra;
        }
    }
}
