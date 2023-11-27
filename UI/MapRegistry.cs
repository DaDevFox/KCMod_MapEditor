using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Assets.Code;
using Fox.Maps.Utils;
using System.Reflection;

namespace Fox.Maps
{

    public class MapRegistry : MonoBehaviour
    {

        public static Color noneB = new Color(216f, 216f, 54f);
        public static Color noneF = new Color(116f, 156f, 7f);
        public static Color noneV = new Color(68f, 102f, 0f);

        public static Color woodB = new Color(90f, 137f, 16f);
        public static Color woodF = new Color(59f, 118f, 2f);
        public static Color woodV = new Color(43f, 102f, 0f);

        public static Color unusablestoneB = new Color(84, 84, 34);
        public static Color unusablestoneF = new Color(53, 65, 19);
        public static Color unusablestoneV = new Color(38, 46, 17);

        public static Color stoneB = new Color(117, 189, 132);
        public static Color stoneF = new Color(146, 170, 117);
        public static Color stoneV = new Color(131, 154, 115);

        public static Color irondepositB = new Color(207, 148, 37);
        public static Color irondepositF = new Color(68, 102, 0);
        public static Color irondepositV = new Color(68, 102, 0);

        public static Color witchhut = new Color(68, 102, 0);
        public static Color cave = new Color(68, 102, 0);
        public static Color waterDeep = new Color(35, 35, 178);
        public static Color waterDeepFish = new Color(89, 89, 183);
        public static Color waterShallow = new Color(45, 76, 229);
        public static Color waterFish = new Color(89f, 130f, 255f);

        public static bool realisticWaterColors = false;

        public static bool active = false;

        private Dictionary<string, MapRegistryItem> items = new Dictionary<string, MapRegistryItem>();

        private Animator animator = new Animator();

        public void Toggle()
        {
            gameObject.SetActive(!gameObject.activeSelf);
            active = gameObject.activeSelf;
        }

        void Start()
        {
            // resource type colors

            // none
            noneB.r /= 255f;
            noneB.g /= 255f;
            noneB.b /= 255f;
            noneF.r /= 255f;
            noneF.g /= 255f;
            noneF.b /= 255f;
            noneV.r /= 255f;
            noneV.g /= 255f;
            noneV.b /= 255f;

            // wood
            woodB.r /= 255f;
            woodB.g /= 255f;
            woodB.b /= 255f;
            woodF.r /= 255f;
            woodF.g /= 255f;
            woodF.b /= 255f;
            woodV.r /= 255f;
            woodV.g /= 255f;
            woodV.b /= 255f;

            // unusablestone
            unusablestoneB.r /= 255f;
            unusablestoneB.g /= 255f;
            unusablestoneB.b /= 255f;
            unusablestoneF.r /= 255f;
            unusablestoneF.g /= 255f;
            unusablestoneF.b /= 255f;
            unusablestoneV.r /= 255f;
            unusablestoneV.g /= 255f;
            unusablestoneV.b /= 255f;

            // stone
            stoneB.r /= 255f;
            stoneB.g /= 255f;
            stoneB.b /= 255f;
            stoneF.r /= 255f;
            stoneF.g /= 255f;
            stoneF.b /= 255f;
            stoneV.r /= 255f;
            stoneV.g /= 255f;
            stoneV.b /= 255f;

            // irondeposit
            irondepositB.r /= 255f;
            irondepositB.g /= 255f;
            irondepositB.b /= 255f;
            irondepositF.r /= 255f;
            irondepositF.g /= 255f;
            irondepositF.b /= 255f;
            irondepositV.r /= 255f;
            irondepositV.g /= 255f;
            irondepositV.b /= 255f;

            // witchhut
            witchhut.r /= 255f;
            witchhut.g /= 255f;
            witchhut.b /= 255f;

            // cave
            cave.r /= 255f;
            cave.g /= 255f;
            cave.b /= 255f;

            // waterDeep
            waterDeep.r /= 255f;
            waterDeep.g /= 255f;
            waterDeep.b /= 255f;

            // waterShallow
            waterShallow.r /= 255f;
            waterShallow.g /= 255f;
            waterShallow.b /= 255f;

            // waterDeepFish
            waterDeepFish.r /= 255f;
            waterDeepFish.g /= 255f;
            waterDeepFish.b /= 255f;

            // waterFish
            waterFish.r /= 255f;
            waterFish.g /= 255f;
            waterFish.b /= 255f;
        }

        void Update()
        {
            foreach (MapSaveLoad.MapSaveData map in MapSaveLoad.registry)
                if (!items.ContainsKey(map.name))
                    items.Add(map.name, CreateMapItem(map));

            List<string> toRemove = new List<string>();

            foreach (string mapName in items.Keys)
            {
                bool contains = false;
                foreach (MapSaveLoad.MapSaveData map in MapSaveLoad.registry)
                    if (map.name == mapName)
                        contains = true;

                if (!contains)
                    toRemove.Add(mapName);
            }

            foreach (string name in toRemove)
            {
                GameObject.Destroy(items[name].gameObject);
                items.Remove(name);
            }
        }

