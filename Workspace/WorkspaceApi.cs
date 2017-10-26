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
using System.Windows;
using Newtonsoft.Json.Linq;

namespace consoleagentappcsharp.Workspace
{
    public class WorkspaceApi
    {
        public static String SESSION_COOKIE = "WORKSPACE_SESSIONID";

        private BayeuxClient bayeuxClient;
        private RestClient restClient;

        private CookieContainer cookieContainer;
        private string apiKey;
        private string baseUrl;
        private bool isDebugEnabled;
        private string bearerAuth;
        private string workspaceSessionId;

        public bool DebugEnabled { get; set; }

        public WorkspaceApi(string apiKey, string baseUrl, bool isDebugEnabled)
        {
            this.apiKey = apiKey;
            this.baseUrl = baseUrl;
            this.isDebugEnabled = isDebugEnabled;

            cookieContainer = new CookieContainer();
            /**
             * Create an instance of a RestClient initialized to the GWSUrl 
             * to use when we want to make requests.
             */
            restClient = new RestClient(baseUrl + "/workspace/v3");
            restClient.CookieContainer = cookieContainer;
            restClient.AddDefaultHeader("x-api-key", apiKey);
        }

        private String extractSessionCookie(IRestResponse response)
        {
            string sessionId = null;

            foreach(Parameter parameter in response.Headers) 
            {
                Console.WriteLine(parameter.ToString());
                if (parameter.Name.Equals("set-cookie"))
                {
                    string cookie = (String)parameter.Value;
                    sessionId = cookie.Split(';')[0].Split('=')[1];
                    restClient.AddDefaultHeader("Cookie", String.Format("{0}={1}", SESSION_COOKIE, workspaceSessionId));
                }
            }

            return sessionId;
        }

        public void Initialize(string token)
        {
            bearerAuth = "bearer " + token;
            restClient.AddDefaultHeader(HttpRequestHeader.Authorization.ToString(), bearerAuth);

            RestRequest restRequest = new RestRequest("initialize-workspace", Method.POST);
            restRequest.AddHeader(HttpRequestHeader.Accept.ToString(), "application/json");

            /**
             * Since we are making a remote request that could potentially take some time, we certainly don't want to block
             * any calling threads, especially the UI thread.  When the response comes back we want to send it back to the 
             * callback and as a convenience we'll marshal it to the UI thread if that is necessary.
             */
            IRestResponse restResponse = restClient.Execute(restRequest);

            Console.WriteLine(restResponse.Content);
            JObject response = JObject.Parse(restResponse.Content);
            if ( response["status"]["code"].Value<Int32>() == 1 )
            {
                workspaceSessionId = extractSessionCookie(restResponse);

                InitializeCometD();
            }
        }

        private void InitializeCometD() 
        {
            var options = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "x-api-key", this.apiKey},
                { "Cookie", String.Format("{0}={1}", WorkspaceApi.SESSION_COOKIE, workspaceSessionId)}
            };

            /**
             * GWS currently only supports LongPolling as a method to receive events.
             * So tell the CometD library to negotiate a handshake with GWS and setup a LongPolling session.
             */
            GWSLongPollingTransport transport = new GWSLongPollingTransport(options, this.cookieContainer);

            bayeuxClient = new BayeuxClient(this.baseUrl + "/workspace/v3/notifications", transport);

            if (bayeuxClient.Handshake(null, 30000))
            {
                Debug.WriteLine("Handshake with GWS Successful");

                /**
                 * The CometD protocol supports the idea of subscribing to different channels, and in fact GWS
                 * publishes events on various channels.  However you can request to be notified of all messages
                 * on all channels by specifying a channel name of '/**', which is what we will do in this sample.
                 */
                IClientSessionChannel channelInitialization = bayeuxClient.GetChannel("/workspace/v3/initialization");
                channelInitialization.Subscribe(new CallbackMessageListener<BayeuxClient>(OnInitializationMessageReceived, bayeuxClient));

                IClientSessionChannel channelVoice = bayeuxClient.GetChannel("/workspace/v3/voice");
                channelVoice.Subscribe(new CallbackMessageListener<BayeuxClient>(OnVoiceMessageReceived, bayeuxClient));
            }
            else
            {
                throw new Exception("Unable to establish CometD handshake with GWS");
            }
        }

        public void Destroy()
        {
            //try {
            //    if (this.workspaceInitialized)
            //    {
            //        notifications.Disconnect();
            //        sessionApi.logout();
            //    }
            //} catch (Exception e) {
            //    throw new Exception("destroy failed.", e);
            //} finally {
            //    this.workspaceInitialized = false;
            //}
        }

        public void Logout()
        {
            Debug.WriteLine("Disconnecting from GWS CometD event stream");

            if (bayeuxClient != null && bayeuxClient.IsConnected)
            {
                bayeuxClient.Disconnect();
            }
        }

        //public event GWSEventHandler GWSEventReceived;
        //public delegate void GWSEventHandler(IMessage message);

        public void OnInitializationMessageReceived(IClientSessionChannel channel, IMessage message, BayeuxClient client)
        {
            Console.WriteLine(message.ToString());
        //    Debug.WriteLine("GWSClient received message on channel " + message.Channel + ": " + message.Data.ToString());

        //    if (GWSEventReceived != null)
        //    {
        //        GWSEventReceived(message);
        //    }
        }

        public void OnVoiceMessageReceived(IClientSessionChannel channel, IMessage message, BayeuxClient client)
        {
            Console.WriteLine(message.ToString());
        }

        //public void SendRequest(GWSRequest gwsRequest, Action<IRestResponse> callback)
        //{
        //    RestRequest restRequest = new RestRequest(gwsRequest.ResourceUri, gwsRequest.Method);

        //    /**
        //     * Include the required HTTP Basic Authorization header with the request
        //     */
        //    restRequest.AddHeader("Authorization", this.basicAuth);

        //    if (csrfHeader != null && csrfToken != null)
        //    {
        //        restRequest.AddHeader(csrfHeader, csrfToken);
        //    }

        //    /**
        //     * Only POST and PUT methods should have content
        //     */
        //    if (gwsRequest.Method == Method.POST ||
        //         gwsRequest.Method == Method.PUT)
        //    {
        //        restRequest.RequestFormat = RestSharp.DataFormat.Json;
        //        JObject json = JObject.Parse(gwsRequest.Content);
        //        restRequest.AddParameter("application/json", json.ToString(), ParameterType.RequestBody);
        //    }

        //    /**
        //     * Since we are making a remote request that could potentially take some time, we certainly don't want to block
        //     * any calling threads, especially the UI thread.  When the response comes back we want to send it back to the 
        //     * callback and as a convenience we'll marshal it to the UI thread if that is necessary.
        //     */
        //    restClient.ExecuteAsync(restRequest, restResponse => {

        //        if (csrfHeader == null && csrfToken == null)
        //        {
        //            foreach (var header in restResponse.Headers)
        //            {
        //                if (header.Name.Equals("X-CSRF-HEADER"))
        //                {
        //                    csrfHeader = (string)header.Value;
        //                }

        //                if (header.Name.Equals("X-CSRF-TOKEN"))
        //                {
        //                    csrfToken = (string)header.Value;
        //                }
        //            }
        //        }

        //        callback(restResponse);
        //    });
        //}
    }
}
