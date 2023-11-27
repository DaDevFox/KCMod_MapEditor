//#define ALPHA

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Harmony;
using System.Reflection;
using Assets.Code;

namespace Fox.Maps
{
    public class MapSelectionTools : MonoBehaviour
    {
        // When none is selected, drag select
        // Selection can be cut, copied, pasted
        // Buttons appear for obviousness??

        // Mirror tool as well?

        // When brush is applied, clear selection

        public static Cell current { get; private set; }

        public static Cell mouseDown = null;
        public static CellMeta[] selection { get; set; } = new CellMeta[0];

        public static Action held { get; private set; } = null;

//#if ALPHA
        public static bool EditModeActive = false;
//#else
//        public static bool EditModeActive = true;
//#endif

        #region Utils

        public static void FillOverlay(Cell a, Cell b, Color color, bool clear = true)
        {
            if (clear)
                TerrainGen.inst.ClearOverlay(false);

            int minX = Math.Min(a.x, b.x);
            int minZ = Math.Min(a.z, b.z);
            int maxX = Math.Max(a.x, b.x);
            int maxZ = Math.Max(a.z, b.z);

            for (int x = minX; x <= maxX; x++)
                for (int z = minZ; z <= maxZ; z++)
                    TerrainGen.inst.SetOverlayPixelColor(x, z, color);

            TerrainGen.inst.UpdateOverlayTextures();

            TerrainGen.inst.FadeOverlay(1f);
        }

        public static Cell GetCellClamped(Vector3 position)
        {
            position.x = Mathf.Clamp(position.x, 1f, World.inst.GridWidth - 2f);
            position.z = Mathf.Clamp(position.z, 1f, World.inst.GridHeight - 2f);

            return World.inst.GetCellDataClamped(position);
        }

#endregion

//#if ALPHA

        [HarmonyPatch(typeof(NewMapUI), "OnEdit")]
        class OnEditPatch
        {
            static void Postfix()
            {
                EditModeActive = true;
            }
        }

        [HarmonyPatch(typeof(NewMapUI), "OnDone")]
        class OnEditDonePatch
        {
            static void Postfix()
            {
                EditModeActive = false;
            }
        }

//#endif

        [HarmonyPatch(typeof(MapEdit), "Update")]
        public class SelectionPatch
        {
            public static Color selectedColor = Color.grey;
            
            private static void UpdateCursor()
            {
                Ray ray = PointingSystem.GetPointer().GetRay();
                Plane plane = new Plane(new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, 0f));
                float distance;
                plane.Raycast(ray, out distance);
                Vector3 point = ray.GetPoint(distance);
                MapSelectionTools.current = GetCellClamped(point);
            }

            static void Postfix(MapEdit __instance)
            {
                UpdateCursor();

                

                if (__instance.brushMode == MapEdit.BrushMode.None)
                {
                    Cam.inst.disableDrag = true;

                    if (PointingSystem.GetPointer().GetPrimaryDown())
                    {
                        Cell current = MapSelectionTools.current;

                        if (mouseDown == null)
                            mouseDown = current;
                    }

                    if (mouseDown != null)
                    {
                        FillCursor(current);
                            
                        if (PointingSystem.GetPointer().GetPrimaryUp() || !EditModeActive)
                        {
                            Select();
                            mouseDown = null;

                            if(!EditModeActive)
                            {
                                if (selection.Length > 0)
                                    selection = new CellMeta[0];
                            }
                        }
                    }
                    else if (selection.Length > 0)
                    {
                        FillSelected();


                        if (PointingSystem.GetPointer().GetSecondaryUp() || !EditModeActive)
                            selection = new CellMeta[0];
                    }
                }
                else
                {
                    mouseDown = null;
                    if (selection.Length > 0)
                        selection = new CellMeta[0];
                }


                UpdateActions();
            }

            

