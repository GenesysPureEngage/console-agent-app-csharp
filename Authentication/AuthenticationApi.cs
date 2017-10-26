using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using CometD.Bayeux;
using CometD.Bayeux.Client;
using CometD.Client;
using CometD.Client.Transport;
using RestSharp;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;

namespace consoleagentappcsharp.Authentication
{
    public class AuthenticationApi
    {
        public ApiClient AuthClient { get; set; }

        private CookieContainer cookieContainer;

        public AuthenticationApi(ApiClient authClient)
        {
            AuthClient = authClient;

            cookieContainer = new CookieContainer();
        }

        public JObject RetrieveToken(String grant_type, String authorizationHeader, String acceptHeader, String scope,
                        String clientId, String username, String password)
        {
            String body = $"grant_type={grant_type}&scope={scope}&clientId={clientId}&username={username}&password={password}";

            byte[] data = Encoding.ASCII.GetBytes(body);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(AuthClient.BasePath + "/oauth/token");
            request.Method = "POST";
            request.CookieContainer = cookieContainer;

            foreach (String key in AuthClient.DefaultHeaders.Keys)
            {
                request.Headers.Add(key, AuthClient.DefaultHeaders[key]);
            }

            request.Headers.Add("Authorization", authorizationHeader);
            request.Accept = acceptHeader;

            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }

            string responseContent = null;

            using (WebResponse response = request.GetResponse())
            {
                using (Stream stream = response.GetResponseStream())
                {
                    using (StreamReader sr = new StreamReader(stream))
                    {
                        responseContent = sr.ReadToEnd();
                    }
                }
            }

            JObject responseJSON = JObject.Parse(responseContent);
            return responseJSON;
        }

    }
}
