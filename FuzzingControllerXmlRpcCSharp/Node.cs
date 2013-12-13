namespace FuzzingControllerXmlRpcCSharp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using CookComputing.XmlRpc;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;

    public class Node
    {
        private const string username = "admin";
        private const string password = "1";

        public INodeService Service { get; private set; }

        public IPAddress Address { get; private set; }

        public ConnectionStatus Status { get; private set; }

        public enum ConnectionStatus
        {
            [DescriptionAttribute("?")]
            Unknown,

            [DescriptionAttribute("+")]
            Online,

            [DescriptionAttribute("-")]
            Offline
        }

        public Node(IPAddress address)
        {
            this.Address = address;
            this.Status = ConnectionStatus.Unknown;
            this.Service = XmlRpcProxyGen.Create<INodeService>();
            this.Service.Url = "http://" + address.ToString() + ":8000/RPC2";
        }

        public ConnectionStatus UpdateStatus()
        {
            try
            {
                this.Service.ping();
                this.Status = ConnectionStatus.Online;
            }
            catch (WebException)
            {
                this.Status = ConnectionStatus.Offline;
            }
            catch (Exception e)
            {
                this.Status = ConnectionStatus.Unknown;
            }

            return this.Status;
        }

        public string ExecuteCommand(string command)
        {
            return this.Service.execute_command(command);
        }

        public void Close()
        {
            try
            {
                this.Service.close();
            }
            catch
            {
            }
        }

        public void Connect()
        {
            PsExec(this.Address, @"-i -d ""C:\Python27\python.exe"" ""C:\fuzzing_tools\node_server.py""");
        }

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

        private static void PsExec(IPAddress address, string command)
        {
            string fullCommand = @"psexec \\" + address.ToString() + " -u " + username + " -p " + password + " " + command;
            ExecuteLocalCommand(fullCommand);
        }

        public void Initialize()
        {
            // Create the fuzzing_tools directory on the target system if it does not exist.
            PsExec(this.Address, @"-w c:\ -d cmd /c mkdir fuzzing_tools");

            // Share the directory on the remote system.
            PsExec(this.Address, @"net share shared=""C:\fuzzing_tools""");

            // Connect a volume on localhost to the directory on the remote system.
            ExecuteLocalCommand(@"net use z: \\" + this.Address.ToString() + @"\shared /user:" + username + " " + password);

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

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash = hash * 23 + this.Address.GetHashCode();

                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(Node)) return false;
            return Equals((Node)obj);
        }

        public bool Equals(Node obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return this.Address.Equals(obj.Address);
        }
    }
}
