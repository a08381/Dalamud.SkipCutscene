using Dalamud;
using Dalamud.Game;
using Dalamud.Game.Internal;
using Dalamud.Plugin;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Plugins.a08381.SkipCutscene
{
    public class SkipCutscene : IDalamudPlugin
    {
        public string Name => "SkipCutscene";

        public CutsceneAddressResolver Address; 


        private DalamudPluginInterface pi;

        public void Dispose()
        {
            if (Address.Offset1 != IntPtr.Zero)
                SafeMemory.Write<short>(Address.Offset1, 13173);
            if (Address.Offset2 != IntPtr.Zero)
                SafeMemory.Write<short>(Address.Offset2, 6260);
            pi.Dispose();
        }

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            pi = pluginInterface;

            Address = new CutsceneAddressResolver();

            SigScanner sig = null;

            Type p_interface = pluginInterface.GetType();
            foreach (FieldInfo field in p_interface.GetFields())
            {
                if (field.Name == "TargetModuleScanner")
                {
                    sig = field.GetValue(pluginInterface) as SigScanner;
                }
            }
            if (sig != null)
                Address.Setup(sig);

            if (Address.Offset1 != IntPtr.Zero && Address.Offset2 != IntPtr.Zero)
            {
                PluginLog.Information("Cutscene Offset Found.");
                SafeMemory.Write<short>(Address.Offset1, -28528);
                SafeMemory.Write<short>(Address.Offset2, -28528);
            }
            else
            {
                PluginLog.Error("Cutscene Offset Not Found.");
                PluginLog.Warning("Plugin Disabling...");
                Dispose();
            }
        }
    }

    public class CutsceneAddressResolver : BaseAddressResolver
    {

        public IntPtr Offset1 { get; private set; }
        public IntPtr Offset2 { get; private set; }

        protected override void Setup64Bit(SigScanner sig)
        {
            Offset1 = sig.ScanText("75 33 48 8B 0D ?? ?? ?? ?? BA ?? 00 00 00 48 83 C1 10 E8 ?? ?? ?? ?? 83 78");
            Offset2 = sig.ScanText("74 18 8B D7 48 8D 0D");
            PluginLog.Information("Offset1: [{0}]", Offset1.ToString("X"));
            PluginLog.Information("Offset2: [{0}]", Offset2.ToString("X"));
        }

    }
}
