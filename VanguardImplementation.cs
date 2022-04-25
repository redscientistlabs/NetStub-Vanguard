using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetStub
{
    using System;
    using System.Threading;
    using System.Windows.Forms;
    using libdebug;
    using NetStub.UI.HexEditor;
    using RTCV.CorruptCore;
    using RTCV.NetCore;
    using RTCV.NetCore.Commands;
    using RTCV.Vanguard;
    using NetStub.StubEndpoints;
    public static class VanguardImplementation
    {
        public static VanguardConnector connector;
        public static HexEditor hexEditor;

        public static StubMode stubMode = StubMode.PS4;

        // PS4
        public static PS4DBG ps4;
        public static ProcessList pl;

        // MAC
        public static StubEndpoints.MacOSX_PPC.RPC.PowerMacRPC mac;

        // Linux (amd64)
        public static StubEndpoints.X86_64_Linux.LinuxRPC linux;

        // Windows XP (x86)
        public static StubEndpoints.WindowsXP.RPC winxp;

        public static string ProcessName = "";

        public static void StartClient()
        {
            try
            {
                hexEditor = new HexEditor();
                ConsoleEx.WriteLine("Starting Vanguard Client");
                Thread.Sleep(500); //When starting in Multiple Startup Project, the first try will be uncessful since
                                   //the server takes a bit more time to start then the client.

                var spec = new NetCoreReceiver();
                spec.Attached = VanguardCore.attached;
                spec.MessageReceived += OnMessageReceived;

                connector = new VanguardConnector(spec);
            }
            catch (Exception ex)
            {
                if (VanguardCore.ShowErrorDialog(ex, true) == DialogResult.Abort)
                    throw new AbortEverythingException();
            }
        }

        public static void RestartClient()
        {
            connector?.Kill();
            connector = null;
            StartClient();
        }
        private static void OnMessageReceived(object sender, NetCoreEventArgs e)
        {
            try
            {
                // This is where you implement interaction.
                // Warning: Any error thrown in here will be caught by NetCore and handled by being displayed in the console.

                var message = e.message;
                var simpleMessage = message as NetCoreSimpleMessage;
                var advancedMessage = message as NetCoreAdvancedMessage;

                ConsoleEx.WriteLine(message.Type);
                switch (message.Type) //Handle received messages here
                {
                    case RTCV.NetCore.Commands.Remote.AllSpecSent:
                        {
                            //We still need to set the emulator's path
                            AllSpec.VanguardSpec.Update(VSPEC.EMUDIR, VanguardCore.emuDir);
                            SyncObjectSingleton.FormExecute(() =>
                            {
                                switch (stubMode)
                                {
                                    case StubMode.PS4:
                                        StubEndpoints.PS4.ProcessWatch.UpdateDomains();
                                        break;
                                    case StubMode.MacOSX_PPC:
                                        StubEndpoints.MacOSX_PPC.ProcessWatch.UpdateDomains();
                                        break;
                                    case StubMode.Linux_AMD64:
                                        StubEndpoints.X86_64_Linux.ProcessWatch.UpdateDomains();
                                        break;
                                    case StubMode.WindowsXP:
                                        StubEndpoints.WindowsXP.ProcessWatch.UpdateDomains();
                                        break;
                                    default:
                                        break;
                                }
                            });
                        }
                        break;
                    case RTCV.NetCore.Commands.Basic.SaveSavestate:
                        e.setReturnValue("");
                        break;

                    case RTCV.NetCore.Commands.Basic.LoadSavestate:
                        e.setReturnValue(true);
                        break;

                    case RTCV.NetCore.Commands.Remote.PreCorruptAction:
                        break;

                    case RTCV.NetCore.Commands.Remote.PostCorruptAction:
                        break;

                    case RTCV.NetCore.Commands.Remote.CloseGame:
                        break;

                    case RTCV.NetCore.Commands.Remote.DomainGetDomains:
                        SyncObjectSingleton.FormExecute(() =>
                        {
                            switch (stubMode)
                            {
                                case StubMode.PS4:
                                    e.setReturnValue(StubEndpoints.PS4.ProcessWatch.GetInterfaces());
                                    break;
                                case StubMode.MacOSX_PPC:
                                    e.setReturnValue(StubEndpoints.MacOSX_PPC.ProcessWatch.GetInterfaces());
                                    break;
                                case StubMode.Linux_AMD64:
                                    e.setReturnValue(StubEndpoints.X86_64_Linux.ProcessWatch.GetInterfaces());
                                    break;
                                case StubMode.WindowsXP:
                                    e.setReturnValue(StubEndpoints.WindowsXP.ProcessWatch.GetInterfaces());
                                    break;
                                default:
                                    break;
                            }
                        });
                        break;

                    case RTCV.NetCore.Commands.Remote.DomainRefreshDomains:
                        SyncObjectSingleton.FormExecute(() => {
                            switch (stubMode)
                            {
                                case StubMode.PS4:
                                    StubEndpoints.PS4.ProcessWatch.UpdateDomains();
                                    break;
                                case StubMode.MacOSX_PPC:
                                    StubEndpoints.MacOSX_PPC.ProcessWatch.UpdateDomains();
                                    break;
                                case StubMode.Linux_AMD64:
                                    StubEndpoints.X86_64_Linux.ProcessWatch.UpdateDomains();
                                    break;
                                case StubMode.WindowsXP:
                                    StubEndpoints.WindowsXP.ProcessWatch.UpdateDomains();
                                    break;
                                default:
                                    break;
                            }
                        });
                        break;

                    case RTCV.NetCore.Commands.Remote.EventEmuMainFormClose:
                        SyncObjectSingleton.FormExecute(() =>
                        {
                            Environment.Exit(0);
                        });
                        break;
                    case RTCV.NetCore.Commands.Remote.IsNormalAdvance:
                        e.setReturnValue(true);
                        break;
                    case RTCV.NetCore.Commands.Remote.OpenHexEditor:
                        SyncObjectSingleton.FormExecute( () =>
                        {
                            hexEditor.Show();
                        });
                        break;

                    case RTCV.NetCore.Commands.Emulator.OpenHexEditorAddress:
                        {
                            var temp = advancedMessage.objectValue as object[];
                            string domain = (string)temp[0];
                            long address = (long)temp[1];

                            MemoryDomainProxy mdp = MemoryDomains.GetProxy(domain, address);
                            long realAddress = MemoryDomains.GetRealAddress(domain, address);

                            SyncObjectSingleton.FormExecute(() =>
                            {
                                if (mdp?.MD == null)
                                    return;
                                hexEditor.Show();
                                hexEditor.SetMemoryDomain(mdp.MD.ToString());
                                hexEditor.GoToAddress(realAddress);
                            });

                            break;
                        }
                    case RTCV.NetCore.Commands.Remote.EventCloseEmulator:
                        Environment.Exit(-1);
                        break;
                }
            }
            catch (Exception ex)
            {
                if (VanguardCore.ShowErrorDialog(ex, true) == DialogResult.Abort)
                    throw new AbortEverythingException();
            }
        }
    }
}
