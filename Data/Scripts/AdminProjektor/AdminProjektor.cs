using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace AdminProjektor
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Projector), false,
        "AdminProjektor_Large",
        "AdminProjektor_Small")]
    public class AdminProjektorLogic : MyGameLogicComponent
    {
        private const string MOD_NAME    = "AdminProjektor";
        private const long   LOG_CHANNEL = 1995999L;
        private const string MOD_FULL   = "Phantombite_AdminProjektor";
        private const float  WELD_AMOUNT       = 5000f;
        private const int    PHASE_DELAY       = 120;
        private const int    MAX_STUCK_FRAMES  = 10;

        // Batch-Größen je nach PerfLevel: 0→5/10 | 1→2/5 | 2→1/2 | 3→1/1
        public static int GetBuildBatch()
        {
            switch (AdminProjektor_Session.PerfLevel)
            {
                case 1:      return 2;
                case 2: case 3: return 1;
                default:     return 5;
            }
        }
        public static int GetRepairBatch()
        {
            switch (AdminProjektor_Session.PerfLevel)
            {
                case 1:  return 5;
                case 2:  return 2;
                case 3:  return 1;
                default: return 10;
            }
        }

        // Phasen: 0=idle, 1=build, 2=delay, 3=repair
        private IMyProjector            _projector;
        private bool                    _isWorking    = false;
        private int                     _phase        = 0;
        private int                     _delayCounter = 0;
        private int                     _stuckCounter = 0;
        private readonly List<IMySlimBlock> _repairQueue = new List<IMySlimBlock>();

        private static bool _controlsCreated = false;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            _projector = Entity as IMyProjector;
            if (_projector == null) return;

            NeedsUpdate = MyEntityUpdateEnum.EACH_100TH_FRAME;
            CreateTerminalControls();
        }

        private static void CreateTerminalControls()
        {
            if (_controlsCreated) return;
            _controlsCreated = true;

            var sep = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyProjector>("AdminProjektor_Sep");
            sep.Visible = IsAdminProjektor;
            MyAPIGateway.TerminalControls.AddControl<IMyProjector>(sep);

            var label = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyProjector>("AdminProjektor_Label");
            label.Label = MyStringId.GetOrCompute("── Admin Projektor ──");
            label.Visible = IsAdminProjektor;
            MyAPIGateway.TerminalControls.AddControl<IMyProjector>(label);

            var btn = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, IMyProjector>("AdminProjektor_Build");
            btn.Title   = MyStringId.GetOrCompute("Fertig bauen");
            btn.Tooltip = MyStringId.GetOrCompute("Baut Projektion fertig und repariert alle Blöcke auf 100%.");
            btn.Visible = IsAdminProjektor;
            btn.Enabled = (block) => !(block.GameLogic.GetAs<AdminProjektorLogic>()?._isWorking ?? true);
            btn.Action  = (block) => block.GameLogic.GetAs<AdminProjektorLogic>()?.StartWork();
            MyAPIGateway.TerminalControls.AddControl<IMyProjector>(btn);
        }

        private static bool IsAdminProjektor(IMyTerminalBlock block)
        {
            return block.BlockDefinition.SubtypeId == "AdminProjektor_Large"
                || block.BlockDefinition.SubtypeId == "AdminProjektor_Small";
        }

        private long GetOwnerId()
        {
            if (_projector == null) return 0;

            long ownerId = _projector.OwnerId;
            if (ownerId == 0 && _projector.CubeGrid.BigOwners.Count > 0)
                ownerId = _projector.CubeGrid.BigOwners[0];
            return ownerId;
        }

        private void StartWork()
        {
            if (_projector == null) return;

            _delayCounter = 0;
            _stuckCounter = 0;
            _repairQueue.Clear();

            if (_projector.IsProjecting && _projector.ProjectedGrid != null)
            {
                _phase = 1;
                MyAPIGateway.Utilities.ShowMessage(MOD_NAME,
                    string.Format("Baue {0} Blöcke...", _projector.RemainingBlocks));
                Log("StartWork: Build — " + _projector.RemainingBlocks + " Blöcke, PerfLevel=" + AdminProjektor_Session.PerfLevel);
            }
            else
            {
                _phase = 2;
                _delayCounter = PHASE_DELAY; // sofort zur Reparatur
                MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Keine Projektion — Reparatur...");
                Log("StartWork: Direkt Reparatur");
            }

            _isWorking   = true;
            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateAfterSimulation()
        {
            if (!_isWorking) return;

            // ── Phase 1: Build-Loop ───────────────────────────────────────────────
            if (_phase == 1)
            {
                if (_projector == null || !_projector.IsProjecting || _projector.ProjectedGrid == null)
                {
                    _phase = 2;
                    _delayCounter = 0;
                    MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Build fertig. Repariere...");
                    return;
                }

                var allProjected = new List<IMySlimBlock>();
                _projector.ProjectedGrid.GetBlocks(allProjected);

                if (allProjected.Count == 0)
                {
                    _phase = 2;
                    _delayCounter = 0;
                    MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Build fertig. Repariere...");
                    Log("Phase 1→2: Build abgeschlossen");
                    return;
                }

                // Nur baubare Blöcke versuchen
                var buildable = new List<IMySlimBlock>();
                foreach (var block in allProjected)
                {
                    if (_projector.CanBuild(block, false) == BuildCheckResult.OK)
                        buildable.Add(block);
                }

                if (buildable.Count == 0)
                {
                    _stuckCounter++;
                    if (_stuckCounter >= MAX_STUCK_FRAMES)
                    {
                        _phase = 2;
                        _delayCounter = 0;
                        MyAPIGateway.Utilities.ShowMessage(MOD_NAME,
                            string.Format("Build fertig ({0} Restblöcke). Repariere...", allProjected.Count));
                    }
                    return;
                }

                _stuckCounter = 0;
                long ownerId = GetOwnerId();
                if (ownerId == 0) return;

                // requestInstant = true — Block sofort erstellen
                int buildBatch = GetBuildBatch();
                int count = Math.Min(buildBatch, buildable.Count);
                Log("Build: " + count + "/" + buildable.Count + " Blöcke (Batch=" + buildBatch + ")", 1);
                for (int i = 0; i < count; i++)
                {
                    try { _projector.Build(buildable[i], ownerId, _projector.EntityId, true, ownerId); }
                    catch { }
                }
                return;
            }

            // ── Phase 2: Delay — Grid-Registrierung abwarten ──────────────────────
            if (_phase == 2)
            {
                _delayCounter++;
                if (_delayCounter >= PHASE_DELAY)
                {
                    // Repair-Queue befüllen
                    _repairQueue.Clear();
                    if (_projector?.CubeGrid != null)
                    {
                        var allGrids = new List<IMyCubeGrid>();
                        MyAPIGateway.GridGroups.GetGroup(_projector.CubeGrid, GridLinkTypeEnum.Mechanical, allGrids);

                        foreach (var grid in allGrids)
                        {
                            if (grid == null) continue;
                            var blocks = new List<IMySlimBlock>();
                            grid.GetBlocks(blocks);
                            foreach (var block in blocks)
                                if (block != null && !block.IsFullIntegrity)
                                    _repairQueue.Add(block);
                        }
                    }

                    if (_repairQueue.Count > 0)
                    {
                        _phase = 3;
                        MyAPIGateway.Utilities.ShowMessage(MOD_NAME,
                            string.Format("Repariere {0} Blöcke...", _repairQueue.Count));
                    }
                    else
                    {
                        Finish();
                    }
                }
                return;
            }

            // ── Phase 3: Repair-Loop — Batch pro Frame ────────────────────────────
            if (_phase == 3)
            {
                if (_repairQueue.Count == 0)
                {
                    Finish();
                    return;
                }

                int repairBatch = GetRepairBatch();
                int count = Math.Min(repairBatch, _repairQueue.Count);
                Log("Repair: " + count + "/" + (_repairQueue.Count) + " Blöcke (Batch=" + repairBatch + ")", 1);
                for (int i = 0; i < count; i++)
                {
                    var block = _repairQueue[0];
                    _repairQueue.RemoveAt(0);

                    if (block == null) continue;
                    if (block.IsFullIntegrity) continue;

                    try
                    {
                        // Komponente ins Stockpile spawnen damit IncreaseMountLevel arbeiten kann
                        // Mehrfach aufrufen bis der Block voll ist — wird im nächsten Frame erneut
                        // zur Queue hinzugefügt falls noch nicht fertig
                        block.SpawnFirstItemInConstructionStockpile();
                        block.IncreaseMountLevel(WELD_AMOUNT, 0L, null, 0f, false,
                            MyOwnershipShareModeEnum.Faction);

                        // Wenn immer noch nicht fertig — wieder in Queue
                        if (!block.IsFullIntegrity)
                            _repairQueue.Add(block);
                    }
                    catch { }
                }
            }
        }

        private void Finish()
        {
            _isWorking = false;
            _phase     = 0;
            _repairQueue.Clear();
            NeedsUpdate &= ~MyEntityUpdateEnum.EACH_FRAME;
            MyAPIGateway.Utilities.ShowMessage(MOD_NAME, "Fertig.");
            Log("Finish: Alle Blöcke gebaut und repariert.");
        }

        private void Log(string msg, int level = 0)
        {
            if (level > AdminProjektor_Session.LogLevel) return;
            try
            {
                MyLog.Default.WriteLineAndConsole("[PB.AdminProjektor] [" + level + "] " + msg);
                MyAPIGateway.Utilities.SendModMessage(LOG_CHANNEL,
                    "LOG|" + MOD_FULL + "|" + level + "|AdminProjektorLogic|" + msg);
            }
            catch { }
        }

        public override void Close()
        {
            _projector = null;
            _repairQueue.Clear();
        }
    }
}