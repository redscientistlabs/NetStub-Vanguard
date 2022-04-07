using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetStub.StubEndpoints.PS4
{
    using libdebug;
    using Iced.Intel;
    public static class Debugger
    {
        public static regs Registers;
        public static fpregs FloatRegisters;
        public static dbregs DebugRegisters;
        public static uint Status;
        public static bool WatchPoint = false;
        public static Decoder InstructionDecoder { get; set; }
        public static void Attach(PS4DBG ps4, int pid)
        {
            ps4.AttachDebugger(pid, DebuggerInterruptCallback);
            ps4.Notify(222, "[NetStub] Debugger attached!");
        }
        public static void Detach(PS4DBG ps4)
        {
            ps4.DetachDebugger();
            ps4.Notify(222, "[NetStub] Debugger detached!");
        }
        public static void DebuggerInterruptCallback(uint lwpid, uint status, string tdname, regs regs, fpregs fpregs, dbregs dbregs)
        {
            Registers = regs;
            DebugRegisters = dbregs;
            FloatRegisters = fpregs;
            Status = status;
        }
    }
}
