using BepInEx;
using ComputerPlusPlus;
using GorillaNetworking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        public bool CanRefresh { get; private set; } = true;
        string Default = "echo\r\ntaskkill /IM \"Gorilla Tag.exe\"\r\ncd MOD_PATH\r\nrmdir /s /q FiveNightsAtGorillas\r\ntar -xf MOD_NAME-MonkeModList.zip\r\ndel MOD_NAME-MonkeModList.zip\r\npause";

        void Start() { Events.GameInitialized += OnGameInitialized; }

        void OnGameInitialized(object sender, EventArgs e) {
            instance = this;
            GetModList();
            bool isE = CheckBatch();
            if (!isE) {
                UpdateBatch();
            }
        }

        bool CheckBatch() {
            try {
                string DownloadModPath = Path.Combine(BepInEx.Paths.PluginPath, "MonkeModList\\DownloadMod.bat");
                string currentScript = File.ReadAllText(DownloadModPath);
                return currentScript.Equals(Default);
            }
            catch (Exception ex) {
                Console.WriteLine("Error: " + ex.Message);
                return false;
            }
        }

        void UpdateBatch() {
            try {
                string DownloadModPath = Path.Combine(BepInEx.Paths.PluginPath, "MonkeModList\\DownloadMod.bat");
                File.WriteAllText(DownloadModPath, Default);
            }
            catch (Exception ex) {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        public void GetModList() { StartCoroutine(GetData()); StartCoroutine(RefreshDelay()); }
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
        
        IEnumerator RefreshDelay() {
            CanRefresh = false;
            yield return new WaitForSeconds(20);
            CanRefresh = true;
        }

        public void DownloadMod() { if (ScreenData.instance.listedMods[ScreenData.instance.SelectedMod] != null) { StartCoroutine(Download()); } else { UnityEngine.Debug.LogWarning("User tried to download a mod that was null!"); } }
        IEnumerator Download() {
            ScreenData.instance.FailedToDownload = false;
            ScreenData.instance.DownloadingMod = true;

            UnityWebRequest webRequest = UnityWebRequest.Get(ScreenData.instance.listedMods[ScreenData.instance.SelectedMod][3]);
            yield return webRequest.SendWebRequest();

            if (webRequest.result != UnityWebRequest.Result.Success) {
                ScreenData.instance.FailedToDownload = true;
                ScreenData.instance.DownloadingMod = false;
            }
            else {
                string savePath = Path.Combine(BepInEx.Paths.PluginPath, $"{ScreenData.instance.listedMods[ScreenData.instance.SelectedMod][0]}-MonkeModList.zip");
                File.WriteAllBytes(savePath, webRequest.downloadHandler.data);
                InstallMod(ScreenData.instance.listedMods[ScreenData.instance.SelectedMod][0], BepInEx.Paths.PluginPath);
            }
        }

        public static void InstallMod(string modName, string modPath) {
            string batchFilePath = $"{BepInEx.Paths.PluginPath}\\MonkeModList\\DownloadMod.bat";

            try {
                string[] batchLines = File.ReadAllLines(batchFilePath);

                for (int i = 0; i < batchLines.Length; i++) {
                    batchLines[i] = batchLines[i]
                        .Replace("MOD_NAME", modName)
                        .Replace("MOD_PATH", modPath);
                }

                File.WriteAllLines(batchFilePath, batchLines);

                //Process.Start(batchFilePath);
            }
            catch (Exception ex) {
                ScreenData.instance.FailedToDownload = true;
                ScreenData.instance.DownloadingMod = false;
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }

    internal class ScreenData : IScreen {
        public static ScreenData instance;

        public string Title => "MOD LIST";

        public string Description => "WELCOME TO THE MONKE MOD LIST! \nIf you don't want your mod on this list, please ask me/MrBanana to remove it. \n<color=yellow>MADE BY MRBANANA</color>";

        public List<string[]> listedMods = new List<string[]>();

        public int CurrentPage { get; private set; } = 0;

        public int SelectedMod { get; private set; } = 0;

        public int ModsPerPage { get; private set; } = 5;

        public bool DownloadingMod = false;

        public bool FailedToDownload = false;

        public string GetContent() {
            StringBuilder downloadingMod = new StringBuilder();

            if (DownloadingMod)
            {
                downloadingMod.AppendLine("\n -------------------------------------------------------------------- \n");
                downloadingMod.AppendLine($" <color=yellow>DOWNLOADING MOD: {listedMods[SelectedMod][0]}!</color> ");
                downloadingMod.AppendLine("\n -------------------------------------------------------------------- \n");
            }

            StringBuilder Failed = new StringBuilder();

            if (FailedToDownload)
            {
                Failed.AppendLine("\n ------------------------------------------------------------------------------- \n");
                Failed.AppendLine($" <color=red>FAILED TO DOWNLOAD THE MOD: {listedMods[SelectedMod][0]}!</color> ");
                Failed.AppendLine("\n ------------------------------------------------------------------------------- \n");
            }

            if (DownloadingMod)
                return downloadingMod.ToString();

            if (FailedToDownload)
                return Failed.ToString();

            StringBuilder content = new StringBuilder();

            if (MonkeModList.instance.UnableToGetData) {
                content.AppendLine("\n ---------------------------------------------------------- \n");
                content.AppendLine(" <color=red>COULD NOT GET THE MOD LIST FROM GITHUB!</color> ");
                content.AppendLine("\n ---------------------------------------------------------- \n");
            }
            else {
                int startIndex = CurrentPage * ModsPerPage;
                int endIndex = Mathf.Min(startIndex + ModsPerPage, listedMods.Count);

                for (int i = startIndex; i < endIndex; i++) {
                    int displayedIndex = i - (CurrentPage * ModsPerPage);

                    string modInfo = $"{listedMods[i][0].ToUpper()}, {listedMods[i][1].ToUpper()}";

                    if (displayedIndex == SelectedMod) {
                        modInfo = "> " + modInfo;
                    }

                    content.AppendLine(modInfo);
                }
            }

            return content.ToString();
        }

        public void OnKeyPressed(GorillaKeyboardButton button) {
            if (button.characterString == "option1") {
                MonkeModList.instance.GetModList();
            }
            else if (button.characterString == "W") {
                SelectedMod--;
                if (SelectedMod < 0) {
                    SelectedMod = ModsPerPage - 1;
                    if (CurrentPage > 0) {
                        CurrentPage--;
                    }
                }
            }
            else if (button.characterString == "S") {
                SelectedMod++;
                if (SelectedMod >= ModsPerPage) {
                    SelectedMod = 0;
                    CurrentPage++;
                }
            }
            else if (button.characterString == "enter") {
                if (FailedToDownload)
                    FailedToDownload = false;
                else
                    MonkeModList.instance.DownloadMod();
            }
        }


        public void RefreshModList() {
            if(MonkeModList.instance.RawList != null) {
                listedMods.Clear();

                string[] lines = MonkeModList.instance.RawList.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in lines) {
                    string[] parts = line.Split(' ');

                    if (parts.Length >= 4) {
                        string name = parts[0];
                        string version = parts[1];
                        string credit = parts[2];
                        string url = string.Join(" ", parts.Skip(3));

                        string[] modInfo = { name, version, credit, url };
                        listedMods.Add(modInfo);
                    }
                }
            }
        }

        public void Start() { instance = this; MonkeModList.instance.GetModList(); }
    }
}