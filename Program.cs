using System;

namespace consoleagentappcsharp
{
    public class Program
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            try
            {
                Options options = Options.parseOptions(args);
                if (options == null)
                {
                    return;
                }

                WorkspaceConsole console = new WorkspaceConsole(options);
                console.Run();

            }
            catch (Exception e)
            {
                Console.WriteLine("Error!:\n" + e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }
    }
}
