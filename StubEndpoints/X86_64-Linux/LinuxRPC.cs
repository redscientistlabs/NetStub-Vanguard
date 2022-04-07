using System;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace NetStub.StubEndpoints.X86_64_Linux
{
    public class LinuxRPC
    {

        private Socket sock = null;
        private IPEndPoint enp = null;
        private bool connected = false;
        public bool IsConnected
        {
            get
            {
                return connected;
            }
        }

        private static int RPC_PORT = 0xBEEF;
        private static uint RPC_PACKET_MAGIC = 0xABADBEEF;
        private static int RPC_MAX_DATA_LEN = 8192;
        private enum RPC_CMDS : uint
        {
            RPC_CMD_READPROC = 0xBB000001,
            RPC_CMD_WRITEPROC = 0xBB000002,
            RPC_CMD_PROCINFO = 0xBB000003,
            RPC_CMD_MAPINFO = 0xBB000004,
            RPC_CMD_ALLOCATE = 0xBB000005,
            RPC_CMD_DISCONNECT = 0xBB0000FF,
        };

        private enum RPC_STATUS : uint
        {
            RPC_STATUS_SUCCESS = 0x00000000,
            RPC_STATUS_READ_DONE = 0x00001000,
            RPC_STATUS_READ_ERROR = 0x80001000,
            RPC_STATUS_WRITE_ERROR = 0x80001001,
            RPC_STATUS_PROC_INVALID = 0x80001002,
            RPC_STATUS_GETINFO_ERROR = 0x80001003,
            RPC_STATUS_ALLOCATION_ERROR = 0x80001004,
            RPC_STATUS_TOO_MUCH_DATA = 0xFF000001,
        }

        /// <summary>
        /// Initializes RPC class
        /// </summary>
        /// <param name="addr">Endpoint IP address</param>
        public LinuxRPC(IPAddress addr)
        {
            enp = new IPEndPoint(addr, RPC_PORT);
            sock = new Socket(enp.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };
            sock.ReceiveTimeout = sock.SendTimeout = 5 * 1000;
        }

        /// <summary>
        /// Initializes PowerMacRPC class
        /// </summary>
        /// <param name="ip">Mac ip address</param>
        public LinuxRPC(string ip)
        {
            IPAddress addr = null;
            try
            {
                addr = IPAddress.Parse(ip);
            }
            catch (FormatException ex)
            {
                throw ex;
            }

            enp = new IPEndPoint(addr, RPC_PORT);
            sock = new Socket(enp.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
            };
        }

        private static bool IsFatalStatus(RPC_STATUS status)
        {
            // if status first byte is FF
            return ((((uint)status & 0xFF000000) >> 24) == 0xFF);
        }

        private static bool IsErrorStatus(RPC_STATUS status)
        {
            // if status first byte is 80
            return ((((uint)status & 0xFF000000) >> 24) == 0x80);
        }

        /// <summary>
        /// Connects to endpoint
        /// </summary>
        public void Connect()
        {
            if (!connected)
            {
                sock.Connect(enp);
                connected = true;
            }
        }

        /// <summary>
        /// Disconnects from endpoint
        /// </summary>
        public void Disconnect()
        {
            //SendCMDPacket(RPC_CMDS.RPC_CMD_DISCONNECT, 0);
            sock.Dispose();
            connected = false;
        }

        private void SendPacketData(int length, params object[] fields)
        {
            MemoryStream rs = new MemoryStream();
            foreach (object field in fields)
            {
                byte[] bytes = null;

                // todo: clean up and find better way
                if (field.GetType() == typeof(char))
                {
                    bytes = BitConverter.GetBytes((char)field);
                }
                else if (field.GetType() == typeof(byte))
                {
                    bytes = BitConverter.GetBytes((byte)field);
                }
                else if (field.GetType() == typeof(short))
                {
                    bytes = BitConverter.GetBytes((short)field);
                }
                else if (field.GetType() == typeof(ushort))
                {
                    bytes = BitConverter.GetBytes((ushort)field);
                }
                else if (field.GetType() == typeof(int))
                {
                    bytes = BitConverter.GetBytes((int)field);
                }
                else if (field.GetType() == typeof(uint))
                {
                    bytes = BitConverter.GetBytes((uint)field);
                }
                else if (field.GetType() == typeof(long))
                {
                    bytes = BitConverter.GetBytes((long)field);
                }
                else if (field.GetType() == typeof(ulong))
                {
                    bytes = BitConverter.GetBytes((ulong)field);
                }
                else if (field.GetType() == typeof(byte[]))
                {
                    bytes = (byte[])field;
                }
                else if (field.GetType() == typeof(char[]))
                {
                    bytes = System.Text.Encoding.ASCII.GetBytes((char[])field);
                }

                rs.Write(bytes, 0, bytes.Length);
            }

            SendData(rs.ToArray(), length);
            rs.Dispose();
        }

        private void SendCMDPacket(RPC_CMDS cmd, ulong length)
        {
            SendPacketData(16, RPC_PACKET_MAGIC, (uint)cmd, length);
        }

        private RPC_STATUS ReceiveRPCStatus()
        {
            byte[] status = new byte[4];
            sock.Receive(status, 4, SocketFlags.None);
            sock.Send(new byte[4] { 0x00, 0x00, 0x00, 0x00 }, SocketFlags.None);
            return (RPC_STATUS)BitConverter.ToUInt32(status, 0);
        }



        private RPC_STATUS CheckRPCStatus()
        {
            RPC_STATUS status = ReceiveRPCStatus();
            if (IsFatalStatus(status) || IsErrorStatus(status))
            {
                throw new Exception("rpc: " + status);
            }

            return status;
        }

        private static byte[] SubArray(byte[] data, int offset, int length)
        {
            byte[] bytes = new byte[length];
            Buffer.BlockCopy(data, offset, bytes, 0, length);
            return bytes;
        }

        private void SendData(byte[] data, int length)
        {
            int left = length;
            int offset = 0;
            int sent = 0;
            while (left > 0)
            {
                if (left > RPC_MAX_DATA_LEN)
                {
                    byte[] bytes = SubArray(data, offset, RPC_MAX_DATA_LEN);
                    sent = sock.Send(bytes, RPC_MAX_DATA_LEN, SocketFlags.None);
                    offset += sent;
                    left -= sent;
                }
                else
                {
                    byte[] bytes = SubArray(data, offset, left);
                    sent = sock.Send(bytes, left, SocketFlags.None);
                    offset += sent;
                    left -= sent;
                }
            }
        }


        private byte[] ReceiveData(int length, bool recieving_memory = false)
        {
            MemoryStream s = new MemoryStream();

            int left = length;
            int recv = 0;
            int offset = 0;

            while (left > 0)
            {
                byte[] b = new byte[RPC_MAX_DATA_LEN];
                recv = sock.Receive(b, (left > RPC_MAX_DATA_LEN) ? RPC_MAX_DATA_LEN : left, SocketFlags.None);
                s.Write(b, 0, recv);
                offset += recv;
                left -= recv;
            }

            byte[] data = s.ToArray();

            s.Dispose();

            return data;
        }
        public ulong AllocateMemory(ulong pid, ulong size)
        {
            if (!connected)
            {
                throw new Exception("rpc: not connected");
            }
            SendCMDPacket(RPC_CMDS.RPC_CMD_ALLOCATE, 24);
            SendPacketData(24, pid, size, (short)0, (short)0, (short)0);
            CheckRPCStatus();
            return BitConverter.ToUInt64(ReceiveData(8), 0);
        }
        public byte[] ReadMemory(ulong pid, ulong address, ulong length)
        {
            if (!connected)
            {
                throw new Exception("rpc: not connected");
            }
            SendCMDPacket(RPC_CMDS.RPC_CMD_READPROC, 24);
            SendPacketData(24, pid, address, length);
            //CheckRPCStatus();
            return ReceiveData((int)length, true);
        }
        public void WriteMemory(ulong pid, ulong address, byte[] data)
        {
            if (!connected)
            {
                throw new Exception("rpc: not connected");
            }

            if (data.Length > RPC_MAX_DATA_LEN)
            {
                // write RPC_MAX_DATA_LEN
                byte[] nowdata = SubArray(data, 0, RPC_MAX_DATA_LEN);

                SendCMDPacket(RPC_CMDS.RPC_CMD_WRITEPROC, 24);
                SendPacketData(24, pid, address, (ulong)RPC_MAX_DATA_LEN);
                //CheckRPCStatus();
                SendData(nowdata, RPC_MAX_DATA_LEN);
                //CheckRPCStatus();

                // call WriteMemory again with rest of it
                int nextlength = data.Length - RPC_MAX_DATA_LEN;
                var nextaddr = address + (ulong)RPC_MAX_DATA_LEN;
                byte[] nextdata = SubArray(data, RPC_MAX_DATA_LEN, nextlength);
                WriteMemory(pid, nextaddr, nextdata);
            }
            else if (data.Length > 0)
            {
                SendCMDPacket(RPC_CMDS.RPC_CMD_WRITEPROC, 24);
                SendPacketData(24, pid, address, (ulong)data.Length);
                //CheckRPCStatus();
                SendData(data, data.Length);
                //CheckRPCStatus();
            }
        }

        private static string GetNullTermString(byte[] data, int offset)
        {
            int length = Array.IndexOf<byte>(data, 0, offset) - offset;
            if (length < 0)
            {
                length = data.Length - offset;
            }

            return Encoding.ASCII.GetString(data, offset, length);
        }

        public Process GetProcessInfo(string process_name)
        {
            if (!connected)
            {
                throw new Exception("rpc: not connected");
            }

            SendCMDPacket(RPC_CMDS.RPC_CMD_PROCINFO, 80);
            C.RPC_PROC_INFO temp = new C.RPC_PROC_INFO(process_name);
            SendPacketData(80, temp.Name, temp.PID, temp.NumMaps);

            RPC_STATUS status = CheckRPCStatus();
            if (status == RPC_STATUS.RPC_STATUS_PROC_INVALID)
            {
                return new Process();
            }

            byte[] data = ReceiveData(80);

            status = CheckRPCStatus();
            if (status == RPC_STATUS.RPC_STATUS_GETINFO_ERROR)
            {
                return new Process();
            }

            temp.PID = (BitConverter.ToUInt32(data, 64));
            temp.NumMaps = (BitConverter.ToUInt32(data, 72));
            List<MemoryMap> maps = new List<MemoryMap>();
            for (uint i = 0; i < temp.NumMaps; i++)
            {
                SendCMDPacket(RPC_CMDS.RPC_CMD_MAPINFO, 8);
                C.RPC_PROC_MAP_INFO temp2 = new C.RPC_PROC_MAP_INFO(temp.PID, i);
                SendPacketData(8, (temp2.Index));
                byte[] temp2b = ReceiveData(336);
                //status = CheckRPCStatus();
                //if (status == RPC_STATUS.RPC_STATUS_GETINFO_ERROR)
                //{
                //    return new Process();
                //}
                //temp2.Name = BitConverter.ToString(temp2b, 0).ToCharArray(0, 32);
                //temp2.FileName = BitConverter.ToString(temp2b, 32).ToCharArray(0, 255);
                temp2.PID = (BitConverter.ToUInt64(temp2b, 288));
                temp2.StartAddress = (BitConverter.ToUInt64(temp2b, 296));
                temp2.EndAddress = (BitConverter.ToUInt64(temp2b, 304));
                temp2.Size = (BitConverter.ToUInt64(temp2b, 312));
                //temp2.Protection = (BitConverter.ToString(temp2b, 304).ToCharArray(0, 7));
                temp2.is_readable = BitConverter.ToInt16(temp2b, 320);
                temp2.is_writable = BitConverter.ToInt16(temp2b, 322);
                temp2.is_executable = BitConverter.ToInt16(temp2b, 324);
                temp2.Index = (BitConverter.ToUInt32(temp2b, 328));
                MemoryMap map = new MemoryMap()
                {
                    Name = GetNullTermString(temp2b, 0),
                    FileName = GetNullTermString(temp2b, 32),
                    PID = temp2.PID,
                    StartAddress = temp2.StartAddress,
                    EndAddress = temp2.EndAddress,
                    Size = temp2.Size,
                    Index = temp2.Index,
                    IsReadable = (temp2.is_readable == 1),
                    IsWritable = (temp2.is_writable == 1),
                    IsExecutable = (temp2.is_executable == 1),
                };
                maps.Add(map);
                //System.Threading.Thread.Sleep(1000);
            }
            return new Process()
            {
                Name = new string(temp.Name),
                PID = temp.PID,
                NumMaps = temp.NumMaps,
                Maps = maps
            };
        }
        /** read wrappers **/
        public Byte ReadByte(ulong pid, ulong address)
        {
            return ReadMemory(pid, address, sizeof(Byte))[0];
        }
        public Char ReadChar(ulong pid, ulong address)
        {
            return BitConverter.ToChar(ReadMemory(pid, address, sizeof(Char)), 0);
        }
        public Int16 ReadInt16(ulong pid, ulong address)
        {
            return BitConverter.ToInt16(ReadMemory(pid, address, sizeof(Int16)), 0);
        }
        public UInt16 ReadUInt16(ulong pid, ulong address)
        {
            return BitConverter.ToUInt16(ReadMemory(pid, address, sizeof(UInt16)), 0);
        }
        public Int32 ReadInt32(ulong pid, ulong address)
        {
            return BitConverter.ToInt32(ReadMemory(pid, address, sizeof(Int32)), 0);
        }
        public UInt32 ReadUInt32(ulong pid, ulong address)
        {
            return BitConverter.ToUInt32(ReadMemory(pid, address, sizeof(UInt32)), 0);
        }
        public Int64 ReadInt64(ulong pid, ulong address)
        {
            return BitConverter.ToInt64(ReadMemory(pid, address, sizeof(Int64)), 0);
        }
        public UInt64 ReadUInt64(ulong pid, ulong address)
        {
            return BitConverter.ToUInt64(ReadMemory(pid, address, sizeof(UInt64)), 0);
        }

        /** write wrappers **/
        public void WriteByte(ulong pid, ulong address, Byte value)
        {
            WriteMemory(pid, address, new byte[] { value });
        }
        public void WriteChar(ulong pid, ulong address, Char value)
        {
            WriteMemory(pid, address, BitConverter.GetBytes(value));
        }
        public void WriteInt16(ulong pid, ulong address, Int16 value)
        {
            WriteMemory(pid, address, BitConverter.GetBytes(value));
        }
        public void WriteUInt16(ulong pid, ulong address, UInt16 value)
        {
            WriteMemory(pid, address, BitConverter.GetBytes(value));
        }
        public void WriteInt32(ulong pid, ulong address, Int32 value)
        {
            WriteMemory(pid, address, BitConverter.GetBytes(value));
        }
        public void WriteUInt32(ulong pid, ulong address, UInt32 value)
        {
            WriteMemory(pid, address, BitConverter.GetBytes(value));
        }
        public void WriteInt64(ulong pid, ulong address, Int64 value)
        {
            WriteMemory(pid, address, BitConverter.GetBytes(value));
        }
        public void WriteUInt64(ulong pid, ulong address, UInt64 value)
        {
            WriteMemory(pid, address, BitConverter.GetBytes(value));
        }

        /* float/double */
        public float ReadSingle(ulong pid, ulong address)
        {
            return BitConverter.ToSingle(ReadMemory(pid, address, sizeof(float)), 0);
        }
        public void WriteSingle(ulong pid, ulong address, float value)
        {
            WriteMemory(pid, address, BitConverter.GetBytes(value));
        }
        public double ReadDouble(ulong pid, ulong address)
        {
            return BitConverter.ToDouble(ReadMemory(pid, address, sizeof(double)), 0);
        }
        public void WriteDouble(ulong pid, ulong address, double value)
        {
            WriteMemory(pid, address, BitConverter.GetBytes(value));
        }

        /* string */
        public string ReadString(ulong pid, ulong address)
        {
            string str = "";
            uint i = 0;

            while (true)
            {
                byte value = ReadByte(pid, address + i);
                if (value == 0)
                {
                    break;
                }

                str += Convert.ToChar(value);
                i++;
            }

            return str;
        }
        public void WriteString(ulong pid, ulong address, string str)
        {
            WriteMemory(pid, address, Encoding.ASCII.GetBytes(str));
            WriteByte(pid, address + (uint)str.Length, 0);
        }
    }
}
