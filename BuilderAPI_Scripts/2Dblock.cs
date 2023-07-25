using System;
using System.Collections.Generic;
using System.Linq;
using static LB_Launcher.Builder2D;
using System.Windows.Forms;
using System.Drawing;
using CharpShell;
using LB_Launcher.Modules;
using LB_Launcher.Builder;

namespace LB_Launcher
{
    public static class _2Dblock
    {
        public const string VERSION = "2.1";
        public static List<Block> GetBlocks() { return Engine.Cubes; }
        public static List<Block> GetBlockByIDTexture(int id)
        {
            List<Block> _blocks = new List<Block>();
            foreach (Block cube in Engine.Cubes)
            {
                if (cube._IndexTexture == id) _blocks.Add(cube);
            }
            return _blocks;
        }
        public static Block GetBlockByID(int id)
        { return Engine.Cubes[id]; }
        public static Block GetBlockByName(string name)
        { foreach (Block cube in Engine.Cubes) { if (cube.BlockMod != null && cube.BlockMod.BlockName == name) return cube; }; return null; }
        public static IEnumerable<Block> GetBlockByNameArray(string name)
        { return Engine.Cubes.Where(block => block.BlockMod != null && block.BlockMod.BlockName == name); }
        public static IEnumerable<Block> GetBlockByModNameArray(string Modname)
        { return Engine.Cubes.Where(block => block.Mod != null && block.Mod.Name == Modname); }
        public static Panel PaintTable() { return (Panel)thisForm.Controls.Find("panel2", true).FirstOrDefault(); }
        public static Panel LoadPanel() { return (Panel)thisForm.Controls.Find("panel6", true).FirstOrDefault(); }
        public static Form Form() { return thisForm; }
        public static string GetCurrentModInfo() { return thisForm.SlotBar.SelectedInv.BlockName + "\n" + thisForm.SlotBar.SelectedInv.ModName; }
        public static Graphics GetGFX() { return Engine.gfx; }
        public static Block GetThisBlock() { return Engine.Cubes.LastOrDefault(); }
        public static Image GetBackground() { return Engine.CurrentGround; }
        public static Inventar GetSelectedInventar() { return thisForm.SlotBar.SelectedInv; }
        public static Control GetCounter() { return thisForm.Controls.Find("counterblock", true).FirstOrDefault(); }
        public static Block TryBuild(int x, int y, ModG pattern)
        {
            List<Block> _list = _2Dblock.GetBlocks();
            bool canbuild = true;

            foreach (Block WTfT in _list)
            {
                if (WTfT.Position.x == x-1 && WTfT.Position.y == y+1) { canbuild = false; break; }
            }

            if (canbuild)
            {
                return Build(x, y, pattern);
            }
            return null;
        }
        public static void Destroy(Block obj)
        {
            var CurrentGround = _2Dblock.GetBackground();
            var Cubes = _2Dblock.GetBlocks();
            var gfx = _2Dblock.GetGFX();
            Point timedBlock = new Point(obj.Position.x,obj.Position.y);

            obj.Destroy();
            Cubes.Remove(obj);
            gfx.FillRectangle(SystemBrushes.ActiveCaption, timedBlock.X, timedBlock.Y, obj.Scale.x, obj.Scale.y);

            if (CurrentGround != null) gfx.DrawImage(CurrentGround, timedBlock); 

            Render();
        }
        public static void Destroy(int x, int y)
        {
            var CurrentGround = _2Dblock.GetBackground();
            var Cubes = _2Dblock.GetBlocks();
            var gfx = _2Dblock.GetGFX();
            Point timedBlock = new Point(x-1, y+1);

            foreach(var block in Cubes)
            {
                if (block == null) continue;
                if (timedBlock == new Point(block.Position.x, block.Position.y))
                {
                    block.Destroy();
                    Cubes.Remove(block);
                    gfx.FillRectangle(SystemBrushes.ActiveCaption, timedBlock.X, timedBlock.Y, block.Scale.x, block.Scale.y);
                    if (CurrentGround != null)
                    {
                        gfx.DrawImage(CurrentGround, timedBlock);
                    }
                    break;
                }
            }
        }
        public static void DestroyAll()
        {
            thisForm.Erase();
        }
        public static Block Build(int x, int y, ModG pattern)
        {
            List<Block> _list = _2Dblock.GetBlocks();
            Graphics _er = _2Dblock.GetGFX();
            Image _s = pattern._Texture;
            
            Block newel = new Block(ref _er, _s, new Point(x-1, y+1), 0, pattern.Mod, pattern, pattern.OnRedraw, pattern.OnDestroyed, pattern.OnClick);
            _list.Add(newel);
            newel.draw();
            return newel;
        }
        public static void Render() { Builder2D.Engine.ApplyRender(); }

