using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetStub.StubEndpoints.X86_64_Linux
{
    public class MemoryMap
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        public ulong PID { get; set; }
        public ulong StartAddress { get; set; }
        public ulong EndAddress { get; set; }
        public ulong Size { get; set; }
        public bool IsReadable { get; set; }
        public bool IsWritable { get; set; }
        public bool IsExecutable { get; set; }
        public ulong Index { get; set; }
    }
    public class Process
    {
        public string Name { get; set; }
        public ulong PID { get; set; }
        public ulong NumMaps { get; set; }
        public List<MemoryMap> Maps { get; set; }
    }
    namespace C
    {
        public class RPC_PROC_INFO
        {
            public char[] Name = new char[64];
            public ulong PID = 0;
            public ulong NumMaps = 0;

            public RPC_PROC_INFO(string name)
            {
                Name = name.ToCharArray();
                Array.Resize(ref Name, 64);
            }
        }

        public class RPC_PROC_MAP_INFO
        {
            public char[] Name = new char[32];
            public char[] FileName = new char[255];
            public char Padding1 = (char)0;
            public ulong PID = 0;
            public ulong StartAddress = 0;
            public ulong EndAddress = 0;
            public ulong Size = 0;
            public short is_readable = 0;
            public short is_writable = 0;
            public short is_executable = 0;
            public char Padding2 = (char)0;
            public ulong Index = 0;

            public RPC_PROC_MAP_INFO(ulong pid, ulong index)
            {
                PID = pid;
                Index = index;
            }
        }
    }
}