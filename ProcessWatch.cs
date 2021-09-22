using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTCV_PS4ConnectionTest
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Windows.Forms;
    using librpc;
    using RTCV.Common;
    using RTCV.CorruptCore;
    using RTCV.NetCore;

    public class ProcessMemoryDomain : IMemoryDomain
    {
        public string Name { get; }
        public bool BigEndian => false;
        public long Size { get; }
        public Mutex mutex;
        private ulong baseAddr { get; }
        private librpc.Process process { get; set; }
        public int WordSize => 4;

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
            ulong uaddr = (ulong)addr;
            if (uaddr >= (ulong)Size || uaddr < 0)
            {
                return 0;
            }
            ulong address = baseAddr + uaddr;
            var ret = VanguardImplementation.ps4.ReadByte(process.pid, address);
            return ret;
        }

        public byte[] PeekBytes(long address, int length)
        {
            byte[] ret = new byte[length];
            ulong uaddr = (ulong)address;
            if (uaddr >= (ulong)Size || uaddr < 0)
            {
                return ret;
            }
            uaddr += baseAddr;
            ret = VanguardImplementation.ps4.ReadMemory(process.pid, uaddr, length);
            return ret;
        }

        public void PokeByte(long addr, byte val)
        {
            ulong uaddr = (ulong)addr;
            if (uaddr >= (ulong)Size || uaddr < 0)
            {
                return;
            }
            VanguardImplementation.ps4.WriteByte(process.pid, baseAddr + uaddr, val);
        }

        public void PokeBytes(long addr, byte[] val)
        {
            ulong uaddr = (ulong)addr;
            if (uaddr >= (ulong)Size || uaddr < 0)
            {
                return;
            }
            uaddr += baseAddr;
            VanguardImplementation.ps4.WriteMemory(process.pid, uaddr, val);
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
                var pid = VanguardImplementation.ps4.GetProcessList().FindProcess("eboot.bin").pid;
                List<MemoryDomainProxy> interfaces = new List<MemoryDomainProxy>();
                foreach (var me in VanguardImplementation.ps4.GetProcessInfo(pid).entries)
                {
                    if (me.name.StartsWith("_") || me.name.ToUpper().StartsWith("SCE") || me.name.ToUpper().StartsWith("LIB"))
                    {
                        continue;
                    }
                    if (me.prot == 0x3)
                    {
                        ProcessMemoryDomain pmd = new ProcessMemoryDomain(me.name, me.start, (long)(me.end - me.start), VanguardImplementation.ps4.GetProcessList().FindProcess("eboot.bin"));
                        interfaces.Add(new MemoryDomainProxy(pmd, true));
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
