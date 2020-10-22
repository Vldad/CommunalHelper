﻿using Celeste.Mod.CommunalHelper.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Reflection;

namespace Celeste.Mod.CommunalHelper {
    public class CommunalHelperModule : EverestModule {

        public static CommunalHelperModule Instance;

        public override Type SettingsType => typeof(CommunalHelperSettings);
        public static CommunalHelperSettings Settings => (CommunalHelperSettings) Instance._Settings;
        
        public override Type SaveDataType => typeof(CommunalHelperSaveData);
        public static CommunalHelperSaveData SaveData => (CommunalHelperSaveData) Instance._SaveData;

        public override Type SessionType => typeof(CommunalHelperSession);
        public static CommunalHelperSession Session => (CommunalHelperSession) Instance._Session;

        public static SpriteBank SpriteBank => Instance._SpriteBank;
        public SpriteBank _SpriteBank;
        
        public CommunalHelperModule() {
            Instance = this;
        }

        public override void Load() {
            Everest.Events.Level.OnLoadEntity += Level_OnLoadEntity;
            Everest.Events.CustomBirdTutorial.OnParseCommand += CustomBirdTutorial_OnParseCommand;

            DreamTunnelDash.Load();
            DreamRefill.Load();

            CustomDreamBlock.Load();
            ConnectedDreamBlock.Hook();
            ConnectedSwapBlockHooks.Hook();
            CustomCassetteBlockHooks.Hook();

            AttachedWallBooster.Hook();
            MoveBlockRedirect.Load();
            MoveSwapBlock.Load();
            SyncedZipMoverActivationControllerHooks.Hook();

            HeartGemShard.Load();
            CustomSummitGem.Load();
        }

        public override void Unload() {
            Everest.Events.Level.OnLoadEntity -= Level_OnLoadEntity;
            Everest.Events.CustomBirdTutorial.OnParseCommand -= CustomBirdTutorial_OnParseCommand;

            DreamTunnelDash.Unload();
            DreamRefill.Unload();

            CustomDreamBlock.Unload();
            ConnectedDreamBlock.Unhook();
            ConnectedSwapBlockHooks.Unhook();
            CustomCassetteBlockHooks.Unhook();

			AttachedWallBooster.Unhook();
            MoveBlockRedirect.Unload();
            MoveSwapBlock.Unload();
            SyncedZipMoverActivationControllerHooks.Unhook();

            HeartGemShard.Unload();
            CustomSummitGem.Unload();
        }

		public override void LoadContent(bool firstLoad) {
            _SpriteBank = new SpriteBank(GFX.Game, "Graphics/CommunalHelper/Sprites.xml");

			StationBlock.InitializeParticles();

            DreamTunnelDash.LoadContent();
            DreamRefill.InitializeParticles();
            DreamMoveBlock.InitializeParticles();
            DreamSwitchGate.InitializeParticles();
            
            ConnectedMoveBlock.InitializeTextures();
            ConnectedSwapBlock.InitializeTextures();

            HeartGemShard.InitializeParticles();
        }

        // Loading "custom" entities
        private bool Level_OnLoadEntity(Level level, LevelData levelData, Vector2 offset, EntityData entityData) {
            // Intercept an attempt to load the custom heart (has nodes)
            // Call Level.LoadCustomEntity again incase we skipped over another custom entity handler
            if (entityData.Name == "CommunalHelper/CrystalHeart") {
                entityData.Name = "blackGem";
                entityData.Values[HeartGemShard.HeartGem_HeartGemID] = new EntityID(levelData.Name, entityData.ID);
                return Level.LoadCustomEntity(entityData, level);
            }

            if (entityData.Name == "CommunalHelper/AdventureHelper/CustomCrystalHeart") {
                entityData.Name = "AdventureHelper/CustomCrystalHeart";
                entityData.Values[HeartGemShard.HeartGem_HeartGemID] = new EntityID(levelData.Name, entityData.ID);
                return Level.LoadCustomEntity(entityData, level);
            }

            return false;
        }

        private object CustomBirdTutorial_OnParseCommand(string command) {
            // Thank you max480.
            if (command == "CommunalHelperSyncedZipMoverBinding") {
                return Settings.AllowActivateRebinding ?
                    Settings.ActivateSyncedZipMovers.Button : Input.Grab;
            }
            return null;
        }
    }

	public static class Util {
		public static void log(string str) {
			Logger.Log("Communal Helper", str);
		}

        public static Player GetPlayer() {
            if(Engine.Scene == null) { return null; }
            else {
                return Engine.Scene.Tracker.GetEntity<Player>();
            }
        }

        public static Color ColorFix(string str, float alpha = 1f) {
            PropertyInfo[] prop = typeof(Color).GetProperties();
            foreach (PropertyInfo p in prop) { if (str == p.Name) { return new Color((Color) p.GetValue(default(Color)), alpha); } }
            return new Color(Calc.HexToColor(str.Trim('#')), alpha);
        }
	}

    #region JaThePlayer's state machine extension code
    internal static class StateMachineExt {
		/// <summary>
		/// Adds a state to a StateMachine
		/// </summary>
		/// <returns>The index of the new state</returns>
		public static int AddState(this StateMachine machine, Func<int> onUpdate, Func<IEnumerator> coroutine = null, Action begin = null, Action end = null) {
			Action[] begins = (Action[])StateMachine_begins.GetValue(machine);
			Func<int>[] updates = (Func<int>[])StateMachine_updates.GetValue(machine);
			Action[] ends = (Action[])StateMachine_ends.GetValue(machine);
			Func<IEnumerator>[] coroutines = (Func<IEnumerator>[])StateMachine_coroutines.GetValue(machine);
			int nextIndex = begins.Length;
			// Now let's expand the arrays
			Array.Resize(ref begins, begins.Length + 1);
			Array.Resize(ref updates, begins.Length + 1);
			Array.Resize(ref ends, begins.Length + 1);
			Array.Resize(ref coroutines, coroutines.Length + 1);
			// Store the resized arrays back into the machine
			StateMachine_begins.SetValue(machine, begins);
			StateMachine_updates.SetValue(machine, updates);
			StateMachine_ends.SetValue(machine, ends);
			StateMachine_coroutines.SetValue(machine, coroutines);
			// And now we add the new functions
			machine.SetCallbacks(nextIndex, onUpdate, coroutine, begin, end);
			return nextIndex;
		}
		private static FieldInfo StateMachine_begins = typeof(StateMachine).GetField("begins", BindingFlags.Instance | BindingFlags.NonPublic);
		private static FieldInfo StateMachine_updates = typeof(StateMachine).GetField("updates", BindingFlags.Instance | BindingFlags.NonPublic);
		private static FieldInfo StateMachine_ends = typeof(StateMachine).GetField("ends", BindingFlags.Instance | BindingFlags.NonPublic);
		private static FieldInfo StateMachine_coroutines = typeof(StateMachine).GetField("coroutines", BindingFlags.Instance | BindingFlags.NonPublic);
	}
    #endregion

}
