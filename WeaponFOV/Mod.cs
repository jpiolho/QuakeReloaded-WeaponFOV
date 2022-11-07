using QuakeReloaded.Interfaces;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.Structs;
using Reloaded.Hooks.ReloadedII.Interfaces;
using Reloaded.Memory.SigScan.ReloadedII.Interfaces;
using Reloaded.Mod.Interfaces;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using WeaponFOV.Configuration;
using WeaponFOV.Template;
using IReloadedHooks = Reloaded.Hooks.Definitions.IReloadedHooks;

namespace WeaponFOV
{
    /// <summary>
    /// Your mod logic goes here.
    /// </summary>
    public class Mod : ModBase // <= Do not Remove.
    {
        /// <summary>
        /// Provides access to the mod loader API.
        /// </summary>
        private readonly IModLoader _modLoader;

        /// <summary>
        /// Provides access to the Reloaded.Hooks API.
        /// </summary>
        /// <remarks>This is null if you remove dependency on Reloaded.SharedLib.Hooks in your mod.</remarks>
        private readonly IReloadedHooks? _hooks;

        /// <summary>
        /// Provides access to the Reloaded logger.
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Entry point into the mod, instance that created this class.
        /// </summary>
        private readonly IMod _owner;

        /// <summary>
        /// Provides access to this mod's configuration.
        /// </summary>
        private Config _configuration;

        /// <summary>
        /// The configuration of the currently executing mod.
        /// </summary>
        private readonly IModConfig _modConfig;

        private unsafe float* _fovValueContainer;

        public Mod(ModContext context)
        {
            _modLoader = context.ModLoader;
            _hooks = context.Hooks;
            _logger = context.Logger;
            _owner = context.Owner;
            _configuration = context.Configuration;
            _modConfig = context.ModConfig;

            var currentProcess = Process.GetCurrentProcess();
            var mainModule = currentProcess.MainModule!;
            var t = _modLoader.GetController<IStartupScanner>();
            if (!t.TryGetTarget(out var scanner))
                throw new Exception("Failed to get scanner");

            var t1 = _modLoader.GetController<IReloadedHooksUtilities>();
            if (!t1.TryGetTarget(out var utilities))
                throw new Exception("Failed to get utilities");

            if (!_modLoader.GetController<IQuakeReloaded>().TryGetTarget(out var qreloaded))
                throw new Exception("Could not get QuakeReloaded API. Are you sure QuakeReloaded is loaded before this mod?");

            qreloaded.Events.RegisterOnInitialized(() =>
            {
                qreloaded.Cvars.Register("r_weaponfov", _configuration.DefaultWeaponFOV.ToString(CultureInfo.InvariantCulture), "", CvarFlags.Float, 0f, 180f);
                qreloaded.Console.PrintLine("WeaponFOV initialized", 0, 255, 0);
            });

            qreloaded.Events.RegisterOnRenderFrame(() =>
            {
                unsafe
                {
                    *_fovValueContainer = qreloaded.Cvars.GetFloatValue("r_weaponfov", _configuration.DefaultWeaponFOV);
                }
            });


            unsafe
            {
                _fovValueContainer = (float*)Marshal.AllocHGlobal(sizeof(float));
                *_fovValueContainer = _configuration.DefaultWeaponFOV;
            }

            // Scan for the place where 
            scanner.AddMainModuleScan("F3 0F 10 15 ?? ?? ?? ?? 0F 29 44 24 ??", result =>
            {
                var offset = mainModule.BaseAddress + result.Offset;

                unsafe
                {
                    _hooks!.CreateAsmHook(new[]
                    {
                        $"use64",
                        $"movss xmm2, dword [qword 0x{new IntPtr(_fovValueContainer):x}]"
                    }, (long)offset, Reloaded.Hooks.Definitions.Enums.AsmHookBehaviour.DoNotExecuteOriginal).Activate();

                }
            });
        }

        #region Standard Overrides
        public override void ConfigurationUpdated(Config configuration)
        {
            // Apply settings from configuration.
            // ... your code here.
            _configuration = configuration;
            _logger.WriteLine($"[{_modConfig.ModId}] Config Updated: Applying");
        }
        #endregion

        #region For Exports, Serialization etc.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public Mod() { }
#pragma warning restore CS8618
        #endregion
    }
}