        public MapRegistryItem GetByName(string name) => items.ContainsKey(name) ? items[name] : null;

        private MapRegistryItem CreateMapItem(MapSaveLoad.MapSaveData data)
        {
            MapRegistryItem item = GameObject.Instantiate(UI.MapRegistryItemPrefab, transform.Find("Scroll View/Viewport/Content")).AddComponent<MapRegistryItem>();
            item.data = data;

            return item;
        }

        public class Animator
        {
            public MapRegistry registry;

            private float value = 0f;
            private float desiredValue = 0f;

            private int initialValue = -1;

            public float transitionSpeed = 2f;

            public void Update()
            {
                RectTransform transform = registry.transform as RectTransform;

                if (active)
                    desiredValue = 1f;
                else
                    desiredValue = 0f;

                value = Mathf.Lerp(value, desiredValue, Time.deltaTime * transitionSpeed);

                //if(initialValue == -1f)
                //    initialValue = transform.
            }
        }
    }

    public class MapRegistryItem : MonoBehaviour
    {


        public MapSaveLoad.MapSaveData data { get; set; }

        private RawImage preview;
        private TextMeshProUGUI nameText;
        private TextMeshProUGUI dimensionsText;
        private TextMeshProUGUI treesText;
        private TextMeshProUGUI rocksText;
        private TextMeshProUGUI stonesText;
        private TextMeshProUGUI ironsText;
        private Button copyButton;
        private Button deleteButton;

        void Start()
        {
            preview = transform.Find("Previe").GetComponent<RawImage>();
            nameText = transform.Find("NameText").GetComponent<TextMeshProUGUI>();
            dimensionsText = transform.Find("InfoPanel/Text (TMP)").GetComponent<TextMeshProUGUI>();
            treesText = transform.Find("InfoPanel/InfosContainer/Trees/Text (TMP)").GetComponent<TextMeshProUGUI>();
            rocksText = transform.Find("InfoPanel/InfosContainer/Rocks/Text (TMP)").GetComponent<TextMeshProUGUI>();
            stonesText = transform.Find("InfoPanel/InfosContainer/Stones/Text (TMP)").GetComponent<TextMeshProUGUI>();
            ironsText = transform.Find("InfoPanel/InfosContainer/Irons/Text (TMP)").GetComponent<TextMeshProUGUI>();
            copyButton = transform.Find("CopyButton").GetComponent<Button>();
            deleteButton = transform.Find("InfoPanel/DeleteButton").GetComponent<Button>();

            nameText.Left();
            dimensionsText.Center();
            treesText.Left();
            rocksText.Left();
            stonesText.Left();
            ironsText.Left();
            copyButton.transform.GetChild(0).gameObject.Center();

            UpdateInformation();

            preview.GetComponent<Button>().onClick.AddListener(() =>
            {
                MapSaveLoad.LoadMap(data);
                UI.MapSaveUI.mapName.text = data.name;
            });

            copyButton.onClick.AddListener(() =>
            {
                try
                {
                    UI.JsonToCode(JsonConvert.SerializeObject(data)).CopyToClipboard();
                }
                catch (Exception ex)
                {
                    Mod.Log(ex);
                }
            });

            deleteButton.onClick.AddListener(() =>
            {
                UI.MapSaveUI.deleteConfirmation.ShowConfirmation(() =>
                {
                    MapSaveLoad.registry.Remove(data);
                    MapSaveLoad.SaveRegistry();
                }, () => { });
            });
        }

        public void UpdateInformation()
        {
            Analyze(out int trees, out int rocks, out int stones, out int irons);
            try
            {
                Texture2D texture = Preview(data);
                preview.texture = texture;
            }
            catch(Exception ex)
            {
                Mod.Log(ex);
            }

            nameText.text = data.name;
            dimensionsText.text = $"{data.terrainData.gridWidth} x {data.terrainData.gridHeight}";
            treesText.text = trees.ToString();
            rocksText.text = rocks.ToString();
            stonesText.text = stones.ToString();
            ironsText.text = irons.ToString();
        }


        private void Analyze(out int treeAmount, out int rockAmount, out int stoneAmount, out int ironAmount)
        {
            treeAmount = 0;
            rockAmount = 0;
            stoneAmount = 0;
            ironAmount = 0;
            for (int i = 0; i < data.terrainData.cellSaveData.Length; i++)
            {
                Cell.CellSaveData cell = data.terrainData.cellSaveData[i];

                treeAmount += cell.amount;
                rockAmount += cell.type == ResourceType.UnusableStone ? 1 : 0;
                stoneAmount += cell.type == ResourceType.Stone ? 1 : 0;
                ironAmount += cell.type == ResourceType.IronDeposit ? 1 : 0;
            }
        }

