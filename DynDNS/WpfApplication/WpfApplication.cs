using System;
using System.Collections;
using System.ComponentModel;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Security;
using System.Security.AccessControl;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32.SafeHandles;
using Sheva.Internal;

namespace Sheva.Windows
{
    /// <summary>
    /// Designates a Windows Presentation Foundation application with added functionalities.
    /// </summary>
    /// <remarks>
    /// Most of the code in this class is extracted from the .NET framework library using reflector:)
    /// </remarks>
    public class WpfApplication : Application
    {
        private Boolean isSingleInstance;
        private EventWaitHandle firstInstanceSemaphore;
        private String firstInstanceSemaphoreId;
        private String remoteMessageSemaphoreId;
        private String memoryMappedFileId;
        private SafeFileHandle memoryMappedFileHandle;

        internal DispatcherOperationCallback startNextInstanceCallback;
        internal EventWaitHandle remoteMessageSemaphore;

        private static String executablePath;

        public event StartupNextInstanceEventHandler StartupNextInstanceEvent;

        /// <summary>
        /// Initializes a new instance of the <see cref="WpfApplication">WpfApplication</see> class.
        /// </summary>
        public WpfApplication() : base() { isSingleInstance = false; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WpfApplication">WpfApplication</see> class.
        /// </summary>
        /// <param name="isSingleInstance">
        /// Specify whether newly created application runs as a single-instance application.
        /// </param>
        public WpfApplication(Boolean isSingleInstance)
            : base()
        {
            this.isSingleInstance = isSingleInstance;
        }

        /// <summary>
        /// Gets and sets if newly created application runs as a single-instance application.
        /// </summary>
        public Boolean IsSingleInstance
        {
            get { return isSingleInstance; }
            set { isSingleInstance = value; }
        }

        /// <summary>
        /// Gets the path for the executable file that started the application, including the executable name.
        /// </summary>
        /// <returns>The path and executable name for the executable file that started the application.</returns>
        public static String ExecutablePath
        {
            get
            {
                if (executablePath == null)
                {
                    Assembly entryAssembly = Assembly.GetEntryAssembly();
                    if (entryAssembly == null)
                    {
                        StringBuilder buffer = new StringBuilder(260);
                        HandleRef nullHandleRef = new HandleRef();
                        UnsafeNativeMethods.GetModuleFileName(nullHandleRef, buffer, buffer.Capacity);
                        executablePath = UnsafeGetFullPath(buffer.ToString());
                    }
                    else
                    {
                        String uriString = entryAssembly.EscapedCodeBase;
                        Uri uri = new Uri(uriString);
                        if (uri.Scheme == "file")
                        {
                            executablePath = GetLocalPath(uriString);
                        }
                        else
                        {
                            executablePath = uri.ToString();
                        }
                    }
                }
                Uri uri2 = new Uri(executablePath);
                if (uri2.Scheme == "file")
                {
                    new FileIOPermission(FileIOPermissionAccess.PathDiscovery, executablePath).Demand();
                }
                return executablePath;
            }
        }

        /// <summary>
        /// Starts a Windows Presentation Foundation application.
        /// </summary>
        /// <returns>
        /// The Int32 application exit code that is returned to the operating system 
        /// when the application shuts down. By default, the exit code value is 0.
        /// </returns>
        public new Int32 Run()
        {
            return Run(null);
        }

        /// <summary>
        /// Starts a Windows Presentation Foundation application and opens the specified window. 
        /// </summary>
        /// <param name="window">A <see cref="Window">Window</see> that opens automatically when an application starts.</param>
        /// <returns>
        /// The Int32 application exit code that is returned to the operating system 
        /// when the application shuts down. By default, the exit code value is 0.
        /// </returns>
        public new Int32 Run(Window window)
        {
            if (!isSingleInstance)
            {
                return base.Run(window);
            }
            else
            {
                return RunSingleInstanceApplication(window);
            }
        }

        /// <summary>
        /// Processes all messages currently in the message queue.
        /// </summary>
        /// <remarks>
        /// This method can potentially cause code re-entrancy problem, so use it with great care.
        /// </remarks>
        public static void DoEvents()
        {
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(delegate { }));
        }

