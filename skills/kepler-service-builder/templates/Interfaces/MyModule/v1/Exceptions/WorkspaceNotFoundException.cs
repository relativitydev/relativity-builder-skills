using System;
using System.Net;
using System.Runtime.Serialization;
using Relativity.Services.Exceptions;

namespace MyCompany.MyApp.Interfaces.MyModule.v1.Exceptions
{
    // Custom exceptions live in the Interfaces assembly so .NET clients can catch them by type.
    // [FaultCode] sets the HTTP status returned to REST callers (a plain ServiceException is a 500).
    // Generate a new GUID for [ExceptionIdentifier]; never reuse one across exception types.
    // For common cases, throw the built-ins instead: ValidationException (400),
    // NotAuthorizedException (403), NotFoundException (404), ConflictException (409).
    [ExceptionIdentifier("00000000-0000-0000-0000-000000000000")]
    [FaultCode(HttpStatusCode.NotFound)]
    [Serializable]
    public class WorkspaceNotFoundException : ServiceException
    {
        public WorkspaceNotFoundException()
        {
        }

        public WorkspaceNotFoundException(string message)
            : base(message)
        {
        }

        public WorkspaceNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected WorkspaceNotFoundException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