        public Texture2D Preview(MapSaveLoad.MapSaveData data)
        {
            Texture2D texture = new Texture2D(data.terrainData.gridWidth, data.terrainData.gridHeight);

            for (int i = 0; i < data.terrainData.cellSaveData.Length; i++)
            {
                int x = i % data.terrainData.gridWidth;
                int z = i / data.terrainData.gridWidth;

                Cell.CellSaveData cell = data.terrainData.cellSaveData[i];

                Color color = Color.blue;


                if (cell == null)
                {
                    color = Color.black;
                    texture.SetPixel(x, z, color);
                    continue;
                }

                Color water = Water.inst.waterMat.GetColor("_Color");
                Color deepWater = Water.inst.waterMat.GetColor("_DeepColor");
                Color saltWater = Water.inst.waterMat.GetColor("_SaltColor");
                Color saltDeepWater = Water.inst.waterMat.GetColor("_SaltDeepColor");

                if (cell.type == ResourceType.Water)
                {
                    color = water;

                    if (cell.deepWater)
                        color = MapRegistry.realisticWaterColors ? deepWater : MapRegistry.waterDeep;
                    else if (!MapRegistry.realisticWaterColors)
                        color = MapRegistry.waterShallow;

                    if (cell.saltWater && MapRegistry.realisticWaterColors)
                        color = saltWater;

                    if (cell.deepWater && cell.saltWater)
                        color = MapRegistry.realisticWaterColors ? saltDeepWater : MapRegistry.waterDeep;

                    if (data.fishData.fishPerCell[i] > 0)
                        color = MapRegistry.waterFish;

                    if (cell.deepWater && data.fishData.fishPerCell[i] > 0 && !MapRegistry.realisticWaterColors)
                        color = MapRegistry.waterDeepFish;
                }

                else if (cell.type == ResourceType.WitchHut)
                    color = new Color(0.5490196f, 0.3490196f, 0.01960784f);
                else if (cell.type == ResourceType.EmptyCave || cell.type == ResourceType.WolfDen)
                    color = MapRegistry.cave;
                //else if(cell.type == ResourceType.None)
                //{
                //    if (cell.fertile == 0)
                //        color = noneB;
                //    if (cell.fertile == 1)
                //        color = noneF;
                //    if (cell.fertile == 2)
                //        color = noneV;
                //}
                //else if(cell.type == ResourceType.Wood)
                //{
                //    if (cell.fertile == 0)
                //        color = woodB;
                //    if (cell.fertile == 1)
                //        color = woodF;
                //    if (cell.fertile == 2)
                //        color = woodV;
                //}
                //else if (cell.type == ResourceType.Stone)
                //{
                //    if (cell.fertile == 0)
                //        color = stoneB;
                //    if (cell.fertile == 1)
                //        color = stoneF;
                //    if (cell.fertile == 2)
                //        color = stoneV;
                //}
                //else if (cell.type == ResourceType.UnusableStone)
                //{
                //    if (cell.fertile == 0)
                //        color = unusablestoneB;
                //    if (cell.fertile == 1)
                //        color = unusablestoneF;
                //    if (cell.fertile == 2)
                //        color = unusablestoneV;
                //}
                //else if (cell.type == ResourceType.IronDeposit)
                //{
                //    if (cell.fertile == 0)
                //        color = irondepositB;
                //    if (cell.fertile == 1)
                //        color = irondepositF;
                //    if (cell.fertile == 2)
                //        color = irondepositV;
                //}
                else
                {
                    color = (Color)typeof(MapRegistry).GetField(cell.type.ToString().ToLower() + (cell.fertile == 0 ? "B" : (cell.fertile == 1 ? "F" : "V")), BindingFlags.Static | BindingFlags.Public).GetValue(this);
                }


                //if (cell.type == ResourceType.None)
                //    color = cell.fertile == 0 ? TerrainGen.inst.barrenColor : (cell.fertile == 1 ? TerrainGen.inst.tileColor : TerrainGen.inst.fertileColor);


                //if (cell.type == ResourceType.UnusableStone)
                //    color = Color.black;

                //if (cell.type == ResourceType.Stone)
                //    color = Color.grey;

                //if (cell.type == ResourceType.IronDeposit)
                //    color = new Color(0.6f, 0.4039216f, 0.3647059f);

                //if (cell.type == ResourceType.Wood || cell.amount > 0)
                //    color = new Color(0.00f, 0.30f, 0.00f);

                texture.SetPixel(x, z, color);

            }

            texture.Apply();
            return texture;
        }

    }
}
