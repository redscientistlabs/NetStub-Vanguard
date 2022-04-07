using System;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

// code based on librpc (see librpc folder)
namespace NetStub.StubEndpoints.MacOSX_PPC.RPC
{

    public class PowerMacRPC
    {
        // https://www.csharp-examples.net/reverse-bytes/
        // reverse byte order (16-bit)
        public static ushort ReverseBytes(ushort value)
        {
            return (ushort)((value & 0xFFU) << 8 | (value & 0xFF00U) >> 8);
        }
        // reverse byte order (32-bit)
        public static uint ReverseBytes(uint value)
        {
            return (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
                   (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
        }
        // reverse byte order (64-bit)
        public static ulong ReverseBytes(ulong value)
        {
            return (value & 0x00000000000000FFUL) << 56 | (value & 0x000000000000FF00UL) << 40 |
                   (value & 0x0000000000FF0000UL) << 24 | (value & 0x00000000FF000000UL) << 8 |
                   (value & 0x000000FF00000000UL) >> 8 | (value & 0x0000FF0000000000UL) >> 24 |
                   (value & 0x00FF000000000000UL) >> 40 | (value & 0xFF00000000000000UL) >> 56;
        }
        // reverse byte order (16-bit)
        public static short ReverseBytes(short value)
        {
            return ((short)((value & 0xFF) << 8 | (value & 0xFF00) >> 8));
        }
        // reverse byte order (32-bit)
        public static int ReverseBytes(int value)
        {
            return (int)(((value & 0x000000FF) << 24) | (value & 0x0000FF00) << 8 |
                   (value & 0x00FF0000) >> 8 | (value & 0xFF000000) >> 24);
        }
        // reverse byte order (64-bit)
        public static long ReverseBytes(long value)
        {
            return (value & 0x00000000000000FFL) << 56 | (value & 0x000000000000FF00L) << 40 |
                   (value & 0x0000000000FF0000L) << 24 | (value & 0x00000000FF000000L) << 8 |
                   (value & 0x000000FF00000000L) >> 8 | (value & 0x0000FF0000000000L) >> 24 |
                   (value & 0x00FF000000000000L) >> 40 | (value & -72057594037927936) >> 56;
        }

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

        private static int RPC_PORT = 7828;
        private static uint RPC_PACKET_MAGIC = 0xABADBEEF;
        private static int RPC_MAX_DATA_LEN = 8192;
        private enum RPC_CMDS : uint
        {
            RPC_CMD_READPROC  = 0xBB000001,
            RPC_CMD_WRITEPROC = 0xBB000002,
            RPC_CMD_PROCINFO  = 0xBB000003,
            RPC_CMD_MAPINFO   = 0xBB000004,
            RPC_CMD_DISCONNECT= 0xBB0000FF,
        };

        private enum RPC_STATUS : byte
        {
            RPC_STATUS_SUCCESS = 0,
            RPC_STATUS_READ_ERROR = 0xF0,
            RPC_STATUS_WRITE_ERROR = 0xF1,
            RPC_STATUS_PROC_INVALID = 0xF2,
            RPC_STATUS_GETINFO_ERROR = 0xF3,
            RPC_STATUS_TOO_MUCH_DATA = 0xE0,
        }

        /// <summary>
        /// Initializes PowerMacRPC class
        /// </summary>
        /// <param name="addr">Mac IP address</param>
        public PowerMacRPC(IPAddress addr)
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
        public PowerMacRPC(string ip)
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
            // if status first nibble starts with F
            return (uint)status >> 4 == 15;
        }

        /// <summary>
        /// Connects to Mac
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
        /// Disconnects from Mac
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

        private void SendCMDPacket(RPC_CMDS cmd, uint length)
        {
            SendPacketData(12, RPC_PACKET_MAGIC, (uint)cmd, length);
        }

        private RPC_STATUS ReceiveRPCStatus()
        {
            byte[] status = new byte[1];
            sock.Receive(status, 1, SocketFlags.None);
            sock.Send(status, 1, SocketFlags.None);
            return (RPC_STATUS)status[0];
        }



        private RPC_STATUS CheckRPCStatus()
        {
            RPC_STATUS status = ReceiveRPCStatus();
            if (IsFatalStatus(status) || status == RPC_STATUS.RPC_STATUS_TOO_MUCH_DATA)
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


        private byte[] ReceiveData(int length)
        {
            MemoryStream s = new MemoryStream();

            int left = length;
            int recv = 0;
            while (left > 0)
            {
                byte[] b = new byte[RPC_MAX_DATA_LEN];
                recv = sock.Receive(b, RPC_MAX_DATA_LEN, SocketFlags.None);
                s.Write(b, 0, recv);
                left -= recv;
            }

            byte[] data = s.ToArray();

            s.Dispose();

            return data;
        }
        public byte[] ReadMemory(uint pid, uint address, uint length)
        {
            if (!connected)
            {
                throw new Exception("rpc: not connected");
            }

            SendCMDPacket(RPC_CMDS.RPC_CMD_READPROC, 12);
            SendPacketData(12, pid, address, length);
            //CheckRPCStatus();
            return ReceiveData((int)length);
        }
        public void WriteMemory(uint pid, uint address, byte[] data)
        {
            if (!connected)
            {
                throw new Exception("rpc: not connected");
            }

            if (data.Length > RPC_MAX_DATA_LEN)
            {
                // write RPC_MAX_DATA_LEN
                byte[] nowdata = SubArray(data, 0, RPC_MAX_DATA_LEN);

                SendCMDPacket(RPC_CMDS.RPC_CMD_WRITEPROC, 12);
                SendPacketData(12, pid, address, (uint)RPC_MAX_DATA_LEN);
                //CheckRPCStatus();
                SendData(nowdata, RPC_MAX_DATA_LEN);
                //CheckRPCStatus();

                // call WriteMemory again with rest of it
                int nextlength = data.Length - RPC_MAX_DATA_LEN;
                uint nextaddr = address + (uint)RPC_MAX_DATA_LEN;
                byte[] nextdata = SubArray(data, RPC_MAX_DATA_LEN, nextlength);
                WriteMemory(pid, nextaddr, nextdata);
            }
            else if (data.Length > 0)
            {
                SendCMDPacket(RPC_CMDS.RPC_CMD_WRITEPROC, 12);
                SendPacketData(12, pid, address, (uint)data.Length);
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

            SendCMDPacket(RPC_CMDS.RPC_CMD_PROCINFO, 72);
            C.RPC_PROC_INFO temp = new C.RPC_PROC_INFO(process_name);
            SendPacketData(72, temp.Name, temp.PID, temp.NumMaps);

            RPC_STATUS status = CheckRPCStatus();
            if (status == RPC_STATUS.RPC_STATUS_PROC_INVALID)
            {
                return new Process();
            }

            byte[] data = ReceiveData(72);

            status = CheckRPCStatus();
            if (status == RPC_STATUS.RPC_STATUS_GETINFO_ERROR)
            {
                return new Process();
            }

            temp.PID = ReverseBytes(BitConverter.ToUInt32(data, 64));
            temp.NumMaps = ReverseBytes(BitConverter.ToUInt32(data, 68));
            List<MemoryMap> maps = new List<MemoryMap>();
            for (uint i = 0; i < temp.NumMaps; i++) {
                SendCMDPacket(RPC_CMDS.RPC_CMD_MAPINFO, 4);
                C.RPC_PROC_MAP_INFO temp2 = new C.RPC_PROC_MAP_INFO(temp.PID, i);
                SendPacketData(4, ReverseBytes(temp2.Index));
                byte[] temp2b = ReceiveData(316);
                //status = CheckRPCStatus();
                //if (status == RPC_STATUS.RPC_STATUS_GETINFO_ERROR)
                //{
                //    return new Process();
                //}
                //temp2.Name = BitConverter.ToString(temp2b, 0).ToCharArray(0, 32);
                //temp2.FileName = BitConverter.ToString(temp2b, 32).ToCharArray(0, 255);
                temp2.PID = ReverseBytes(BitConverter.ToUInt32(temp2b, 288));
                temp2.StartAddress = ReverseBytes(BitConverter.ToUInt32(temp2b, 292));
                temp2.EndAddress = ReverseBytes(BitConverter.ToUInt32(temp2b, 296));
                temp2.Size = ReverseBytes(BitConverter.ToUInt32(temp2b, 300));
                //temp2.Protection = (BitConverter.ToString(temp2b, 304).ToCharArray(0, 7));
                temp2.Index = ReverseBytes(BitConverter.ToUInt32(temp2b, 312));
                MemoryMap map = new MemoryMap()
                {
                    Name = GetNullTermString(temp2b, 0),
                    FileName = GetNullTermString(temp2b, 32),
                    PID = temp2.PID,
                    StartAddress = temp2.StartAddress,
                    EndAddress = temp2.EndAddress,
                    Size = temp2.Size,
                    Protection = GetNullTermString(temp2b, 304),
                    Index = temp2.Index
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
        public Byte ReadByte(uint pid, uint address)
        {
            return ReadMemory(pid, address, sizeof(Byte))[0];
        }
        public Char ReadChar(uint pid, uint address)
        {
            return BitConverter.ToChar(ReadMemory(pid, address, sizeof(Char)), 0);
        }
        public Int16 ReadInt16(uint pid, uint address)
        {
            return BitConverter.ToInt16(ReadMemory(pid, address, sizeof(Int16)), 0);
        }
        public UInt16 ReadUInt16(uint pid, uint address)
        {
            return BitConverter.ToUInt16(ReadMemory(pid, address, sizeof(UInt16)), 0);
        }
        public Int32 ReadInt32(uint pid, uint address)
        {
            return BitConverter.ToInt32(ReadMemory(pid, address, sizeof(Int32)), 0);
        }
        public UInt32 ReadUInt32(uint pid, uint address)
        {
            return BitConverter.ToUInt32(ReadMemory(pid, address, sizeof(UInt32)), 0);
        }
        public Int64 ReadInt64(uint pid, uint address)
        {
            return BitConverter.ToInt64(ReadMemory(pid, address, sizeof(Int64)), 0);
        }
        public UInt64 ReadUInt64(uint pid, uint address)
        {
            return BitConverter.ToUInt64(ReadMemory(pid, address, sizeof(UInt64)), 0);
        }

        /** write wrappers **/
        public void WriteByte(uint pid, uint address, Byte value)
        {
            WriteMemory(pid, address, new byte[] { value });
        }
        public void WriteChar(uint pid, uint address, Char value)
        {
            WriteMemory(pid, address, BitConverter.GetBytes(value));
        }
        public void WriteInt16(uint pid, uint address, Int16 value)
        {
            WriteMemory(pid, address, BitConverter.GetBytes(value));
        }
        public void WriteUInt16(uint pid, uint address, UInt16 value)
        {
            WriteMemory(pid, address, BitConverter.GetBytes(value));
        }
        public void WriteInt32(uint pid, uint address, Int32 value)
        {
            WriteMemory(pid, address, BitConverter.GetBytes(value));
        }
        public void WriteUInt32(uint pid, uint address, UInt32 value)
        {
            WriteMemory(pid, address, BitConverter.GetBytes(value));
        }
        public void WriteInt64(uint pid, uint address, Int64 value)
        {
            WriteMemory(pid, address, BitConverter.GetBytes(value));
        }
        public void WriteUInt64(uint pid, uint address, UInt64 value)
        {
            WriteMemory(pid, address, BitConverter.GetBytes(value));
        }

        /* float/double */
        public float ReadSingle(uint pid, uint address)
        {
            return BitConverter.ToSingle(ReadMemory(pid, address, sizeof(float)), 0);
        }
        public void WriteSingle(uint pid, uint address, float value)
        {
            WriteMemory(pid, address, BitConverter.GetBytes(value));
        }
        public double ReadDouble(uint pid, uint address)
        {
            return BitConverter.ToDouble(ReadMemory(pid, address, sizeof(double)), 0);
        }
        public void WriteDouble(uint pid, uint address, double value)
        {
            WriteMemory(pid, address, BitConverter.GetBytes(value));
        }

        /* string */
        public string ReadString(uint pid, uint address)
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
        public void WriteString(uint pid, uint address, string str)
        {
            WriteMemory(pid, address, Encoding.ASCII.GetBytes(str));
            WriteByte(pid, address + (uint)str.Length, 0);
        }
    }
}
