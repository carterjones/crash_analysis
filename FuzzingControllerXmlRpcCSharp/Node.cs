﻿namespace FuzzingControllerXmlRpcCSharp
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
        public class OutErr
        {
            public string Output { get; set; }
            public string Error { get; set; }

            public OutErr()
            {
                this.Output = string.Empty;
                this.Error = string.Empty;
            }

            public OutErr(OutErr oe)
            {
                this.Output = oe.Output;
                this.Error = oe.Error;
            }
        }

        /// <summary>
        /// The username for this node.
        /// </summary>
        private const string Username = "admin";

        /// <summary>
        /// The password for this node.
        /// </summary>
        private const string Password = "1";

        private static OutErr globalOutErr = new OutErr();

        /// <summary>
        /// An interface to the service for communication purposes.
        /// </summary>
        private INodeService service;

        public InstallationStatus PythonInstalled { get; private set; }
        public InstallationStatus PsutilInstalled { get; private set; }
        public InstallationStatus WindbgInstalled { get; private set; }
        public InstallationStatus BangExploitableInstalled { get; private set; }
        public InstallationStatus AutoitInstalled { get; private set; }

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
            this.PythonInstalled = InstallationStatus.Unknown;
            this.PsutilInstalled = InstallationStatus.Unknown;
            this.WindbgInstalled = InstallationStatus.Unknown;
            this.BangExploitableInstalled = InstallationStatus.Unknown;
            this.AutoitInstalled = InstallationStatus.Unknown;
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
        /// A collection of installation statuses that represent if an application is installed on the node.
        /// </summary>
        public enum InstallationStatus
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
        /// Remotely start the node server that connects back to this controller if it is not already online.
        /// </summary>
        public void Connect()
        {
            if (!this.IsOnline())
            {
                PsExec(this.Address, @"-i -d ""C:\Python27\python.exe"" ""C:\fuzzing_tools\node_server.py""");
            }
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

            // Copy the scripts to the remote system.
            File.Copy(@"..\..\..\node_server.py", @"z:\node_server.py", true);
            File.Copy(@"..\..\..\start_minifuzz.au3", @"z:\start_minifuzz.au3", true);
            File.Copy(@"..\..\..\stop_minifuzz.au3", @"z:\stop_minifuzz.au3", true);
            File.Copy(@"..\..\..\triage.py", @"z:\triage.py", true);

            // Delete the volume on localhost that is connected to the directory on the remote system.
            ExecuteLocalCommand("net use z: /delete");

            // Unshare the directory on the remote system.
            PsExec(this.Address, "net share shared /delete");

            // Verify that the necessary software is installed.
            bool allInstallationsExist = CheckInstallations();

            // Start the node server on the remote system.
            if (allInstallationsExist)
            {
                this.Connect();
            }
        }

        /// <summary>
        /// Installs necessary software on the node.
        /// </summary>
        public void InstallSoftware()
        {
            // See what software is installed.
            this.CheckInstallations();

            // Create the fuzzing_tools directory on the target system if it does not exist.
            PsExec(this.Address, @"-w c:\ -d cmd /c mkdir fuzzing_tools\installers");

            // Share the directory on the remote system.
            PsExec(this.Address, @"net share shared=""C:\fuzzing_tools\installers""");

            // Connect a volume on localhost to the directory on the remote system.
            ExecuteLocalCommand(@"net use z: \\" + this.Address.ToString() + @"\shared /user:" + Username + " " + Password);

            // Install AutoIt.
            if (this.AutoitInstalled != InstallationStatus.Installed)
            {
                File.Copy(@"..\..\..\installers\autoit-v3-setup.exe", @"z:\autoit-v3-setup.exe", true);
                PsExec(this.Address, @"c:\fuzzing_tools\installers\autoit-v3-setup.exe /S");
            }

            // Install WinDbg.
            if (this.WindbgInstalled != InstallationStatus.Installed)
            {
                File.Copy(@"..\..\..\installers\dbg_x86.msi", @"z:\dbg_x86.msi", true);
                PsExec(this.Address, @"msiexec /i c:\fuzzing_tools\installers\dbg_x86.msi /q");
            }

            // Install !exploitable.
            if (this.BangExploitableInstalled != InstallationStatus.Installed)
            {
                File.Copy(@"..\..\..\installers\MSEC.dll", @"z:\MSEC.dll", true);
                PsExec(this.Address, @"cmd /c copy C:\fuzzing_tools\installers\MSEC.dll ""C:\Program Files\Debugging Tools for Windows (x86)\MSEC.dll""");
            }

            // Install Python 2.7.
            if (this.PythonInstalled != InstallationStatus.Installed)
            {
                File.Copy(@"..\..\..\installers\python-2.7.6.msi", @"z:\python-2.7.6.msi", true);
                PsExec(this.Address, @"msiexec /i c:\fuzzing_tools\installers\python-2.7.6.msi /q");
            }

            // Install psutils.
            if (this.PsutilInstalled != InstallationStatus.Installed)
            {
                if (!File.Exists(@"..\..\..\installers\ez_setup.py"))
                {
                    using (WebClient Client = new WebClient())
                    {
                        Client.DownloadFile("https://bitbucket.org/pypa/setuptools/raw/bootstrap/ez_setup.py", @"..\..\..\installers\ez_setup.py");
                    }
                }

                File.Copy(@"..\..\..\installers\ez_setup.py", @"z:\ez_setup.py", true);
                PsExec(this.Address, @"C:\Python27\python.exe c:\fuzzing_tools\installers\ez_setup.py");
                PsExec(this.Address, @"c:\python27\scripts\easy_install psutil");
            }

            // Delete the volume on localhost that is connected to the directory on the remote system.
            ExecuteLocalCommand("net use z: /delete");

            // Unshare the directory on the remote system.
            PsExec(this.Address, "net share shared /delete");
        }

        public bool IsOnline()
        {
            return this.UpdateStatus() == ConnectionStatus.Online;
        }

        public bool CheckInstallations()
        {
            OutErr oe = PsExec(this.Address, @"cmd /c dir c:\");
            if (oe.Output.ToLower().Contains("python27"))
            {
                this.PythonInstalled = InstallationStatus.Installed;
            }
            else
            {
                this.PythonInstalled = InstallationStatus.NotInstalled;
            }

            oe = PsExec(this.Address, @"cmd /c dir ""C:\Python27\Lib\site-packages\""");
            if (oe.Output.ToLower().Contains("psutil"))
            {
                this.PsutilInstalled = InstallationStatus.Installed;
            }
            else
            {
                this.PsutilInstalled = InstallationStatus.NotInstalled;
            }

            oe = PsExec(this.Address, @"cmd /c dir ""c:\program files\""");
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

            oe = PsExec(this.Address, @"cmd /c dir ""c:\program files\debugging tools for windows (x86)\""");
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

        private static void OutputReciever(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!string.IsNullOrEmpty(outLine.Data))
            {
                globalOutErr.Output += outLine.Data + Environment.NewLine;
            }
        }

        private static void ErrorReciever(object sendingProcess, DataReceivedEventArgs errLine)
        {
            if (!string.IsNullOrEmpty(errLine.Data))
            {
                globalOutErr.Error += errLine.Data + Environment.NewLine;
            }
        }

        /// <summary>
        /// Execute a system command locally.
        /// </summary>
        /// <param name="command">the command to be executed</param>
        /// <param name="showOutput">true if the command's output should be shown</param>
        private static OutErr ExecuteLocalCommand(string command, bool showOutput = false)
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

            globalOutErr.Output = string.Empty;
            globalOutErr.Error = string.Empty;

            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.OutputDataReceived += new DataReceivedEventHandler(OutputReciever);
            p.ErrorDataReceived += new DataReceivedEventHandler(ErrorReciever);
            p.StartInfo.Arguments = arguments;
            p.StartInfo.UseShellExecute = false;
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();

            if (showOutput)
            {
                Console.WriteLine(globalOutErr.Output);
                Console.WriteLine(globalOutErr.Error);
            }

            return new OutErr(globalOutErr);
        }

        /// <summary>
        /// Use the PsExec utility to remotely execute a command.
        /// </summary>
        /// <param name="address">the IP address of the system on which to execute the command</param>
        /// <param name="command">the command to be executed</param>
        private static OutErr PsExec(IPAddress address, string command)
        {
            string fullCommand = @"psexec \\" + address.ToString() + " -u " + Username + " -p " + Password + " " + command;
            return ExecuteLocalCommand(fullCommand);
        }
    }
}
