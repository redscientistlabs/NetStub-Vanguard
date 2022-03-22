using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetStub
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Windows.Forms;
    using libdebug;
    using RTCV.Common;
    using RTCV.CorruptCore;
    using RTCV.NetCore;
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
        private ulong baseAddr { get; }
        private libdebug.Process process { get; set; }
        public int WordSize => 4;
        public byte[] MemoryDump { get; private set; }
        private List<ValueChange> values = new List<ValueChange>();

        public override string ToString()
        {
            return Name;
        }

        public ProcessMemoryDomain(string name, ulong _addr, long size, Process p) 
        {
            baseAddr = _addr;
            Size = size;
            Name = $"{name}:{baseAddr:X}:{Size:X}";
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
            VanguardImplementation.ps4.Notify(222, $"[RTCV] Making a dump of memory domain \"{Name}\"...");
            MemoryDump = VanguardImplementation.ps4.ReadMemory(process.pid, baseAddr, (int)Size);
            VanguardImplementation.ps4.Notify(222, $"[RTCV] ...Dumped!");
        }

        public void UpdateMemory()
        {
            VanguardImplementation.ps4.Notify(222, $"[RTCV] Applying changes to memory domain \"{Name}\"...");
            int i = 0;
            foreach (var value in values)
            {
                VanguardImplementation.ps4.Notify(222, $"[RTCV] Patching value {(i+1)}/{values.Count}...");
                VanguardImplementation.ps4.WriteMemory(process.pid, value.address, value.value);
                i++;
            }
            VanguardImplementation.ps4.Notify(222, $"[RTCV] ...Applied!");
            values.Clear();
            //MemoryDump = null;
        }
    }
    public static class PS4ProcessWatch
    {
        public static object CorruptLock = new object();
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
                var pid = VanguardImplementation.ps4.GetProcessList().FindProcess("eboot.bin").pid;
                List<MemoryDomainProxy> interfaces = new List<MemoryDomainProxy>();
                foreach (var me in VanguardImplementation.ps4.GetProcessMaps(pid).entries)
                {
                    if (me.name.StartsWith("_") || me.name.ToUpper().StartsWith("SCE") || me.name.ToUpper().StartsWith("LIB"))
                    {
                        continue;
                    }
                    if (me.prot == 0x3 || me.prot == 0x1)
                    {
                        ProcessMemoryDomain pmd = new ProcessMemoryDomain(me.name, me.start, (long)(me.end - me.start), VanguardImplementation.ps4.GetProcessList().FindProcess("eboot.bin"));
                        var mi = new MemoryDomainProxy(pmd, true);
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
