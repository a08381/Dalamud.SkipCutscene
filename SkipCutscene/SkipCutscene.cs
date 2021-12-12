using Dalamud;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Plugins.a08381.SkipCutscene
{
    public class SkipCutscene : IDalamudPlugin
    {

        private Config config;

        private RNGCryptoServiceProvider csp;

        private readonly decimal _base = uint.MaxValue;

        public SkipCutscene()
        {
            if (Interface.GetPluginConfig() is not Config configuration || configuration.Version == 0)
                configuration = new Config() { IsEnabled = true, Version = 1 };

            this.config = configuration;

            Address = new CutsceneAddressResolver();

            Address.Setup(SigScanner);

            if (Address.Offset1 != IntPtr.Zero && Address.Offset2 != IntPtr.Zero)
            {
                PluginLog.Information("Cutscene Offset Found.");
                if (this.config.IsEnabled)
                    SetEnabled(true);
            }
            else
            {
                PluginLog.Error("Cutscene Offset Not Found.");
                PluginLog.Warning("Plugin Disabling...");
                Dispose();
                return;
            }

            csp = new();

            CommandManager.AddHandler("/sc", new CommandInfo(this.OnCommand)
            {
                HelpMessage = "/sc: Roll your sanity check dice."
            });
        }

        public void Dispose()
        {
            SetEnabled(false);
        }

        public string Name => "SkipCutscene";

        [PluginService]
        public DalamudPluginInterface Interface { get; private set; }

        [PluginService]
        public SigScanner SigScanner { get; private set; }

        [PluginService]
        public CommandManager CommandManager { get; private set; }

        [PluginService]
        public ChatGui ChatGui { get; private set; }

        public CutsceneAddressResolver Address { get; private set; }

        public void SetEnabled(bool isEnable)
        {
            if (Address.Offset1 != IntPtr.Zero && Address.Offset2 != IntPtr.Zero)
            {
                if (isEnable)
                {
                    SafeMemory.Write<short>(Address.Offset1, -28528);
                    SafeMemory.Write<short>(Address.Offset2, -28528);
                }
                else
                {
                    SafeMemory.Write<short>(Address.Offset1, 13173);
                    SafeMemory.Write<short>(Address.Offset2, 6260);
                }
            }
        }

        private void OnCommand(string command, string arguments)
        {
            if (command.ToLower() == "/sc")
            {
                byte[] rndSeries = new byte[4];
                csp.GetBytes(rndSeries);
                int rnd = (int)Math.Abs(BitConverter.ToUInt32(rndSeries, 0) / _base * 50 + 1);
                if (this.config.IsEnabled)
                {
                    ChatGui.Print(string.Format("sancheck: 1d100={0}, Failed", rnd + 50));
                }
                else
                {
                    ChatGui.Print(string.Format("sancheck: 1d100={0}, Passed", rnd));
                }
                this.config.IsEnabled = !this.config.IsEnabled;
                SetEnabled(this.config.IsEnabled);
                Interface.SavePluginConfig(this.config);
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
            PluginLog.Information(
                "Offset1: [\"ffxiv_dx11.exe\"+{0}]",
                (Offset1.ToInt64() - Process.GetCurrentProcess().MainModule.BaseAddress.ToInt64()).ToString("X")
                );
            PluginLog.Information(
                "Offset2: [\"ffxiv_dx11.exe\"+{0}]",
                (Offset2.ToInt64() - Process.GetCurrentProcess().MainModule.BaseAddress.ToInt64()).ToString("X")
                );
        }

    }
}
