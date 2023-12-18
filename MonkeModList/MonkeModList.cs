using BepInEx;
using ComputerPlusPlus;
using GorillaNetworking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Utilla;

namespace MonkeModList {
    [ModdedGamemode]
    [BepInDependency("org.legoandmars.gorillatag.utilla")]
    [BepInDependency("com.kylethescientist.gorillatag.computerplusplus")]
    [BepInPlugin(ModInfo.GUID, ModInfo.Name, ModInfo.Version)]
    public class MonkeModList : BaseUnityPlugin {

        public static MonkeModList instance;
        public bool UnableToGetData { get; private set; } = true;
        public string RawList { get; private set; } = null;

        void Start() { Events.GameInitialized += OnGameInitialized; }

        void OnGameInitialized(object sender, EventArgs e) {
            instance = this;
            GetModList();
        }

        public void GetModList() { StartCoroutine(GetData()); }
        IEnumerator GetData() {
            UnityWebRequest www = UnityWebRequest.Get("https://raw.githubusercontent.com/MrBanana01/MonkeModList/master/List");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success) {
                RawList = www.downloadHandler.text;
                UnableToGetData = false;
            }
            else {
                UnableToGetData = true;
                RawList = null;
            }

            ScreenData.instance.RefreshModList();
        }
    }

    internal class ScreenData : IScreen {
        public static ScreenData instance;

        public string Title => "MOD LIST";

        public string Description => "WELCOME TO THE MONKE MOD LIST! \nW or S = Select Mod \nA or D = Switch Page \n Enter = Download Mod \n<color=yellow>MADE BY MRBANANA</color>";

        public List<string[]> listedMods = new List<string[]>();

        public int CurrentPage { get; private set; } = 0;

        public int SelectedMod { get; private set; } = 0;

        public int ModsPerPage { get; private set; } = 6;

        public string GetContent() {
            StringBuilder content = new StringBuilder();

            if (MonkeModList.instance.UnableToGetData) {
                content.AppendLine("\n ---------------------------------------------------------- \n");
                content.AppendLine("\n <color=red>COULD NOT GET THE MOD LIST FROM GITHUB!</color> \n");
                content.AppendLine("\n ---------------------------------------------------------- \n");
            }
            else {
                int startIndex = CurrentPage * ModsPerPage;
                int endIndex = Mathf.Min(startIndex + ModsPerPage, listedMods.Count);

                for (int i = startIndex; i < endIndex; i++) {
                    string modInfo = $"{listedMods[i][0].ToUpper()}, {listedMods[i][1].ToUpper()}";

                    if (i == SelectedMod) {
                        modInfo = $"> {modInfo}";
                    }

                    content.AppendLine(modInfo);
                }
            }

            return content.ToString();
        }

        public void OnKeyPressed(GorillaKeyboardButton button)
        {
            if (button.characterString == "option1")
                RefreshModList();
            else if (button.characterString == "W") {
                int totalModsOnPage = Mathf.Min(listedMods.Count - CurrentPage * ModsPerPage, ModsPerPage);
                SelectedMod = (SelectedMod - 1 + totalModsOnPage) % totalModsOnPage;
            }
            else if (button.characterString == "S") {
                int totalModsOnPage = Mathf.Min(listedMods.Count - CurrentPage * ModsPerPage, ModsPerPage);
                SelectedMod = (SelectedMod + 1) % totalModsOnPage;
            }
            else if (button.characterString == "A") {
                CurrentPage--;
                CurrentPage = Mathf.Clamp(CurrentPage, 0, Mathf.CeilToInt((float)listedMods.Count / ModsPerPage) - 1);
                SelectedMod = Mathf.Min(SelectedMod, Mathf.Min(listedMods.Count - CurrentPage * ModsPerPage, ModsPerPage) - 1);
            }
            else if (button.characterString == "D") {
                CurrentPage++;
                CurrentPage = Mathf.Clamp(CurrentPage, 0, Mathf.CeilToInt((float)listedMods.Count / ModsPerPage) - 1);
                SelectedMod = 0;
            }
        }

        public void RefreshModList() {
            if(MonkeModList.instance.RawList != null) {
                listedMods.Clear();

                string[] lines = MonkeModList.instance.RawList.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in lines)
                {
                    string[] parts = line.Split(' ');

                    if (parts.Length >= 3)
                    {
                        string name = parts[0];
                        string version = parts[1];
                        string url = string.Join(" ", parts.Skip(2));

                        string[] modInfo = { name, version, url };
                        listedMods.Add(modInfo);
                    }
                }
            }
        }

        public void Start() { instance = this; MonkeModList.instance.GetModList(); }
    }
}