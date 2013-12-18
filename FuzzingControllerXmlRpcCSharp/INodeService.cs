namespace FuzzingControllerXmlRpcCSharp
{
    using CookComputing.XmlRpc;

    /// <summary>
    /// The interface for the XML-RPC service that is run on each node.
    /// </summary>
    public interface INodeService : IXmlRpcProxy
    {
        /// <summary>
        /// Execute a command on the node.
        /// </summary>
        /// <param name="command">the command to be executed</param>
        /// <returns>the output of the command that was run</returns>
        [XmlRpcMethod("execute_command")]
        string ExecuteCommand(string command);

        /// <summary>
        /// Closes the listening server on the node.
        /// </summary>
        [XmlRpcMethod("close")]
        void Close();

        /// <summary>
        /// Tests to make sure the connection to the node is still alive.
        /// </summary>
        /// <returns>always returns true</returns>
        [XmlRpcMethod("test_connection")]
        bool TestConnection();
    }
}
