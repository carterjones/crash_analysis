namespace FuzzingControllerXmlRpcCSharp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    internal class NodeManager
    {
        /// <summary>
        /// The currently tracked list of nodes.
        /// </summary>
        private List<Node> nodes = new List<Node>();

        /// <summary>
        /// Add an uninitialized node to the list of nodes. If the node already exists, returns the existing node.
        /// </summary>
        /// <param name="address">the IP address of the node</param>
        internal Node AddNode(IPAddress address)
        {
            Node node = new Node(address);
            Node existingNode = this.nodes.Find(x => x.Equals(node));
            if (existingNode == null)
            {
                this.nodes.Add(node);
                return node;
            }
            else
            {
                return existingNode;
            }
        }

        /// <summary>
        /// Shut down the servers listening on each of the nodes.
        /// </summary>
        /// TODO: make this threaded
        internal void CloseNodeListeners()
        {
            foreach (Node node in this.nodes)
            {
                node.Close();
            }
        }

        /// <summary>
        /// Reconnects nodes that are not online.
        /// </summary>
        /// TODO: make this threaded
        internal void ReconnectNodes()
        {
            foreach (Node node in this.nodes)
            {
                if (node.UpdateStatus() != Node.ConnectionStatus.Online)
                {
                    node.Connect();
                }
            }
        }

        internal void InitializeNodes()
        {
            PerformActionOnNodesInParallel(n => n.Initialize());
        }

        /// <summary>
        /// List the base software installed on a node.
        /// </summary>
        internal void ListSoftware()
        {
            PerformActionOnNodesInParallel(n => { n.CheckForBaseInstallations(); n.UpdateStatus(); });

            foreach (Node node in this.nodes)
            {
                if (!node.IsConnectedToController())
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

        /// <summary>
        /// Prints the tracked nodes out to the console.
        /// </summary>
        internal void ListNodes()
        {
            Console.WriteLine("Status | IP Address");
            foreach (Node node in this.nodes)
            {
                Console.WriteLine("  [" + node.Status.DescriptionAttr() + "]  | " + node.Address.ToString());
            }
        }

        internal void UpdateNodes()
        {
            PerformActionOnNodesInParallel(n => n.UpdateStatus());
        }

        internal void ConnectNodes()
        {
            PerformActionOnNodesInParallel(n => n.Connect());
        }

        internal void InstallBaseSoftware()
        {
            PerformActionOnNodesInParallel(n => n.InstallBaseSoftware());
        }

        internal void DeployVlc()
        {
            PerformActionOnNodesInParallel(n => n.DeployVlc());
        }

        internal void DeployMiniFuzz()
        {
            PerformActionOnNodesInParallel(n => n.DeployMiniFuzz());
        }

        private void PerformActionOnNodesInParallel(Action<Node> a)
        {
            List<Task> tasks = new List<Task>();
            foreach (Node node in this.nodes)
            {
                tasks.Add(Task.Run(() => a(node)));
            }

            Task.WaitAll(tasks.ToArray());
        }
    }
}
