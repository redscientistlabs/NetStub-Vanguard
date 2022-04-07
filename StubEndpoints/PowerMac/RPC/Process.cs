using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetStub.StubEndpoints.MacOSX_PPC.RPC
{
    public class MemoryMap
    {
        public string Name { get; set; }
        public string FileName { get; set; }
        public uint PID { get; set; }
        public uint StartAddress { get; set; }
        public uint EndAddress { get; set; }
        public uint Size { get; set; }
        public string Protection { get; set; }
        public uint Index { get; set; }
    }
    public class Process
    {
        public string Name { get; set; }
        public uint PID { get; set; }
        public uint NumMaps { get; set; }
        public List<MemoryMap> Maps { get; set; }
    }

    namespace C
    {
        public class RPC_PROC_INFO
        {
            public char[] Name = new char[64];
            public uint PID = 0;
            public uint NumMaps = 0;

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
            public uint PID = 0;
            public uint StartAddress = 0;
            public uint EndAddress = 0;
            public uint Size = 0;
            public char[] Protection = new char[7];
            public char Padding2 = (char)0;
            public uint Index = 0;

            public RPC_PROC_MAP_INFO(uint pid, uint index)
            {
                PID = pid;
                Index = index;
            }
        }
    }
}
