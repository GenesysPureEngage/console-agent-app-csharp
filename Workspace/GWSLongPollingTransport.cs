using System;
using System.Collections.Generic;
using System.Net;
using CometD.Client.Transport;

namespace consoleagentappcsharp.Workspace
{
    public class GWSLongPollingTransport : LongPollingTransport
    {
        private IDictionary<string, object> options;
        private CookieContainer cookieContainer;
        private CookieCollection cookieCollection = new CookieCollection();

        public GWSLongPollingTransport(IDictionary<string, object> options, CookieContainer cookieContainer)
            : base(options)
        {
            this.options = options;
            this.cookieContainer = cookieContainer;

            foreach(Cookie cookie in cookieContainer.GetCookies(new Uri("https://api-usw1.genhtcc.com/workspace/v3/notifications")))
            {
                this.SetCookie(cookie);
            }
        }

        /// <summary>
        /// Returns the <see cref="Cookie"/> with a specific name from this HTTP transport.
        /// </summary>
        override public Cookie GetCookie(string name)
        {
            Cookie cookie = base.GetCookie(name);
            return cookie;
        }

        /// <summary>
        /// Adds a <see cref="Cookie"/> to this HTTP transport.
        /// </summary>
        override public void SetCookie(Cookie cookie)
        {
            Console.WriteLine("Adding cookie to CometD Cookies: " + cookie.Name + "=" + cookie.Value);
            this.cookieCollection.Add(cookie);

            base.SetCookie(cookie);
        }

        /// <summary>
        /// Setups HTTP request headers.
        /// </summary>
        override protected void ApplyRequestHeaders(HttpWebRequest request)
        {
            if (null == request)
                throw new ArgumentNullException("request");

            foreach(String key in this.options.Keys)
            {
                request.Headers[key] = (string)this.options[key];
            }
        }

        /// <summary>
        /// Setups HTTP request cookies.
        /// </summary>
        override protected void ApplyRequestCookies(HttpWebRequest request)
        {
            if (null == request)
                throw new ArgumentNullException("request");

            request.CookieContainer = new CookieContainer();

            foreach(Cookie c in this.cookieCollection)
            {
                request.CookieContainer.Add(new Cookie(c.Name, c.Value, "/", "api-usw1.genhtcc.com"));
            }
        }
    }
}
