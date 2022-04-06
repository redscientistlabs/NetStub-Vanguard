using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetStub.Clients.PS4
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Windows.Forms;
    using libdebug;
    using RTCV.Common;
    using RTCV.CorruptCore;
    using RTCV.NetCore;
    using NetStub;

    public class ProcessMemoryDomain : IRPCMemoryDomain
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
        private libdebug.Process process { get; set; }
        public int WordSize => 8;
        public byte[] MemoryDump { get; private set; }
        private List<ValueChange> values = new List<ValueChange>();

        public override string ToString()
        {
            return Name;
        }

        public ProcessMemoryDomain(string name, ulong _addr, long size, int prot, Process p)
        {
            baseAddr = _addr;
            string protection = $"{prot}";
            switch (prot)
            {
                case 1:
                    protection = "r";
                    break;
                case 2:
                    protection = "w";
                    break;
                case 3:
                    protection = "rw";
                    break;
                case 4:
                    protection = "x";
                    break;
                case 5:
                    protection = "rx";
                    break;
                case 7:
                    protection = "rwx";
                    break;
            }
            Size = size;
            Name = (name == "(NoName)clienthandler") ? "clienthandler" : $"{name}:{protection}:{baseAddr:X}:{(Size / 1024f / 1024f):0.00}MB";
            process = p;
            mutex = new Mutex();
        }

        public byte PeekByte(long addr)
        {
            if (addr < 0 || addr >= Size)
            {
                return 0;
            }
            //ulong uaddr = (ulong)addr;
            //if (uaddr >= (ulong)Size || uaddr < 0)
            //{
            //    return 0;
            //}
            //ulong address = baseAddr + uaddr;
            //var ret = VanguardImplementation.ps4.ReadMemory<byte>(process.pid, address);
            //return ret;

            return MemoryDump[addr];
        }

        public byte[] PeekBytes(long address, int length)
        {
            //byte[] ret = new byte[length];
            //ulong uaddr = (ulong)address;
            //if (uaddr >= (ulong)Size || uaddr < 0)
            //{
            //    return ret;
            //}
            //uaddr += baseAddr;
            //ret = VanguardImplementation.ps4.ReadMemory(process.pid, uaddr, length);
            //return ret;
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
            //ulong uaddr = (ulong)addr;
            //if (uaddr >= (ulong)Size || uaddr < 0)
            //{
            //    return;
            //}
            //uaddr += baseAddr;
            //VanguardImplementation.ps4.Notify(222, $"Poking...");
            //VanguardImplementation.ps4.WriteMemory(process.pid, uaddr, val);
            //MemoryDump[addr] = val;
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
            //uaddr += baseAddr;
            //VanguardImplementation.ps4.Notify(222, $"Changing value 0x{BitConverter.ToUInt64(PeekBytes(addr, val.Length), 0):X} at address {addr:X}h to value 0x{BitConverter.ToUInt64(val, 0):X}");
            //VanguardImplementation.ps4.WriteMemory(process.pid, uaddr, val);
            //VanguardImplementation.ps4.Notify(222, $"Value at address {addr:X}h is now 0x{BitConverter.ToUInt64(val, 0):X}");
            values.Add(new ValueChange() { address = baseAddr + (ulong)addr, value = val });
        }

        public void DumpMemory()
        {
            while (ProcessWatch.RPCBeingUsed)
            {

            }
            ProcessWatch.RPCBeingUsed = true;
            NetStub.VanguardImplementation.ps4.Notify(222, $"[RTCV] Making a dump of memory domain \"{Name}\"...");
            MemoryDump = VanguardImplementation.ps4.ReadMemory(process.pid, baseAddr, (int)Size);
            VanguardImplementation.ps4.Notify(222, $"[RTCV] ...Dumped!");
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
            VanguardImplementation.ps4.Notify(222, $"[RTCV] Applying {values.Count} changes to memory domain \"{Name}\"...");
            int i = 0;
            foreach (var value in values)
            {
                //VanguardImplementation.ps4.Notify(222, $"[RTCV] Patching value {(i+1)}/{values.Count}...");
                VanguardImplementation.ps4.WriteMemory(process.pid, value.address, value.value);
                i++;
            }
            VanguardImplementation.ps4.Notify(222, $"[RTCV] ...Applied!");
            values.Clear();
            ProcessWatch.RPCBeingUsed = false;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            //MemoryDump = null;
        }

        (MemoryInterface, ulong, long) IRPCMemoryDomain.AllocateMemory(int size)
        {
            while (ProcessWatch.RPCBeingUsed)
            {

            }
            ProcessWatch.RPCBeingUsed = true;
            //VanguardImplementation.ps4.Notify(222, $"[RTCV] Allocating memory of size {size}...");
            ulong addr = VanguardImplementation.ps4.AllocateMemory(process.pid, size);
            ////string[] selected = (string[])AllSpec.UISpec[UISPEC.SELECTEDDOMAINS];
            //string[] selected = { Name, "clienthandler" };
            //ProcessWatch.UpdateDomains();
            ////AllSpec.UISpec.Update(UISPEC.SELECTEDDOMAINS, MemoryDomains.MemoryInterfaces?.Keys.ToArray());
            var mi = MemoryDomains.GetInterface("clienthandler");
            ////var list = selected.ToList();
            ////list.Add(mi.Name);
            ////selected = list.ToArray();
            //AllSpec.UISpec.Update(UISPEC.SELECTEDDOMAINS, selected);
            //VanguardImplementation.ps4.Notify(222, $"[RTCV] Memory allocated at address {addr:X}!");
            ProcessWatch.RPCBeingUsed = false;
            return (mi, addr, (long)(addr - ((ProcessMemoryDomain)(((MemoryDomainProxy)mi).RPCMD)).baseAddr));

        }

        public void FreeMemory(ulong addr, int size)
        {
            while (ProcessWatch.RPCBeingUsed)
            {

            }
            ProcessWatch.RPCBeingUsed = true;
            VanguardImplementation.ps4.FreeMemory(process.pid, addr, size);
            ProcessWatch.RPCBeingUsed = false;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public byte[] NopInstruction(long instructionAddress)
        {
            ulong addr = baseAddr + (ulong)instructionAddress;
            return new byte[12];
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
        public static ulong LibKernelBase = 0x0;
        public static ulong RPCStubAddress = 0x0;
        public static ulong DummyFuncAddress = 0x0;
        public static byte[] JumpToDummyFunc = null;
        public static List<ProcessMemoryDomain> RTCVMadeDomains = new List<ProcessMemoryDomain>();
        public static MemoryInterface GetMemoryInterfaceByBaseAddr(ulong addr)
        {
            var domains = (string[])AllSpec.UISpec[UISPEC.SELECTEDDOMAINS];
            for (int i = 0; i < domains.Length; i++)
            {
                var domain = domains[i];
                MemoryInterface mi = MemoryDomains.GetInterface(domain);
                if (((ProcessMemoryDomain)(((MemoryDomainProxy)mi).RPCMD)).baseAddr == addr)
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
                gameDone[VSPEC.SYSTEM] = "PS4";
                gameDone[VSPEC.GAMENAME] = VanguardImplementation.ProcessName;
                gameDone[VSPEC.SYSTEMPREFIX] = "PS4";
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
                var process = VanguardImplementation.ps4.GetProcessList().FindProcess(VanguardImplementation.ProcessName);
                var pid = process.pid;
                var pm = VanguardImplementation.ps4.GetProcessMaps(pid);
                var tmp = pm.FindEntry("libkernel.sprx")?.start;
                if (tmp == null)
                {
                    MessageBox.Show("libkernel not found!", "Error");
                    return null;
                }
                LibKernelBase = (ulong)tmp;
                RPCStubAddress = pm.FindEntry("(NoName)clienthandler") == null ? VanguardImplementation.ps4.InstallRPC(pid) : pm.FindEntry("(NoName)clienthandler").start;
                List<MemoryDomainProxy> interfaces = new List<MemoryDomainProxy>();
                foreach (var pmd in RTCVMadeDomains)
                {
                    var mi = new MemoryDomainProxy(pmd, true);
                    interfaces.Add(mi);
                }
                foreach (var me in VanguardImplementation.ps4.GetProcessMaps(pid).entries)
                {
                    if (me.name.StartsWith("_") || me.name.ToUpper().StartsWith("SCE") || me.name.ToUpper().StartsWith("LIB") || me.name.ToUpper().StartsWith("(NONAME)SCE") || me.name.ToUpper().StartsWith("(NONAME)LIB") || (me.end - me.start) >= int.MaxValue)
                    {
                        continue;
                    }
                    if (me.name.StartsWith("(NoName)clienthandler") && interfaces.Find(x => x.Name.StartsWith("clienthandler")) != null)
                        continue;
                    if (true)
                    {
                        ProcessMemoryDomain pmd = new ProcessMemoryDomain(me.name, me.start, (long)(me.end - me.start), (int)me.prot, process);
                        var mi = new MemoryDomainProxy(pmd, true);
                        interfaces.Add(mi);
                    }
                }
                if (DummyFuncAddress == 0) DummyFuncAddress = VanguardImplementation.ps4.AllocateMemory(pid, 8);
                VanguardImplementation.ps4.WriteMemory(pid, DummyFuncAddress, CustomFunctions.DummyFunction);
                byte[] codePatch = new byte[12];
                codePatch[0] = 0x48;
                codePatch[1] = 0xB8;
                ulong v = DummyFuncAddress;
                for (int o = 0; o < 8; o++, v >>= 8)
                    codePatch[2 + o] = (byte)v;
                codePatch[10] = 0xFF;
                codePatch[11] = 0xE0;
                JumpToDummyFunc = codePatch;
                if (OverrideExceptionHandlers && !ExceptionHandlerApplied)
                {
                    var ret = VanguardImplementation.ps4.Call(pid, RPCStubAddress, LibKernelBase + FunctionOffsets.sceKernelInstallExceptionHandler, (uint)1, DummyFuncAddress);
                    ExceptionHandlerApplied = (ret == 0);
                    if (!ExceptionHandlerApplied)
                    {
                        MessageBox.Show($"sceKernelInstallExceptionHandler returned 0x{ret:X}", "Error");
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
