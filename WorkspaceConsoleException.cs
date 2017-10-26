using System;
namespace consoleagentappcsharp
{
    public class WorkspaceConsoleException : Exception
    {
        public WorkspaceConsoleException(String message) : base(message)
        {
        }

        public WorkspaceConsoleException(String message, Exception cause): base(message, cause)
        {
        }
    }
}
