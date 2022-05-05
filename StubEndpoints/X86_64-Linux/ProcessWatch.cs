using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetStub.StubEndpoints.X86_64_Linux
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Windows.Forms;
    using RTCV.Common;
    using RTCV.CorruptCore;
    using RTCV.NetCore;
    using NetStub;
    public class CodeCave : IRPCCodeCave
    {
        public ulong RealAddress { get; set; }
        public int AllocatedSize { get; set; }
        public byte[] Data { get; set; }

        int pid;

        public CodeCave(int pid, ulong realAddress, int allocatedSize)
        {
            this.pid = pid;
            RealAddress = realAddress;
            AllocatedSize = allocatedSize;
        }

        public void DumpMemory()
        {
            Data = VanguardImplementation.linux.ReadMemory((ulong)pid, RealAddress, (ulong)AllocatedSize);
        }

        public void UpdateMemory()
        {
            VanguardImplementation.linux.WriteMemory((ulong)pid, RealAddress, Data);
        }
    }

    public class CodeCavesDomain : IRPCCodeCavesDomain
    {
        public Dictionary<long, ICodeCave> Caves { get; set; } = new Dictionary<long, ICodeCave>();
        public long Size { get; set; } = 0;
        int pid;

        public string Name => "Code Caves";

        public override string ToString()
        {
            return Name;
        }

        public int WordSize => 8;

        public bool BigEndian => false;

        public CodeCavesDomain(int _pid)
        {
            pid = _pid;
        }

        public (long, ulong) AllocateMemory(int size)
        {
            //while (/*ProcessWatch.RPCBeingUsed*/false)
            //{
            //}
            //ProcessWatch.RPCBeingUsed = true;
            //ulong addr = VanguardImplementation.linux.AllocateMemory((ulong)pid, (ulong)size);
            //CodeCave codeCave = new CodeCave(pid, addr, size);
            if (ProcessWatch.CaveList == null || ProcessWatch.CaveSize != size)
            {
                Caves.Clear();
                Size = 0;
                ProcessWatch.CaveList = ProcessWatch.FindCodeCaves(size);
            }
            Random r = new Random();
            var codeCave = new CodeCave(pid, ProcessWatch.CaveList[r.Next(ProcessWatch.CaveList.Count - 1)].Item2, size);
            foreach (var cc in Caves.Values)
            {
                if (cc.RealAddress == codeCave.RealAddress)
                {
                    codeCave = new CodeCave(pid, ProcessWatch.CaveList[r.Next(ProcessWatch.CaveList.Count - 1)].Item2, size);
                }
            }
            foreach (var cc in Caves.Values)
            {
                if (cc.RealAddress == codeCave.RealAddress)
                {
                    codeCave = new CodeCave(pid, ProcessWatch.CaveList[r.Next(ProcessWatch.CaveList.Count - 1)].Item2, size);
                }
            }
            foreach (var cc in Caves.Values)
            {
                if (cc.RealAddress == codeCave.RealAddress)
                {
                    // if we still can't get a new codecave, break
                    return (0, 0);
                }
            }
            long fake_addr = Size;
            Caves[fake_addr] = codeCave;
            Size = fake_addr + size;
            if (Size % WordSize != 0)
            {
                while (Size % WordSize != 0)
                {
                    Size++;
                }
            }

            S.GET<StubForm>().lbCaveCount.Text = $"Available Caves: {ProcessWatch.CaveList.Count - Caves.Count}/{ProcessWatch.CaveList.Count}";
            //ProcessWatch.RPCBeingUsed = false;
            return (fake_addr, codeCave.RealAddress);
        }

        public void DisposeCave(long addr)
        {
            Caves.Remove(addr);
        }

        public void DumpMemory()
        {
            while (ProcessWatch.RPCBeingUsed)
            {
            }
            ProcessWatch.RPCBeingUsed = true;
            foreach (var cave in Caves)
            {
                (cave.Value as IRPCCodeCave).DumpMemory();
            }
            ProcessWatch.RPCBeingUsed = false;
        }

        public (long, ICodeCave) GetCodeCave(long addr)
        {
            foreach (var cave in Caves)
            {
                if (addr >= cave.Key && addr < (cave.Key + cave.Value.AllocatedSize))
                {
                    return (cave.Key, cave.Value);
                }
            }
            return (0, null);
        }

        public byte PeekByte(long addr)
        {
            var cave = GetCodeCave(addr);
            if (cave.Item2 != null)
            {
                return cave.Item2.Data[addr - cave.Item1];
            }
            return 0;
        }

        public byte[] PeekBytes(long address, int length)
        {
            if (address + length > Size)
            {
                return null;
            }
            byte[] ret = new byte[length];

            for (int i = 0; i < length; i++)
                ret[i] = PeekByte(address + i);
            return ret;
        }

        public void PokeByte(long addr, byte val)
        {
            var cave = GetCodeCave(addr);
            if (cave.Item2 != null)
            {
                cave.Item2.Data[addr - cave.Item1] = val;
            }
        }

        public void PokeBytes(long addr, byte[] val)
        {
            if (addr + val.Length > Size)
            {
                return;
            }

            for (int i = 0; i < val.Length; i++)
                PokeByte(addr + i, val[i]);
        }

        public void UpdateMemory()
        {
            while (ProcessWatch.RPCBeingUsed)
            {
            }
            ProcessWatch.RPCBeingUsed = true;
            foreach (var cave in Caves)
            {
                (cave.Value as CodeCave).UpdateMemory();
            }
            ProcessWatch.RPCBeingUsed = false;
        }
    }
    public class ProcessMemoryDomain : IRPCMemoryDomain, ICodeCavable
    {
        public struct ValueChange
        {
            public ulong address;
            public byte[] value;
        }
        public string Name { get; }
        public bool BigEndian => false;
        public long Size { get; }
        public Mutex mutex;
        public ulong baseAddr { get; private set; }
        private Process process { get; set; }
        public int WordSize => 8;
        public byte[] MemoryDump { get; private set; }
        public ICodeCavesDomain CodeCaves { get; set; } = ProcessWatch.CodeCaves;

        private List<ValueChange> values = new List<ValueChange>();

        public override string ToString()
        {
            return Name;
        }

        public ProcessMemoryDomain(int index, string name, ulong _addr, long size, bool[] prot, Process p)
        {
            baseAddr = _addr;
            char[] protection = new char[3];
            if (prot[0] == true)
                protection[0] = 'r';
            else
                protection[0] = '-';
            if (prot[1] == true)
                protection[1] = 'w';
            else
                protection[1] = '-';
            if (prot[2] == true)
                protection[2] = 'x';
            else
                protection[2] = '-';
            Size = size;
            Name = $"{index}:{name}:{new string(protection)}:{baseAddr:X}:{(Size / 1024f / 1024f):0.00}MB";
            process = p;
            mutex = new Mutex();
        }

        public byte PeekByte(long addr)
        {
            if (addr < 0 || addr >= Size)
            {
                return 0;
            }

            return MemoryDump[addr];
        }

        public byte[] PeekBytes(long address, int length)
        {
            if (address + length > Size)
            {
                return null;
            }
            byte[] ret = new byte[length];

            for (int i = 0; i < length; i++)
                ret[i] = PeekByte(address + i);
            return ret;
        }

        public void PokeByte(long addr, byte val)
        {
            if (addr < 0 || addr >= Size)
                return;
            byte[] arr = new byte[] { val };
            PokeBytes(addr, arr);
        }

        public void PokeBytes(long addr, byte[] val)
        {
            //ulong uaddr = (ulong)addr;
            if (addr + val.Length >= Size || addr < 0)
            {
                return;
            }
            values.Add(new ValueChange() { address = baseAddr + (ulong)addr, value = val });
        }

        public void DumpMemory()
        {
            while (ProcessWatch.RPCBeingUsed)
            {

            }
            ProcessWatch.RPCBeingUsed = true;
            MemoryDump = VanguardImplementation.linux.ReadMemory(process.PID, baseAddr, (ulong)Size);
            ProcessWatch.RPCBeingUsed = false;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public void UpdateMemory()
        {
            while (ProcessWatch.RPCBeingUsed)
            {

            }
            ProcessWatch.RPCBeingUsed = true;
            int i = 0;
            foreach (var value in values)
            {
                //VanguardImplementation.linux.Notify(222, $"[RTCV] Patching value {(i+1)}/{values.Count}...");
                VanguardImplementation.linux.WriteMemory(process.PID, value.address, value.value);
                i++;
            }
            values.Clear();
            ProcessWatch.RPCBeingUsed = false;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            //MemoryDump = null;
        }
        public byte[] GetMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return MemoryDump;
        }
    }
    public static class ProcessWatch
    {
        public static object CorruptLock = new object();
        public static bool RPCBeingUsed = false;
        public static bool OverrideExceptionHandlers = false;
        public static bool ExceptionHandlerApplied = false;
        public static List<ProcessMemoryDomain> RTCVMadeDomains = new List<ProcessMemoryDomain>();

        public static CodeCavesDomain CodeCaves { get; set; }

        public static List<(long, ulong)> CaveList { get; set; }

        public static int CaveSize = 0;

        public static List<(long, ulong)> FindCodeCaves(int requested_size)
        {
            CaveSize = requested_size;
            List<(long, ulong)> ret = new List<(long, ulong)>();
            if (requested_size < 0)
            {
                return null;
            }
            foreach (var domain in (string[])AllSpec.UISpec[UISPEC.SELECTEDDOMAINS_FORCAVESEARCH])
            {
                if (domain == null || domain == "Code Caves")
                {
                    continue;
                }
                var mi = MemoryDomains.GetInterface(domain) as MemoryDomainProxy;
                var pmd = mi.MD as ProcessMemoryDomain;
                pmd.DumpMemory();
                int byte_count = 0;
                long ccaddress = 0;
                for (long a = 0; a < pmd.Size; a++)
                {
                    if (a == pmd.Size - 1)
                    {
                        if (byte_count >= requested_size)
                        {
                            if (byte_count > requested_size)
                            {
                                while (byte_count > requested_size)
                                {
                                    long address = ccaddress;
                                    ulong real_address = pmd.baseAddr + (ulong)address;
                                    ret.Add((address, real_address));
                                    byte_count -= requested_size;
                                }
                            }
                            else
                            {
                                long address = ccaddress;
                                ulong real_address = pmd.baseAddr + (ulong)address;
                                ret.Add((address, real_address));
                            }
                        }
                        byte_count = 0;
                        ccaddress = a;
                    }
                    if (pmd.PeekByte(a) == 0)
                    {
                        byte_count++;
                    }
                    else
                    {
                        if (byte_count >= requested_size)
                        {
                            if (byte_count > requested_size)
                            {
                                while (byte_count > requested_size)
                                {
                                    long address = ccaddress;
                                    ulong real_address = pmd.baseAddr + (ulong)address;
                                    ret.Add((address, real_address));
                                    byte_count -= requested_size;
                                }
                            } else
                            {
                                long address = ccaddress;
                                ulong real_address = pmd.baseAddr + (ulong)address;
                                ret.Add((address, real_address));
                            }
                        }
                        byte_count = 0;
                        ccaddress = a;
                    }
                }
            }
            S.GET<StubForm>().lbCaveCount.Text = $"Available Caves: {ret.Count}";
            return ret;
        }

        public static MemoryInterface GetMemoryInterfaceByBaseAddr(ulong addr)
        {
            var domains = (string[])AllSpec.UISpec[UISPEC.SELECTEDDOMAINS];
            for (int i = 0; i < domains.Length; i++)
            {
                var domain = domains[i];
                MemoryInterface mi = MemoryDomains.GetInterface(domain);
                if (((ProcessMemoryDomain)((mi as MemoryDomainProxy).MD as IRPCMemoryDomain)).baseAddr == addr)
                {
                    return mi;
                }
            }
            return null;
        }
        public static void Start()
        {
            StubForm.AutoCorruptTimer = new System.Timers.Timer();
            StubForm.AutoCorruptTimer.Interval = 16;
            StubForm.AutoCorruptTimer.AutoReset = false;
            StubForm.AutoCorruptTimer.Elapsed += StepCorrupt;
            StubForm.AutoCorruptTimer.Start();
        }
        private static void StepCorrupt(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!VanguardCore.vanguardConnected || !VanguardCore.vanguardStarted || AllSpec.CorruptCoreSpec == null)
            {
                StubForm.AutoCorruptTimer.Start();
                return;
            }
            lock (CorruptLock)
            {
                StepActions.Execute();
                RtcClock.StepCorrupt(true, true);
            }
            StubForm.AutoCorruptTimer.Start();

        }
        public static void UpdateDomains()
        {
            if (!VanguardCore.vanguardConnected)
            {
                return;
            }
            try
            {
                PartialSpec gameDone = new PartialSpec("VanguardSpec");
                gameDone[VSPEC.SYSTEM] = "Linux";
                gameDone[VSPEC.GAMENAME] = VanguardImplementation.ProcessName;
                gameDone[VSPEC.SYSTEMPREFIX] = "Linux";
                gameDone[VSPEC.SYSTEMCORE] = "NetStub";
                gameDone[VSPEC.OPENROMFILENAME] = VanguardImplementation.ProcessName;
                gameDone[VSPEC.MEMORYDOMAINS_BLACKLISTEDDOMAINS] = Array.Empty<string>();
                gameDone[VSPEC.MEMORYDOMAINS_INTERFACES] = GetInterfaces();
                gameDone[VSPEC.CORE_DISKBASED] = false;
                AllSpec.VanguardSpec.Update(gameDone);

                //This is local. If the domains changed it propgates over netcore
                LocalNetCoreRouter.Route(RTCV.NetCore.Endpoints.CorruptCore, RTCV.NetCore.Commands.Remote.EventDomainsUpdated, true, true);

                //Asks RTC to restrict any features unsupported by the stub
                LocalNetCoreRouter.Route(RTCV.NetCore.Endpoints.CorruptCore, RTCV.NetCore.Commands.Remote.EventRestrictFeatures, true, true);

            }
            catch (Exception ex)
            {
                if (VanguardCore.ShowErrorDialog(ex) == DialogResult.Abort)
                {
                    throw new RTCV.NetCore.AbortEverythingException();
                }
            }
        }
        public static MemoryDomainProxy[] GetInterfaces()
        {
            try
            {

                Console.WriteLine($"getInterfaces()");
                var process = VanguardImplementation.linux.GetProcessInfo(VanguardImplementation.ProcessName);
                var pid = process.PID;
                var pm = process.Maps;
                List<MemoryDomainProxy> interfaces = new List<MemoryDomainProxy>();
                if (CodeCaves == null)
                    CodeCaves = new CodeCavesDomain((int)pid);
                interfaces.Add(new MemoryDomainProxy(CodeCaves, true, false));
                foreach (var me in pm)
                {
                    if ((me.Size) >= int.MaxValue)
                    {
                        continue;
                    }
                    if (true)
                    {
                        ProcessMemoryDomain pmd = new ProcessMemoryDomain((int)me.Index, me.FileName, me.StartAddress, (long)me.Size, new bool[] { me.IsReadable, me.IsWritable, me.IsExecutable }, process);
                        var mi = new MemoryDomainProxy(pmd, true, false);
                        interfaces.Add(mi);
                    }
                }
                return interfaces.ToArray();
            }
            catch (Exception ex)
            {
                if (VanguardCore.ShowErrorDialog(ex, true) == DialogResult.Abort)
                    throw new RTCV.NetCore.AbortEverythingException();

                return Array.Empty<MemoryDomainProxy>();
            }
        }
    }
}
