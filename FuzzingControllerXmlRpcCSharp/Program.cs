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
        private static string[] nodeAddresses = { "192.168.139.148" };

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
                    svc.ReconnectNodes();
                }
                else if (userInput.Equals("help"))
                {
                    Console.WriteLine("available commands:");
                    Console.WriteLine("  nodes     - list the nodes that have successfully connected to this controller");
                    Console.WriteLine("  update    - updates the status of each of the nodes");
                    Console.WriteLine("  reconnect - reconnect any nodes that have been disconnected");
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
            IPAddress address = null;

            foreach (string addressStr in nodeAddresses)
            {
                if (IPAddress.TryParse(addressStr, out address))
                {
                    svc.AddNode(address);
                }
                else
                {
                    Console.WriteLine("[-] Could not parse IP address: " + addressStr);
                }
            }
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
    }
}
