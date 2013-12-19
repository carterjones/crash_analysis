namespace FuzzingControllerXmlRpcCSharp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// A command interpreter.
    /// </summary>
    internal class Interpreter
    {
        /// <summary>
        /// A collection of commands that can be called by a user.
        /// </summary>
        private HashSet<UserCommand> commands = new HashSet<UserCommand>();

        /// <summary>
        /// A flag indicating if the command interpreter should stop.
        /// </summary>
        private bool stopFlag = false;

        /// <summary>
        /// Starts the command interpreter loop.
        /// </summary>
        public void Start()
        {
            while (true)
            {
                if (this.stopFlag)
                {
                    break;
                }

                Console.Write("> ");
                string userInput = Console.ReadLine();
                foreach (UserCommand cmd in this.commands)
                {
                    if (userInput.Equals("help"))
                    {
                        this.ShowHelp();
                        break;
                    }
                    else if (userInput.Equals(cmd.Name))
                    {
                        cmd.Action();
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Signals to the interpreter that it should stop at the next opportunity.
        /// </summary>
        public void Stop()
        {
            this.stopFlag = true;
        }

        /// <summary>
        /// Adds a command to the list of useable commands.
        /// </summary>
        /// <param name="name">the command name</param>
        /// <param name="description">the description of the command</param>
        /// <param name="action">the action to perform when the command is called</param>
        internal void AddCommand(string name, string description, Action action)
        {
            this.commands.Add(new UserCommand(name, description, action));
        }

        /// <summary>
        /// Shows all the available commands, along with their descriptions.
        /// </summary>
        private void ShowHelp()
        {
            int maxCommandLength = 0;
            this.commands.ToList().ForEach(c => maxCommandLength = c.Name.Length > maxCommandLength ? c.Name.Length : maxCommandLength);

            Console.WriteLine("available commands:");
            foreach (UserCommand command in this.commands)
            {
                Console.WriteLine("  " + command.Name.PadRight(maxCommandLength, ' ') + " - " + command.Description);
            }
        }

        /// <summary>
        /// Represents an interpreter command, which will perform an action when called.
        /// </summary>
        private class UserCommand
        {
            /// <summary>
            /// Initializes a new instance of the UserCommand class.
            /// </summary>
            /// <param name="name">the command name</param>
            /// <param name="description">the description of the command</param>
            /// <param name="action">the action to perform when the command is called</param>
            internal UserCommand(string name, string description, Action action)
            {
                this.Name = name;
                this.Description = description;
                this.Action = action;
            }

            /// <summary>
            /// Gets the command name.
            /// </summary>
            internal string Name { get; private set; }

            /// <summary>
            /// Gets the description of the command.
            /// </summary>
            internal string Description { get; private set; }

            /// <summary>
            /// Gets the action to perform when the command is called.
            /// </summary>
            internal Action Action { get; private set; }
        }
    }
}
