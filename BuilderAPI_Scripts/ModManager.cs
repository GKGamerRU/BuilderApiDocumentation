using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using LB_Launcher.Modules;
using CharpShell;
using System.Media;
using System.Drawing.Imaging;
using Newtonsoft.Json;

namespace LB_Launcher.Modules
{
    public static class ModManager
    {
        public static List<Mod> Mods = new List<Mod>();
        public static Mod GetMod(string name)
        {
            return Mods.Where(mod => mod.Name == name).First();
        }

        public static Image[] _TextureLibrary =
      { LB_Launcher.Properties.Resources._0,
        LB_Launcher.Properties.Resources._1,
        LB_Launcher.Properties.Resources._2,
        LB_Launcher.Properties.Resources._3,
        LB_Launcher.Properties.Resources._4_0,
        LB_Launcher.Properties.Resources._5,
        LB_Launcher.Properties.Resources._6,
        LB_Launcher.Properties.Resources.brick_blue,
        LB_Launcher.Properties.Resources.brick_floor,
        LB_Launcher.Properties.Resources.brick_floor_blue,
        LB_Launcher.Properties.Resources.brick_floor_brown,
        LB_Launcher.Properties.Resources.brick_floor_russet,
        LB_Launcher.Properties.Resources.sand_2,
        LB_Launcher.Properties.Resources.Screenshot_145,
        LB_Launcher.Properties.Resources.Screenshot_148,
        LB_Launcher.Properties.Resources.zemla };

        public static string CurrentMod, CurrentBlockName;

        static string[,] methodListMod = {
            { @"\script.cs", "LB_Launcher.Builder2D.OnModsLoaded += delegate" },
            { @"\OnMapRedraw.cs", "LB_Launcher.Builder2D.Engine.OnMapRedraw += delegate" },
            { @"\OnGlobalTimerTick.cs", "LB_Launcher.Builder2D.OnGlobalTimerUpdated += delegate" },
            { @"\OnAbout.cs","LB_Launcher.Modules.ModManager.Modded.OnAbout = delegate(ModManager.Mod obj)" }
        };
        static string[,] methodBlocksMod = {
            { @"\script.cs", "LB_Launcher.Modules.ModManager.Modded.Blocks[(num)].OnBlockPlaced = delegate(Block obj)" },
            { @"\OnSelect.cs", "LB_Launcher.Modules.ModManager.Modded.Blocks[(num)].OnBlockSelect = delegate(ModG obj)" },
            { @"\OnRedraw.cs", "LB_Launcher.Modules.ModManager.Modded.Blocks[(num)].OnRedraw = delegate(Block obj)" },
            { @"\OnDestroy.cs", "LB_Launcher.Modules.ModManager.Modded.Blocks[(num)].OnDestroyed = delegate(Block obj)" },
            { @"\OnClick.cs", "LB_Launcher.Modules.ModManager.Modded.Blocks[(num)].OnClick = delegate(Block obj, int MouseClick)" }
        };

