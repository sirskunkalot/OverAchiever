// OverAchiever
// a Valheim mod skeleton using Jötunn
// 
// File:    OverAchiever.cs
// Project: OverAchiever

using BepInEx;
using HarmonyLib;
using Jotunn;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace OverAchiever
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class OverAchiever : BaseUnityPlugin
    {
        public const string PluginGUID = "com.jotunn.OverAchiever";
        public const string PluginName = "OverAchiever";
        public const string PluginVersion = "0.0.1";
        
        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private Harmony harmony;

        private void Awake()
        {
            harmony = new Harmony(PluginGUID);
            harmony.PatchAll(typeof(AchievementPatches));

            CreatePOIPiece();
        }

        private void CreatePOIPiece()
        {
            var prefab = PrefabManager.Instance.CreateEmptyPrefab("OA_poi_piece");
            var piece = prefab.AddComponent<Piece>();
            piece.m_name = prefab.name;
            var icon = RenderManager.Instance.Render(prefab, RenderManager.IsometricRotation);
            var piececonfig = new PieceConfig();
            piececonfig.Name = "$OA_poi_piece";
            piececonfig.Description = "$OA_poi_piece_description";
            piececonfig.Icon = icon;
            piececonfig.PieceTable = "Hammer";
            piececonfig.Category = "Misc";
            PieceManager.Instance.AddPiece(new CustomPiece(prefab, false, piececonfig));

            var achievement = prefab.AddComponent<ApproximityAchievement>();
            achievement.ID = "poipiece";
            achievement.Text = "Found that POI";
        }
    }

    public static class AchievementPatches
    {
        [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned)), HarmonyPostfix]
        public static void Player_OnSpawned(Player __instance)
        {
            if (__instance == Player.m_localPlayer)
            {
                __instance.gameObject.GetOrAddComponent<AchievementSystem>();
            }
        }
    }

    public class AchievementSystem : MonoBehaviour
    {
        private Player Player;

        private void Start()
        {
            Player = GetComponent<Player>();
        }

        public void RedeemAchievement(Achievement achievement)
        {
            if (Player == null)
            {
                return;
            }

            var id = $"{WorldGenerator.instance.m_world.m_uid}!{achievement.GetID()}";
            
            if (Player.m_knownTexts.ContainsKey(id))
            {
                return;
            }

            Player.AddKnownText(id, achievement.GetText());
            Player.Message(MessageHud.MessageType.Center, achievement.GetText());
            Logger.LogDebug($"Redeemed {id}");
        }
    }

    public interface Achievement
    {
        string GetID();

        string GetText();
    }

    public class ApproximityAchievement : MonoBehaviour, Achievement, Interactable, Hoverable
    {
        public string ID;
        public string Text;
        
        private float LastLookedTime = -9999f;
        private float LastUseTime = -9999f;
        private readonly float HoldRepeatInterval = 1f;

        public string GetID()
        {
            return ID;
        }

        public string GetText()
        {
            return Text;
        }
        
        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (!(user is Player player))
            {
                return false;
            }

            if (hold)
            {
                if (Time.time - LastUseTime < HoldRepeatInterval)
                {
                    return false;
                }

                LastUseTime = Time.time;
            }

            player.GetComponent<AchievementSystem>()?.RedeemAchievement(this);
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        public string GetHoverText()
        {
            return ID;
        }

        public string GetHoverName()
        {
            return ID;
        }
    }
}

