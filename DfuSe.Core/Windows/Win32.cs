using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace DfuSe.Core.Windows
{
    public class Win32
    {
        #region Kernel32

        /// <summary>
        /// Creates or opens a file or I/O device. The most commonly used I/O devices are as follows: file, file stream, directory, physical disk, volume, console buffer, tape drive, communications resource, mailslot, and pipe. The function returns a handle that can be used to access the file or device for various types of I/O depending on the file or device and the flags and attributes specified.
        /// To perform this operation as a transacted operation, which results in a handle that can be used for transacted I/O, use the CreateFileTransacted function.
        /// https://docs.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-createfilea
        /// </summary>
        /// <param name="lpFileName">The name of the file or device to be created or opened. You may use either forward slashes (/) or backslashes (\) in this name.
        /// In the ANSI version of this function, the name is limited to MAX_PATH characters. To extend this limit to 32,767 wide characters, call the Unicode version of the function and prepend "\\?\" to the path. For more information, see Naming Files, Paths, and Namespaces.
        /// For information on special device names, see Defining an MS-DOS Device Name.
        /// To create a file stream, specify the name of the file, a colon, and then the name of the stream.For more information, see File Streams.</param>
        /// <param name="dwDesiredAccess">The requested access to the file or device, which can be summarized as read, write, both or neither zero).
        /// The most commonly used values are GENERIC_READ, GENERIC_WRITE, or both(GENERIC_READ | GENERIC_WRITE). For more information, see Generic Access Rights, File Security and Access Rights, File Access Rights Constants, and ACCESS_MASK.
        /// If this parameter is zero, the application can query certain metadata such as file, directory, or device attributes without accessing that file or device, even if GENERIC_READ access would have been denied.
        /// You cannot request an access mode that conflicts with the sharing mode that is specified by the dwShareMode parameter in an open request that already has an open handle.
        /// For more information, see the Remarks section of this topic and Creating and Opening Files.</param>
        /// <param name="dwShareMode">The requested sharing mode of the file or device, which can be read, write, both, delete, all of these, or none (refer to the following table). Access requests to attributes or extended attributes are not affected by this flag.
        /// If this parameter is zero and CreateFile succeeds, the file or device cannot be shared and cannot be opened again until the handle to the file or device is closed.For more information, see the Remarks section.
        /// You cannot request a sharing mode that conflicts with the access mode that is specified in an existing request that has an open handle. CreateFile would fail and the GetLastError function would return ERROR_SHARING_VIOLATION.
        /// To enable a process to share a file or device while another process has the file or device open, use a compatible combination of one or more of the following values. For more information about valid combinations of this parameter with the dwDesiredAccess parameter, see Creating and Opening Files.</param>
        /// <param name="lpSecurityAttributes">A pointer to a SECURITY_ATTRIBUTES structure that contains two separate but related data members: an optional security descriptor, and a Boolean value that determines whether the returned handle can be inherited by child processes.
        /// This parameter can be <see langword="null"/>.
        /// If this parameter is <see langword="null"/>, the handle returned by <see cref="CreateFile"/> cannot be inherited by any child processes the application may create and the file or device associated with the returned handle gets a default security descriptor.
        /// The lpSecurityDescriptor member of the structure specifies a SECURITY_DESCRIPTOR for a file or device. If this member is <see langword="null"/>, the file or device associated with the returned handle is assigned a default security descriptor.
        /// <see cref="CreateFile"/> ignores the lpSecurityDescriptor member when opening an existing file or device, but continues to use the bInheritHandle member.
        /// The bInheritHandlemember of the structure specifies whether the returned handle can be inherited.
        /// For more information, see the Remarks section.</param>
        /// <param name="dwCreationDisposition">An action to take on a file or device that exists or does not exist.
        /// For devices other than files, this parameter is usually set to OPEN_EXISTING.
        /// For more information, see the Remarks section.</param>
        /// <param name="dwFlagsAndAttributes"></param>
        /// <param name="hTemplateFile"></param>
        /// <returns></returns>
        /// 
        /// https://www.pinvoke.net/default.aspx/kernel32/CreateFile.html
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern SafeFileHandle CreateFile(
           string fileName,
           EFileAccess desiredAccess,
           EFileShare dwShareMode,
           IntPtr securityAttributes,
           ECreationDisposition creationDisposition,
           EFileAttributes flagsAndAttributes,
           IntPtr templateFile);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeFileHandle CreateFileW(
             string filename,
             EFileAccess access,
             EFileShare share,
             IntPtr securityAttributes,
             ECreationDisposition creationDisposition,
             EFileAttributes flagsAndAttributes,
             IntPtr templateFile);

        /// <summary>
        /// Sends a control code directly to a specified device driver, causing the corresponding device to perform the corresponding operation.
        /// </summary>
        /// <param name="hDevice">A handle to the device on which the operation is to be performed. The device is typically a volume, directory, file, or stream. To retrieve a device handle, use the <see cref="CreateFile"/> function. For more information, see Remarks.</param>
        /// <param name="dwIoControlCode">The control code for the operation. This value identifies the specific operation to be performed and the type of device on which to perform it. 
        /// For a list of the control codes, see Remarks.The documentation for each control code provides usage details for the <paramref name="lpInBuffer"/>, <paramref name="nInBufferSize"/>, <paramref name="lpOutBuffer"/>, and <paramref name="nOutBufferSize"/> parameters.</param>
        /// <param name="lpInBuffer">A pointer to the input buffer that contains the data required to perform the operation. The format of this data depends on the value of the <paramref name="dwIoControlCode"/> parameter.
        /// This parameter can be <see langword="null"/> if <paramref name="dwIoControlCode"/> specifies an operation that does not require input data.</param>
        /// <param name="nInBufferSize">The size of the input buffer, in bytes.</param>
        /// <param name="lpOutBuffer">A pointer to the output buffer that is to receive the data returned by the operation. The format of this data depends on the value of the <paramref name="dwIoControlCode"/> parameter.
        /// This parameter can be <see langword="null"/> if <paramref name="dwIoControlCode"/> specifies an operation that does not return data.</param>
        /// <param name="nOutBufferSize">The size of the output buffer, in bytes.</param>
        /// <param name="lpBytesReturned">A pointer to a variable that receives the size of the data stored in the output buffer, in bytes.
        /// If the output buffer is too small to receive any data, the call fails, GetLastError returns ERROR_INSUFFICIENT_BUFFER, and <paramref name="lpBytesReturned"/> is zero.
        /// If the output buffer is too small to hold all of the data but can hold some entries, some drivers will return as much data as fits. In this case, the call fails, GetLastError returns ERROR_MORE_DATA, and lpBytesReturned indicates the amount of data received. Your application should call <see cref="DeviceIoControl"/> again with the same operation, specifying a new starting point.
        /// If <paramref name="lpOverlapped"/> is <see langword="null"/>, <paramref name="lpBytesReturned"/> cannot be <see langword="null"/>. Even when an operation returns no output data and <paramref name="lpOutBuffer"/> is <see langword="null"/>, <see cref="DeviceIoControl"/> makes use of <paramref name="lpBytesReturned"/>. After such an operation, the value of <paramref name="lpBytesReturned"/> is meaningless.
        /// If <paramref name="lpOverlapped"/> is not <see langword="null"/>, <paramref name="lpBytesReturned"/> can be <see langword="null"/>. If this parameter is not <see langword="null"/> and the operation returns data, <paramref name="lpBytesReturned"/> is meaningless until the overlapped operation has completed. To retrieve the number of bytes returned, call GetOverlappedResult. If <paramref name="hDevice"/> is associated with an I/O completion port, you can retrieve the number of bytes returned by calling GetQueuedCompletionStatus.</param>
        /// <param name="lpOverlapped">A pointer to an OVERLAPPED structure.
        /// If <paramref name="hDevice"/> was opened without specifying FILE_FLAG_OVERLAPPED, <paramref name="lpOverlapped"/> is ignored.
        /// If <paramref name="hDevice"/> was opened with the FILE_FLAG_OVERLAPPED flag, the operation is performed as an overlapped (asynchronous) operation. In this case, <paramref name="lpOverlapped"/> must point to a valid OVERLAPPED structure that contains a handle to an event object. Otherwise, the function fails in unpredictable ways.
        /// For overlapped operations, <see cref="DeviceIoControl"/> returns immediately, and the event object is signaled when the operation has been completed. Otherwise, the function does not return until the operation has been completed or an error occurs.</param>
        /// <returns>If the operation completes successfully, the return value is nonzero.
        /// If the operation fails or is pending, the return value is zero. To get extended error information, call GetLastError.</returns>
        /// <remarks>
        /// https://docs.microsoft.com/en-us/windows/win32/api/ioapiset/nf-ioapiset-deviceiocontrol
        /// https://www.pinvoke.net/default.aspx/kernel32.deviceiocontrol
        /// </remarks>
        //[DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        //public static extern bool DeviceIoControl(
        //    SafeFileHandle hDevice,
        //    EIOControlCode ioControlCode,
        //    IntPtr inBuffer,
        //    uint nInBufferSize,
        //    ref IntPtr outBuffer,
        //    int nOutBufferSize,
        //    out uint pBytesReturned,
        //    IntPtr overlapped);


        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            EIOControlCode ioControlCode,
            [MarshalAs(UnmanagedType.AsAny)]
            [In] object InBuffer,
            uint nInBufferSize,
            [MarshalAs(UnmanagedType.AsAny)]
            [Out] object OutBuffer,
            uint nOutBufferSize,
            ref uint pBytesReturned,
            [In] IntPtr Overlapped
            );

        [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool DeviceIoControl(
            SafeFileHandle hDevice,
            EIOControlCode ioControlCode,
            byte[] inBuffer,
            uint nInBufferSize,
            byte[] outBuffer,
            uint nOutBufferSize,
            ref uint pBytesReturned,
            IntPtr overlapped
            );

        #endregion

        #region DeviceIoControl helpers

        public static T DeviceIoControl<T>(
            SafeFileHandle handle,
            EIOControlCode controlCode)
        {
            uint returnedBytes = 0;

            // instantiate output object
            object output = default(T);

            // figure out size of output object
            uint outputSize = (uint)Marshal.SizeOf<T>();

            // invoke DeviceIoControl
            bool success = DeviceIoControl(
                handle,
                controlCode,
                null,
                0,
                output,
                outputSize,
                ref returnedBytes,
                IntPtr.Zero);

            if (!success)
            {
                int lastError = Marshal.GetLastWin32Error();
                
                throw new Win32Exception($"Exception invoking DeviceIoControl with control code {controlCode}: {new Win32Exception(lastError).Message}");
            }

            return (T)output;
        }

        public static T DeviceIoControl<T, V>(
            SafeFileHandle handle,
            EIOControlCode controlCode,
            V input)
        {
            uint returnedBytes = 0;

            // instantiate output object
            object output = default(T);

            // figure out size of output object
            uint outputSize = (uint)Marshal.SizeOf<T>();
            // figure out size of input object
            uint inputSize = (uint)Marshal.SizeOf<V>();

            bool success = DeviceIoControl(
                handle,
                controlCode,
                input,
                inputSize,
                output,
                outputSize,
                ref returnedBytes,
                IntPtr.Zero);

            if (!success)
            {
                int lastError = Marshal.GetLastWin32Error();
                
                throw new Win32Exception($"Exception invoking DeviceIoControl with control code {controlCode}: {new Win32Exception(lastError).Message}");
            }

            return (T)output;
        }

        public static byte[] DeviceIoControl(
            SafeFileHandle handle,
            EIOControlCode controlCode,
            uint input,
            uint outputLength = 512)
        {
            uint returnedBytes = 0;

            byte[] output = new byte[outputLength];

            bool success = DeviceIoControl(
                handle,
                controlCode,
                input,
                1,
                output,
                outputLength,
                ref returnedBytes,
                IntPtr.Zero);

            if (!success)
            {
                int lastError = Marshal.GetLastWin32Error();

                throw new Win32Exception($"Exception invoking DeviceIoControl with control code {controlCode}: {new Win32Exception(lastError).Message}");
            }

            // resize buffer to match returned bytes
            Array.Resize(ref output, (int)returnedBytes);

            return output;
        }

        public static byte[] DeviceIoControl(
            SafeFileHandle handle,
            EIOControlCode controlCode,
            byte[] input,
            uint outputLength = 512)
        {
            uint returnedBytes = 0;

            byte[] output = new byte[outputLength];

            bool success = DeviceIoControl(
                handle,
                controlCode,
                input,
                (uint)input.Length,
                output,
                outputLength,
                ref returnedBytes,
                IntPtr.Zero);

            if (!success)
            {
                int lastError = Marshal.GetLastWin32Error();

                throw new Win32Exception($"Exception invoking DeviceIoControl with control code {controlCode}: {new Win32Exception(lastError).Message}");
            }

            // resize buffer to match returned bytes
            Array.Resize(ref output, (int)returnedBytes);

            return output;
        }

        #endregion
    }
}
