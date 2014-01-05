using System;
using System.Runtime.Serialization;

namespace Sheva.Windows
{
    public delegate void StartupNextInstanceEventHandler(Object sender, StartupNextInstanceEventArgs e);

    public class StartupNextInstanceEventArgs : EventArgs
    {
        private Boolean bringToForeground;

        public StartupNextInstanceEventArgs(Boolean bringToForeground)
        {
            this.bringToForeground = bringToForeground;
        }

        public Boolean BringToForeground
        {
            get { return bringToForeground; }
            set { bringToForeground = value; }
        }
    }

    [Serializable]
    public class CannotStartSingleInstanceException : Exception
    {
        public CannotStartSingleInstanceException() : base("No se puede conectar a la aplicación") { }
        public CannotStartSingleInstanceException(String message) : base(message) { }
        protected CannotStartSingleInstanceException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        public CannotStartSingleInstanceException(String message, Exception inner) : base(message, inner) { }
    }
}