        public static ToolButton AddTool(Bitmap icon, Action onSelect, Action onDeselect) => thisForm.BuilderToolBar.AddTool(icon, onSelect, onDeselect);
        public static void RemoveTool(ToolButton tool) => thisForm.BuilderToolBar.RemoveTool(tool);
    }
    public class Block
    {
        public Action<Block> OnRedraw = delegate(Block obj) { };
        public Action<Block> OnDestroy = delegate(Block obj) { };
        public Action<Block, int> OnClick = delegate (Block obj, int click) { };

        public Vector2 Position;
        public Vector2 Scale;
        public Image Texture;

        Graphics _GFX; 
        public int _IndexTexture;

        public ModManager.Mod Mod;
        public ModG BlockMod;

        public bool IsInteractive = false;

        public Block(ref Graphics _Parent, Image _texture, Point _pos, int index, ModManager.Mod _mod, ModG blockName, Action<Block> redraw = null, Action<Block> destroy = null, Action<Block, int> click = null)
        {
            BlockMod = blockName;
            Mod = _mod;
            Position = new Vector2(_pos.X, _pos.Y);
            Scale = new Vector2(_texture.Width, _texture.Height);
            Texture = _texture;
            _IndexTexture = index;
            _GFX = _Parent;
            OnRedraw += delegate(Block obj) { if(redraw != null) redraw(obj); };
            OnDestroy += delegate(Block obj) { if (destroy != null) destroy(obj); };
            OnClick += delegate (Block obj, int MouseClick) { if (click != null) click(obj, MouseClick); };
            IsInteractive = click != null;
        }
        public void Execute(string code)
        {
            if (code != null && code !="")
            {
                CharpExecuter cs;
                cs = new CharpExecuter(new ExecuteLogHandler(delegate { }));
                cs.FormatSources(code);
                cs.Execute();
            }
        }
        public void draw() { _GFX.DrawImage(Texture, Position.x, Position.y); OnRedraw(this); }
        public void Destroy() { OnDestroy.Invoke(this); }
        public void Interact(MouseEventArgs e) {
            int index = -1;
            if (e.Button == MouseButtons.Left) index = 0;
            if (e.Button == MouseButtons.Middle) index = 1;
            if (e.Button == MouseButtons.Right) index = 2;
            OnClick?.Invoke(this, index);
        }

        public Dictionary<string, string> Properties = new Dictionary<string, string>();

        public string GetProperty(string name)
        {
            if (Properties.ContainsKey(name))
            {
                return Properties[name].Replace("(IpThe)", ":").Replace("(IjThe)", "|");
            }
            return null;
        }
        public void SetProperty(string name, string value)
        {
            string temp = value.Replace(":", "(IpThe)").Replace("|", "(IjThe)");
            if (name == "Description") IsInteractive = true;

            if (Properties.ContainsKey(name))
            {
                Properties[name] = temp;
            }
            else
            {
                Properties.Add(name, temp);
            }
        }
    }
    public class Inventar
    {
        public ModG MyMod;
        public string ModName = "";
        public string BlockName = "";
        public Bitmap _Texture;
    }
    public class ModG
    {
        public Action<Block> OnBlockPlaced = delegate(Block obj) { };
        public Action<Block> OnDestroyed = delegate(Block obj) { };
        public Action<ModG> OnBlockSelect = delegate(ModG obj) { };
        public Action<Block> OnRedraw = delegate(Block obj) { };
        public Action<Block, int> OnClick = null;

        public ModManager.Mod Mod;
        public string ModName = "";
        public Bitmap _Texture;
        public string BlockName = "";
    }
}
