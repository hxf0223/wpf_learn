using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace native_pipe
{
	[Flags]
	public enum PipeOpenModeFlags : uint
	{
		PIPE_ACCESS_DUPLEX = 0x00000003,
		PIPE_ACCESS_INBOUND = 0x00000001,
		PIPE_ACCESS_OUTBOUND = 0x00000002,
		FILE_FLAG_FIRST_PIPE_INSTANCE = 0x00080000,
		FILE_FLAG_WRITE_THROUGH = 0x80000000,
		FILE_FLAG_OVERLAPPED = 0x40000000,
		WRITE_DAC = 0x00040000,
		WRITE_OWNER = 0x00080000,
		ACCESS_SYSTEM_SECURITY = 0x01000000
	}

	[Flags]
	public enum PipeModeFlags : uint
	{
		//One of the following type modes can be specified. The same type mode must be specified for each instance of the pipe.
		PIPE_TYPE_BYTE = 0x00000000,
		PIPE_TYPE_MESSAGE = 0x00000004,
		//One of the following read modes can be specified. Different instances of the same pipe can specify different read modes
		PIPE_READMODE_BYTE = 0x00000000,
		PIPE_READMODE_MESSAGE = 0x00000002,
		//One of the following wait modes can be specified. Different instances of the same pipe can specify different wait modes.
		PIPE_WAIT = 0x00000000,
		PIPE_NOWAIT = 0x00000001,
		//One of the following remote-client modes can be specified. Different instances of the same pipe can specify different remote-client modes.
		PIPE_ACCEPT_REMOTE_CLIENTS = 0x00000000,
		PIPE_REJECT_REMOTE_CLIENTS = 0x00000008
	}

	[Flags]
	public enum DesireMode : uint
	{
		GENERIC_READ = 0x80000000,
		GENERIC_WRITE = 0x40000000
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct SECURITY_ATTRIBUTES
	{
		public uint nLength;
		public IntPtr lpSecurityDescriptor;
		public bool bInheritHandle;
	}

	public class Pipe
	{
		public static readonly IntPtr INVALID_HANDLE = new IntPtr(-1);

		[DllImport("kernel32.dll")]
		public static extern IntPtr CreateNamedPipe(
			string lpName,
			uint dwOpenMode,
			uint dwPipeMode,
			uint nMaxInstances,
			uint nOutBufferSize,
			uint nInBufferSize,
			uint nDefaultTimeOut,
			/*[In] ref SECURITY_ATTRIBUTES */ IntPtr lpSecurityAttributes);

		[DllImport( "kernel32.dll", SetLastError = true)]
		public static extern IntPtr CreateFile(
			string lpFileName,
			uint dwDesiredAccess,
			uint dwShareMode,
			/*[In] ref SECURITY_ATTRIBUTES*/ IntPtr lpSecurityAttributes,
			uint dwCreationDisposition,
			uint dwFlagsAndAttributes,
			IntPtr hTemplateFile);

		[DllImport("kernel32.dll")]
		public static extern bool ConnectNamedPipe(
			IntPtr hNamedPipe,
			ref NativeOverlapped lpOverlapped);

		[DllImport( "kernel32.dll" )]
		public static extern bool ConnectNamedPipe(
			IntPtr hNamedPipe,
			IntPtr lpOverlapped );

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool ReadFile(
			IntPtr hFile,
			byte[] lpBuffer,
			uint nNumberOfBytesToRead,
			ref uint lpNumberOfBytesRead,
			ref NativeOverlapped lpOverlapped);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool ReadFile(
			IntPtr hFile,
			byte[] lpBuffer,
			uint nNumberOfBytesToRead,
			ref uint lpNumberOfBytesRead,
			IntPtr lpOverlapped);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool WriteFile(
			IntPtr hFile,
			byte[] lpBuffer,
			uint nNumberOfBytesToWrite,
			ref uint lpNumberOfBytesWritten,
			ref NativeOverlapped lpOverlapped);

		[DllImport("kernel32.dll", SetLastError = true)]
		internal static extern bool WriteFile(
			IntPtr hFile,
			byte[] lpBuffer,
			uint nNumberOfBytesToWrite,
			ref uint lpNumberOfBytesWritten,
			IntPtr lpOverlapped);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool CloseHandle( IntPtr hFile);

		[DllImport("kernel32.dll")]
		public static extern uint GetLastError();

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool PeekNamedPipe(
			IntPtr hFile,
			IntPtr lpBuffer,
			uint nBufferSize,
			ref uint lpBytesRead,
			ref uint lpTotalBytesAvail,
			ref uint lpBytesLeftThisMessage);

	}

}
