//#define ALPHA

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Harmony;
using System.Reflection;
using Fox.Maps.AssetManagement;

namespace Fox.Maps
{
    public class Mod : MonoBehaviour
    {
        public static string EditingMapName = "";
        public static KCModHelper helper { get; private set; }

        private void Preload(KCModHelper helper)
        {
            var harmony = HarmonyInstance.Create("fox.mods.maps");
            harmony.PatchAll();
            Mod.helper = helper;

            Application.logMessageReceived += (condition, stacktrace, type) =>
            {
                if (type == LogType.Exception)
                    Log(condition + "\n" + stacktrace);
            };

            helper.Log("Preload");

        }

        private void SceneLoaded()
        {
            UI.Init();
            MapSaveLoad.Init();
        }

        void Update()
        {
        }

        public static void Log(object message) => Mod.helper.Log(message.ToString());

    }


    public static class MapSaveLoad
    {
        // Create special save game without any actual game data at special location (not where game reads), but with map data instead. 

        [Serializable]
        public class MapSaveData
        {
            public string name;
            public World.WorldSaveData terrainData;
            public FishSystem.FishSystemSaveData fishData;
            public SerializableDictionary<string, string> customData = new SerializableDictionary<string, string>();
        }

        public static bool unpackExtraData = false;

        public static bool packing { get; private set; } = false;
        public static bool unpacking { get; private set; } = false;

        public static string saveLocation => Application.persistentDataPath + "/hidden/maps";

        public static List<MapSaveData> registry { get; private set; } = new List<MapSaveData>();
        public static MapSaveData editing { get; private set; }
        public static string editingData => JsonConvert.SerializeObject(editing);

        public static void Init()
        {
            LoadRegistry();
        }

        /// <summary>
        /// Saves the entire registry to the computer
        /// </summary>
        public static void SaveRegistry()
        {
            packing = true;
            LoadSave.Save(saveLocation);
            packing = false;
        }

        /// <summary>
        /// Appends the map being edited to the local map registry; if item with same name as editing item exists, will overwwrite. 
        /// </summary>
        public static MapSaveData Append()
        {
            Compile();

            // Overwrite if same name
            for (int i = 0; i < registry.Count; i++)
            {
                if (registry[i].name == editing.name)
                {
                    registry[i].terrainData = editing.terrainData;
                    registry[i].fishData = editing.fishData;
                    registry[i].customData = editing.customData;
                    SaveRegistry();
                    return registry[i];
                }
            }

            registry.Add(editing);
            SaveRegistry();
            return editing;
        }

        /// <summary>
        /// Loads an entire registry from the computer
        /// </summary>
        public static void LoadRegistry()
        {
            unpacking = true;
            LoadSave.LoadAtPath(saveLocation, "world");
            unpacking = false;
        }

