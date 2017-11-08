using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Genesys.Authentication.Api;
using Genesys.Authentication.Client;
using Genesys.Authentication.Model;
using Genesys.Workspace;
using Genesys.Workspace.Model;
using RestSharp;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace consoleagentappcsharp
{
    public class Command
    {
        public String Name { get; set; }
        public List<String> Args;

        public Command(String name, List<String> args)
        {
            this.Name = name;
            this.Args = args;
        }
    }

    public class CompleteParams
    {
        public String ConnId { get; set; }
        public String ParentConnId { get; set; }

        public CompleteParams(String connId, String parentConnId)
        {
            this.ConnId = connId;
            this.ParentConnId = parentConnId;
        }
    }

    public class WorkspaceConsole
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Options options;
        private readonly WorkspaceApi api;
        private User user;

        public WorkspaceConsole(Options options)
        {
            this.options = options;
            api = new WorkspaceApi(options.ApiKey, options.BaseUrl);

            api.Voice().CallStateChanged += (call, message) => {
                this.Write("CallStateChanged: " + call.ToString());
            };

            api.Voice().DnStateChanged += (dn, message) => {
                this.Write("DnStateChanged: " + dn.ToString());
            };

            api.Voice().VoiceErrorReceived += (msg, code, message) => {
                this.Write("VoiceErrorRecevied: " + msg + " - code [" + code + "]");
            };
        }

        private void Write(String msg)
        {
            //String moveBack = "\u001b[50D";
            //Console.Write(moveBack);
            Console.WriteLine(msg);
        }

        private void Prompt()
        {
            //String moveBack = "\u001b[50D";
            //Console.Write(moveBack);
            Console.Write("cmd> ");
        }

        private void Prompt(String msg)
        {
            Console.Write(msg);
        }

        private Command ParseInput(String input)
        {
            String[] pieces = input.Split(' ');
            if (pieces.Length == 0)
            {
                return null;
            }

            String name = pieces[0].ToLower();
            List<String> args = new List<String>();
            if (pieces.Length > 1)
            {
                for (int i = 1; i < pieces.Length; i++)
                {
                    args.Add(pieces[i]);
                }
            }

            return new Command(name, args);
        }

        private void PrintHelp()
        {
            this.Write("Workspace Api Console commands:");
            this.Write("initialize|init|i");
            this.Write("destroy|logout|l");
            this.Write("activate-channels|ac <agentId> <dn>");
            this.Write("user|u");
            this.Write("configuration|c <type>");
            this.Write("dn");
            this.Write("calls");
            this.Write("ready|r");
            this.Write("not-ready|nr");
            this.Write("dnd-on");
            this.Write("dnd-off");
            this.Write("voice-login");
            this.Write("voice-logout");
            this.Write("set-forward <destination>");
            this.Write("cancel-forward");
            this.Write("make-call|mc <destination>");
            this.Write("answer|a <id>");
            this.Write("hold|h <id>");
            this.Write("retrieve|ret <id>");
            this.Write("release|rel <id>");
            this.Write("clear-call <id>");
            this.Write("redirect <id> <destination>");
            this.Write("initiate-conference|ic <id> <destination>");
            this.Write("complete-conference|cc <id> <parentConnId>");
            this.Write("initiate-transfer|it <id> <destination>");
            this.Write("complete-transfer|ct <id> <parentConnId>");
            this.Write("delete-from-conference|dfc <id> <dnToDrop>");
            this.Write("send-dtmf|dtmf <id> <digits>");
            this.Write("alternate|alt <id> <heldConnId>");
            this.Write("merge <id> <otherConnId>");
            this.Write("reconnect <id> <heldConnId>");
            this.Write("single-step-transfer <id> <destination>");
            this.Write("single-step-conference <id> <destination>");
            this.Write("attach-user-data|aud <id> <key> <value>");
            this.Write("update-user-data|uud <id> <key> <value>");
            this.Write("delete-user-data-pair|dp <id> <key>");
            this.Write("start-recording <id>");
            this.Write("pause-recording <id>");
            this.Write("resume-recording <id>");
            this.Write("stop-recording <id>");
            this.Write("send-user-event <key> <value> <callUuid>");
            this.Write("target-search|ts <searchTerm> <limit>");
            this.Write("clear|cls");
            this.Write("config|conf");
            this.Write("exit|x");
            this.Write("debug|d");
            this.Write("help|?");
            this.Write("");
            this.Write("Note: <id> parameter can be omitted for call operations if there is only one active call.");
            this.Write("");
        }

        private String GetCallSummary(Call call)
        {
            return call.ToString();
        }

        private String GetDnSummary(Dn dn)
        {
            return dn.ToString();
        }

        private String GetCallId(List<String> args)
        {
            // If we get an id as an argument use that
            if (args != null && args.Count == 1)
            {
                return args[0];
            }

            // Otherwise if there is only one call use that id.
            if ( this.api.Voice().Calls.Count != 0 )
            {
                return this.api.Voice().Calls[this.api.Voice().Calls.Keys.First()].id.ToString();
            }

            return null;
        }

        private CompleteParams GetCallIdAndParent(List<String> args)
        {
            if (args != null && args.Count == 2)
            {
                return new CompleteParams(args[0], args[1]);
            }

            // If ids were not provided, see if there is only one
            // possibility.
            CompleteParams completeParams = null;
            if (this.api.Voice().Calls.Count == 2)
            {
                Call call = null;

                foreach(Call c in this.api.Voice().Calls.Values)
                {
                    if ( c.parentConnId != null)
                    {
                        call = c;
                        break;
                    }
                }

                if (call != null)
                {
                    completeParams = new CompleteParams(call.id, call.parentConnId);
                }
            }

            return completeParams;
        }

        private String GetAuthToken()
        {
            log.Debug("Getting auth token...");
            String baseUrl = this.options.AuthBaseUrl ?? this.options.BaseUrl;
            ApiClient authClient = new ApiClient(baseUrl + "/auth/v3");
            authClient.Configuration.ApiClient = authClient;  // circular reference?!?
            authClient.Configuration.AddApiKey("x-api-key", this.options.ApiKey);
            authClient.Configuration.AddDefaultHeader("x-api-key", this.options.ApiKey);
            authClient.RestClient.AddDefaultHeader("x-api-key", this.options.ApiKey);
            
            String encoded = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(this.options.ClientId + ":" + this.options.ClientSecret));
            String authorization = "Basic " + encoded;

            AuthenticationApi authApi = new AuthenticationApi(authClient.Configuration);

            DefaultOAuth2AccessToken response = authApi.RetrieveToken(
                    "password", authorization, null, "*",
                    this.options.ClientId, null, this.options.Username, this.options.Password);

            return response.AccessToken;
        }

        private async Task Init()
        {
            try 
            {
                String token = this.GetAuthToken();
                if (token == null) {
                    throw new WorkspaceConsoleException("Failed to get auth token.");
                }

                this.Write("Initializing API...");

                this.user = await this.api.Initialize(token);

                this.Write("Initialization complete.");
                this.Write(user.ToString());
            }
            catch (Exception e)
            {
                log.Error("Failed to initialize", e);
            }
        }

        private void ActivateChannels(List<String> args)
        {
            bool hasArgs = (args != null && args.Count == 2);

            if (!hasArgs && (this.options.DefaultAgentId == null || this.options.DefaultDn == null)) {
                this.Write("Usage: activate-channels <agentId> <dn>");
                return;
            }

            String agentId = hasArgs ? args[0] : this.options.DefaultAgentId;
            String dn = hasArgs ? args[1] : this.options.DefaultDn;

            this.Write("Sending activate-channels with agentId [" + agentId + "] and dn " + dn + "]...");
            this.api.ActivateChannels(agentId, dn, null, null);
        }

        private void NotReady(List<String> args)
        {
            bool hasArgs = (args != null && args.Count > 0);

            if ( !hasArgs )
            {
                this.api.Voice().SetAgentNotReady();
                return;
            }

            string reasonCode = args.Count >= 1 ? args[0] : null;
            string workMode = args.Count >= 2 ? args[1] : null;

            this.api.Voice().SetAgentNotReady(workMode, reasonCode);
        }

        private async Task DoAutoLogin()
        {
            try
            {
                if (this.options.AutoLogin)
                {
                    this.Write("autoLogin is true...");
                    await this.Init();
                    this.ActivateChannels(null);
                }
            }
            catch (Exception e)
            {
                this.Write("autoLogin failed!" + e);
            }
        }


        private void MakeCall(List<String> args)
        {
            bool hasArgs = (args.Count > 0);
            if (!hasArgs && this.options.DefaultDestination == null) {
                this.Write("Usage: make-call <destination>");
                return;
            }

            String destination = hasArgs ? args[0] : this.options.DefaultDestination;
            this.Write("Sending make-call with destination [" + destination + "]...");
            this.api.Voice().MakeCall(destination);
        }

        private String GetBusinessAttributeSummary()
        {
            String summary = "Business Attributes:\n";

            if (this.api.BusinessAttributes.Count != 0)
            {
                foreach (BusinessAttribute businessAttribute in this.api.BusinessAttributes)
                {
                    summary += businessAttribute.ToString() + "\n";
                }
            }
            else
            {
                summary += "<none>\n";
            }

            return summary;
        }

        private String GetActionCodeSummary()
        {
            String summary = "Action Codes:\n";

            if ( this.api.ActionCodes.Count != 0)
            {
                foreach (ActionCode actionCode in this.api.ActionCodes)
                {
                    summary += actionCode.ToString() + "\n";
                }
            }
            else
            {
                summary += "<none>\n";
            }

            return summary;
        }

        private String GetSettingsSummary()
        {
            return "Settings:\n" + this.api.Settings == null ? "<none>" : this.api.Settings.ToString();
        }

        private String GetTransactionsSummary()
        {
            String summary = "Transactions:\n";

            if ( this.api.Transactions.Count != 0 )
            {
                foreach (Transaction transaction in this.api.Transactions)
                {
                    summary += transaction.ToString() + "\n";
                }
            }
            else
            {
                summary += "<none>\n";
            }

            return summary;
        }

        private String GetAgentGroupsSummary()
        {
            String summary = "Agent Groups:\n";

            if ( this.api.AgentGroups.Count != 0 )
            {
                foreach (AgentGroup agentGroup in this.api.AgentGroups)
                {
                    summary += agentGroup.ToString() + "\n";
                }
            }
            else
            {
                summary += "<none>\n";
            }

            return summary;
        }

        private String GetCallsSummary()
        {
            String summary = "Calls:\n";

            if (this.api.Voice().Calls.Count != 0)
            {
                foreach (string key in this.api.Voice().Calls.Keys)
                {
                    summary += this.api.Voice().Calls[key].ToString() + "\n";
                }
            }
            else
            {
                summary += "<none>\n";
            }

            return summary;
        }

        private void PrintConfiguration(List<String> args)
        {
            String type = args.Count == 1 ? args[0] : "all";

            String msg = "";
            switch (type)
            {
                case "action-codes":
                    msg += this.GetActionCodeSummary();
                    break;

                case "agent-groups":
                    msg += this.GetAgentGroupsSummary();
                    break;

                case "ba":
                    msg += this.GetBusinessAttributeSummary();
                    break;

                case "txn":
                    msg += this.GetTransactionsSummary();
                    break;

                case "settings":
                    msg += this.GetSettingsSummary();
                    break;

                case "all":
                    msg += this.GetActionCodeSummary() + "\n";
                    msg += this.GetAgentGroupsSummary() + "\n";
                    msg += this.GetBusinessAttributeSummary() + "\n";
                    msg += this.GetTransactionsSummary() + "\n";
                    msg += this.GetSettingsSummary() + "\n";
                    break;

                default:
                    msg = "Usage: configuration <type> where type is one of action-codes, agent-groups, ba, txn, settings";
                    break;
            }

            this.Write(msg);
        }

        public async Task Run()
        {
            try
            {
                this.Write("Workspace Api Console");
                this.Write("");
                await this.DoAutoLogin();

                for (;;)
                {
                    try
                    {
                        this.Prompt();
                        Command cmd = this.ParseInput(Console.ReadLine());
                        if (cmd == null)
                        {
                            continue;
                        }

                        List<string> args = cmd.Args;
                        string id;
                        string destination;
                        string key;
                        string value;
                        CompleteParams completeParams;

                        switch (cmd.Name)
                        {
                            case "initialize":
                            case "init":
                            case "i":
                                await this.Init();
                                break;

                            case "debug":
                            case "d":
                                this.api.DebugEnabled = !this.api.DebugEnabled;
                                this.Write("Debug enabled:" + this.api.DebugEnabled);
                                break;

                            case "dn":
                                this.Write("Dn: " + this.GetDnSummary(this.api.Voice().Dn));
                                break;

                            case "calls":
                                this.Write(this.GetCallsSummary());
                                break;

                            case "configuration":
                            case "c":
                                this.PrintConfiguration(args);
                                break;

                            case "activate-channels":
                            case "ac":
                                this.ActivateChannels(args);
                                break;

                            case "iac":
                                await this.Init();
                                this.ActivateChannels(args);
                                break;

                            case "not-ready":
                            case "nr":
                                this.Write("Sending not-ready...");
                                this.NotReady(args);
                                break;

                            case "ready":
                            case "r":
                                this.Write("Sending ready...");
                                this.api.Voice().SetAgentReady();
                                break;

                            case "dnd-on":
                                this.Write("Sending dnd-on...");
                                this.api.Voice().DndOn();
                                break;

                            case "dnd-off":
                                this.Write("Sending dnd-off...");
                                this.api.Voice().DndOff();
                                break;

                            case "set-forward":
                                if (args.Count < 1)
                                {
                                    this.Write("Usage: set-forward <destination>");
                                }
                                else
                                {
                                    this.Write("Sending set-forward with destination [" + args[0] + "]...");
                                    this.api.Voice().SetForward(args[0]);
                                }
                                break;

                            case "cancel-forward":
                                this.Write("Sending cancel-forward...");
                                this.api.Voice().CancelForward();
                                break;

                            case "voice-login":
                                this.Write("Sending voice login...");
                                this.api.Voice().Login();
                                break;

                            case "voice-logout":
                                this.Write("Sending voice logout...");
                                this.api.Voice().Logout();
                                break;

                            case "make-call":
                            case "mc":
                                this.MakeCall(args);
                                break;

                            case "release":
                            case "rel":
                                id = this.GetCallId(args);
                                if (id == null)
                                {
                                    this.Write("Usage: release <id>");
                                }
                                else
                                {
                                    this.Write("Sending release for call [" + id + "]...");
                                    this.api.Voice().ReleaseCall(id);
                                }
                                break;

                            case "answer":
                            case "a":
                                id = this.GetCallId(args);
                                if (id == null)
                                {
                                    this.Write("Usage: answer <id>");
                                }
                                else
                                {
                                    this.Write("Sending answer for call [" + id + "]...");
                                    this.api.Voice().AnswerCall(id);
                                }
                                break;

                            case "hold":
                            case "h":
                                id = this.GetCallId(args);
                                if (id == null)
                                {
                                    this.Write("Usage: hold <id>");
                                }
                                else
                                {
                                    this.Write("Sending hold for call [" + id + "]...");
                                    this.api.Voice().HoldCall(id);
                                }
                                break;

                            case "retrieve":
                            case "ret":
                                id = this.GetCallId(args);
                                if (id == null)
                                {
                                    this.Write("Usage: receive <id>");
                                }
                                else
                                {
                                    this.Write("Sending retrieve for call [" + id + "]...");
                                    this.api.Voice().RetrieveCall(id);
                                }
                                break;

                            case "clear-call":
                                id = this.GetCallId(args);
                                if (id == null)
                                {
                                    this.Write("Usage: clear-call <id>");
                                }
                                else
                                {
                                    this.Write("Sending clear for call [" + id + "]...");
                                    this.api.Voice().ClearCall(id);
                                }
                                break;

                            case "redirect":
                                if (args.Count < 1)
                                {
                                    this.Write("Usage: redirect <id> <destination>");
                                }
                                else
                                {
                                    // If there is only one argument take it as the destination.
                                    destination = args[args.Count - 1];
                                    id = this.GetCallId(args.Count == 1 ? null : args);
                                    if (id == null)
                                    {
                                        this.Write("Usage: redirect <id> <destination>");
                                    }
                                    else
                                    {
                                        this.Write("Sending redirect for call [" + id
                                                + "] and destination [" + destination + "]...");
                                        this.api.Voice().RedirectCall(id, destination);
                                    }
                                }
                                break;

                            case "initiate-conference":
                            case "ic":
                                if (args.Count < 1)
                                {
                                    this.Write("Usage: initiate-conference <id> <destination>");
                                }
                                else
                                {
                                    // If there is only one argument take it as the destination.
                                    destination = args[args.Count - 1];
                                    id = this.GetCallId(args.Count == 1 ? null : args);
                                    if (id == null)
                                    {
                                        this.Write("Usage: initiate-conference <id> <destination>");
                                    }
                                    else
                                    {
                                        this.Write("Sending initiate-conference for call [" + id
                                                + "] and destination [" + destination + "]...");
                                        this.api.Voice().InitiateConference(id, destination);
                                    }
                                }
                                break;

                            case "complete-conference":
                            case "cc":
                                completeParams = this.GetCallIdAndParent(args);
                                if (completeParams == null) 
                                {
                                    this.Write("Usage: complete-conference <id> <parentConnId>");
                                } 
                                else 
                                {
                                    this.Write("Sending complete-conference for call ["
                                               + completeParams.ConnId + "] and parentConnId ["
                                               + completeParams.ParentConnId + "]...");
                                    
                                    this.api.Voice().CompleteConference(completeParams.ConnId, 
                                                                        completeParams.ParentConnId);
                                }
                                break;

                            case "delete-from-conference":
                            case "dfc":
                                if (args.Count < 1)
                                {
                                    this.Write("Usage: delete-from-conference <id> <dnToDrop>");
                                }
                                else
                                {
                                    // If there is only one argument take it as the dn to drop.
                                    String dnToDrop = args[args.Count - 1];
                                    id = this.GetCallId(args.Count == 1 ? null : args);
                                    if (id == null)
                                    {
                                        this.Write("Usage: delete-from-conference <id> <dnToDrop>");
                                    }
                                    else
                                    {
                                        this.Write("Sending delete-from-conference for call [" + id
                                                + " and dnToDrop [" + dnToDrop + "]...");
                                        this.api.Voice().DeleteFromConference(id, dnToDrop);
                                    }
                                }
                                break;


                            case "initiate-transfer":
                            case "it":
                                if (args.Count < 1)
                                {
                                    this.Write("Usage: initiate-transfer <id> <destination>");
                                }
                                else
                                {
                                    // If there is only one argument take it as the destination.
                                    destination = args[args.Count - 1];
                                    id = this.GetCallId(args.Count == 1 ? null : args);
                                    if (id == null)
                                    {
                                        this.Write("Usage: initiate-transfer <id> <destination>");
                                    }
                                    else
                                    {
                                        this.Write("Sending initiate-transfer for call [" + id
                                                + "] and destination [" + destination + "]...");
                                        this.api.Voice().InitiateTransfer(id, destination);
                                    }
                                }
                                break;

                            case "complete-transfer":
                            case "ct":
                                completeParams = this.GetCallIdAndParent(args);
                                if (completeParams == null) 
                                {
                                    this.Write("Usage: complete-transfer <id> <parentConnId>");
                                } 
                                else 
                                {
                                    this.Write("Sending complete-transfer for call ["
                                                + completeParams.ConnId + "] and parentConnId ["
                                                + completeParams.ParentConnId + "]...");

                                    this.api.Voice().CompleteTransfer(completeParams.ConnId, 
                                                                      completeParams.ParentConnId);
                                }
                                break;


                            case "single-step-transfer":
                            case "sst":
                                if (args.Count < 1)
                                {
                                    this.Write("Usage: single-step-transfer <id> <destination>");
                                }
                                else
                                {
                                    // If there is only one argument take it as the destination.
                                    destination = args[args.Count - 1];
                                    id = this.GetCallId(args.Count == 1 ? null : args);
                                    if (id == null)
                                    {
                                        this.Write("Usage: single-step-transfer <id> <destination>");
                                    }
                                    else
                                    {
                                        this.Write("Sending single-step-transfer for call [" + id
                                                + "] and destination [" + destination + "]...");
                                        this.api.Voice().SingleStepTransfer(id, destination);
                                    }
                                }
                                break;

                            case "single-step-conference":
                            case "ssc":
                                if (args.Count < 1)
                                {
                                    this.Write("Usage: single-step-conference <id> <destination>");
                                }
                                else
                                {
                                    // If there is only one argument take it as the destination.
                                    destination = args[args.Count - 1];
                                    id = this.GetCallId(args.Count == 1 ? null : args);
                                    if (id == null)
                                    {
                                        this.Write("Usage: single-step-conference <id> <destination>");
                                    }
                                    else
                                    {
                                        this.Write("Sending single-step-conference for call [" + id
                                                + "] and destination [" + destination + "]...");
                                        this.api.Voice().SingleStepConference(id, destination);
                                    }
                                }
                                break;

                            case "attach-user-data":
                            case "aud":
                                if (args.Count < 3)
                                {
                                    this.Write("Usage: attach-user-data <id> <key> <value>");
                                }
                                else
                                {
                                    id = args[0];
                                    key = args[1];
                                    value = args[2];

                                    this.Write("Sending attach-user-data for call [" + id
                                            + "] and data [" + key + "=" + value + "]...");

                                    KeyValueCollection userData = new KeyValueCollection();
                                    userData.addString(key, value);
                                    this.api.Voice().AttachUserData(id, userData);
                                }
                                break;

                            case "update-user-data":
                            case "uud":
                                if (args.Count < 3)
                                {
                                    this.Write("Usage: update-user-data <id> <key> <value>");
                                }
                                else
                                {
                                    id = args[0];
                                    key = args[1];
                                    value = args[2];

                                    this.Write("Sending update-user-data for call [" + id
                                            + "] and data [" + key + "=" + value + "]...");

                                    KeyValueCollection userData = new KeyValueCollection();
                                    userData.addString(key, value);
                                    this.api.Voice().UpdateUserData(id, userData);
                                }
                                break;

                            case "delete-user-data-pair":
                            case "dp":
                                if (args.Count < 1)
                                {
                                    this.Write("Usage: delete-user-data-pair <id> <key>");
                                }
                                else
                                {
                                    // If there is only one argument take it as the destination.
                                    key = args[args.Count - 1];
                                    id = this.GetCallId(args.Count == 1 ? null : args);
                                    if (id == null)
                                    {
                                        this.Write("Usage: delete-user-data-pair <id> <key>");
                                    }
                                    else
                                    {
                                        this.Write("Sending delete-user-data-pair for call [" + id
                                                + " and key [" + key + "]...");
                                        this.api.Voice().DeleteUserDataPair(id, key);
                                    }
                                }
                                break;

                            case "alternate":
                            case "alt":
                                if (args.Count < 2)
                                {
                                    this.Write("Usage: alternate <id> <heldConnId>");
                                }
                                else
                                {
                                    this.Write("Sending alternate for call ["
                                            + args[0] + "] and heldConnId ["
                                            + args[1] + "]...");
                                    this.api.Voice().AlternateCalls(args[0], args[1]);
                                }
                                break;

                            case "merge":
                                if (args.Count < 2)
                                {
                                    this.Write("Usage: merge <id> <otherConnId>");
                                }
                                else
                                {
                                    this.Write("Sending merge for call ["
                                            + args[0] + "] and otherConnId ["
                                            + args[1] + "]...");
                                    this.api.Voice().MergeCalls(args[0], args[1]);
                                }
                                break;

                            case "reconnect":
                                if (args.Count < 2)
                                {
                                    this.Write("Usage: reconnect <id> <heldConnId>");
                                }
                                else
                                {
                                    this.Write("Sending reconnect for call ["
                                            + args[0] + "] and heldConnId ["
                                            + args[1] + "]...");
                                    this.api.Voice().ReconnectCall(args[0], args[1]);
                                }
                                break;

                            case "send-dtmf":
                            case "dtmf":
                                if (args.Count < 1)
                                {
                                    this.Write("Usage: send-dtmf <id> <digits>");
                                }
                                else
                                {
                                    // If there is only one argument take it as the dtmf digits.
                                    String digits = args[args.Count - 1];
                                    id = this.GetCallId(args.Count == 1 ? null : args);
                                    if (id == null)
                                    {
                                        this.Write("Usage: send-dtmf <id> <digits>");
                                    }
                                    else
                                    {
                                        this.Write("Sending send-dtmf for call [" + id
                                                + " and dtmfDigits [" + digits + "]...");
                                        this.api.Voice().SendDTMF(id, digits);
                                    }
                                }
                                break;

                            case "start-recording":
                                id = this.GetCallId(args);
                                if (id == null)
                                {
                                    this.Write("Usage: start-recording <id>");
                                }
                                else
                                {
                                    this.Write("Sending start-recording for call [" + id + "]...");
                                    this.api.Voice().StartRecording(id);
                                }
                                break;

                            case "pause-recording":
                                id = this.GetCallId(args);
                                if (id == null)
                                {
                                    this.Write("Usage: pause-recording <id>");
                                }
                                else
                                {
                                    this.Write("Sending pause-recording for call [" + id + "]...");
                                    this.api.Voice().PauseRecording(id);
                                }
                                break;

                            case "resume-recording":
                                id = this.GetCallId(args);
                                if (id == null)
                                {
                                    this.Write("Usage: resume-recording <id>");
                                }
                                else
                                {
                                    this.Write("Sending resume-recortding for call [" + id + "]...");
                                    this.api.Voice().ResumeRecording(id);
                                }
                                break;

                            case "stop-recording":
                                id = this.GetCallId(args);
                                if (id == null)
                                {
                                    this.Write("Usage: stop-recording <id>");
                                }
                                else
                                {
                                    this.Write("Sending stop-recording for call [" + id + "]...");
                                    this.api.Voice().StopRecording(id);
                                }
                                break;

                            case "send-user-event":
                            if (args.Count < 3)
                            {
                                this.Write("Usage: send-user-event <key> <value> <callUuid>");
                            }
                            else
                            {
                                // If there are only two arguments take them as the key/value.
                                key = args[0];
                                value = args[1];
                                String uuid = args[2];

                                this.Write("Sending send-user-event with data [" + key + "=" + value
                                        + "] and callUuid [" + uuid + "...");

                                KeyValueCollection userData = new KeyValueCollection();
                                userData.addString(key, value);
                                this.api.Voice().SendUserEvent(null, uuid);
                            }
                            break;

                            case "target-search":
                            case "ts":
                                if (args.Count < 1)
                                {
                                    this.Write("Usage: target-search <search term>");
                                }
                                else
                                {
                                    TargetSearchResult result = this.api.Targets().search(args[0]);
                                    String resultMsg = "Search results:\n";
                                    if (result.Targets != null && result.Targets.Count != 0)
                                    {
                                        foreach (Target target in result.Targets)
                                        {
                                            resultMsg += "    " + target + "\n";
                                        }
                                        resultMsg += "Total matches: " + result.TotalMatches;
                                    }
                                    else
                                    {
                                        resultMsg += "<none>\n";
                                    }

                                    this.Write(resultMsg);
                                }
                                break;

                            case "destroy":
                            case "logout":
                                this.Write("Cleaning up and logging out...");
                                this.api.Destroy();
                                break;

                            case "user":
                            case "u":
                                if (this.api.User != null)
                                {
                                    this.Write("User details:\n" + this.api.User.ToString() + "\n");
                                }
                                break;

                            case "console-config":
                                this.Write("Configuration:\n"
                                    + "apiKey: " + this.options.ApiKey + "\n"
                                    + "baseUrl: " + this.options.BaseUrl + "\n"
                                    + "clientId: " + this.options.ClientId + "\n"
                                    + "username: " + this.options.Username + "\n"
                                    + "password: " + this.options.Password + "\n"
                                    + "debugEnabled: " + this.options.DebugEnabled + "\n"
                                    + "autoLogin: " + this.options.AutoLogin + "\n"
                                    + "defaultAgentId: " + this.options.DefaultAgentId + "\n"
                                    + "defaultDn: " + this.options.DefaultDn + "\n"
                                    + "defaultDestination: " + this.options.DefaultDestination + "\n"
                                    );
                                break;

                            case "clear":
                            case "cls":
                                // Low tech...
                                for (int i = 0; i < 80; ++i) this.Write("");
                                break;

                            case "exit":
                            case "x":
                                this.Write("Cleaning up and exiting...");
                                this.api.Destroy();
                                return;

                            case "?":
                            case "help":
                                this.PrintHelp();
                                break;

                            default:
                                break;

                        }
                    }
                    catch(Exception e)
                    {
                        Write("Exception!" + e.Message);
                    }
                }
            } 
            catch(Exception e) 
            {
                Write("Exception!" + e.Message);
            }
        }
    }
}
