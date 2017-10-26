using System;
using System.Collections.Generic;
using System.Net;
using RestSharp;

namespace consoleagentappcsharp.Authentication
{
    public class ApiClient
    {
        public String BasePath { get; set; }
        public Dictionary<String, String> DefaultHeaders = new Dictionary<string, string>();

        public ApiClient()
        {
        }

    }
}
