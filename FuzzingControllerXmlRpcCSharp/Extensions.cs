namespace FuzzingControllerXmlRpcCSharp
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// An collection of class extensions containing various auxiliary functions.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Gets the description attribute of the object, if it exists.
        /// </summary>
        /// <typeparam name="T">the type of object</typeparam>
        /// <param name="source">the object</param>
        /// <returns>returns the description attribute of the object, if it exists</returns>
        public static string DescriptionAttr<T>(this T source)
        {
            FieldInfo fi = source.GetType().GetField(source.ToString());

            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attributes != null && attributes.Length > 0)
            {
                return attributes[0].Description;
            }
            else
            {
                return source.ToString();
            }
        }
    }
}
