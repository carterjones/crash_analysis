namespace FuzzingControllerXmlRpcCSharp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using CookComputing.XmlRpc;
    using System.Net;

    public class ControllerService : XmlRpcListenerService
    {
        List<Node> nodes = new List<Node>();

        [XmlRpcMethod("register_fuzzing_node")]
        public void RegisterFuzzingNode(string addressStr)
        {
            IPAddress address = null;
            if (!IPAddress.TryParse(addressStr, out address))
            {
                return;
            }

            this.AddNode(address);
        }

        public void AddNode(IPAddress address)
        {
            Node node = new Node(address);
            if (!nodes.Contains(node))
            {
                nodes.Add(node);
                node.Initialize();
            }
        }

        public void CloseNodeListeners()
        {
            foreach (Node node in this.nodes)
            {
                node.Close();
            }
        }

        public void UpdateStatuses()
        {
            foreach (Node node in nodes)
            {
                node.UpdateStatus();
            }
        }

        public List<Node> GetNodes()
        {
            return nodes;
        }

        public void ReconnectNodes()
        {
            foreach (Node node in nodes)
            {
                if (node.UpdateStatus() != Node.ConnectionStatus.Online)
                {
                    node.Connect();
                }
            }
        }
    }
}