        public static void LoadMap(MapSaveData data)
        {
            UI.MapSaveUI.mapName.text = data.name;

            typeof(World).GetMethod("Setup", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(World.inst, new object[] { data.terrainData.gridWidth, data.terrainData.gridHeight });

            data.terrainData.Unpack(World.inst);
            data.fishData.Unpack(FishSystem.inst);


            LoadSave.CustomSaveData_DontAccessDirectly = data.customData;
            Broadcast.OnLoadedEvent.Broadcast(new OnLoadedEvent());

            GameState.inst.mainMenuMode.TransitionTo(MainMenuMode.State.NewMap);

            editing = data;
        }

        /// <summary>
        /// Compiles the editing map into a MapSaveData
        /// </summary>
        /// <returns></returns>
        public static void Compile()
        {
            MapSaveData data = new MapSaveData();

            data.name = Mod.EditingMapName;
            data.terrainData = new World.WorldSaveData().Pack(World.inst);
            data.fishData = new FishSystem.FishSystemSaveData().Pack(FishSystem.inst);

            Broadcast.OnSaveEvent.Broadcast(new OnSaveEvent());

            SerializableDictionary<string, string> customData = new SerializableDictionary<string, string>();
            foreach (KeyValuePair<string, string> entry in LoadSave.CustomSaveData_DontAccessDirectly)
                if (entry.Key != "localmaps")
                    customData.Add(entry);
            
            data.customData = customData;

            editing = data;
        }


        public static bool Contains(string mapName)
        {
            foreach (MapSaveData map in registry)
                if (map.name == mapName)
                    return true;
            return false;
        }

        public static bool Contains(MapSaveData map) => Contains(map.name);

        public static LoadSaveContainer CompileRegistryJSON()
        {
            LoadSaveContainer container = new LoadSaveContainer()
            {
                CustomSaveData = LoadSave.CustomSaveData_DontAccessDirectly
            };

            container.CustomSaveData["localmaps"] = JsonConvert.SerializeObject(registry);

            return container;
        }

        public static MapSaveData CreateFromJson(JToken token)
        {
            MapSaveData data = new MapSaveData();
            data.name = (string)token["name"];

            #region Terrain Data

            World.WorldSaveData terrainData = new World.WorldSaveData();

            Mod.Log(1);
            terrainData.seed = (int)token["terrainData"]["seed"];
            Mod.Log(2);
            terrainData.gridWidth = (int)token["terrainData"]["gridWidth"];
            terrainData.gridHeight = (int)token["terrainData"]["gridHeight"];
            terrainData.cellSaveData = new Cell.CellSaveData[terrainData.gridWidth * terrainData.gridHeight];
            Mod.Log(3);

            JArray cells = (JArray)token["terrainData"]["cellSaveData"];
            Cell dummy = new Cell(0, 0);


            for (int i = 0; i < terrainData.cellSaveData.Length; i++)
            {
                JToken cell = cells.ElementAt(i);

//#if ALPHA
                Cell.CellSaveData cellData = new Cell.CellSaveData();
//#else
//                Cell.CellSaveData cellData = new Cell.CellSaveData(dummy);
//#endif

                cellData.type = (ResourceType)(int)cell["type"];
                cellData.amount = (int)cell["amount"];
                cellData.fertile = (int)cell["fertile"];
                cellData.saltWater = (bool)cell["saltWater"];
                cellData.deepWater = (bool)cell["deepWater"];

                terrainData.cellSaveData[i] = cellData;
            }

            Mod.Log(4);

            #region Witches

            terrainData.witchHuts = new List<WitchHut.WitchHutSaveData>();

            foreach (JToken hutData in token["terrainData"]["witchHuts"])
            {
                WitchHut.WitchHutSaveData witchHut = new WitchHut.WitchHutSaveData();

                witchHut.x = (int)hutData["x"];
                witchHut.z = (int)hutData["z"];

                if (unpackExtraData)
                {

                    witchHut.status = (WitchHut.Status)(int)hutData["status"];
                    witchHut.relationship = (WitchHut.Relationship)(int)hutData["relationship"];
                    witchHut.yearsToComplete = (int)hutData["yearsToComplete"];
                    witchHut.missionIdx = (int)hutData["missionIdx"];
                    witchHut.latestSpell = (WitchHut.Spells)(int)hutData["latestSpell"];
                    witchHut.maxResourcesEver = hutData["yearsToComplete"].ToObject<Assets.Code.ResourceAmount>();
                    witchHut.curseYear = (int)hutData["curseYear"];
                    witchHut.lastMissionSuccess = (bool)hutData["lastMissionSuccess"];
                    witchHut.currSpellCooldown = ((JArray)hutData["currSpellCooldown"]).Values<int>().ToArray();

                }

                terrainData.witchHuts.Add(witchHut);
            }

            Mod.Log(5);

#endregion

            terrainData.placedCavesWitches = (bool)token["terrainData"]["placedCavesWitches"];
            terrainData.placedFish = (bool)token["terrainData"]["placedFish"];
            terrainData.hasStoneUI = unpackExtraData ? (bool)token["hasStoneUI"] : false;

            Mod.Log(6);

            data.terrainData = terrainData;

#endregion

#region Fish Data

            FishSystem.FishSystemSaveData fishData = new FishSystem.FishSystemSaveData();

            fishData.fishPerCell = ((JArray)token["fishData"]["fishPerCell"]).Values<int>().ToArray();
            if (unpackExtraData)
                fishData.probabilities = !token["fishData"]["probabilities"].IsNull() ? ((JArray)token["fishData"]["probabilities"]).Values<float>().ToArray() : null;

            Mod.Log(7);

            data.fishData = fishData;

            #endregion

            #region Custom Data

            //Mod.Log(token.ToString());

            SerializableDictionary<string, string> customDataSerializable = new SerializableDictionary<string, string>();

            if (((JObject)token).TryGetValue("customData", StringComparison.Ordinal, out JToken dictionary))
                customDataSerializable = JsonConvert.DeserializeObject<SerializableDictionary<string, string>>(dictionary.ToString());

            //customDataSerializable = JsonConvert.DeserializeObject<SerializableDictionary<string, string>>((string)token["customData"]);
            //Dictionary<string, string> customData = new Dictionary<string, string>();
            //foreach (KeyValuePair<string, string> entry in customDataSerializable)
            //    customData.Add(entry.Key, entry.Value);

            //data.customData = customDataSerializable;

            Mod.Log($"Custom data entries: {customDataSerializable.Count}");

            #endregion 

            return data;
        }

        [HarmonyPatch(typeof(LoadSaveContainer), "Pack")]
        class PackOverride
        {
            static bool Prefix(ref LoadSaveContainer __result)
            {
                if (packing)
                    __result = CompileRegistryJSON();

                return !packing;
            }
        }

        [HarmonyPatch(typeof(LoadSaveContainer), "Unpack")]
        public class UnpackOverride
        {
            static bool Prefix(LoadSaveContainer __instance)
            {
                if (unpacking)
                {
                    if(__instance.CustomSaveData != null && __instance.CustomSaveData.ContainsKey("localmaps"))
                    {
                        JArray read = (JArray)JsonConvert.DeserializeObject(__instance.CustomSaveData["localmaps"]);

                        registry.Clear();
                        foreach (JToken token in read.ToList())
                            registry.Add(CreateFromJson(token));

                        Mod.Log($"Registry loaded; {registry.Count} maps");
                    }
                }


                return !unpacking;
            }

            
        }
    }
}
