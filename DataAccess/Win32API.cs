//programmed by Binbinsoft
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

using HANDLE = System.IntPtr;
using HWND = System.IntPtr;

namespace WindowsAPI
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct MSG
    {
        public HWND hwnd;
        public int message;
        public IntPtr wParam;
        public IntPtr lParam;
        public UInt32 time;
        public POINT pt;
//        public UInt32 lPrivate;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct OVERLAPPED
    {
        public int Internal;
        public int InternalHigh;
        public int offset;
        public int OffsetHigh;
        public HANDLE hEvent;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        public IntPtr lpSecurityDescriptor;
        public int bInheritHandle;
    }

    public class Win32API
    {
        public const int PM_REMOVE = 0x1;
        public const UInt32 WAIT_FAILED = 0xFFFFFFFF;
        public const UInt32 WAIT_OBJECT_0 = 0;
        public const UInt32 WAIT_TIMEOUT = 0x00000102;
        public const UInt32 INFINITE = 0xFFFFFFFF;
        public const Int32 WM_USER = 0x400;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int PeekMessage(ref MSG lpMsg, HWND hwnd, int wMsgFilterMin, int wMsgFilterMax, int wRemoveMsg);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(HWND hwnd, int wMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostMessage(HWND hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool PostThreadMessage(uint threadId, uint msg, IntPtr wParam, IntPtr lParam);

//        [return: MarshalAs(UnmanagedType.Bool)]
//        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
//        public static extern bool PostThreadMessage(uint threadId, uint msg, UIntPtr wParam, ref WRDataBlock lParam);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int MsgWaitForMultipleObjects(int nCount, ref IntPtr pHandles, int fWaitAll, int dwMilliseconds, int dwWakeMask);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int MessageBox(HWND hwnd, string lpText, string lpCaption, int wType);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int MessageBoxTimeout(HWND hwnd, String text, String title, uint type, Int16 wLanguageId, Int32 milliseconds);

        [DllImport("kernel32.dll")]
        public static extern HANDLE CreateEvent(IntPtr lpEventAttributes, int bManualReset, int bInitialState, string lpName);

        [DllImport("kernel32.dll")]
        public static extern int WaitForMultipleObjects(int nCount, ref IntPtr lpHandles, int bWaitAll, uint dwMilliseconds);

        [DllImport("kernel32.dll")]
        public static extern int WaitForMultipleObjectsEx(int nCount, ref IntPtr lpHandles, int bWaitAll, uint dwMilliseconds, int bAlertable);

        [DllImport("kernel32.dll")]
        public static extern UInt32 WaitForSingleObject(HANDLE hHandle, UInt32 dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int CloseHandle(HANDLE hObject);

        [DllImport("kernel32.dll")]
        public static extern HANDLE GetCurrentThread();

        [DllImport("kernel32.dll")]
        public static extern UInt32 GetCurrentThreadId();

    }
}
