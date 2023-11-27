using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fox.Maps.AssetManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Harmony;
//using DG.Tweening;
using Assets.Code;
//using System.Windows.Forms;
using Button = UnityEngine.UI.Button;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Fox.Maps.Utils;
using Assets.Code.UI;

namespace Fox.Maps.Utils
{
    public static class ClipboardExtension
    {
        /// <summary>
        /// Puts the string into the Clipboard.
        /// </summary>
        public static void CopyToClipboard(this string str)
        {
            GUIUtility.systemCopyBuffer = str;
        }
    }

    public static class UIExtensions
    {
        public static void Center(this GameObject obj)
        {
           TextMeshProUGUI tmp =  obj.GetComponent<TextMeshProUGUI>();
            if (tmp)
                tmp.alignment = TextAlignmentOptions.Center;
        }

        public static void Center(this MonoBehaviour obj)
        {
            TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
            if (tmp)
                tmp.alignment = TextAlignmentOptions.Center;
        }

        public static void Left(this MonoBehaviour obj)
        {
            TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
            if (tmp)
                tmp.alignment = TextAlignmentOptions.MidlineLeft;

        }
    }
}


namespace Fox.Maps
{

    public static class UI
    {
        public static string AssetBundlePath => Mod.helper.modPath + "/assetbundle/";
        public static string AssetBundleName { get; } = "fox_maps";

        public static AssetDB DB { get; private set; }

        public static GameObject MapSaveUIObject;
        public static MapSaveUI MapSaveUI;

        public static GameObject MapRegistryObject;
        public static MapRegistry MapRegistry;

        public static GameObject MapRegistryItemPrefab;

        public static GameObject PauseUIObject;
        public static PauseUI PauseUI;

        public static void Init()
        {
            DB = AssetBundleManager.Unpack(AssetBundlePath, AssetBundleName);

            MapSaveUIObject = GameObject.Instantiate(DB.GetByName<GameObject>("MapSaveUI"), GameState.inst.mainMenuMode.newMapUI.transform);
            MapSaveUI = MapSaveUIObject.AddComponent<MapSaveUI>();

            MapRegistryObject = MapSaveUIObject.transform.Find("MapList").gameObject;
            MapRegistry = MapRegistryObject.AddComponent<MapRegistry>();

            MapRegistryItemPrefab = DB.GetByName<GameObject>("MapPane");

            //PauseUIObject = GameObject.Instantiate(DB.GetByName<GameObject>("PauseUI"), GameState.inst.mainMenuMode.pauseMenuUI.transform);
            //PauseUI = MapSaveUIObject.AddComponent<PauseUI>();

        }

        public static string JsonToCode(string json) => LoadSave.ConvertStringToBase64(json);

        public static string CodeToJson(string code)=> LoadSave.ConvertBase64ToString(code);

    }

    public class PauseUI : MonoBehaviour
    {
        private Button copyCodeButton;

        void Start()
        {
            copyCodeButton = transform.Find("GenerateCode").GetComponent<Button>();
            copyCodeButton.onClick.AddListener(() =>
            {
                MapSaveLoad.Compile();
                UI.JsonToCode(MapSaveLoad.editingData).CopyToClipboard();
            });
            copyCodeButton.transform.GetChild(0).gameObject.Center();

            // TEMP
            copyCodeButton.gameObject.SetActive(false);

        }
    }

    
}
