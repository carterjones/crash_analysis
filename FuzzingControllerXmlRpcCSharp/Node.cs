namespace FuzzingControllerXmlRpcCSharp
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Text;
    using System.Threading.Tasks;
    using CookComputing.XmlRpc;

    /// <summary>
    /// Represents a fuzzing machine (node).
    /// </summary>
    internal class Node
    {
        #region Fields

        /// <summary>
        /// The username for this node.
        /// </summary>
        private string username = "admin";

        /// <summary>
        /// The password for this node.
        /// </summary>
        private string password = "1";

        /// <summary>
        /// Used for storing the output and error messages for processes started this node object.
        /// </summary>
        private OutErr globalOutErr = new OutErr();

        /// <summary>
        /// An interface to the service for communication purposes.
        /// </summary>
        private INodeService service;

        /// <summary>
        /// A flag indicating that the controller has successfully authenticated with the node by using "net use".
        /// </summary>
        private bool isAuthenticatedWithNetUse = false;

        /// <summary>
        /// A flag indicating that the installer directory has been created on the node.
        /// </summary>
        private bool installerDirectoryCreated = false;

        /// <summary>
        /// A flag indicating that the node has been initialized.
        /// </summary>
        private bool isInitialized = false;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Initializes a new instance of the Node class.
        /// </summary>
        /// <param name="address">the IP address of the node</param>
        internal Node(IPAddress address)
        {
            this.Address = address;
            this.Status = ConnectionStatus.Unknown;
            this.service = XmlRpcProxyGen.Create<INodeService>();
            this.service.Url = "http://" + address.ToString() + ":8000/RPC2";
            this.PythonInstalled = InstallationStatus.Unknown;
            this.PsutilInstalled = InstallationStatus.Unknown;
            this.WindbgInstalled = InstallationStatus.Unknown;
            this.BangExploitableInstalled = InstallationStatus.Unknown;
            this.AutoitInstalled = InstallationStatus.Unknown;
        }

        #endregion

        #region Enumerations

        /// <summary>
        /// A collection of connection statuses that represent various states of the node.
        /// </summary>
        internal enum ConnectionStatus
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
        /// A collection of installation statuses that represent if an application is installed on the node.
        /// </summary>
        internal enum InstallationStatus
        {
            /// <summary>
            /// Represents an unknown state of the installation.
            /// </summary>
            [DescriptionAttribute("Unknown")]
            Unknown,

            /// <summary>
            /// The installation exists.
            /// </summary>
            [DescriptionAttribute("Installed")]
            Installed,

            /// <summary>
            /// The installation does not exist.
            /// </summary>
            [DescriptionAttribute("Not installed")]
            NotInstalled
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the IP address of the node.
        /// </summary>
        internal IPAddress Address { get; private set; }

        /// <summary>
        /// Gets the last cached status of the connection.
        /// </summary>
        internal ConnectionStatus Status { get; private set; }

        /// <summary>
        /// Gets a value indicating if Python 2.7 is installed on the node.
        /// </summary>
        internal InstallationStatus PythonInstalled { get; private set; }

        /// <summary>
        /// Gets a value indicating if psutil is installed on the node.
        /// </summary>
        internal InstallationStatus PsutilInstalled { get; private set; }

        /// <summary>
        /// Gets a value indicating if WinDbg is installed on the node.
        /// </summary>
        internal InstallationStatus WindbgInstalled { get; private set; }

        /// <summary>
        /// Gets a value indicating if !exploitable is installed on the node.
        /// </summary>
        internal InstallationStatus BangExploitableInstalled { get; private set; }

        /// <summary>
        /// Gets a value indicating if AutoIt is installed on the node.
        /// </summary>
        internal InstallationStatus AutoitInstalled { get; private set; }

        #endregion

        #region Public Methods

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

        #endregion

        #region Internal Methods

        /// <summary>
        /// Tests if the node is online, offline, or something unknown.
        /// </summary>
        /// <returns>the result of the test</returns>
        internal ConnectionStatus UpdateStatus()
        {
            if (!this.IsOnline())
            {
                this.Status = ConnectionStatus.Offline;
            }
            else
            {
                try
                {
                    this.service.TestConnection();
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
            }

            return this.Status;
        }

        /// <summary>
        /// Executes a system command on the node.
        /// </summary>
        /// <param name="command">the command to be executed</param>
        /// <returns>the output of the command</returns>
        internal string ExecuteCommand(string command)
        {
            return this.service.ExecuteCommand(command);
        }

        /// <summary>
        /// Close the listening server on the node.
        /// </summary>
        internal void Close()
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
        /// Remotely start the node server that connects back to this controller if it is not already online.
        /// </summary>
        internal void Connect()
        {
            if (!this.IsOnline())
            {
                return;
            }

            if (!this.IsConnectedToController(true))
            {
                this.PsExec(@"-i -d ""C:\Python27\python.exe"" ""C:\fuzzing_tools\node_server.py""");
            }
        }

        /// <summary>
        /// Copies a file to the node.
        /// </summary>
        /// <param name="localPath">the path to the local file</param>
        /// <param name="nodePath">the destination path on the node</param>
        internal void CopyFileToNode(string localPath, string nodePath)
        {
            this.AuthenticateWithNetUse();
            string normalizedLocalPath = Path.GetFullPath(localPath);
            string normalizedNodePath = nodePath.ToLower().Replace("c:", @"c$");
            this.ExecuteLocalCommand("cmd /c copy " + localPath + @" \\" + this.Address + @"\" + normalizedNodePath);
        }

        /// <summary>
        /// Initializes the scripts to be run on a node. Connects the node to this controller.
        /// </summary>
        internal void Initialize()
        {
            if (this.isInitialized)
            {
                return;
            }

            if (!this.IsOnline())
            {
                return;
            }

            // Create a directory on the remote system.
            this.PsExec(@"cmd /c mkdir c:\fuzzing_tools");

            // Copy the scripts to the remote system.
            string[] scripts = { "node_server.py", "start_minifuzz.au3", "stop_minifuzz.au3", "triage.py" };
            foreach (string script in scripts)
            {
                this.CopyFileToNode(@"..\..\..\" + script, @"c:\fuzzing_tools\" + script);
            }

            // Verify that the necessary software is installed.
            bool allInstallationsExist = this.CheckForBaseInstallations();

            // Start the node server on the remote system.
            if (allInstallationsExist)
            {
                this.Connect();
            }

            this.isInitialized = true;
        }

        /// <summary>
        /// Installs necessary software on the node.
        /// </summary>
        internal void InstallBaseSoftware()
        {
            // See what software is installed.
            this.CheckForBaseInstallations();

            // Install AutoIt.
            if (this.AutoitInstalled != InstallationStatus.Installed)
            {
                this.CopyInstallerToNode("autoit-v3-setup.exe");
                this.PsExec(@"c:\fuzzing_tools\installers\autoit-v3-setup.exe /S");
            }

            // Install WinDbg.
            if (this.WindbgInstalled != InstallationStatus.Installed)
            {
                this.CopyInstallerToNode("dbg_x86.msi");
                this.PsExec(@"msiexec /i c:\fuzzing_tools\installers\dbg_x86.msi /q");
            }

            // Install !exploitable.
            if (this.BangExploitableInstalled != InstallationStatus.Installed)
            {
                this.CopyInstallerToNode("MSEC.dll");
                this.PsExec(@"cmd /c copy C:\fuzzing_tools\installers\MSEC.dll ""C:\Program Files\Debugging Tools for Windows (x86)\MSEC.dll""");
            }

            // Install Python 2.7.
            if (this.PythonInstalled != InstallationStatus.Installed)
            {
                this.CopyInstallerToNode("python-2.7.6.msi");
                this.PsExec(@"msiexec /i c:\fuzzing_tools\installers\python-2.7.6.msi /q");
            }

            // Install psutils.
            if (this.PsutilInstalled != InstallationStatus.Installed)
            {
                if (!File.Exists(@"..\..\..\installers\ez_setup.py"))
                {
                    using (WebClient client = new WebClient())
                    {
                        client.DownloadFile("https://bitbucket.org/pypa/setuptools/raw/bootstrap/ez_setup.py", @"..\..\..\installers\ez_setup.py");
                    }
                }

                this.CopyInstallerToNode("ez_setup.py");
                this.PsExec(@"C:\Python27\python.exe c:\fuzzing_tools\installers\ez_setup.py");
                this.PsExec(@"c:\python27\scripts\easy_install psutil");
            }
        }

        /// <summary>
        /// Determines if the node is online and connected to the controller service.
        /// </summary>
        /// <param name="performUpdate">if true, the node status is updated prior to returning the result</param>
        /// <returns>true if the node is online and connected to the controller service</returns>
        internal bool IsConnectedToController(bool performUpdate = false)
        {
            if (performUpdate)
            {
                return this.UpdateStatus() == ConnectionStatus.Online;
            }
            else
            {
                return this.Status == ConnectionStatus.Online;
            }
        }

        /// <summary>
        /// Ping the node to see if it is online.
        /// </summary>
        /// <returns>true if the host successfully responds to the ping</returns>
        internal bool IsOnline()
        {
            PingReply reply = new Ping().Send(this.Address);
            return reply.Status == IPStatus.Success;
        }

        /// <summary>
        /// Determines if the node has various pieces of software installed required for controlling the node.
        /// </summary>
        /// <returns>true if all the necessary software is installed</returns>
        internal bool CheckForBaseInstallations()
        {
            OutErr oe = this.PsExec(@"cmd /c dir c:\");
            if (oe.Output.ToLower().Contains("python27"))
            {
                this.PythonInstalled = InstallationStatus.Installed;
            }
            else
            {
                this.PythonInstalled = InstallationStatus.NotInstalled;
            }

            oe = this.PsExec(@"cmd /c dir ""C:\Python27\Lib\site-packages\""");
            if (oe.Output.ToLower().Contains("psutil"))
            {
                this.PsutilInstalled = InstallationStatus.Installed;
            }
            else
            {
                this.PsutilInstalled = InstallationStatus.NotInstalled;
            }

            oe = this.PsExec(@"cmd /c dir ""c:\program files\""");
            if (oe.Output.ToLower().Contains("debugging tools for windows (x86)"))
            {
                this.WindbgInstalled = InstallationStatus.Installed;
            }
            else
            {
                this.WindbgInstalled = InstallationStatus.NotInstalled;
            }

            if (oe.Output.ToLower().Contains("autoit3"))
            {
                this.AutoitInstalled = InstallationStatus.Installed;
            }
            else
            {
                this.AutoitInstalled = InstallationStatus.NotInstalled;
            }

            oe = this.PsExec(@"cmd /c dir ""c:\program files\debugging tools for windows (x86)\""");
            if (oe.Output.ToLower().Contains("msec.dll"))
            {
                this.BangExploitableInstalled = InstallationStatus.Installed;
            }
            else
            {
                this.BangExploitableInstalled = InstallationStatus.NotInstalled;
            }

            return
                this.PythonInstalled == InstallationStatus.Installed &&
                this.PsutilInstalled == InstallationStatus.Installed &&
                this.WindbgInstalled == InstallationStatus.Installed &&
                this.BangExploitableInstalled == InstallationStatus.Installed &&
                this.AutoitInstalled == InstallationStatus.Installed;
        }

        /// <summary>
        /// Install VLC on the node, if it is not already installed.
        /// </summary>
        internal void DeployVlc()
        {
            OutErr oe = this.PsExec(@"cmd /c dir ""c:\program files\""");
            if (oe.Output.ToLower().Contains("videolan"))
            {
                // VLC is already installed.
                return;
            }

            // Install VLC.
            this.CopyInstallerToNode("vlc-2.1.1-win32.exe");
            this.PsExec(@"c:\fuzzing_tools\installers\vlc-2.1.1-win32.exe /L=1033 /S");
        }

        /// <summary>
        /// Install MiniFuzz on the node, if it is not already installed.
        /// </summary>
        internal void DeployMiniFuzz()
        {
            OutErr oe = this.PsExec(@"cmd /c dir ""C:\Program Files\Microsoft\""");
            if (oe.Output.ToLower().Contains("minifuzz"))
            {
                // MiniFuzz is already installed.
                return;
            }

            // Install MiniFuzz.
            this.CopyInstallerToNode("MiniFuzzSetup.msi");
            this.PsExec(@"msiexec /i c:\fuzzing_tools\installers\MiniFuzzSetup.msi /q");

            // Copy configuration file over to node.
            this.CopyInstallerToNode("minifuzz.cfg");
            this.PsExec(@"cmd /c copy c:\fuzzing_tools\installers\minifuzz.cfg ""C:\Program Files\Microsoft\MiniFuzz\minifuzz.cfg"" /y");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Authenticate with the node using "net use".
        /// </summary>
        private void AuthenticateWithNetUse()
        {
            if (!this.isAuthenticatedWithNetUse)
            {
                this.ExecuteLocalCommand(@"net use \\" + this.Address + " /user:" + this.username + " " + this.password);
                this.isAuthenticatedWithNetUse = true;
            }
        }

        /// <summary>
        /// Copies the specified installation file to the node's installation directory.
        /// </summary>
        /// <param name="installerFileName">the filename of the installation file in the local installer directory</param>
        private void CopyInstallerToNode(string installerFileName)
        {
            if (!this.installerDirectoryCreated)
            {
                this.PsExec(@"cmd /c mkdir c:\fuzzing_tools\installers");
                this.installerDirectoryCreated = true;
            }

            this.CopyFileToNode(@"..\..\..\installers\" + installerFileName, @"c:\fuzzing_tools\installers\" + installerFileName);
        }

        /// <summary>
        /// Receives and saves the output message to the global error message object.
        /// </summary>
        /// <param name="sendingProcess">the process that generated the output message</param>
        /// <param name="outLine">the output message</param>
        private void OutputReciever(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!string.IsNullOrEmpty(outLine.Data))
            {
                this.globalOutErr.Output += outLine.Data + Environment.NewLine;
            }
        }

        /// <summary>
        /// Receives and saves the error message to the global error message object.
        /// </summary>
        /// <param name="sendingProcess">the process that generated the error message</param>
        /// <param name="errLine">the error message</param>
        private void ErrorReciever(object sendingProcess, DataReceivedEventArgs errLine)
        {
            if (!string.IsNullOrEmpty(errLine.Data))
            {
                this.globalOutErr.Error += errLine.Data + Environment.NewLine;
            }
        }

        /// <summary>
        /// Execute a system command locally.
        /// </summary>
        /// <param name="command">the command to be executed</param>
        /// <param name="showOutput">true if the command's output should be shown</param>
        /// <returns>the output and error messages of the process</returns>
        private OutErr ExecuteLocalCommand(string command, bool showOutput = false)
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

            this.globalOutErr.Output = string.Empty;
            this.globalOutErr.Error = string.Empty;

            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.OutputDataReceived += new DataReceivedEventHandler(this.OutputReciever);
            p.ErrorDataReceived += new DataReceivedEventHandler(this.ErrorReciever);
            p.StartInfo.Arguments = arguments;
            p.StartInfo.UseShellExecute = false;
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();

            if (showOutput)
            {
                Console.WriteLine(this.globalOutErr.Output);
                Console.WriteLine(this.globalOutErr.Error);
            }

            return new OutErr(this.globalOutErr);
        }

        /// <summary>
        /// Use the PsExec utility to remotely execute a command on the node.
        /// </summary>
        /// <param name="command">the command to be executed</param>
        /// <returns>the output and error messages of the process</returns>
        private OutErr PsExec(string command)
        {
            if (!this.IsOnline())
            {
                throw new InvalidOperationException(this.Address.ToString() + " is offline. Cannot execute PsExec command.");
            }

            string fullCommand = @"psexec \\" + this.Address.ToString() + " -u " + this.username + " -p " + this.password + " " + command;
            return this.ExecuteLocalCommand(fullCommand);
        }

        #endregion
    }
}
