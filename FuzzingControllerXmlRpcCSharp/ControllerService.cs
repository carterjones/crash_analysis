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
    internal class ControllerService : XmlRpcListenerService
    {
        /// <summary>
        /// The node manager to be used by this service.
        /// </summary>
        private NodeManager nm = null;

        /// <summary>
        /// Initializes a new instance of the ControllerService class.
        /// </summary>
        /// <param name="nm">a reference to the node manager to be used by this service</param>
        internal ControllerService(ref NodeManager nm)
        {
            this.nm = nm;
        }

        /// <summary>
        /// An XML-RPC function that can be called to register a new node.
        /// </summary>
        /// <param name="addressStr">the IP address of the node</param>
        [XmlRpcMethod("register_fuzzing_node")]
        internal void RegisterFuzzingNode(string addressStr)
        {
            IPAddress address = null;
            if (!IPAddress.TryParse(addressStr, out address))
            {
                return;
            }

            Node node = this.nm.AddNode(address);
            node.Initialize();
        }
    }
}
