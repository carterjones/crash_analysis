namespace FuzzingControllerXmlRpcCSharp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// A structure used to hold output and error messages sent from a process.
    /// </summary>
    public class OutErr
    {
        /// <summary>
        /// Initializes a new instance of the OutErr class.
        /// </summary>
        public OutErr()
        {
            this.Output = string.Empty;
            this.Error = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the OutErr class, based on another OutErr object.
        /// </summary>
        /// <param name="oe">the OutErr object to copy</param>
        public OutErr(OutErr oe)
        {
            this.Output = oe.Output;
            this.Error = oe.Error;
        }

        /// <summary>
        /// Gets or sets the output message data.
        /// </summary>
        public string Output { get; set; }

        /// <summary>
        /// Gets or sets the error message data.
        /// </summary>
        public string Error { get; set; }
    }
}