        /// <summary>
        /// When overridden in a derived class, allows for code to run when a subsequent instance of a single-instance application starts. 
        /// </summary>
        /// <param name="eventArgs">
        /// <see cref="StartupNextInstanceEventArgs">StartupNextInstanceEventArgs</see> Contains the command-line arguments of the subsequent 
        /// application instance and indicates whether the first application instance should be brought to the foreground upon exiting the exception handler.
        /// </param>
        protected virtual void OnStartupNextInstance(StartupNextInstanceEventArgs eventArgs)
        {
            if (this.StartupNextInstanceEvent != null)
            {
                this.StartupNextInstanceEvent(this, eventArgs);
            }

            new UIPermission(UIPermissionWindow.AllWindows).Assert();

            if (eventArgs.BringToForeground && (this.MainWindow != null))
            {
                if (this.MainWindow.WindowState == WindowState.Minimized)
                {
                    this.MainWindow.WindowState = WindowState.Normal;
                }

                this.MainWindow.Activate();
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Get an Id uniquely identifying the single-instance application.
        /// </summary>
        /// <param name="asm">assembly which the single-instance application loads.</param>
        /// <returns>return an unique Id identifying the single-instance application.</returns>
        private String GetApplicationId(Assembly asm)
        {
            PermissionSet ps = new PermissionSet(PermissionState.None);
            ps.AddPermission(new FileIOPermission(PermissionState.Unrestricted));
            ps.AddPermission(new SecurityPermission(SecurityPermissionFlag.UnmanagedCode));
            ps.Assert();
            Guid guid = Marshal.GetTypeLibGuidForAssembly(asm);
            String[] versionEntities = asm.GetName().Version.ToString().Split('.');
            PermissionSet.RevertAssert();
            return (guid.ToString() + versionEntities[0] + "." + versionEntities[1]);
        }

        private static String GetLocalPath(String fileName)
        {
            Uri uri = new Uri(fileName);
            return (uri.LocalPath + uri.Fragment);
        }

        private static String UnsafeGetFullPath(String fileName)
        {
            String fullPath = fileName;
            FileIOPermission permission = new FileIOPermission(PermissionState.None);
            permission.AllFiles = FileIOPermissionAccess.PathDiscovery;
            permission.Assert();
            try
            {
                fullPath = System.IO.Path.GetFullPath(fileName);
            }
            finally
            {
                CodeAccessPermission.RevertAssert();
            }
            return fullPath;
        }

        /// <summary>
        /// Adapts the OnStartupNextInstance method to match the signature of DispatcherOperationCallback delegate.
        /// </summary>
        private Object OnStartupNextInstanceAdaptor(Object state)
        {
            this.OnStartupNextInstance(new StartupNextInstanceEventArgs(true));
            return null;
        }

        /// <summary>
        /// Register a channel on the calling AppDomain to listen to remote messages.
        /// </summary>
        private TcpChannel RegisterChannel(Boolean secureChannel)
        {
            PermissionSet ps = new PermissionSet(PermissionState.None);
            ps.AddPermission(new SecurityPermission(SecurityPermissionFlag.ControlPrincipal | SecurityPermissionFlag.SerializationFormatter | SecurityPermissionFlag.UnmanagedCode));
            ps.AddPermission(new SocketPermission(NetworkAccess.Accept, TransportType.Tcp, "127.0.0.1", 0));
            ps.AddPermission(new EnvironmentPermission(EnvironmentPermissionAccess.Read, "USERNAME"));
            ps.AddPermission(new SecurityPermission(SecurityPermissionFlag.RemotingConfiguration));
            ps.Assert();
            IDictionary dic = new Hashtable(3);
            dic.Add("bindTo", "127.0.0.1");
            dic.Add("port", 0);
            if (secureChannel)
            {
                dic.Add("secure", true);
                dic.Add("tokenimpersonationlevel", TokenImpersonationLevel.Impersonation);
                dic.Add("impersonate", true);
            }

            TcpChannel channel = new TcpChannel(dic, null, null);
            ChannelServices.RegisterChannel(channel, false);
            PermissionSet.RevertAssert();
            return channel;
        }

        private void WriteUrlToMemoryMappedFile(String url)
        {
            IntPtr mappedFileViewHandle = IntPtr.Zero;
            HandleRef memoryMappedFileHandleRef;
            HandleRef invalidHandleRef = new HandleRef(null, (IntPtr)(-1));
            using (NativeTypes.SECURITY_ATTRIBUTES security_attributes = new NativeTypes.SECURITY_ATTRIBUTES())
            {
                Boolean convertSuccess = false;
                security_attributes.bInheritHandle = false;
                try
                {
                    new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Assert();
                    convertSuccess = NativeMethods.ConvertStringSecurityDescriptorToSecurityDescriptor("D:(A;;GA;;;CO)(A;;GR;;;AU)", 1, ref security_attributes.lpSecurityDescriptor, IntPtr.Zero);
                    CodeAccessPermission.RevertAssert();
                }
                catch (EntryPointNotFoundException)
                {
                    security_attributes.lpSecurityDescriptor = IntPtr.Zero;
                }
                catch (DllNotFoundException)
                {
                    security_attributes.lpSecurityDescriptor = IntPtr.Zero;
                }
                if (!convertSuccess)
                {
                    security_attributes.lpSecurityDescriptor = IntPtr.Zero;
                }
                this.memoryMappedFileHandle = new SafeFileHandle(UnsafeNativeMethods.CreateFileMapping(invalidHandleRef, security_attributes, 4, 0, (url.Length + 1) * 2, this.memoryMappedFileId), true);
                if (this.memoryMappedFileHandle.IsInvalid)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot Create Memory Mapped File");
                }
            }
            try
            {
                memoryMappedFileHandleRef = new HandleRef(null, this.memoryMappedFileHandle.DangerousGetHandle());
                mappedFileViewHandle = UnsafeNativeMethods.MapViewOfFile(memoryMappedFileHandleRef, 2, 0, 0, 0);
                if (mappedFileViewHandle == IntPtr.Zero)
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot Get Memory Mapped File");
                }

                Char[] urlChars = url.ToCharArray();
                Marshal.Copy(urlChars, 0, mappedFileViewHandle, urlChars.Length);
            }
            finally
            {
                if (mappedFileViewHandle != IntPtr.Zero)
                {
                    memoryMappedFileHandleRef = new HandleRef(null, mappedFileViewHandle);
                    UnsafeNativeMethods.UnmapViewOfFile(memoryMappedFileHandleRef);
                }
            }
        }

