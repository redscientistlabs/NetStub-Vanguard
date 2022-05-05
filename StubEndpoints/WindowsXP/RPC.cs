using System;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace NetStub.StubEndpoints.WindowsXP
{
    public class RPC
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
            RPC_CMD_FLUSHINSN = 0xBB000005,
            RPC_CMD_PROTECT   = 0xBB000006,
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
            RPC_STATUS_TOO_MUCH_DATA = 0xFF000001,
        }

        /// <summary>
        /// Initializes RPC class
        /// </summary>
        /// <param name="addr">Endpoint IP address</param>
        public RPC(IPAddress addr)
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
        public RPC(string ip)
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

        private void SendCMDPacket(RPC_CMDS cmd, int length)
        {
            SendPacketData(12, RPC_PACKET_MAGIC, (uint)cmd, length);
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
        public void FlushInstructionCache(uint handle, uint address, uint length)
        {
            if (!connected)
            {
                throw new Exception("rpc: not connected");
            }
            SendCMDPacket(RPC_CMDS.RPC_CMD_FLUSHINSN, 12);
            SendPacketData(12, handle, address, length);
        }

        public void SetProtection(uint handle, uint address, uint length, uint protection)
        {
            if (!connected)
            {
                throw new Exception("rpc: not connected");
            }
            SendCMDPacket(RPC_CMDS.RPC_CMD_PROTECT, 16);
            SendPacketData(16, handle, address, length, protection);
        }
        public byte[] ReadMemory(uint handle, uint address, uint length)
        {
            if (!connected)
            {
                throw new Exception("rpc: not connected");
            }
            SendCMDPacket(RPC_CMDS.RPC_CMD_READPROC, 12);
            SendPacketData(12, handle, address, length);
            //CheckRPCStatus();
            return ReceiveData((int)length, true);
        }
        public void WriteMemory(uint handle, uint address, byte[] data)
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
                SendPacketData(12, handle, address, RPC_MAX_DATA_LEN);
                //CheckRPCStatus();
                SendData(nowdata, RPC_MAX_DATA_LEN);
                //CheckRPCStatus();

                // call WriteMemory again with rest of it
                int nextlength = data.Length - RPC_MAX_DATA_LEN;
                var nextaddr = address + RPC_MAX_DATA_LEN;
                byte[] nextdata = SubArray(data, RPC_MAX_DATA_LEN, nextlength);
                WriteMemory(handle, (uint)nextaddr, nextdata);
            }
            else if (data.Length > 0)
            {
                SendCMDPacket(RPC_CMDS.RPC_CMD_WRITEPROC, 12);
                SendPacketData(12, handle, address, (uint)data.Length);
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
            SendPacketData(72, temp.Name, temp.Handle, temp.NumMaps);

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

            temp.Handle = (BitConverter.ToUInt32(data, 64));
            temp.NumMaps = (BitConverter.ToUInt32(data, 68));
            List<MemoryMap> maps = new List<MemoryMap>();
            for (uint i = 0; i < temp.NumMaps; i++)
            {
                SendCMDPacket(RPC_CMDS.RPC_CMD_MAPINFO, 4);
                C.RPC_PROC_MAP_INFO temp2 = new C.RPC_PROC_MAP_INFO(temp.Handle, i);
                SendPacketData(4, (temp2.Index));
                byte[] temp2b = ReceiveData(312);
                //status = CheckRPCStatus();
                //if (status == RPC_STATUS.RPC_STATUS_GETINFO_ERROR)
                //{
                //    return new Process();
                //}
                //temp2.Name = BitConverter.ToString(temp2b, 0).ToCharArray(0, 32);
                //temp2.FileName = BitConverter.ToString(temp2b, 32).ToCharArray(0, 255);
                temp2.Handle = (BitConverter.ToUInt32(temp2b, 288));
                temp2.StartAddress = (BitConverter.ToUInt32(temp2b, 292));
                temp2.EndAddress = (BitConverter.ToUInt32(temp2b, 296));
                temp2.Size = (BitConverter.ToUInt32(temp2b, 300));
                temp2.Protection = BitConverter.ToUInt32(temp2b, 304);
                temp2.Index = (BitConverter.ToUInt32(temp2b, 308));
                MemoryMap map = new MemoryMap()
                {
                    Name = GetNullTermString(temp2b, 0),
                    FileName = GetNullTermString(temp2b, 32),
                    Handle = temp2.Handle,
                    StartAddress = temp2.StartAddress,
                    EndAddress = temp2.EndAddress,
                    Size = temp2.Size,
                    Index = temp2.Index,
                    Protection = temp2.Protection,
                };
                maps.Add(map);
                //System.Threading.Thread.Sleep(1000);
            }
            return new Process()
            {
                Name = new string(temp.Name),
                Handle = temp.Handle,
                NumMaps = temp.NumMaps,
                Maps = maps
            };
        }
    }
}
