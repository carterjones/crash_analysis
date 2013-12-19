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

            // Add the nodes to the node manager.
            ReadIpAddressesFromFile("ip_list.txt").ToList().ForEach(n => nm.AddNode(n));

            // Initialize the nodes.
            nm.InitializeNodes();

            Interpreter i = new Interpreter();
            i.AddCommand(
                "exit",
                "exits the console, closing out any connections to this controller",
                () => { nm.CloseNodeListeners(); listener.Stop(); i.Stop(); });
            i.AddCommand(
                "nodes",
                "list the nodes that have successfully connected to this controller",
                () => nm.ListNodes());
            i.AddCommand(
                "update",
                "updates the status of each of the nodes",
                () => { nm.UpdateStatusOfNodes(); nm.ListNodes(); });
            i.AddCommand(
                "reconnect",
                "reconnect any nodes that have been disconnected",
                () => nm.ConnectNodes());
            i.AddCommand(
                "software",
                "show which necessary software is or is not installed nodes that are not connected",
                () => nm.ListSoftware());
            i.AddCommand(
                "install",
                "install necessary software to nodes that are not connected",
                () => nm.InstallBaseSoftware());
            i.AddCommand(
                "deploy-vlc",
                "install a target to be fuzzed on all nodes that do not have it yet",
                () => nm.DeployVlc());
            i.AddCommand(
                "deploy-minifuzz",
                "install minifuzz on all nodes that do not have it yet",
                () => nm.DeployMiniFuzz());
            i.Start();
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
        /// Reads a list of IP addresses from a file.
        /// </summary>
        /// <param name="filename">a file containing IP addresses separated by newline</param>
        /// <returns>an enumerable containing IP addresses read from the file</returns>
        private static IEnumerable<IPAddress> ReadIpAddressesFromFile(string filename)
        {
            IPAddress address = null;

            foreach (string addressStr in File.ReadAllLines(filename))
            {
                if (IPAddress.TryParse(addressStr, out address))
                {
                    yield return address;
                }
                else
                {
                    Console.WriteLine("[-] Could not parse IP address: " + addressStr);
                }
            }
        }
    }
}
