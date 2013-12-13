namespace FuzzingControllerXmlRpcCSharp
{
    using CookComputing.XmlRpc;

    public interface INodeService: IXmlRpcProxy
    {
        [XmlRpcMethod]
        string execute_command(string command);

        [XmlRpcMethod]
        void close();

        [XmlRpcMethod]
        bool ping();
    }
}
