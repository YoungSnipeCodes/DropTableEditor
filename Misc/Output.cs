using System;
using System.Diagnostics;

namespace DropTableEditor.Misc
{
    /// <summary>
    /// Output class - Simplified for standalone app.
    /// </summary>
    public static class Output
    {
        /// <summary>
        /// Message type
        /// </summary>
        public enum MessageType
        {
            /// <summary>
            /// Normal.
            /// </summary>
            Normal = 0,

            /// <summary>
            /// Error.
            /// </summary>
            Error = 1,

            /// <summary>
            /// Event.
            /// </summary>
            Event = 2
        }

        /// <summary>
        /// Output a message to Debug console.
        /// </summary>
        /// <param name="messageType">Type of message to send.</param>
        /// <param name="message">Message.</param>
        public static void WriteLine(MessageType messageType, string message)
        {
            string prefix = messageType == MessageType.Error ? "ERROR: " :
                           messageType == MessageType.Event ? "EVENT: " : "";
            
            string output = string.Format("[{0:00}:{1:00}:{2:00}.{3:000}] {4}{5}",
                DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Millisecond,
                prefix, message);
            
            Debug.WriteLine(output);
            Console.WriteLine(output);
        }
    }
}