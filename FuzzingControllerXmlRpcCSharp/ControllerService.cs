namespace FuzzingControllerXmlRpcCSharp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using CookComputing.XmlRpc;

    /// <summary>
    /// An XML-RPC service that permits nodes to register with this controller.
    /// </summary>
    public class ControllerService : XmlRpcListenerService
    {
        /// <summary>
        /// The currently tracked list of nodes.
        /// </summary>
        private List<Node> nodes = new List<Node>();

        /// <summary>
        /// An XML-RPC function that can be called to register a new node.
        /// </summary>
        /// <param name="addressStr">the IP address of the node</param>
        [XmlRpcMethod("register_fuzzing_node")]
        public void RegisterFuzzingNode(string addressStr)
        {
            IPAddress address = null;
            if (!IPAddress.TryParse(addressStr, out address))
            {
                return;
            }

            Node node = this.AddNode(address);
            node.Initialize();
        }

        /// <summary>
        /// Add an uninitialized node to the list of nodes. If the node already exists, returns the existing node.
        /// </summary>
        /// <param name="address">the IP address of the node</param>
        public Node AddNode(IPAddress address)
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
        public void CloseNodeListeners()
        {
            foreach (Node node in this.nodes)
            {
                node.Close();
            }
        }

        /// <summary>
        /// Update the list of nodes to see which ones are online/offline/unknown.
        /// </summary>
        public void UpdateStatuses()
        {
            foreach (Node node in this.nodes)
            {
                node.UpdateStatus();
            }
        }

        /// <summary>
        /// Obtains a list of nodes that have been connected to the service.
        /// </summary>
        /// <returns>returns a list of nodes that have been connected to the service</returns>
        public IEnumerable<Node> GetNodes()
        {
            return this.nodes;
        }

        /// <summary>
        /// Reconnects nodes that are not online.
        /// </summary>
        public void ReconnectNodes()
        {
            foreach (Node node in this.nodes)
            {
                if (node.UpdateStatus() != Node.ConnectionStatus.Online)
                {
                    node.Connect();
                }
            }
        }
    }
}
