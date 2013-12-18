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
        private static string[] nodeAddresses = { //"192.168.139.134",
                                                  "192.168.139.148",
                                                  "192.168.139.149",
                                                  "192.168.139.150"};//,
                                                  //"192.168.139.151",
                                                  //"192.168.139.152",
                                                  //"192.168.139.153" };

        /// <summary>
        /// The entry point of the program.
        /// </summary>
        /// <param name="args">command line arguments passed to the program</param>
        public static void Main(string[] args)
        {
            ControllerService svc = new ControllerService();
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://192.168.139.1:8000/");
            listener.Start();

            // Start thread handler.
            Thread handleRequests = new Thread(() => HandleIncomingRequests(listener, svc));
            handleRequests.Start();

            // Initialize nodes.
            InitializeNodes(svc);

            while (true)
            {
                Console.Write("> ");
                string userInput = Console.ReadLine();
                if (userInput.Equals("exit"))
                {
                    svc.CloseNodeListeners();
                    listener.Stop();
                    break;
                }
                else if (string.IsNullOrEmpty(userInput))
                {
                    continue;
                }
                else if (userInput.Equals("nodes"))
                {
                    ListNodes(svc);
                }
                else if (userInput.Equals("update"))
                {
                    svc.UpdateStatuses();
                    ListNodes(svc);
                }
                else if (userInput.Equals("reconnect"))
                {
                    PerformActionOnNodesInParallel(n => n.Connect(), svc.GetNodes());
                }
                else if (userInput.Equals("software"))
                {
                    ListSoftware(svc.GetNodes());
                }
                else if (userInput.Equals("install"))
                {
                    PerformActionOnNodesInParallel(n => n.InstallBaseSoftware(), svc.GetNodes());
                }
                else if (userInput.Equals("deploy-vlc"))
                {
                    PerformActionOnNodesInParallel(n => n.DeployVlc(), svc.GetNodes());
                }
                else if (userInput.Equals("deploy-minifuzz"))
                {
                    PerformActionOnNodesInParallel(n => n.DeployMiniFuzz(), svc.GetNodes());
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
        private static void InitializeNodes(ControllerService svc)
        {
            List<Node> nodes = new List<Node>();
            IPAddress address = null;

            // Add the nodes to the controller.
            foreach (string addressStr in nodeAddresses)
            {
                if (IPAddress.TryParse(addressStr, out address))
                {
                    nodes.Add(svc.AddNode(address));
                }
                else
                {
                    Console.WriteLine("[-] Could not parse IP address: " + addressStr);
                }
            }

            // Initialize the nodes.
            PerformActionOnNodesInParallel(n => n.Initialize(), svc.GetNodes());
        }

        /// <summary>
        /// Prints the tracked nodes out to the console.
        /// </summary>
        /// <param name="svc">the Controller service</param>
        private static void ListNodes(ControllerService svc)
        {
            Console.WriteLine("Status | IP Address");
            foreach (Node node in svc.GetNodes())
            {
                Console.WriteLine("  [" + node.Status.DescriptionAttr() + "]  | " + node.Address.ToString());
            }
        }

        /// <summary>
        /// List the base software installed on a node.
        /// </summary>
        /// <param name="svc">the controller service</param>
        private static void ListSoftware(IEnumerable<Node> nodes)
        {
            PerformActionOnNodesInParallel(n => { n.CheckForBaseInstallations(); n.UpdateStatus(); }, nodes);

            foreach (Node node in nodes)
            {
                if (!node.IsOnline())
                {
                    Console.WriteLine(node.Address.ToString() + ":");
                    Console.WriteLine("  python:       " + node.PythonInstalled.DescriptionAttr());
                    Console.WriteLine("  psutil:       " + node.PsutilInstalled.DescriptionAttr());
                    Console.WriteLine("  windbg:       " + node.WindbgInstalled.DescriptionAttr());
                    Console.WriteLine("  !exploitable: " + node.BangExploitableInstalled.DescriptionAttr());
                    Console.WriteLine("  AutoIt:       " + node.AutoitInstalled.DescriptionAttr());
                    Console.WriteLine();
                }
            }
        }

        private static void PerformActionOnNodesInParallel(Action<Node> a, IEnumerable<Node> nodes)
        {
            List<Task> tasks = new List<Task>();
            foreach (Node node in nodes)
            {
                tasks.Add(Task.Run(() => a(node)));
            }

            Task.WaitAll(tasks.ToArray());
        }
    }
}
