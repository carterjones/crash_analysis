namespace FuzzingControllerXmlRpcCSharp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Performs bulk operations on a collection of nodes.
    /// </summary>
    internal class NodeManager
    {
        #region Fields

        /// <summary>
        /// The currently tracked list of nodes.
        /// </summary>
        private List<Node> nodes = new List<Node>();

        #endregion

        #region Internal Methods

        /// <summary>
        /// Add an uninitialized node to the list of nodes. If the node already exists, returns the existing node.
        /// </summary>
        /// <param name="address">the IP address of the node</param>
        /// <returns>the node that was added, or the matching pre-existing node</returns>
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
        /// Shut down the servers listening on all nodes controlled by this manager.
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
        /// Reconnects offline nodes controlled by this manager.
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

        /// <summary>
        /// Initialize all nodes controlled by this manager.
        /// </summary>
        internal void InitializeNodes()
        {
            this.PerformActionOnNodesInParallel(n => n.Initialize());
        }

        /// <summary>
        /// List the base software installed on all nodes controlled by this manager.
        /// </summary>
        internal void ListSoftware()
        {
            this.PerformActionOnNodesInParallel(n => { n.CheckForBaseInstallations(); n.UpdateStatus(); });

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
        /// Prints the IP address of all nodes controlled by this manager.
        /// </summary>
        internal void ListNodes()
        {
            Console.WriteLine("Status | IP Address");
            foreach (Node node in this.nodes)
            {
                Console.WriteLine("  [" + node.Status.DescriptionAttr() + "]  | " + node.Address.ToString());
            }
        }

        /// <summary>
        /// Update the status of all nodes controlled by this manager.
        /// </summary>
        internal void UpdateStatusOfNodes()
        {
            this.PerformActionOnNodesInParallel(n => n.UpdateStatus());
        }

        /// <summary>
        /// Cause all nodes controlled by this manager to connect to this controller.
        /// </summary>
        internal void ConnectNodes()
        {
            this.PerformActionOnNodesInParallel(n => n.Connect());
        }

        /// <summary>
        /// Install necessary software to all nodes controlled by this manager.
        /// </summary>
        internal void InstallBaseSoftware()
        {
            this.PerformActionOnNodesInParallel(n => n.InstallBaseSoftware());
        }

        /// <summary>
        /// Deploy VLC to all nodes controlled by this manager.
        /// </summary>
        internal void DeployVlc()
        {
            this.PerformActionOnNodesInParallel(n => n.DeployVlc());
        }

        /// <summary>
        /// Deploy MiniFuzz to all nodes controlled by this manager.
        /// </summary>
        internal void DeployMiniFuzz()
        {
            this.PerformActionOnNodesInParallel(n => n.DeployMiniFuzz());
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Execute a node function on all nodes controlled by this manager.
        /// </summary>
        /// <param name="a">the function call to be performed</param>
        private void PerformActionOnNodesInParallel(Action<Node> a)
        {
            List<Task> tasks = new List<Task>();
            foreach (Node node in this.nodes)
            {
                tasks.Add(Task.Run(() => a(node)));
            }

            Task.WaitAll(tasks.ToArray());
        }

        #endregion
    }
}