            private static void UpdateActions()
            {
                if (held == null)
                {
                    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.C))
                    {
                        // Copy (paste builtin)
                        held = new CopyAction();
                        return;
                    }
                    if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.X))
                    {
                        // Cut (paste builtin)
                        held = new CopyAction(true);
                        return;
                    }
                    if(Input.GetKey(KeyCode.Delete))
                    {
                        // Delete
                        held = new DeleteAction();
                        return;
                    }
                }
                else
                {
                    if (!held.inited)
                        held.OnInit();

                    if (held.Cancel() || !EditModeActive)
                        held = null;

                    if (held.Tick())
                    {
                        held.Complete();
                        held = null;
                    }
                }
            }

            private static void FillCursor(Cell current)
            {
                Color cursorColor = (Color)typeof(MapEdit).GetField("cursorColor", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(GameState.inst.mainMenuMode.mapEditUI.GetComponent<MapEdit>());

                FillOverlay(mouseDown, current, cursorColor);
            }
            
            private static void FillSelected()
            {
                FillOverlay(selection[0].cell, selection[selection.Length - 1].cell, selectedColor);
            }


            

            public static void Select()
            {
                if (mouseDown == null)
                    return;

                Cell current = MapSelectionTools.current;

                int minX = Math.Min(mouseDown.x, current.x);
                int minZ = Math.Min(mouseDown.z, current.z);
                int maxX = Math.Max(mouseDown.x, current.x);
                int maxZ = Math.Max(mouseDown.z, current.z);

                List<CellMeta> selected = new List<CellMeta>();

                for (int x = minX; x <= maxX; x++) 
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        selected.Add(CellMeta.Create(World.inst.GetCellData(x, z)));
                    }
                }

                MapSelectionTools.selection = selected.ToArray();
            }
        }
    }

    public class CellMeta
    {
        public Cell cell => World.inst.GetCellData(x, z);

        public int x;
        public int z;

        public ResourceType type;
        public int trees;
        public bool fish;

        public static CellMeta Create(Cell cell)
        {
            return new CellMeta()
            {
                x = cell.x,
                z = cell.z,
                type = cell.Type,
                trees = cell.TreeAmount,
                fish = FishSystem.inst.fishCells.Any((fishCell) => fishCell.x == cell.x && fishCell.z == cell.z && fishCell.fish.Count > 0)
            };
        }

        public static void CopyTo(CellMeta meta, Cell to)
        {
            meta.CopyTo(to);
        }

        public void CopyTo(Cell to)
        {
            MapEdit.BrushMode brush = MapEdit.BrushMode.None;

            if (type == ResourceType.Stone)
                brush = MapEdit.BrushMode.Stone;
            if (type == ResourceType.UnusableStone)
                brush = MapEdit.BrushMode.UnusableStone;
            if (type == ResourceType.IronDeposit)
                brush = MapEdit.BrushMode.Iron;
            
            if (type == ResourceType.WolfDen || type == ResourceType.EmptyCave)
                brush = MapEdit.BrushMode.EmptyCave;
            if (type == ResourceType.WitchHut)
                brush = MapEdit.BrushMode.Witch;
            if (type == ResourceType.Water) 
            {
                if (cell.deepWater)
                    brush = MapEdit.BrushMode.DeepWater;
                else
                    brush = MapEdit.BrushMode.ShallowWater;
            }
            if(type == ResourceType.None)
            {
                if (cell.fertile == 0)
                    brush = MapEdit.BrushMode.BarrenLand;
                else if (cell.fertile == 1)
                    brush = MapEdit.BrushMode.FertileLand;
                else
                    brush = MapEdit.BrushMode.VeryFertileLand;
            }
            if (cell.TreeAmount > 0)
            {
                to.TreeAmount = 0;
                brush = MapEdit.BrushMode.Forest;
            }
            if (FishSystem.inst.fishCells.Any((fishCell) => fishCell.x == x && fishCell.z == z && fishCell.fish.Count > 0))
                brush = MapEdit.BrushMode.Fish;


            ApplyBrush(brush, to);

            TerrainGen.inst.SetFertileTile(to.x, to.z, cell.fertile);
        }

        public static void ApplyBrush(MapEdit.BrushMode brush, Cell c)
        {
            Color shallowColor = Water.inst.waterMat.GetColor("_Color");
            Color deepColor = Water.inst.waterMat.GetColor("_DeepColor");
            Color shallowSaltColor = Water.inst.waterMat.GetColor("_SaltColor");
            Color deepSaltColor = Water.inst.waterMat.GetColor("_SaltDeepColor");

            MapEdit mapedit = GameState.inst.mainMenuMode.mapEditUI.GetComponent<MapEdit>();

            typeof(MapEdit).GetMethod("ApplyBrush", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(mapedit, new object[] {brush, c, shallowColor, deepColor, shallowSaltColor, deepSaltColor });
        }
    }

    /// <summary>
    /// Actions are independant tools such as Cut, Copy, Paste, and Delete that can run at any time
    /// </summary>
    public abstract class Action
    {
        public bool inited { get; private set; } = false;

        /// <summary>
        /// Called when the action inits
        /// </summary>
        public virtual void OnInit()
        {
            inited = true;
        }

        /// <summary>
        ///  Return true to complete
        /// </summary>
        /// <returns></returns>
        public abstract bool Tick();

        /// <summary>
        /// Return true to cancel (doesn't call Complete)
        /// </summary>
        /// <returns></returns>
        public virtual bool Cancel()
        {
            return false;
        }

        /// <summary>
        /// Called when the action is completed
        /// </summary>
        public virtual void Complete()
        {

        }

    }

    /// <summary>
    /// Modifiers are active when a brush is active and affects the brush, how it places, cancels it, etc. 
    /// </summary>
    public abstract class Modifier
    {
        public virtual void OnPlace(CellMeta[] placed)
        {

        }
    }

    public static class InputControls
    {
        /// <summary>
        /// Universal mapping for 'cancel'
        /// </summary>
        /// <returns></returns>
        public static bool Cancel() => Input.GetKeyDown(KeyCode.Escape);

        /// <summary>
        /// Universal mapping for 'rotate'
        /// </summary>
        /// <returns></returns>
        public static bool Rotate() => Input.GetKeyDown(KeyCode.R);

        public static bool PrimaryDown() => PointingSystem.GetPointer().GetPrimaryDownThisFrame() && !GameUI.inst.PointerOverUI();
        public static bool PrimaryUp() => PointingSystem.GetPointer().GetPrimaryUp();
        public static bool Primary() => PointingSystem.GetPointer().GetPrimaryDown();

        public static bool SecondaryUp() => PointingSystem.GetPointer().GetSecondaryUp();
        public static bool SecondaryDown() => PointingSystem.GetPointer().GetSecondaryDown() && !GameUI.inst.PointerOverUI();

    }

    /// <summary>
    /// Copies or Cuts the current selection and pastes it at the cursor location when CTRL+V or LMB is used
    /// </summary>
    public class CopyAction : Action
    {
        public delegate Tuple<int, int> Transformation(int x, int z);

        public static Color copyColorValid { get; } = Color.green;
        public static Color copyColorInvalid { get; } = Color.red;
        public static Color cutColor { get; } = new Color(0.5f, 0f, 8f);

        private CellMeta[] copied;
        private Vector3 offset;

        public int xCenter_untransformed { get; private set; }
        public int zCenter_untransformed { get; private set; }

        public int xCenter { get; private set; }
        public int zCenter { get; private set; }

        private float rotationDelta = 0f;

        public Dictionary<string, Transformation> transformations = new Dictionary<string, Transformation>();
        public bool cut = false;

        public CopyAction(bool cut = false)
        {
            this.cut = cut;
        }


        public bool Input_Complete() => Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.V) || InputControls.PrimaryDown();

        public bool Input_Rotate() => InputControls.Rotate();
        
        public bool Input_Cancel() => PointingSystem.GetPointer().GetSecondaryUp() || InputControls.Cancel();


        public override void OnInit()
        {
            base.OnInit();

            copied = new CellMeta[MapSelectionTools.selection.Length];

            MapSelectionTools.selection.CopyTo(copied, 0);
            offset = copied[copied.Length - 1].cell.Center - copied[0].cell.Center;
        }

        public override bool Tick()
        {
            // Update Dimension vars
            UpdateDimensions();

            // Update Input
            UpdateInput();

            // Update the overlay
            UpdateOverlay();

            // Chain multiple pastes together by holding shift
            if (Input_Complete() && Input.GetKey(KeyCode.LeftShift))
            {
                Complete();
                return false;
            }

            return Input_Complete();
        }

        private void UpdateDimensions()
        {
            Tuple<Cell, Cell> domain_untransformed = GetUntransformedDomain();
            int x1_untransformed = domain_untransformed.Item1.x;
            int z1_untransformed = domain_untransformed.Item1.z;
            int x2_untransformed = domain_untransformed.Item2.x;
            int z2_untransformed = domain_untransformed.Item2.z;

            xCenter_untransformed = (Math.Min(x1_untransformed, x2_untransformed) + Math.Max(x1_untransformed, x2_untransformed)) / 2;
            zCenter_untransformed = (Math.Min(z1_untransformed, z2_untransformed) + Math.Max(z1_untransformed, z2_untransformed)) / 2;

            Tuple<Cell, Cell> domain = GetDomain();
            int x1 = domain.Item1.x;
            int z1 = domain.Item1.z;
            int x2 = domain.Item2.x;
            int z2 = domain.Item2.z;

            xCenter = (Math.Min(x1, x2) + Math.Max(x1, x2)) / 2;
            zCenter = (Math.Min(z1, z2) + Math.Max(z1, z2)) / 2;
        }

        private void UpdateInput()
        {
            // Rotate if input read
            if (Input_Rotate())
            {
                rotationDelta += (Mathf.PI / 4f);
                rotationDelta %= Mathf.PI * 2f;

                if (!transformations.ContainsKey("rotate"))
                    transformations.Add("rotate", (x, z) => 
                    {
                        return Transformations.Rotate(x, z, MapSelectionTools.current.x, MapSelectionTools.current.z, rotationDelta);
                    });
            }
        }

        private int GetXCenter() => xCenter_untransformed;
        private int GetZCenter() => zCenter_untransformed;

        private void UpdateOverlay()
        {
            Tuple<Cell, Cell> domain = GetDomain();
            Cell a = domain.Item1;
            Cell b = domain.Item2;

            Color copyColor = copyColorValid;
            if (b.Center - a.Center != offset)
                copyColor = copyColorInvalid;

            MapSelectionTools.FillOverlay(a, b, copyColor);

            if(cut)
                MapSelectionTools.FillOverlay(copied[0].cell, copied[copied.Length - 1].cell, cutColor, false);
        }

        private Tuple<Cell, Cell> GetUntransformedDomain()
        {
            Cell a = MapSelectionTools.current;
            Cell b = GetCellClamped(a.Position + offset);
            return new Tuple<Cell, Cell>(a, b);
        }

        private Tuple<Cell, Cell> GetDomain()
        {
            Cell a = MapSelectionTools.current;
            Cell b = GetCellClamped(a.Position + offset);

            int ax = a.x;
            int az = a.z;
            int bx = b.x;
            int bz = b.z;

            
            Tuple<int, int> aCoords = Map(ax, az);
            ax = aCoords.Item1;
            az = aCoords.Item2;

            Tuple<int, int> bCoords = Map(bx, bz);
            bx = bCoords.Item1;
            bz = bCoords.Item2;
            

            a = GetCellClamped(new Vector3(ax, 0f, az));
            b = GetCellClamped(new Vector3(bx, 0f, bz));
            return new Tuple<Cell, Cell>(a, b);
        }

        private Tuple<int, int> Map(int xOriginal, int zOriginal)
        {
            int x = xOriginal;
            int z = zOriginal;

            foreach (Transformation transformation in transformations.Values)
            {
                Tuple<int, int> coords = transformation(x, z);
                x = coords.Item1;
                z = coords.Item2;
            }

            return new Tuple<int, int>(x, z);
        }

        private Cell GetCellClamped(Vector3 position) => MapSelectionTools.GetCellClamped(position);

        public override bool Cancel() => Input_Cancel() || copied.Length == 0;

        public override void Complete()
        {

            Cell current = MapSelectionTools.current;
            Cell other = GetCellClamped(current.Position + offset);

            int minX = Math.Min(other.x, current.x);
            int minZ = Math.Min(other.z, current.z);
            int maxX = Math.Max(other.x, current.x);
            int maxZ = Math.Max(other.z, current.z);

            int i = 0; 
            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (copied[i].x > 0 && copied[i].z > 0 && copied[i].x < World.inst.GridWidth - 1 && copied[i].z < World.inst.GridHeight - 1)
                        copied[i].CopyTo(World.inst.GetCellData(x, z));
                    i++;
                }
            }

            if (cut)
                CutOriginal();

            TerrainGen.inst.UpdateTextures();
            Water.inst.SetupSaltwater();
            Water.inst.UpdateWaterTexture();
            StorePostGenerationTypes();
        }

        private void CutOriginal()
        {
            Cell a = copied[0].cell;
            Cell b = copied[copied.Length - 1].cell;

            int minX = Math.Min(b.x, a.x);
            int minZ = Math.Min(b.z, a.z);
            int maxX = Math.Max(b.x, a.x);
            int maxZ = Math.Max(b.z, a.z);

            for (int x = minX; x <= maxX; x++)
                for (int z = minZ; z <= maxZ; z++)
                    CellMeta.ApplyBrush(MapEdit.BrushMode.DeepWater, World.inst.GetCellData(x, z));

            TerrainGen.inst.UpdateTextures();
            Water.inst.SetupSaltwater();
            Water.inst.UpdateWaterTexture();
            StorePostGenerationTypes();
        }

        private void StorePostGenerationTypes()
        {
            Cell[] cellsData = World.inst.GetCellsData();
            for (int i = 0; i < cellsData.Length; i++)
            {
                cellsData[i].StorePostGenerationType();
            }
        }
    }

    /// <summary>
    /// Deletes the current selection (sets to deep water)
    /// </summary>
    public class DeleteAction : Action
    {
        public override void OnInit()
        {
            base.OnInit();

            foreach(CellMeta meta in MapSelectionTools.selection)
                CellMeta.ApplyBrush(MapEdit.BrushMode.DeepWater, meta.cell);
        }

        public override bool Tick()
        {
            return true;
        }
    }

    public static class Transformations
    {
        public static Tuple<int, int> Rotate(int x, int z, int originX, int originZ, float delta = Mathf.PI / 2f)
        {
            double _x = (double)originX + Math.Cos(delta) * (x - originX);
            double _z = (double)originZ + Math.Sin(delta) * (z - originZ);


            //_x -= originX;
            //_z -= originZ;

            //int xPrime = (int)(x * Mathf.Cos(delta) - z * Mathf.Sin(delta));
            //int zPrime = (int)(z * Mathf.Cos(delta) + x * Mathf.Sin(delta));

            //_x = xPrime;
            //_z = zPrime;

            //_x += originX;
            //_z += originZ;

            return new Tuple<int, int>((int)_x, (int)_z);
        }
    }
}
