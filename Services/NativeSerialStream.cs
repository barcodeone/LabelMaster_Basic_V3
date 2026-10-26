using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using BarcodePrint.Model;

namespace BarcodePrint.Services
{
    /// <summary>
    /// .NET SerialPort의 가상 블루투스 직렬 포트(BthModem - FILE_TYPE_PIPE) 미지원 결함(ArgumentException)을
    /// 완벽히 해결하는 Win32 기반 네이티브 직렬 스트림
    /// </summary>
    public class NativeSerialStream : IDisposable
    {
        private SafeFileHandle _handle;
        private FileStream _stream;
        public string PortName { get; private set; }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool PurgeComm(SafeFileHandle hFile, uint dwFlags);

        public bool IsOpen 
        { 
            get { return _handle != null && !_handle.IsInvalid && !_handle.IsClosed; } 
        }

        public static NativeSerialStream Open(string portName)
        {
            string devPath = portName.StartsWith(@"\\.\", StringComparison.OrdinalIgnoreCase) 
                ? portName 
                : @"\\.\" + portName;

            // 0xC0000000 = GENERIC_READ | GENERIC_WRITE
            // FILE_FLAG_OVERLAPPED = 0x40000000
            SafeFileHandle handle = CreateFile(
                devPath,
                0xC0000000,
                0, // 독점 모드
                IntPtr.Zero,
                3, // OPEN_EXISTING
                0x40000000, // FILE_FLAG_OVERLAPPED
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                int err = Marshal.GetLastWin32Error();
                throw new IOException(string.Format("Win32 CreateFile 실패 (Port: {0}, ErrorCode: {1})", portName, err));
            }

            PurgeComm(handle, 0x0004 | 0x0008); // PURGE_TXCLEAR | PURGE_RXCLEAR

            var res = new NativeSerialStream();
            res.PortName = portName;
            res._handle = handle;
            res._stream = new FileStream(handle, FileAccess.ReadWrite, 4096, true);
            return res;
        }

        public void DiscardBuffers()
        {
            if (IsOpen)
            {
                PurgeComm(_handle, 0x0004 | 0x0008);
            }
        }

        public void Write(byte[] buffer, int offset, int count, int timeoutMs = 2000)
        {
            if (!IsOpen) throw new InvalidOperationException("포트가 열려있지 않습니다.");
            IAsyncResult ar = _stream.BeginWrite(buffer, offset, count, null, null);
            if (!ar.AsyncWaitHandle.WaitOne(timeoutMs))
            {
                throw new TimeoutException(string.Format("포트 쓰기 타임아웃 ({0}ms)", timeoutMs));
            }
            _stream.EndWrite(ar);
            _stream.Flush();
        }

        public void WriteString(string text, int timeoutMs = 2000)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(text);
            Write(bytes, 0, bytes.Length, timeoutMs);
        }

        public int Read(byte[] buffer, int offset, int count, int timeoutMs = 2500)
        {
            if (!IsOpen) throw new InvalidOperationException("포트가 열려있지 않습니다.");
            IAsyncResult ar = _stream.BeginRead(buffer, offset, count, null, null);
            if (!ar.AsyncWaitHandle.WaitOne(timeoutMs))
            {
                return 0;
            }
            try
            {
                return _stream.EndRead(ar);
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// 명령 전송 후 지정된 시간 동안 수신되는 모든 패킷을 모아서 문자열로 반환
        /// </summary>
        public string QueryTsplCommand(string command, int timeoutMs = 2500)
        {
            if (!IsOpen) return null;

            DiscardBuffers();
            WriteString(command);
            PrinterSerialLogger.WriteCommLog("BT-TX", string.Format("[{0}] TX: {1}", PortName, command.Trim()));

            // 첫 응답 수신 대기
            var sw = Stopwatch.StartNew();
            var acc = new List<byte>();
            byte[] buf = new byte[1024];

            // 수신 패킷 수집 루프
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                int waitChunk = Math.Min(300, (int)(timeoutMs - sw.ElapsedMilliseconds));
                if (waitChunk <= 0) break;

                int read = Read(buf, 0, buf.Length, waitChunk);
                if (read > 0)
                {
                    for (int i = 0; i < read; i++) acc.Add(buf[i]);

                    // 응답이 들어왔으면 잠시 추가 데이터가 오는지 짧게(80ms) 추가 확인
                    Thread.Sleep(80);
                    int more = Read(buf, 0, buf.Length, 150);
                    if (more > 0)
                    {
                        for (int i = 0; i < more; i++) acc.Add(buf[i]);
                    }
                    break;
                }
            }

            if (acc.Count > 0)
            {
                string resp = Encoding.ASCII.GetString(acc.ToArray()).Trim();
                PrinterSerialLogger.WriteCommLog("BT-RX", string.Format("[{0}] RX: '{1}' ({2} bytes)", PortName, resp, acc.Count));
                return resp;
            }

            PrinterSerialLogger.WriteCommLog("BT-RX", string.Format("[{0}] 응답 없음 (0 bytes)", PortName));
            return null;
        }

        public void Dispose()
        {
            try
            {
                if (_stream != null)
                {
                    _stream.Close();
                    _stream.Dispose();
                }
            }
            catch { }
            try
            {
                if (_handle != null && !_handle.IsClosed)
                    _handle.Dispose();
            }
            catch { }
            _stream = null;
            _handle = null;
        }
    }
}
