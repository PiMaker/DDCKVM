using System;
using System.Runtime.InteropServices;

namespace DDCKVMService
{
    public class WindowsApi
    {
        [DllImport("kernel32.dll", EntryPoint = "WTSGetActiveConsoleSessionId", SetLastError = true)]
        public static extern uint WTSGetActiveConsoleSessionId();

        [DllImport("Wtsapi32.dll", EntryPoint = "WTSQueryUserToken", SetLastError = true)]
        public static extern bool WTSQueryUserToken(uint SessionId, ref IntPtr phToken);

        [DllImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
        public static extern bool CloseHandle([In()] IntPtr hObject);

        [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUserW", SetLastError = true)]
        public static extern bool CreateProcessAsUser([In()] IntPtr hToken, IntPtr lpApplicationName, [In()][MarshalAs(UnmanagedType.LPWStr)] string lpCommandLine, [In()] IntPtr lpProcessAttributes, [In()] IntPtr lpThreadAttributes, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandles, uint dwCreationFlags, [In()] IntPtr lpEnvironment, [In()][MarshalAs(UnmanagedType.LPWStr)] string lpCurrentDirectory, [In()] ref STARTUPINFOW lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public uint nLength;
            public IntPtr lpSecurityDescriptor;

            [MarshalAs(UnmanagedType.Bool)]
            public bool bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFOW
        {
            public uint cb;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpReserved;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpDesktop;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpTitle;

            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public ushort wShowWindow;
            public ushort cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }
    }
}