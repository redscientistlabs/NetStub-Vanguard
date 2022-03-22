using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetStub.Clients.PowerMac
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Windows.Forms;
    using RPC;
    using RTCV.Common;
    using RTCV.CorruptCore;
    using RTCV.NetCore;

    public class ProcessMemoryDomain : IRPCMemoryDomain
    {
        public struct ValueChange
        {
            public uint address;
            public byte[] value;
        }
        public string Name { get; }
        public bool BigEndian => true;
        public long Size { get; }
        public Mutex mutex;
        private uint baseAddr { get; }
        private uint pid { get; }
        public int WordSize => 4;
        byte[] bytes;
        private List<ValueChange> values = new List<ValueChange>();

        public override string ToString()
        {
            return Name;
        }

        public ProcessMemoryDomain(string name, string filename, string prot, uint _addr, long size, Process p)
        {
            baseAddr = _addr;
            Size = size;
            Name = $"{name.Trim()}|{baseAddr:X}h|{(((float)size)/1024f/1024f):0.00}MB";
            pid = p.PID;
            mutex = new Mutex();
        }

        public byte PeekByte(long addr)
        {
            if (addr < 0 || addr >= Size)
            {
                return 0;
            }

            return bytes[addr];
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
            //bytes[addr] = val;
            throw new Exception("Attempted to poke a single byte in an rpc memory interface");
        }

        public void PokeBytes(long addr, byte[] val)
        {
            if (addr < 0 || addr + val.Length >= Size)
                return;
            values.Add(new ValueChange() { address = baseAddr + (uint)addr, value = val });
        }

        public void DumpMemory()
        {
            bytes = VanguardImplementation.mac.ReadMemory(pid, baseAddr, (uint)Size);
        }

        public void UpdateMemory()
        {
            foreach (ValueChange change in values)
            {
                VanguardImplementation.mac.WriteMemory(pid, change.address, change.value);
            }
            bytes = null;
        }
    }
    public static class ProcessWatch
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
                gameDone[VSPEC.SYSTEM] = "FileSystem";
                gameDone[VSPEC.GAMENAME] = VanguardImplementation.ProcessName;
                gameDone[VSPEC.SYSTEMPREFIX] = "ProcessStub";
                gameDone[VSPEC.SYSTEMCORE] = "ProcessStub";
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
                var process = VanguardImplementation.mac.GetProcessInfo(VanguardImplementation.ProcessName);
                List<MemoryDomainProxy> interfaces = new List<MemoryDomainProxy>();
                foreach (var me in process.Maps)
                {
                    if (me.FileName.StartsWith("/System") || me.FileName.StartsWith("/usr") || me.FileName.StartsWith("/Developer") || me.Name == "__PAGEZERO")
                    {
                        continue;
                    }
                    if (true)
                    {
                        ProcessMemoryDomain pmd = new ProcessMemoryDomain(me.Name, me.FileName, me.Protection, me.StartAddress, me.Size, process);
                        var mi = new MemoryDomainProxy(pmd, true, me.Name == "__TEXT");
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
