using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security.Cryptography;
using Dalamud;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace Plugins.a08381.SkipCutscene
{
    public class SkipCutscene : IDalamudPlugin
    {

        private readonly Config _config;
        private readonly RandomNumberGenerator _csp;

        private readonly decimal _base = uint.MaxValue;


        public SkipCutscene()
        {
            if (Interface.GetPluginConfig() is not Config configuration || configuration.Version == 0)
                configuration = new Config { IsEnabled = true, Version = 1 };

            _config = configuration;

            Address = new CutsceneAddressResolver();

            Address.Setup(SigScanner);

            if (Address.Valid)
            {
                PluginLog.Information("Cutscene Offset Found.");
                if (_config.IsEnabled)
                    SetEnabled(true);
            }
            else
            {
                PluginLog.Error("Cutscene Offset Not Found.");
                PluginLog.Warning("Plugin Disabling...");
                Dispose();
                return;
            }
            _csp = RandomNumberGenerator.Create();

            CommandManager.AddHandler("/sc", new CommandInfo(OnCommand)
            {
                HelpMessage = "/sc: Roll your sanity check dice."
            });
        }

        public void Dispose()
        {
            SetEnabled(false);
            GC.SuppressFinalize(this);
        }

        public string Name => "SkipCutscene";
        
        [PluginService] public static IDalamudPluginInterface Interface { get; private set; }
        
        [PluginService] public static ISigScanner SigScanner { get; private set; }

        [PluginService] public static ICommandManager CommandManager { get; private set; }
        
        [PluginService] public static IChatGui ChatGui { get; private set; }

        [PluginService] public static IPluginLog PluginLog { get; private set; }

        public CutsceneAddressResolver Address { get; }

        public void SetEnabled(bool isEnable)
        {
            if (!Address.Valid) return;
            if (isEnable)
            {
                SafeMemory.Write<short>(Address.Offset1, -28528);
                SafeMemory.Write<short>(Address.Offset2, -28528);
            }
            else
            {
                SafeMemory.Write(Address.Offset1, Address.MagicNumber1);
                SafeMemory.Write(Address.Offset2, Address.MagicNumber2);
            }
        }

        private void OnCommand(string command, string arguments)
        {
            if (command.ToLower() != "/sc") return;
            byte[] rndSeries = new byte[4];
            _csp.GetBytes(rndSeries);
            int rnd = (int)Math.Abs(BitConverter.ToUInt32(rndSeries, 0) / _base * 50 + 1);
            ChatGui.Print(_config.IsEnabled
                ? $"sancheck: 1d100={rnd + 50}, Failed"
                : $"sancheck: 1d100={rnd}, Passed");
            _config.IsEnabled = !_config.IsEnabled;
            SetEnabled(_config.IsEnabled);
            Interface.SavePluginConfig(_config);
        }
    }

    public class CutsceneAddressResolver : BaseAddressResolver
    {

        public bool Valid => Offset1 != IntPtr.Zero && Offset2 != IntPtr.Zero;

        public IntPtr Offset1 { get; private set; }
        public IntPtr Offset2 { get; private set; }

        public short MagicNumber1 => _magicNumber1;
        private short _magicNumber1;

        public short MagicNumber2 => _magicNumber2;
        private short _magicNumber2;

        protected override void Setup64Bit(ISigScanner sig)
        {
            Offset1 = sig.ScanText("75 ?? 48 8B 0D ?? ?? ?? ?? BA ?? 00 00 00 48 83 C1 10 E8 ?? ?? ?? ?? 83 78 ?? ?? 74");
            Offset2 = sig.ScanText("74 18 8B D7 48 8D 0D");
            SkipCutscene.PluginLog.Information(
                "Offset1: [\"ffxiv_dx11.exe\"+{0}]",
                (Offset1.ToInt64() - Process.GetCurrentProcess().MainModule!.BaseAddress.ToInt64()).ToString("X")
                );
            SkipCutscene.PluginLog.Information(
                "Offset2: [\"ffxiv_dx11.exe\"+{0}]",
                (Offset2.ToInt64() - Process.GetCurrentProcess().MainModule!.BaseAddress.ToInt64()).ToString("X")
                );
            if ( Offset1 != IntPtr.Zero && Offset2 != IntPtr.Zero )
            {
                ReadMagicNumbers();
                SkipCutscene.PluginLog.Information("MagicNumber1: {0}", MagicNumber1);
                SkipCutscene.PluginLog.Information("MagicNumber2: {0}", MagicNumber2);
            }
        }

        private void ReadMagicNumbers()
        {
            SafeMemory.Read(Offset1, out _magicNumber1);
            SafeMemory.Read(Offset2, out _magicNumber2);
        }
    }
}
