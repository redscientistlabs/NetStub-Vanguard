﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTCV_PS4ConnectionTest
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using System.Windows.Forms;
    using RTCV.Common;
    using RTCV.CorruptCore;
    using RTCV.NetCore;
    using RTCV.Vanguard;
    public static class VanguardCore
    {
        public static string[] args;
        public static bool vanguardStarted;

        public static bool attached = false;

        public static string emuDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        public static string logPath = Path.Combine(emuDir, "EMU_LOG.txt");

        public static bool vanguardConnected => VanguardImplementation.connector != null && VanguardImplementation.connector.netcoreStatus == RTCV.NetCore.Enums.NetworkStatus.CONNECTED;

        public static string System
        {
            get => (string)AllSpec.VanguardSpec[VSPEC.SYSTEM];
            set => AllSpec.VanguardSpec.Update(VSPEC.SYSTEM, value);
        }

        public static string GameName
        {
            get => (string)AllSpec.VanguardSpec[VSPEC.GAMENAME];
            set => AllSpec.VanguardSpec.Update(VSPEC.GAMENAME, value);
        }

        public static string SystemPrefix
        {
            get => (string)AllSpec.VanguardSpec[VSPEC.SYSTEMPREFIX];
            set => AllSpec.VanguardSpec.Update(VSPEC.SYSTEMPREFIX, value);
        }

        public static string SystemCore
        {
            get => (string)AllSpec.VanguardSpec[VSPEC.SYSTEMCORE];
            set => AllSpec.VanguardSpec.Update(VSPEC.SYSTEMCORE, value);
        }

        public static string SyncSettings
        {
            get => (string)AllSpec.VanguardSpec[VSPEC.SYNCSETTINGS];
            set => AllSpec.VanguardSpec.Update(VSPEC.SYNCSETTINGS, value);
        }

        public static string OpenRomFilename
        {
            get => (string)AllSpec.VanguardSpec[VSPEC.OPENROMFILENAME];
            set => AllSpec.VanguardSpec.Update(VSPEC.OPENROMFILENAME, value);
        }

        public static int LastLoaderRom
        {
            get => (int)AllSpec.VanguardSpec[VSPEC.CORE_LASTLOADERROM];
            set => AllSpec.VanguardSpec.Update(VSPEC.CORE_LASTLOADERROM, value);
        }

        internal static string[] BlacklistedDomains
        {
            get => (string[])AllSpec.VanguardSpec[VSPEC.MEMORYDOMAINS_BLACKLISTEDDOMAINS];
            set => AllSpec.VanguardSpec.Update(VSPEC.MEMORYDOMAINS_BLACKLISTEDDOMAINS, value);
        }

        internal static MemoryDomainProxy[] MemoryInterfaces
        {
            get => (MemoryDomainProxy[])AllSpec.VanguardSpec[VSPEC.MEMORYDOMAINS_INTERFACES];
            set => AllSpec.VanguardSpec.Update(VSPEC.MEMORYDOMAINS_INTERFACES, value);
        }

        internal static DialogResult ShowErrorDialog(Exception exception, bool canContinue = false)
        {
            return new CloudDebug(exception, canContinue).Start();
        }
        public static PartialSpec getDefaultPartial()
        {
            var partial = new PartialSpec("VanguardSpec");

            partial[VSPEC.NAME] = "PS4Stub";
            partial[VSPEC.SYSTEM] = "PS4";
            partial[VSPEC.GAMENAME] = string.Empty;
            partial[VSPEC.SYSTEMPREFIX] = string.Empty;
            partial[VSPEC.OPENROMFILENAME] = string.Empty;
            partial[VSPEC.SYNCSETTINGS] = string.Empty;
            partial[VSPEC.MEMORYDOMAINS_BLACKLISTEDDOMAINS] = Array.Empty<string>();
            partial[VSPEC.MEMORYDOMAINS_INTERFACES] = Array.Empty<MemoryDomainProxy>();
            partial[VSPEC.CORE_LASTLOADERROM] = -1;
            partial[VSPEC.SUPPORTS_RENDERING] = false;
            partial[VSPEC.SUPPORTS_CONFIG_MANAGEMENT] = false;
            partial[VSPEC.SUPPORTS_CONFIG_HANDOFF] = false;
            partial[VSPEC.SUPPORTS_SAVESTATES] = false;
            partial[VSPEC.SUPPORTS_REFERENCES] = false;
            partial[VSPEC.OVERRIDE_DEFAULTMAXINTENSITY] = 1000000;
            partial[VSPEC.SUPPORTS_GAMEPROTECTION] = false;
            partial[VSPEC.SUPPORTS_REALTIME] = true;
            partial[VSPEC.SUPPORTS_KILLSWITCH] = false;
            partial[VSPEC.SUPPORTS_MIXED_STOCKPILE] = false;
            partial[VSPEC.USE_INTEGRATED_HEXEDITOR] = true;
            partial[VSPEC.REPLACE_MANUALBLAST_WITH_GHCORRUPT] = true;
            partial[VSPEC.EMUDIR] = emuDir;
            partial[VSPEC.SUPPORTS_MULTITHREAD] = null;

            //partial[VSPEC.CONFIG_PATHS] = new[] { Path.Combine(emuDir, "config.ini") };

            return partial;
        }
        public static void RegisterVanguardSpec()
        {
            PartialSpec emuSpecTemplate = new PartialSpec("VanguardSpec");

            emuSpecTemplate.Insert(getDefaultPartial());

            AllSpec.VanguardSpec = new FullSpec(emuSpecTemplate, !RtcCore.Attached); //You have to feed a partial spec as a template

            if (attached)
                VanguardConnector.PushVanguardSpecRef(AllSpec.VanguardSpec);

            LocalNetCoreRouter.Route(RTCV.NetCore.Endpoints.CorruptCore, RTCV.NetCore.Commands.Remote.PushVanguardSpec, emuSpecTemplate, true);
            LocalNetCoreRouter.Route(RTCV.NetCore.Endpoints.UI, RTCV.NetCore.Commands.Remote.PushVanguardSpec, emuSpecTemplate, true);

            AllSpec.VanguardSpec.SpecUpdated += (o, e) =>
            {
                PartialSpec partial = e.partialSpec;

                LocalNetCoreRouter.Route(RTCV.NetCore.Endpoints.CorruptCore, RTCV.NetCore.Commands.Remote.PushVanguardSpecUpdate, partial, true);
                LocalNetCoreRouter.Route(RTCV.NetCore.Endpoints.UI, RTCV.NetCore.Commands.Remote.PushVanguardSpecUpdate, partial, true);
            };
        }


        public static void Start()
        {
            vanguardStarted = true;

            //Grab an object on the main thread to use for netcore invokes
            SyncObjectSingleton.SyncObject = S.GET<StubForm>();
            SyncObjectSingleton.EmuThreadIsMainThread = true;
            SyncObjectSingleton.EmuInvokeDelegate = null;

            //Start everything
            VanguardImplementation.StartClient();
            RegisterVanguardSpec();
            RtcCore.StartEmuSide();

            //Refocus on Bizhawk
            S.GET<StubForm>().Focus();

            //If it's attached, lie to vanguard
            if (attached)
                VanguardConnector.ImplyClientConnected();
        }
    }
}