        public static Mod Modded;
        public static ModG TempMod;
        public static async Task Init()
        {
            if (!Directory.Exists(Application.StartupPath + @"\2Dbuilder"))
            { Directory.CreateDirectory(Application.StartupPath + @"\2Dbuilder"); }
            if (!Directory.Exists(Application.StartupPath + @"\2Dbuilder\mods"))
            { Directory.CreateDirectory(Application.StartupPath + @"\2Dbuilder\mods"); }

            DirectoryInfo[] cDirs = new DirectoryInfo(Application.StartupPath + @"\2Dbuilder\mods").GetDirectories();
            LBConsole.AddString("[Builder API] Загрузка информации о модах...");
            Mods.Clear();
            LBConsole.AddString("[Builder API] Список модов очищен!");
            
            foreach (DirectoryInfo Mod in cDirs)
            {
                await Task.Run(() => {
                    Mod ModedFile = new Mod(Mod.Name);
                    Modded = ModedFile;
                    ModedFile.Path = Mod.FullName;
                    ModedFile.ResourcePath = Mod.FullName + @"\Resources";
                    ModedFile.LoadProperties();

                    CharpExecuter cs = new CharpExecuter(new ExecuteLogHandler(delegate { }));
                    string globalScript = "";
                    for (int i = 0; i < methodListMod.GetUpperBound(0) + 1; i++)
                    {
                        if (File.Exists(Mod.FullName + methodListMod[i, 0]))
                        {
                            string code = File.ReadAllText(Mod.FullName + methodListMod[i, 0]);
                            globalScript += methodListMod[i, 1] + " { " + code + " };\n";
                        }
                    }
                    cs.FormatSources(globalScript);
                    cs.Execute();

                    Mods.Add(ModedFile);

                    DirectoryInfo[] Blocks = new DirectoryInfo(Application.StartupPath + @"\2Dbuilder\mods\" + Mod.Name).GetDirectories();

                    CharpExecuter cs2 = new CharpExecuter(new ExecuteLogHandler(delegate { }));
                    StringBuilder blockScript = new StringBuilder("");
                    int blockCounter = 0;
                    foreach (DirectoryInfo block in Blocks)
                    {
                        if (block.Name == "Resources") continue;

                        Bitmap _texture = (Bitmap)Image.FromFile(block.FullName + @"\texture.png");
                        ModG newMod = new ModG();
                        TempMod = newMod;
                        newMod.ModName = Mod.Name;

                        Bitmap temp = new Bitmap(_texture.Width, _texture.Height, PixelFormat.Format32bppPArgb);
                        var g = Graphics.FromImage(temp);
                        g.DrawImageUnscaled(_texture,0,0);

                        newMod._Texture = temp;

                        newMod.BlockName = block.Name;
                        newMod.Mod = ModedFile;
                        
                        for (int i = 0; i < methodBlocksMod.GetUpperBound(0) + 1; i++)
                        {
                            if (File.Exists(block.FullName + methodBlocksMod[i, 0]))
                            {
                                string code = File.ReadAllText(block.FullName + methodBlocksMod[i, 0]);
                                int modNum = blockCounter;
                                blockScript.Append(methodBlocksMod[i, 1].Replace("(num)", modNum.ToString()) + " { " + code + " };");
                            }
                        }
                        ModedFile.Blocks.Add(newMod);
                        blockCounter++;
                    }
                    cs2.FormatSources(blockScript.ToString());
                    cs2.Execute();
                });
                LBConsole.AddString($"[Builder API] {Mod.Name} Инициализирован.");
            }
        }

        public class Mod
        {
            public Action<Mod> OnAbout = null;

            public string Name;
            public string Path;
            public string ResourcePath;

            public List<ModG> Blocks = new List<ModG>();
            public ModG GetBlock(string name)
            {
                return Blocks.Where(mod => mod.BlockName == name).First();
            }
            public Mod(string _modName, List<ModG> blocks)
            {
                Name = _modName;
                Blocks = blocks;
            }
            public Mod(string _modName)
            {
                Name = _modName;
            }

            Dictionary<string,object> Properties = new Dictionary<string, object>();
            public void SaveProperties()
            {
                Dictionary<string, object> temp = new Dictionary<string, object>();
                foreach (var collection in Properties)
                {
                    if (collection.Key[0] == '@') temp.Add(collection.Key, collection.Value);
                }
                if (temp.Keys.Count <= 0) return;

                var serializationSettings = new JsonSerializerSettings();
                serializationSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                serializationSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
                serializationSettings.TypeNameHandling = TypeNameHandling.Auto;
                
                string map = JsonConvert.SerializeObject(temp, serializationSettings);
                string path = System.IO.Path.Combine(Path, "GlobalProperties.json");
                if (File.Exists(path))
                { File.Delete(path); }
                
                try
                {
                    File.WriteAllText(path, map);
                }
                catch { }
            }
            public void LoadProperties()
            {
                string path = System.IO.Path.Combine(Path, "GlobalProperties.json");
                if (File.Exists(path))
                {
                    var serializationSettings = new JsonSerializerSettings();
                    serializationSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    serializationSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
                    serializationSettings.TypeNameHandling = TypeNameHandling.Auto;
                    serializationSettings.MissingMemberHandling = MissingMemberHandling.Ignore;

                    JsonConvert.PopulateObject(File.ReadAllText(path), Properties, serializationSettings);
                }
            }

            public object GetProperty(string name)
            {
                if (Properties.ContainsKey(name))
                {
                    return Properties[name];
                }
                return null;
            }
            public void SetProperty(string name, object value)
            {
                if (Properties.ContainsKey(name))
                {
                    Properties[name] = value;
                }
                else
                {
                    Properties.Add(name, value);
                }
            }

            public string GetTextFromResources(string FileName)
            {
                if (File.Exists(ResourcePath + @"\" + FileName))
                {
                    return File.ReadAllText(ResourcePath + @"\" + FileName);
                }
                return null;
            }
            public Image GetImageFromResources(string FileName)
            {
                if (File.Exists(ResourcePath + @"\" + FileName))
                {
                    return Image.FromFile(ResourcePath + @"\" + FileName);
                }
                return null;
            }
            public byte[] GetBytesFromResources(string FileName)
            {
                if (File.Exists(ResourcePath + @"\" + FileName))
                {
                    return File.ReadAllBytes(ResourcePath + @"\" + FileName);
                }
                return null;
            }
            public SoundPlayer GetSoundFromResources(string FileName)
            {
                if (File.Exists(ResourcePath + @"\" + FileName))
                {
                    return new SoundPlayer(ResourcePath + @"\" + FileName);
                }
                return null;
            }
            public void SetTextToResources(string FileName, string content)
            {
                File.WriteAllText(ResourcePath + @"\" + FileName, content);
            }
            public void SetImageToResources(string FileName, Image content)
            {
                content.Save(ResourcePath + @"\" + FileName);
            }
            public void SetBytesToResources(string FileName, byte[] content)
            {
                File.WriteAllBytes(ResourcePath + @"\" + FileName, content);
            }
        }
    }
}
