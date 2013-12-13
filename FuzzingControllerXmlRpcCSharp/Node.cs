namespace FuzzingControllerXmlRpcCSharp
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using CookComputing.XmlRpc;

    /// <summary>
    /// Represents a fuzzing machine (node).
    /// </summary>
    public class Node
    {
        /// <summary>
        /// The username for this node.
        /// </summary>
        private const string Username = "admin";

        /// <summary>
        /// The password for this node.
        /// </summary>
        private const string Password = "1";

        /// <summary>
        /// An interface to the service for communication purposes.
        /// </summary>
        private INodeService service;

        /// <summary>
        /// Initializes a new instance of the Node class.
        /// </summary>
        /// <param name="address">the IP address of the node</param>
        public Node(IPAddress address)
        {
            this.Address = address;
            this.Status = ConnectionStatus.Unknown;
            this.service = XmlRpcProxyGen.Create<INodeService>();
            this.service.Url = "http://" + address.ToString() + ":8000/RPC2";
        }

        /// <summary>
        /// A collection of connection statuses that represent various states of the node.
        /// </summary>
        public enum ConnectionStatus
        {
            /// <summary>
            /// Represents an unknown state of the connection.
            /// </summary>
            [DescriptionAttribute("?")]
            Unknown,

            /// <summary>
            /// Represents an online connection.
            /// </summary>
            [DescriptionAttribute("+")]
            Online,

            /// <summary>
            /// Represents an offline connection.
            /// </summary>
            [DescriptionAttribute("-")]
            Offline
        }

        /// <summary>
        /// Gets the IP address of the node.
        /// </summary>
        public IPAddress Address { get; private set; }

        /// <summary>
        /// Gets the last cached status of the connection.
        /// </summary>
        public ConnectionStatus Status { get; private set; }

        /// <summary>
        /// Tests if the node is online, offline, or something unknown.
        /// </summary>
        /// <returns>the result of the test</returns>
        public ConnectionStatus UpdateStatus()
        {
            try
            {
                this.service.Ping();
                this.Status = ConnectionStatus.Online;
            }
            catch (WebException)
            {
                this.Status = ConnectionStatus.Offline;
            }
            catch (Exception)
            {
                this.Status = ConnectionStatus.Unknown;
            }

            return this.Status;
        }

        /// <summary>
        /// Executes a system command on the node.
        /// </summary>
        /// <param name="command">the command to be executed</param>
        /// <returns>the output of the command</returns>
        public string ExecuteCommand(string command)
        {
            return this.service.ExecuteCommand(command);
        }

        /// <summary>
        /// Close the listening server on the node.
        /// </summary>
        public void Close()
        {
            try
            {
                this.service.Close();
            }
            catch
            {
            }
        }

        /// <summary>
        /// Remotely start the node server that connects back to this controller.
        /// </summary>
        public void Connect()
        {
            PsExec(this.Address, @"-i -d ""C:\Python27\python.exe"" ""C:\fuzzing_tools\node_server.py""");
        }

        /// <summary>
        /// Initializes the scripts to be run on a node. Connects the node to this controller.
        /// </summary>
        public void Initialize()
        {
            // Create the fuzzing_tools directory on the target system if it does not exist.
            PsExec(this.Address, @"-w c:\ -d cmd /c mkdir fuzzing_tools");

            // Share the directory on the remote system.
            PsExec(this.Address, @"net share shared=""C:\fuzzing_tools""");

            // Connect a volume on localhost to the directory on the remote system.
            ExecuteLocalCommand(@"net use z: \\" + this.Address.ToString() + @"\shared /user:" + Username + " " + Password);

            // Copy the node server script to the remote system.
            File.Copy(@"..\..\..\node_server.py", @"z:\node_server.py", true);
            File.Copy(@"..\..\..\start_minifuzz.au3", @"z:\start_minifuzz.au3", true);
            File.Copy(@"..\..\..\stop_minifuzz.au3", @"z:\stop_minifuzz.au3", true);
            File.Copy(@"..\..\..\triage.py", @"z:\triage.py", true);

            // Delete the volume on localhost that is connected to the directory on the remote system.
            ExecuteLocalCommand("net use z: /delete");

            // Unshare the directory on the remote system.
            PsExec(this.Address, "net share shared /delete");

            // Start the node server on the remote system.
            this.Connect();
        }

        /// <summary>
        /// Calculates a hash to represent the identity of this object.
        /// </summary>
        /// <returns>the hash representation of this object</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash = (hash * 23) + this.Address.GetHashCode();

                return hash;
            }
        }

        /// <summary>
        /// Determines if the supplied object is the same as the current object.
        /// </summary>
        /// <param name="obj">the object to be tested</param>
        /// <returns>true if the supplied object is the same as the current object</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != typeof(Node))
            {
                return false;
            }

            return this.Equals((Node)obj);
        }

        /// <summary>
        /// Determines if the supplied object is the same as the current object.
        /// </summary>
        /// <param name="obj">the object to be tested</param>
        /// <returns>true if the supplied object is the same as the current object</returns>
        public bool Equals(Node obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return this.Address.Equals(obj.Address);
        }

        /// <summary>
        /// Execute a system command locally.
        /// </summary>
        /// <param name="command">the command to be executed</param>
        /// <param name="showOutput">true if the command's output should be shown</param>
        private static void ExecuteLocalCommand(string command, bool showOutput = false)
        {
            if (string.IsNullOrEmpty(command))
            {
                throw new ArgumentException("No command has been specified.");
            }

            string[] parts = command.Split(' ');
            Process p = new Process();
            p.StartInfo.FileName = parts[0];
            string arguments = string.Empty;
            if (parts.Length > 1)
            {
                arguments = parts[1];
                for (int i = 2; i < parts.Length; ++i)
                {
                    arguments += " " + parts[i];
                }
            }

            p.StartInfo.Arguments = arguments;
            p.StartInfo.UseShellExecute = false;
            if (!showOutput)
            {
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
            }

            p.Start();
            p.WaitForExit();
        }

        /// <summary>
        /// Use the PsExec utility to remotely execute a command.
        /// </summary>
        /// <param name="address">the IP address of the system on which to execute the command</param>
        /// <param name="command">the command to be executed</param>
        private static void PsExec(IPAddress address, string command)
        {
            string fullCommand = @"psexec \\" + address.ToString() + " -u " + Username + " -p " + Password + " " + command;
            ExecuteLocalCommand(fullCommand);
        }
    }
}
