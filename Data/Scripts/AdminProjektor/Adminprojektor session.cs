using System;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Utils;
using VRage.Game.Components;

namespace AdminProjektor
{
    /// <summary>
    /// AdminProjektor_Session — Core v2.0.0 Anbindung
    ///
    /// Kanal: 1995011 (Core → AdminProjektor)
    ///
    /// Performance:
    ///   Level 0 — Build: 5/Frame, Repair: 10/Frame
    ///   Level 1 — Build: 2/Frame, Repair: 5/Frame
    ///   Level 2 — Build: 1/Frame, Repair: 2/Frame
    ///   Level 3 — Build: 1/Frame, Repair: 1/Frame
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class AdminProjektor_Session : MySessionComponentBase
    {
        private const long   CORE_CHANNEL  = 1995000L;
        private const long   MY_CHANNEL    = 1995011L;
        private const long   LOG_CHANNEL   = 1995999L;
        private const string MOD_NAME      = "Phantombite_AdminProjektor";
        private const string VERSION       = "1.0.0";

        private int _logLevel = 0;

        /// <summary>Statischer PerfLevel — von AdminProjektorLogic gelesen.</summary>
        public static int PerfLevel { get; private set; } = 0;

        // ── LoadData ─────────────────────────────────────────────────────────

        public override void LoadData()
        {
            try
            {
                if (MyAPIGateway.Multiplayer.IsServer)
                    MyAPIGateway.Utilities.RegisterMessageHandler(MY_CHANNEL, OnCoreMessage);
                Log("LoadData — warte auf Core READY");
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("[PB.AdminProjektor] [ERROR] LoadData: " + ex);
            }
        }

        // ── Core Kommunikation ────────────────────────────────────────────────

        private void OnCoreMessage(object data)
        {
            try
            {
                string msg = data as string;
                if (string.IsNullOrEmpty(msg)) return;

                if (msg == "READY")
                {
                    SendRegister();
                    Log("READY empfangen — REGISTER gesendet");
                    return;
                }

                if (msg.StartsWith("LOGLEVEL|"))
                {
                    int lvl;
                    if (int.TryParse(msg.Substring(9), out lvl))
                        _logLevel = Math.Max(0, Math.Min(3, lvl));
                    Log("LOGLEVEL gesetzt: " + _logLevel, 1);
                    return;
                }

                if (msg.StartsWith("PERFLEVEL|"))
                {
                    int lvl;
                    if (int.TryParse(msg.Substring(10), out lvl))
                    {
                        PerfLevel = Math.Max(0, Math.Min(3, lvl));
                        Log("PERFLEVEL gesetzt: " + PerfLevel +
                            " — Build:" + AdminProjektorLogic.GetBuildBatch() +
                            " Repair:" + AdminProjektorLogic.GetRepairBatch() + "/Frame");
                        MyAPIGateway.Utilities.SendModMessage(CORE_CHANNEL,
                            "PERFACK|adminprojektor|" + PerfLevel);
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("[PB.AdminProjektor] [ERROR] OnCoreMessage: " + ex);
            }
        }

        private void SendRegister()
        {
            // Kein Command — nur PERFLEVEL-Steuerung (Button im Terminal)
            string msg = "REGISTER|adminprojektor|Admin Projektor|" + VERSION + "|" + MY_CHANNEL;
            MyAPIGateway.Utilities.SendModMessage(CORE_CHANNEL, msg);
            MyAPIGateway.Utilities.SendModMessage(CORE_CHANNEL, "PERFACK|adminprojektor|0");
        }

        // ── Unload ───────────────────────────────────────────────────────────

        protected override void UnloadData()
        {
            try
            {
                if (MyAPIGateway.Utilities != null && MyAPIGateway.Multiplayer.IsServer)
                    MyAPIGateway.Utilities.UnregisterMessageHandler(MY_CHANNEL, OnCoreMessage);
                PerfLevel = 0;
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("[PB.AdminProjektor] [ERROR] UnloadData: " + ex);
            }
        }

        // ── Logging ───────────────────────────────────────────────────────────

        private void Log(string msg, int level = 0)
        {
            if (level > _logLevel) return;
            try
            {
                MyLog.Default.WriteLineAndConsole("[PB.AdminProjektor] [" + level + "] " + msg);
                MyAPIGateway.Utilities.SendModMessage(LOG_CHANNEL,
                    "LOG|" + MOD_NAME + "|" + level + "|AdminProjektor_Session|" + msg);
            }
            catch { }
        }
    }
}