        private String ReadUrlFromMemoryMappedFile()
        {
            using (SafeFileHandle memoryMappedFileHandle = new SafeFileHandle(UnsafeNativeMethods.OpenFileMapping(4, false, this.memoryMappedFileId), true))
            {
                IntPtr mappedFileViewHandle = IntPtr.Zero;
                HandleRef memoryMappedFileHandleRef;
                if (memoryMappedFileHandle.IsInvalid)
                {
                    return null;
                }
                try
                {
                    memoryMappedFileHandleRef = new HandleRef(null, memoryMappedFileHandle.DangerousGetHandle());
                    mappedFileViewHandle = UnsafeNativeMethods.MapViewOfFile(memoryMappedFileHandleRef, 4, 0, 0, 0);
                    if (mappedFileViewHandle == IntPtr.Zero)
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot Get Memory Mapped File");
                    }
                    return Marshal.PtrToStringUni(mappedFileViewHandle);
                }
                finally
                {
                    if (mappedFileViewHandle != IntPtr.Zero)
                    {
                        memoryMappedFileHandleRef = new HandleRef(null, mappedFileViewHandle);
                        UnsafeNativeMethods.UnmapViewOfFile(memoryMappedFileHandleRef);
                    }
                }
            }
        }

        private Int32 RunSingleInstanceApplication(Window window)
        {
            String appId = GetApplicationId(Assembly.GetCallingAssembly());
            this.memoryMappedFileId = appId + "MappedFile";
            this.firstInstanceSemaphoreId = appId + "FirstInstance";
            this.remoteMessageSemaphoreId = appId + "RemoteMessageSemaphore";
            this.startNextInstanceCallback = new DispatcherOperationCallback(OnStartupNextInstanceAdaptor);
            new SecurityPermission(SecurityPermissionFlag.ControlPrincipal).Assert();
            String currentUserName = WindowsIdentity.GetCurrent().Name;
            Boolean userLoggedOn = !String.IsNullOrEmpty(currentUserName);
            CodeAccessPermission.RevertAssert();

            Boolean createdNew = false;
            if (userLoggedOn)
            {
                EventWaitHandleAccessRule accessRule = new EventWaitHandleAccessRule(currentUserName, EventWaitHandleRights.FullControl, AccessControlType.Allow);
                EventWaitHandleSecurity security = new EventWaitHandleSecurity();
                security.AddAccessRule(accessRule);
                this.firstInstanceSemaphore = new EventWaitHandle(false, EventResetMode.ManualReset, this.firstInstanceSemaphoreId, out createdNew, security);
                Boolean dummyFlag = false;
                this.remoteMessageSemaphore = new EventWaitHandle(false, EventResetMode.AutoReset, this.remoteMessageSemaphoreId, out dummyFlag, security);
            }
            else
            {
                this.firstInstanceSemaphore = new EventWaitHandle(false, EventResetMode.ManualReset, this.firstInstanceSemaphoreId, out createdNew);
                this.remoteMessageSemaphore = new EventWaitHandle(false, EventResetMode.AutoReset, this.remoteMessageSemaphoreId);
            }

            if (createdNew)
            {
                try
                {
                    TcpChannel channel = this.RegisterChannel(userLoggedOn);
                    RemoteCommunicator communicator = new RemoteCommunicator(this);
                    string memoryMappedFileUri = appId + ".rem";
                    new SecurityPermission(SecurityPermissionFlag.RemotingConfiguration).Assert();
                    RemotingServices.Marshal(communicator, memoryMappedFileUri);
                    CodeAccessPermission.RevertAssert();
                    String memoryMappedFileUrl = channel.GetUrlsForUri(memoryMappedFileUri)[0];
                    this.WriteUrlToMemoryMappedFile(memoryMappedFileUrl);
                    this.firstInstanceSemaphore.Set();
                    base.Run(window);
                }
                finally
                {
                    if (this.remoteMessageSemaphore != null)
                    {
                        this.remoteMessageSemaphore.Close();
                    }
                    if (this.firstInstanceSemaphore != null)
                    {
                        this.firstInstanceSemaphore.Close();
                    }
                    if ((this.memoryMappedFileHandle != null) && !this.memoryMappedFileHandle.IsInvalid)
                    {
                        this.memoryMappedFileHandle.Close();
                    }
                }
            }
            else
            {
                if (!this.firstInstanceSemaphore.WaitOne(1000, false))
                {
                    throw new CannotStartSingleInstanceException();
                }
                this.RegisterChannel(userLoggedOn);
                String memoryMappedFileUrl = this.ReadUrlFromMemoryMappedFile();
                if (memoryMappedFileUrl == null)
                {
                    throw new CannotStartSingleInstanceException();
                }

                RemoteCommunicator communicator = (RemoteCommunicator)RemotingServices.Connect(typeof(RemoteCommunicator), memoryMappedFileUrl);
                PermissionSet ps = new PermissionSet(PermissionState.None);
                ps.AddPermission(new SecurityPermission(SecurityPermissionFlag.ControlPrincipal | SecurityPermissionFlag.SerializationFormatter | SecurityPermissionFlag.UnmanagedCode));
                ps.AddPermission(new DnsPermission(PermissionState.Unrestricted));
                ps.AddPermission(new SocketPermission(NetworkAccess.Connect, TransportType.Tcp, "127.0.0.1", -1));
                ps.AddPermission(new EnvironmentPermission(EnvironmentPermissionAccess.Read, "USERNAME"));
                ps.Assert();
                communicator.RunNextInstance(null);
                PermissionSet.RevertAssert();
                if (!this.remoteMessageSemaphore.WaitOne(2500, false))
                {
                    throw new CannotStartSingleInstanceException();
                }
            }

            return Environment.ExitCode;
        }

        #endregion
    }
}
