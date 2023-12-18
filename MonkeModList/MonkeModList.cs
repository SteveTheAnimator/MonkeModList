using BepInEx;
using ComputerPlusPlus;
using GorillaNetworking;
using System;
using UnityEngine;
using Utilla;

namespace MonkeModList {
    [ModdedGamemode]
    [BepInDependency("org.legoandmars.gorillatag.utilla")]
    [BepInDependency("com.kylethescientist.gorillatag.computerplusplus")]
    [BepInPlugin(ModInfo.GUID, ModInfo.Name, ModInfo.Version)]
    public class MonkeModList : BaseUnityPlugin {
        void Start(){ Utilla.Events.GameInitialized += OnGameInitialized; }

        void OnEnable() {
            HarmonyPatches.ApplyHarmonyPatches();
        }

        void OnDisable() {
            HarmonyPatches.RemoveHarmonyPatches();
            enabled = true;
        }

        void OnGameInitialized(object sender, EventArgs e) {
            
        }
    }

    internal class ScreenData : IScreen {
        public string Title => "MOD LIST";

        public string Description => "WELCOME TO THE MONKE MOD LIST! \n <color=yellow>MADE BY MRBANANA</color>";

        public string GetContent() {

        }

        public void OnKeyPressed(GorillaKeyboardButton button) {

        }

        public void Start() { }
    }
}