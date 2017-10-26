using System;
using PowerArgs;

namespace consoleagentappcsharp
{
    public class Options
    {
        [ArgRequired(PromptIfMissing = true)]
        public string ApiKey { get; set; }

        [ArgRequired(PromptIfMissing = true)]
        public string Username { get; set; }

        [ArgRequired(PromptIfMissing = true)]
        public string Password { get; set; }

        [ArgRequired(PromptIfMissing = true)]
        public string BaseUrl { get; set; }

        [ArgRequired(PromptIfMissing = true)]
        public string ClientId { get; set; }

        [ArgRequired(PromptIfMissing = true)]
        public string ClientSecret { get; set; }

        public string AuthBaseUrl { get; set; }

        public bool IsDebugEnabled { get; set; }

        public string DefaultAgentId { get; set; }

        public string DefaultDn { get; set; }

        public string DefaultDestination { get; set; }

        public bool IsAutoLogin { get; set; }

        public static Options parseOptions(string[] args)
        {

            Options options = null;

            try
            {
                options = Args.Parse<Options>(args);
            }
            catch (ArgException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ArgUsage.GenerateUsageFromTemplate<Options>());
            }

            return options;
        }
    }
}
