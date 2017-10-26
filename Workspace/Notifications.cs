using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using CometD.Bayeux;
using CometD.Bayeux.Client;
using CometD.Client;

namespace consoleagentappcsharp.Workspace
{
    public class Notifications
    {
        private BayeuxClient bayeuxClient;
        private CookieContainer cookieContainer = new CookieContainer();

        public event CometDEventHandler CometDEventReceived;
        public delegate void CometDEventHandler(IClientSessionChannel channel, IMessage message, BayeuxClient client);

        public Notifications()
        {
        }

        public void Initialize(string baseUrl, string apiKey, string workspaceSessionId)
        {
            var options = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "x-api-key", apiKey},
                { "Cookie", String.Format("{0}={1}", WorkspaceApi.SESSION_COOKIE, workspaceSessionId)}
            };

            /**
             * GWS currently only supports LongPolling as a method to receive events.
             * So tell the CometD library to negotiate a handshake with GWS and setup a LongPolling session.
             */
            GWSLongPollingTransport transport = new GWSLongPollingTransport(options, this.cookieContainer);

            bayeuxClient = new BayeuxClient(baseUrl + "/workspace/v3/notifications", transport);

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
    
        public void OnInitializationMessageReceived(IClientSessionChannel channel, IMessage message, BayeuxClient client)
        {
            if ( CometDEventReceived != null )
            {
                CometDEventReceived(channel, message, client);
            }
        }

        public void OnVoiceMessageReceived(IClientSessionChannel channel, IMessage message, BayeuxClient client)
        {
            if (CometDEventReceived != null)
            {
                CometDEventReceived(channel, message, client);
            }
        }

        public void Disconnect()
        {
            if (bayeuxClient != null && bayeuxClient.IsConnected)
            {
                bayeuxClient.Disconnect();
            } 
        }
    }
}
