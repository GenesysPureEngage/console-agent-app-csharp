using System;

namespace consoleagentappcsharp.Workspace.Common
{
    public class WorkspaceApiException : Exception
    {
        public WorkspaceApiException(string message) : base(message)
        {
        }
    }
}
