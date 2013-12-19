namespace FuzzingControllerXmlRpcCSharp
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using CookComputing.XmlRpc;

    /// <summary>
    /// A class that encapsulates the main program.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// A list of IP addresses of fuzzing nodes.
        /// </summary>
        private static string[] nodeAddresses = { "192.168.91.128",
                                                  "192.168.139.134",
                                                  "192.168.139.148",
                                                  "192.168.139.149",
                                                  "192.168.139.150",
                                                  "192.168.139.151",
                                                  "192.168.139.152",
                                                  "192.168.139.153" };

        /// <summary>
        /// The entry point of the program.
        /// </summary>
        /// <param name="args">command line arguments passed to the program</param>
        public static void Main(string[] args)
        {
            NodeManager nm = new NodeManager();
            ControllerService svc = new ControllerService(ref nm);
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://192.168.139.1:8000/");
            listener.Start();

            // Start thread handler.
            Thread handleRequests = new Thread(() => HandleIncomingRequests(listener, svc));
            handleRequests.Start();

            // Initialize nodes.
            InitializeNodes(nm);

            while (true)
            {
                Console.Write("> ");
                string userInput = Console.ReadLine();
                if (userInput.Equals("exit"))
                {
                    nm.CloseNodeListeners();
                    listener.Stop();
                    break;
                }
                else if (string.IsNullOrEmpty(userInput))
                {
                    continue;
                }
                else if (userInput.Equals("nodes"))
                {
                    nm.ListNodes();
                }
                else if (userInput.Equals("update"))
                {
                    nm.UpdateNodes();
                    nm.ListNodes();
                }
                else if (userInput.Equals("reconnect"))
                {
                    nm.ConnectNodes();
                }
                else if (userInput.Equals("software"))
                {
                    nm.ListSoftware();
                }
                else if (userInput.Equals("install"))
                {
                    nm.InstallBaseSoftware();
                }
                else if (userInput.Equals("deploy-vlc"))
                {
                    nm.DeployVlc();
                }
                else if (userInput.Equals("deploy-minifuzz"))
                {
                    nm.DeployMiniFuzz();
                }
                else if (userInput.Equals("help"))
                {
                    Console.WriteLine("available commands:");
                    Console.WriteLine("  help       - display this help documentation");
                    Console.WriteLine("  nodes      - list the nodes that have successfully connected to this controller");
                    Console.WriteLine("  update     - updates the status of each of the nodes");
                    Console.WriteLine("  reconnect  - reconnect any nodes that have been disconnected");
                    Console.WriteLine("  software   - show which necessary software is or is not installed nodes that are not connected");
                    Console.WriteLine("  install    - install necessary software to nodes that are not connected");
                    Console.WriteLine("  deploy-vlc - install a target to be fuzzed on all nodes that do not have it yet");
                    Console.WriteLine("  deploy-minifuzz - install minifuzz on all nodes that do not have it yet");
                }
            }
        }

        /// <summary>
        /// Process XML-RPC requests as they come in to the HTTP listener.
        /// </summary>
        /// <param name="listener">the HTTP listener</param>
        /// <param name="svc">the Controller service</param>
        private static void HandleIncomingRequests(HttpListener listener, XmlRpcListenerService svc)
        {
            while (true)
            {
                try
                {
                    HttpListenerContext context = listener.GetContext();
                    svc.ProcessRequest(context);
                }
                catch (HttpListenerException e)
                {
                    // Error code 995 is thrown when the listener is stopped. It is expected. Throw all other errors.
                    if (e.ErrorCode != 995)
                    {
                        throw e;
                    }

                    break;
                }
            }
        }

        /// <summary>
        /// Initializes all fuzzing nodes, causing them to connect back to the controller.
        /// </summary>
        /// <param name="svc">the Controller service</param>
        private static void InitializeNodes(NodeManager nm)
        {
            List<Node> nodes = new List<Node>();
            IPAddress address = null;

            // Add the nodes to the node manager.
            foreach (string addressStr in nodeAddresses)
            {
                if (IPAddress.TryParse(addressStr, out address))
                {
                    nodes.Add(nm.AddNode(address));
                }
                else
                {
                    Console.WriteLine("[-] Could not parse IP address: " + addressStr);
                }
            }

            // Initialize the nodes.
            nm.InitializeNodes();
        }
    }
}
