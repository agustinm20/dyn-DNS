using System;
using System.Security;
using System.Threading;
using System.Windows.Threading;
using System.Security.Principal;
using System.ComponentModel;
using System.Security.Permissions;

namespace Sheva.Windows
{
    internal class RemoteCommunicator : MarshalByRefObject
    {
        private Dispatcher dispatcherInUIThread;
        private EventWaitHandle remoteMessageSemaphore;
        private WindowsIdentity originalUser;
        private DispatcherOperationCallback startNextInstanceCallback;

        internal RemoteCommunicator(WpfApplication app)
        {
            new SecurityPermission(SecurityPermissionFlag.ControlPrincipal).Assert();
            this.originalUser = WindowsIdentity.GetCurrent();
            CodeAccessPermission.RevertAssert();
            this.dispatcherInUIThread = app.Dispatcher;
            this.startNextInstanceCallback = app.startNextInstanceCallback;
            this.remoteMessageSemaphore = app.remoteMessageSemaphore;

        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        [System.Runtime.Remoting.Messaging.OneWay]
        public void RunNextInstance(Object state)
        {
            new SecurityPermission(SecurityPermissionFlag.ControlPrincipal).Assert();
            if (this.originalUser.User == WindowsIdentity.GetCurrent().User)
            {
                this.remoteMessageSemaphore.Set();
                CodeAccessPermission.RevertAssert();
                this.dispatcherInUIThread.BeginInvoke(DispatcherPriority.Send, this.startNextInstanceCallback, null);
            }
        }
    }
}
