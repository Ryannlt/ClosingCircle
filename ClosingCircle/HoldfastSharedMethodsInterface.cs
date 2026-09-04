using ClosingCircle.Centres;
using ClosingCircle.ConfigVariables;
using ClosingCircle.ConsoleCommands;
using ClosingCircle.Core;
using ClosingCircle.Domain;
using ClosingCircle.Sync;
using ClosingCircle.Systems;
using ClosingCircle.Visual;
using ClosingCircle.Visual.Menu;
using HoldfastBridge;
using HoldfastSharedMethods;
using UnityEngine;

// Callbacks in. Together with GameFacade this is the only file that names a Holdfast type, which is what would
// make a prebuilt DLL build against the game assemblies a small change rather than a rewrite.

namespace ClosingCircle
{
    public class HoldfastSharedMethodsInterface : IHoldfastSharedMethods, IHoldfastSharedMethods2,
                                                  IHoldfastSharedMethods3, IHoldfastGame
    {
        private static bool _isServer;
        private static bool _isClient;
        private static bool _sawRoundTimer;
        private static bool _warnedAboutTimer;

        // A shipped mod guarded its damage this way, so a client that also reports itself as a server cannot
        // start slapping people.
        private static bool ShouldEnforce => _isServer && !_isClient;

        public void OnGameMethodsInitialized(IHoldfastGameMethods holdfastGameMethods) =>
            GameFacade.Initialize(holdfastGameMethods);

        public void OnIsServer(bool server)
        {
            _isServer = server;
            Logger.Log(server ? "Running on the server." : "Not running as the server.", LogLevel.INFO);
        }

        public void OnIsClient(bool client, ulong steamId)
        {
            _isClient = client;

            if (!client) return;

            LocalPlayer.OnIsClient(steamId);

            // Scoped by steam id, so two accounts on one machine keep their own look.
            ClientDisplay.Load(steamId);
        }

        public void PassConfigVariables(string[] value) => ConfigManager.Process(value);

        public void OnRoundDetails(int roundId, string serverName, string mapName, FactionCountry attackingFaction,
                                   FactionCountry defendingFaction, GameplayMode gameplayMode, GameType gameType)
        {
            // Client side only: this decides whether the F3 hint is due again on this server.
            if (_isClient) ClientDisplay.NoteRound(roundId, serverName);

            _sawRoundTimer = false;
            _warnedAboutTimer = false;

            PlayerRegistry.Reset();
            VehicleRegistry.Reset();
            ZoneEnforcer.Reset();
            ZoneVisual.Reset();
            ZoneWall.Reset();
            ZoneHud.Reset();
            ZonePanel.Reset();
            ZonePusher.Reset();
            LocalPlayer.Reset();
            StageAnnouncer.Reset();
            CentreResolver.Reset();
            PlanAudit.Reset();
            AdminAccess.Reset();
            ZoneService.ResetRoundClock();
            PlanBroadcaster.Reset();
            PlanReceiver.Reset();
            PlanPreview.Reset();

            Logger.Log($"Round {roundId} on {mapName}. {ZoneService.Describe()}", LogLevel.INFO);
        }

        // Fires every frame on both sides with the same synchronised value, so it is the only clock the mod
        // needs and no state has to be sent anywhere.
        public void OnUpdateTimeRemaining(float time)
        {
            // Both of these run before the guards below: a change must reach clients even while the zone is
            // switched off, and a preview must draw on a rotation that has no round timer at all.
            if (ShouldEnforce) PlanBroadcaster.Step();
            if (_isClient)
            {
                ZonePanel.Step();
                AdminAccess.Step();
                PlanPreview.Draw(time);
            }

            // Hiding has to happen on this path rather than behind an early return. Skipping the client
            // entirely is what left a zone switched off mid-round still drawn and still solid.
            if (!ZoneService.Enabled || time < 0f)
            {
                if (_isClient)
                {
                    ZoneVisual.Hide();
                    ZoneWall.Hide();
                    ZoneHud.Hide();
                }

                if (ZoneService.Enabled) WarnAboutTimerOnce();
                return;
            }

            _sawRoundTimer = true;

            if (ShouldEnforce)
            {
                // The round length is only knowable from the clock, so the audit waits for the first tick
                // rather than running back when config arrived.
                ZoneService.NoteRoundClock(time);
                PlanAudit.ReportOnce();

                // Before the enforcer, so a stage that comes due this frame is enforced at its resolved
                // centre rather than at the one it is about to stop using.
                CentreResolver.Step(time);
                ZoneEnforcer.Step(time);
                ZonePusher.Step(ZoneService.Evaluate(time));
                StageAnnouncer.Step(time);
            }

            if (_isClient)
            {
                // One evaluation feeds both, so the wall can never sit where the visual is not.
                ZoneSnapshot zone = ZoneService.Evaluate(time);
                ZoneVisual.Update(zone);
                ZoneWall.Update(zone);
                ZoneHud.Update(zone, time);
            }
        }

        // The game reports a flat -1 both when a rotation has no round timer at all and when a round has run
        // past its end, and those are indistinguishable from here unless a real value arrived first.
        private static void WarnAboutTimerOnce()
        {
            if (_warnedAboutTimer) return;
            _warnedAboutTimer = true;

            Logger.Log(_sawRoundTimer
                    ? "The round has run past its timer, so the zone is holding where it was."
                    : "This rotation has no round timer, so the zone cannot run. Set round_time_minutes above 0.",
                LogLevel.WARNING);
        }

        public void OnPlayerSpawned(int playerId, int spawnSectionId, FactionCountry playerFaction,
                                    PlayerClass playerClass, int uniformId, GameObject playerObject)
        {
            // The client follows only itself, so it can stand the wall down when it is the one outside.
            if (_isClient) LocalPlayer.OnSpawned(playerId, playerObject);

            if (!ShouldEnforce) return;

            PlayerRegistry.OnSpawned(playerId, playerObject, playerFaction);
            ZoneEnforcer.OnSpawned(playerId);

            // Catch up anyone who joined after the last push, rather than leaving them on the config's plan.
            PlanBroadcaster.PushTo(playerId);
        }

        public void OnPlayerHurt(int playerId, byte oldHp, byte newHp, EntityHealthChangedReason reason)
        {
            if (ShouldEnforce) PlayerRegistry.OnHurt(playerId, newHp);
        }

        public void OnPlayerLeft(int playerId)
        {
            if (_isClient) LocalPlayer.OnLeft(playerId);

            if (!ShouldEnforce) return;

            PlayerRegistry.OnLeft(playerId);
            PlanBroadcaster.OnLeft(playerId);
        }

        public void OnSyncValueState(int value) { }
        public void OnUpdateSyncedTime(double time) { }
        public void OnUpdateElapsedTime(float time) { }
        public void OnPlayerJoined(int playerId, ulong steamId, string name, string regimentTag, bool isBot)
        {
            if (_isClient) LocalPlayer.OnJoined(playerId, steamId);

            // Server side this is the only place the mod is told a player is a bot, and it needs to know:
            // a bot cannot receive the private message the zone is pushed over.
            if (ShouldEnforce) PlayerRegistry.OnJoined(playerId, isBot);
        }
        public void OnPlayerKilledPlayer(int killerPlayerId, int victimPlayerId, EntityHealthChangedReason reason, string details) { }
        public void OnScorableAction(int playerId, int score, ScorableActionType reason) { }
        public void OnPlayerShoot(int playerId, bool dryShot) { }
        public void OnShotInfo(int playerId, int shotCount, Vector3[][] shotsPointsPositions, float[] trajectileDistances, float[] distanceFromFiringPositions, float[] horizontalDeviationAngles, float[] maxHorizontalDeviationAngles, float[] muzzleVelocities, float[] gravities, float[] damageHitBaseDamages, float[] damageRangeUnitValues, float[] damagePostTraitAndBuffValues, float[] totalDamages, Vector3[] hitPositions, Vector3[] hitDirections, int[] hitPlayerIds, int[] hitDamageableObjectIds, int[] hitShipIds, int[] hitVehicleIds) { }
        public void OnPlayerBlock(int attackingPlayerId, int defendingPlayerId) { }
        public void OnPlayerMeleeStartSecondaryAttack(int playerId) { }
        public void OnPlayerWeaponSwitch(int playerId, string weapon) { }
        public void OnPlayerStartCarry(int playerId, CarryableObjectType carryableObject) { }
        public void OnPlayerEndCarry(int playerId) { }
        public void OnPlayerShout(int playerId, CharacterVoicePhrase voicePhrase) { }
        public void OnConsoleCommand(string input, string output, bool success) { }
        public void OnRCLogin(int playerId, string inputPassword, bool isLoggedIn)
        {
            if (_isClient) AdminAccess.OnLogin(isLoggedIn);
        }
        public void OnRCCommand(int playerId, string input, string output, bool success)
        {
            // Reaching a client at all means the server answered us, and it only answers admins.
            if (_isClient) AdminAccess.OnCommandAnswered();

            if (ShouldEnforce) ConsoleCommandHandler.Process(playerId, input);
        }
        // The server pushes zone state to clients over the quiet-message channel, so this is the receiving
        // end rather than anything to do with chat.
        public void OnTextMessage(int playerId, TextChatChannel channel, string text)
        {
            if (!_isClient || channel != TextChatChannel.None) return;
            if (PlanReceiver.OnMessage(text)) return;
            if (PreviewSignal.TryDecode(text, out float seconds)) PlanPreview.Show(seconds);
        }
        public void OnAdminPlayerAction(int playerId, int adminId, ServerAdminAction action, string reason) { }
        public void OnDamageableObjectDamaged(GameObject damageableObject, int damageableObjectId, int shipId, int oldHp, int newHp) { }
        public void OnInteractableObjectInteraction(int playerId, int interactableObjectId, GameObject interactableObject, InteractionActivationType interactionActivationType, int nextActivationStateTransitionIndex) { }
        public void OnEmplacementPlaced(int itemId, GameObject objectBuilt, EmplacementType emplacementType) { }
        public void OnEmplacementConstructed(int itemId) { }
        public void OnCapturePointCaptured(int capturePoint) { }
        public void OnCapturePointOwnerChanged(int capturePoint, FactionCountry factionCountry) { }
        public void OnCapturePointDataUpdated(int capturePoint, int defendingPlayerCount, int attackingPlayerCount) { }
        public void OnBuffStart(int playerId, BuffType buff) { }
        public void OnBuffStop(int playerId, BuffType buff) { }
        public void OnRoundEndFactionWinner(FactionCountry factionCountry, FactionRoundWinnerReason reason) { }
        public void OnRoundEndPlayerWinner(int playerId) { }
        public void OnVehicleSpawned(int vehicleId, FactionCountry vehicleFaction, PlayerClass vehicleClass,
                                     GameObject vehicleObject, int ownerPlayerId)
        {
            if (ShouldEnforce) VehicleRegistry.OnSpawned(vehicleId, vehicleObject);
        }

        public void OnVehicleHurt(int vehicleId, byte oldHp, byte newHp, EntityHealthChangedReason reason) =>
            VehicleRegistry.OnHurt(vehicleId, newHp);

        public void OnPlayerKilledVehicle(int killerPlayerId, int victimVehicleId, EntityHealthChangedReason reason, string details) { }
        public void OnShipSpawned(int shipId, GameObject shipObject, FactionCountry shipfaction, ShipType shipType, int shipName) { }
        public void OnShipDamaged(int shipId, int oldHp, int newHp) { }

        public void OnPlayerPacket(int playerId, byte? instance, Vector3? ownerPosition, double? packetTimestamp, Vector2? ownerInputAxis, float? ownerRotationY, float? ownerPitch, float? ownerYaw, PlayerActions[] actionCollection, Vector3? cameraPosition, Vector3? cameraForward, ushort? shipID, bool swimming) { }
        public void OnVehiclePacket(int vehicleId, Vector2 inputAxis, bool shift, bool strafe, PlayerVehicleActions[] actionCollection) { }
        public void OnOfficerOrderStart(int officerPlayerId, HighCommandOrderType officerOrderType, Vector3 orderPosition, float orderRotationY, int voicePhraseRandomIndex) { }
        public void OnOfficerOrderStop(int officerPlayerId, HighCommandOrderType officerOrderType) { }

        public void OnStartSpectate(int playerId, int spectatedPlayerId) { }
        public void OnStopSpectate(int playerId, int spectatedPlayerId) { }
        // Raised on both sides: the client gets its own player, the server gets every player.
        public void OnStartFreeflight(int playerId) => OnFreeflight(playerId, flying: true);

        public void OnStopFreeflight(int playerId) => OnFreeflight(playerId, flying: false);

        private void OnFreeflight(int playerId, bool flying)
        {
            if (_isClient) LocalPlayer.OnFreeflight(flying);
            if (ShouldEnforce) ZoneEnforcer.OnFreeflight(playerId, flying);
        }
        public void OnMeleeArenaRoundEndFactionWinner(int roundId, bool attackers) { }
        // The real connect event, raised from PostAuthentication before the player enters limbo. OnPlayerJoined
        // is not: server side it means "entered the round", so it never fires for someone who only spectates.
        public void OnPlayerConnected(int playerId, bool isAutoAdmin, string backendId)
        {
            if (ShouldEnforce) PlanBroadcaster.OnConnected(playerId);
        }

        public void OnPlayerDisconnected(int playerId)
        {
            if (ShouldEnforce) PlanBroadcaster.OnLeft(playerId);
        }
    }
}
