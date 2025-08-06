using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ClientPlugin.Settings;
using ClientPlugin.Settings.Layouts;
using HarmonyLib;
using System.Threading.Tasks;
using Pfim;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using VRage.Plugins;
using VRage.Utils;
using ImageFormat = Pfim.ImageFormat;


namespace ClientPlugin
{
    // ReSharper disable once UnusedType.Global
    public class Plugin: IPlugin
    {
        public const string Name = "SecondScreenDisplay";
        public static Plugin Instance { get; private set; }
        private SettingsGenerator _settingsGenerator;
        public bool WindowOpen;
        public bool InitHudLcdPatch;
        public bool ImagesConverted;
        public bool InitSpritePatch;
        public static Harmony HarmonyPatcher { get; private set; }
        public readonly Dictionary<string, string> FileRefs = new Dictionary<string, string>();
    
        public static bool IsControlled => MyAPIGateway.Session?.LocalHumanPlayer?.Controller?.ControlledEntity is IMyTerminalBlock;
        private bool _prevControlled;

        private int _counter;
    
        /*
        what do I need to do?
        MVP is displaying stuff on an LCD in another window, preserving the location used for hudlcd.
        so, get currently piloting grid, look for LCDs on it, if they exist and are configured for hudlcd.
        edit the custom data to something like PLCD, grab the location and scale data.
        then send the currently displayed stuff to the second screen for displaying.
        HUDLCD uses -1 to 1 to determine where to place the item and uses top left corner as origin
        at first it will stop hudlcd from displaying the text and place it on the window
        later add options to have it on both or only SE.
        
        
        For Sprites:
        
        the method I need to hook into is: Sandbox.Game.Entities.Blocks.MyTextPanelComponent.UpdateSpritesTexture()
        
        this is where the sprites are sent to the renderer to be rendered.
        I am thinking I add a prepatch where I perform the same checks and if they succeed I will send the sprites off
        to another thread where they can be processed without touching perf.
        
        sprites are stored here: D:\games and stuff\steamapps\common\SpaceEngineers\Content\Textures\Sprites

        */
    
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void Init(object gameInstance)
        {
            Instance = this;
            Instance._settingsGenerator = new SettingsGenerator();

            // TODO: Put your one time initialization code here.
            HudLcdPatch.Instance = new HudLcdPatch();
            HarmonyPatcher = new Harmony(Name);
            MyLog.Default.Info("Second Screen display Init Complete");
            //here i need to convert all the .dds sprites to .bmp
            LoadSprites();
        }

        private async Task LoadSprites()
        {
            await Task.Run(() =>
            {
                    MyLog.Default.Info("Converting sprites");
                    var convFileStorage = AppContext.BaseDirectory + "Plugins/Local/SecondScreenDisplay/resources/sprites";
                    //for pluginhub version, change to "Plugins/Github/Brillcrafter/Second-Screen-Display/resources"
                    //for local testing, change to "Plugins/Local/SecondScreenDisplay/resources"
                    convFileStorage = convFileStorage.Replace(@"\", "/");
                    if (Directory.Exists(convFileStorage))
                    {
                       //then the files are already there
                       ImagesConverted = true;
                       return;
                    }
                    Directory.CreateDirectory(convFileStorage);
                    //getting where the sprites are stored
                    var mainFolder = AppContext.BaseDirectory;
                    mainFolder = mainFolder.Replace(@"\", "/");
                    mainFolder = mainFolder.Remove(mainFolder.Length - 6, 6);
                    var folderList = new List<string>
                    {
                        mainFolder + "Content/Textures/Sprites",
                        mainFolder + "Content/Textures/Sprites/ArtificialHorizon",
                        mainFolder + "Content/Textures/Sprites/ArtificialHorizon",
                        mainFolder + "Content/Textures/Sprites/Emotes",
                        mainFolder + "Content/Textures/GUI/Icons/ammo",
                        mainFolder + "Content/Textures/GUI/Icons/component",
                        mainFolder + "Content/Textures/GUI/Icons/Items",
                        mainFolder + "Content/Textures/GUI/Icons/ingot",
                        mainFolder + "Content/Textures/GUI/Icons",
                        mainFolder + "Content/Textures/FactionLogo",
                        mainFolder + "Content/Textures/FactionLogo/Builders",
                        mainFolder + "Content/Textures/FactionLogo/Miners",
                        mainFolder + "Content/Textures/FactionLogo/Others",
                        mainFolder + "Content/Textures/FactionLogo/Traders"
                    };

                    var files = new List<string>();
                    foreach (var folder in folderList)
                    {
                        files.AddRange(Directory.GetFiles(folder, "*", 
                            SearchOption.TopDirectoryOnly));
                    }
                    foreach (var file in files)
                    {
                        var fileValid = file.Replace(@"\", "/");
                        var filename = Path.GetFileName(fileValid);
                        filename = filename.Replace(".dds", "");
                        filename = filename.Replace(".DDS", "");
                        if (filename.Contains(".png"))
                        {
                            //then just copy it over
                            File.Copy(fileValid, convFileStorage + "/" + filename, true);
                            continue;
                        }
                        ConvertSprite(fileValid, convFileStorage, filename );
                    }
                    MyLog.Default.Info("Sprites converted");
                    ImagesConverted = true;
            });
        }

        private static void ConvertSprite(string file, string dest, string filename)
        {
            using (var image = Pfimage.FromFile(file))
            {
                PixelFormat format;
                
                switch (image.Format)
                {
                    case ImageFormat.Rgba32:
                        format = PixelFormat.Format32bppArgb;
                        break;
                    default:
                        throw new NotSupportedException($"Unsupported DDS format: {image.Format}");
                }
                var data = Marshal.UnsafeAddrOfPinnedArrayElement(image.Data, 0);
                using (var bitmap = new Bitmap(image.Width, image.Height, image.Stride, format, data))
                {
                    var renamedFile = RenameFileToId(filename);
                    var outputPath = dest + "/"  + renamedFile  + ".png";
                    Instance.FileRefs.Add(renamedFile, outputPath);
                    bitmap.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
                }
            }
        }

        //I love keen's Naming Conventions SO VERY MUCH!
        //I have needed to replace "/" with ";" and "\" with "#" due to filename restrictions
        //DONT FORGET THIS!..
          private static string RenameFileToId(string filename)
        {
            //I wish keen named these IDs better, otherwise this would not be nessecary
            //Finally DONE, JESUS KEEN!
            switch (filename)
            {
                case "GravityHudNegDegrees":
                    return "AH_GravityHudNegativeDegrees";
                case "GravityHudPosDegrees":
                    return "AH_GravityHudPositiveDegrees";
                case "PullUp":
                    return "AH_PullUp";
                case "TextBox":
                    return "AH_TextBox";
                case "VelocityVec":
                    return "AH_VelocityVector";
                case "Circle_Hollow":
                    return "CircleHollow";
                case "UnderConstruction":
                    return "Construction";
                case "DangerZone":
                    return "Danger";
                case "Left_Bracket":
                    return "DecorativeBracketLeft";
                case "Right_Bracket":
                    return "DecorativeBracketRight";
                case "LCD_Grid":
                    return "Grid";
                case "OxygenIcon":
                    return "IconOxygen";
                case "HydrogenIcon":
                    return "IconHydrogen";
                case "EnergyIcon":
                    return "IconEnergy";
                case "LCD_Economy_Economy_Badge":
                    return "LCD_Economy_Badge";
                case "LCD_Economy_Graph_1":
                    return "LCD_Economy_Graph";
                case "LCD_Economy_Blueprint":
                    return "LCD_Economy_SC_Blueprint";
                case "LCD_Economy_VendingMachine":
                    return "LCD_Economy_Vending_Bg";
                case "Angry":
                    return "LCD_Emote_Angry";
                case "Annoyed":
                    return "LCD_Emote_Annoyed";
                case "Confused":
                    return "LCD_Emote_Confused";
                case "crying":
                    return "LCD_Emote_Crying";
                case "Dead":
                    return "LCD_Emote_Dead";
                case "Evil":
                    return "LCD_Emote_Evil";
                case "Happy":
                    return "LCD_Emote_Happy";
                case "Love":
                    return "LCD_Emote_Love";
                case "Neutral":
                    return "LCD_Emote_Neutral";
                case "Sad":
                    return "LCD_Emote_Sad";
                case "Shocked":
                    return "LCD_Emote_Shocked";
                case "Skeptical":
                    return "LCD_Emote_Skeptical";
                case "Sleepy":
                    return "LCD_Emote_Sleepy";
                case "Suspicious_Left":
                    return "LCD_Emote_Suspicious_Left";
                case "Suspicious_Right":
                    return "LCD_Emote_Suspicious_Right";
                case "Wink":
                    return "LCD_Emote_Wink";
                case "LCD_Frostbite_Poster_01":
                    return "LCD_Frozen_Poster01";
                case "LCD_Frostbite_Poster_04":
                    return "LCD_Frozen_Poster02";
                case "LCD_Frostbite_Poster_03":
                    return "LCD_Frozen_Poster04";
                case "LCD_Frostbite_Poster_02":
                    return "LCD_Frozen_Poster03";
                case "LCD_Frostbite_Poster_05":
                    return "LCD_Frozen_Poster05";
                case "LCD_Frostbite_Poster_06":
                    return "LCD_Frozen_Poster06";
                case "LCD_Frostbite_Poster_07":
                    return "LCD_Frozen_Poster07";
                case "LCD_HeavyIndustry_Poster1_Revised_H_White":
                    return "LCD_HI_Poster1_Landscape";
                case "LCD_HeavyIndustry_Poster1_Revised_S_White":
                    return "LCD_HI_Poster1_Square";
                case "LCD_HeavyIndustry_Poster1_Revised_V_White":
                    return "LCD_HI_Poster1_Vertical";
                case "LCD_HeavyIndustry_Poster2_Revised_H_White":
                    return "LCD_HI_Poster2_Landscape";
                case "LCD_HeavyIndustry_Poster2_Revised_S_White":
                    return "LCD_HI_Poster2_Square";
                case "LCD_HeavyIndustry_Poster2_Revised_V_White":
                    return "LCD_HI_Poster2_Vertical";
                case "LCD_HeavyIndustry_Poster3_RevisedCA_H_White":
                    return "LCD_HI_Poster3_Landscape";
                case "LCD_HeavyIndustry_Poster3_RevisedCA_S_White":
                    return "LCD_HI_Poster3_Square";
                case "LCD_HeavyIndustry_Poster3_RevisedCA_V_White":
                    return "LCD_HI_Poster3_Vertical";
                case "LCD_SoF_BrightFuture_Landscape_White":
                    return "LCD_SoF_BrightFuture_Landscape";
                case "LCD_SoF_BrightFuture_Square_White":
                    return "LCD_SoF_BrightFuture_Square";
                case "LCD_SoF_BrightFuture_Portrait_White":
                    return "LCD_SoF_BrightFuture_Portrait";
                case "LCD_SoF_CosmicTeam_Landscape_White":
                    return "LCD_SoF_CosmicTeam_Landscape";
                case "LCD_SoF_CosmicTeam_Square_White":
                    return "LCD_SoF_CosmicTeam_Square";
                case "LCD_SoF_CosmicTeam_Portrait_White":
                    return "LCD_SoF_CosmicTeam_Portrait";
                case "LCD_SoF_Exploration_Landscape_White":
                    return "LCD_SoF_Exploration_Landscape";
                case "LCD_SoF_Exploration_Square_White":
                    return "LCD_SoF_Exploration_Square";
                case "LCD_SoF_Exploration_Portrait_White":
                    return "LCD_SoF_Exploration_Portrait";
                case "LCD_SoF_SpaceTravel_Landscape_White":
                    return "LCD_SoF_SpaceTravel_Landscape";
                case "LCD_SoF_SpaceTravel_Square_White":
                    return "LCD_SoF_SpaceTravel_Square";
                case "LCD_SoF_SpaceTravel_Portrait_White":
                    return "LCD_SoF_SpaceTravel_Portrait";
                case "LCD_SoF_ThunderFleet_Landscape_White":
                    return "LCD_SoF_ThunderFleet_Landscape";
                case "LCD_SoF_ThunderFleet_Square_White":
                    return "LCD_SoF_ThunderFleet_Square";
                case "LCD_SoF_ThunderFleet_Portrait_White":
                    return "LCD_SoF_ThunderFleet_Portrait";
                case "Ammo_Box":
                    return "MyObjectBuilder_AmmoMagazine;NATO_25x184mm";
                case "AutocanonShellBox":
                    return "MyObjectBuilder_AmmoMagazine;AutocannonClip";
                case "Rifle_Ammo_SemiAuto":
                    return "MyObjectBuilder_AmmoMagazine;AutomaticRifleGun_Mag_20rd";
                case "Pistol_Elite_Warfare_Ammo":
                    return "MyObjectBuilder_AmmoMagazine;ElitePistolMagazine";
                case "FireworksBox":
                    return "MyObjectBuilder_AmmoMagazine;FireworksBoxBlue";
                case "FlareGun_Ammo":
                    return "MyObjectBuilder_AmmoMagazine;FlareClip";
                case "Pistol_FullAuto_Warfare_Ammo":
                    return "MyObjectBuilder_AmmoMagazine;FullAutoPistolMagazine";
                case "LargeCalibreShell":
                    return "MyObjectBuilder_AmmoMagazine;LargeCalibreAmmo";
                case "RailgunAmmoLarge":
                    return "MyObjectBuilder_AmmoMagazine;LargeRailgunAmmo";
                case "MediumCalibreShell":
                    return "MyObjectBuilder_AmmoMagazine;MediumCalibreAmmo";
                case "Small_Rocket":
                    return "MyObjectBuilder_AmmoMagazine;Missile200mm";
                case "Rifle_Ammo":
                    return "MyObjectBuilder_AmmoMagazine;NATO_5p56x45mm";
                case "Rifle_Ammo_Precise":
                    return "MyObjectBuilder_AmmoMagazine;PreciseAutomaticRifleGun_Mag_5rd";
                case "Rifle_Ammo_RapidFire":
                    return "MyObjectBuilder_AmmoMagazine;RapidFireAutomaticRifleGun_Mag_50rd";
                case "Pistol_Warfare_Ammo":
                    return "MyObjectBuilder_AmmoMagazine;SemiAutoPistolMagazine";
                case "RailgunAmmo":
                    return "MyObjectBuilder_AmmoMagazine;SmallRailgunAmmo";
                case "Rifle_Ammo_Elite":
                    return "MyObjectBuilder_AmmoMagazine;UltimateAutomaticRifleGun_Mag_30rd";
                case "bulletproof_glass_component":
                    return "MyObjectBuilder_Component;BulletproofGlass";
                case "Cartridge_Icon":
                    return "MyObjectBuilder_Component;Canvas";
                case "computer_component":
                    return "MyObjectBuilder_Component;Computer";
                case "construction_components_component":
                    return "MyObjectBuilder_Component;Construction";
                case "detector_components_component":
                    return "MyObjectBuilder_Component;Detector";
                case "display_component":
                    return "MyObjectBuilder_Component;Display";
                case "Plushie":
                    return "MyObjectBuilder_Component;EngineerPlushie";
                case "ExplosivesComponent":
                    return "MyObjectBuilder_Component;Explosives";
                case "girder_component":
                    return "MyObjectBuilder_Component;Girder";
                case "gravity_generator_components_component":
                    return "MyObjectBuilder_Component;GravityGenerator";
                case "interior_plate_component":
                    return "MyObjectBuilder_Component;InteriorPlate";
                case "large_tube_component":
                    return "MyObjectBuilder_Component;LargeTube";
                case "medical_components_component":
                    return "MyObjectBuilder_Component;Medical";
                case "metal_grid_component":
                    return "MyObjectBuilder_Component;MetalGrid";
                case "motor_component":
                    return "MyObjectBuilder_Component;Motor";
                case "BatteryComponent":
                    return "MyObjectBuilder_Component;PowerCell";
                case "PrototechFrame":
                    return "MyObjectBuilder_Component;PrototechFrame";
                case "PrototechPanel_Component":
                    return "MyObjectBuilder_Component;PrototechPanel";
                case "PrototechMachinery_Icon":
                    return "MyObjectBuilder_Component;PrototechMachinery";
                case "PrototechCapacitor_Component":
                    return "MyObjectBuilder_Component;PrototechCapacitor";
                case "prototech_circuitry_component":
                    return "MyObjectBuilder_Component;PrototechCircuitry";
                case "PrototechCoolingUnit":
                    return "MyObjectBuilder_Component;PrototechCoolingUnit";
                case "PrototechThrusterComponent":
                    return "MyObjectBuilder_Component;PrototechPropulsionUnit";
                case "radio_communication_components_component":
                    return "MyObjectBuilder_Component;RadioCommunication";
                case "reactor_components_component":
                    return "MyObjectBuilder_Component;Reactor";
                case "ScrapMetalComponent":
                    return "MyObjectBuilder_Ingot;Scrap";
                case "ScrapPrototechComponent":
                    return "MyObjectBuilder_Ingot;PrototechScrap";
                case "small_tube_component":
                    return "MyObjectBuilder_Component;SmallTube";
                case "SolarCellComponent":
                    return "MyObjectBuilder_Component;SolarCell";
                case "steel_plate_component":
                    return "MyObjectBuilder_Component;SteelPlate";
                case "superconductor_conducts_component":
                    return "MyObjectBuilder_Component;Superconductor";
                case "thrust_components_component":
                    return "MyObjectBuilder_Component;Thrust";
                case "ZoneChip_Item":
                    return "MyObjectBuilder_Component;ZoneChip";
                case "ClangCola":
                    return "MyObjectBuilder_ConsumableItem;ClangCola";
                case "CosmicCoffee":
                    return "MyObjectBuilder_ConsumableItem;CosmicCoffee";
                case "MedKit":
                    return "MyObjectBuilder_ConsumableItem;MedKit";
                case "PowerKit":
                    return "MyObjectBuilder_ConsumableItem;Powerkit";
                case "Datapad_Item":
                    return "MyObjectBuilder_Datapad;Datapad";
                case "HydrogenBottle_Component":
                    return "MyObjectBuilder_GasContainerObject;HydrogenBottle";
                case "cobalt_ingot":
                    return "MyObjectBuilder_Ingot;Cobalt";
                case "gold_ingot":
                    return "MyObjectBuilder_Ingot;Gold";
                case "iron_ingot":
                    return "MyObjectBuilder_Ingot;Iron";
                case "magnesium_ingot":
                    return "MyObjectBuilder_Ingot;Magnesium";
                case "nickel_ingot":
                    return "MyObjectBuilder_Ingot;Nickel";
                case "platinum_ingot":
                    return "MyObjectBuilder_Ingot;Platinum";
                case "silver_ingot":
                    return "MyObjectBuilder_Ingot;Silver";
                case "silicon_ingot":
                    return "MyObjectBuilder_Ingot;Silicon";
                case "gravel_ingot":
                    return "MyObjectBuilder_Ingot;Stone";
                case "uranium_ingot":
                    return "MyObjectBuilder_Ingot;Uranium";
                case "ore_Co_cobalt":
                    return "MyObjectBuilder_Ore;Cobalt";
                case "ore_Au_gold":
                    return "MyObjectBuilder_Ore;Gold";
                case "ore_H2O_ice":
                    return "MyObjectBuilder_Ore;Ice";
                case "ore_Fe_iron":
                    return "MyObjectBuilder_Ore;Iron";
                case "ore_Mg_magnesium":
                    return "MyObjectBuilder_Ore;Magnesium";
                case "ore_Ni_nickel":
                    return "MyObjectBuilder_Ore;Nickel";
                case "ore_biomass":
                    return "MyObjectBuilder_Ore;Organic";
                case "ore_Pt_platinum":
                    return "MyObjectBuilder_Ore;Platinum";
                case "ore_Ag_silver":
                    return "MyObjectBuilder_Ore;Silver";
                case "ore_Si_silicon":
                    return "MyObjectBuilder_Ore;Silicon";
                case "ore_UO2_uranite":
                    return "MyObjectBuilder_Ore;Uranium";
                case "ore_rock":
                    return "MyObjectBuilder_Ore;Rock";
                case "OxygenBottleComponent":
                    return "MyObjectBuilder_OxygenContainerObject;OxygenBottle";
                case "WeaponRocketLauncher_Precise":
                    return "MyObjectBuilder_PhysicalGunObject;AdvancedHandHeldLauncherItem";
                case "WeaponGrinder_1":
                    return "MyObjectBuilder_PhysicalGunObject;AngleGrinder2Item";
                case "WeaponGrinder_2":
                    return "MyObjectBuilder_PhysicalGunObject;AngleGrinder3Item";
                case "WeaponGrinder_3":
                    return "MyObjectBuilder_PhysicalGunObject;AngleGrinder4Item";
                case "WeaponGrinder":
                    return "MyObjectBuilder_PhysicalGunObject;AngleGrinderItem";
                case "WeaponAutomaticRifle":
                    return "MyObjectBuilder_PhysicalGunObject;AutomaticRifleItem";
                case "WeaponRocketLauncher_Regular":
                    return "MyObjectBuilder_PhysicalGunObject;BasicHandHeldLauncherItem";
                case "WeaponPistol_Elite_Warfare":
                    return "MyObjectBuilder_PhysicalGunObject;ElitePistolItem";
                case "FlareGun":
                    return "MyObjectBuilder_PhysicalGunObject;FlareGunItem";
                case "WeaponPistol_FullAuto_Warfare":
                    return "MyObjectBuilder_PhysicalGunObject;FullAutoPistolItem";
                case "WeaponDrill_1":
                    return "MyObjectBuilder_PhysicalGunObject;HandDrill2Item";
                case "WeaponDrill_2":
                    return "MyObjectBuilder_PhysicalGunObject;HandDrill3Item";
                case "WeaponDrill_3":
                    return "MyObjectBuilder_PhysicalGunObject;HandDrill4Item";
                case "WeaponDrill":
                    return "MyObjectBuilder_PhysicalGunObject;HandDrillItem";
                case "WeaponAutomaticRifle_Precise":
                    return "MyObjectBuilder_PhysicalGunObject;PreciseAutomaticRifleItem";
                case "WeaponAutomaticRifle_RapidFire":
                    return "MyObjectBuilder_PhysicalGunObject;RapidFireAutomaticRifleItem";
                case "WeaponPistol_Warfare":
                    return "MyObjectBuilder_PhysicalGunObject;SemiAutoPistolItem";
                case "WeaponAutomaticRifle_Elite":
                    return "MyObjectBuilder_PhysicalGunObject;UltimateAutomaticRifleItem";
                case "WeaponWelder_1":
                    return "MyObjectBuilder_PhysicalGunObject;Welder2Item";
                case "WeaponWelder_2":
                    return "MyObjectBuilder_PhysicalGunObject;Welder3Item";
                case "WeaponWelder_3":
                    return "MyObjectBuilder_PhysicalGunObject;Welder4Item";
                case "WeaponWelder":
                    return "MyObjectBuilder_PhysicalGunObject;WelderItem";
                case "SpaceCredit":
                    return "MyObjectBuilder_PhysicalObject;SpaceCredit";
                case "NoEntry":
                    return "No Entry";
                case "default_offline":
                    return "Offline";
                case "default_offline_wide":
                    return "Offline_wide";
                case "default_online":
                    return "Online";
                case "default_online_wide":
                    return "Online_wide";
                case "Right_Triangle":
                    return "RightTriangle";
                case "Semi_Circle":
                    return "SemiCircle";
                case "Square_Hollow":
                    return "SquareHollow";
                case "Blank":
                    return "SquareSimple";
                case "WhiteSprite": //have a conflict with another one below
                    return "SquareTapered";
                case "LCD_Economy_1":
                    return "StoreBlock2";
                case "BuilderIcon_1":
                    return @"Textures#FactionLogo#Builders#BuilderIcon_1.dds";
                case "BuilderIcon_2":
                    return @"Textures#FactionLogo#Builders#BuilderIcon_2.dds";
                case "BuilderIcon_3":
                    return @"Textures#FactionLogo#Builders#BuilderIcon_3.dds";
                case "BuilderIcon_4":
                    return @"Textures#FactionLogo#Builders#BuilderIcon_4.dds";
                case "BuilderIcon_5":
                    return @"Textures#FactionLogo#Builders#BuilderIcon_5.dds";
                case "BuilderIcon_6":
                    return @"Textures#FactionLogo#Builders#BuilderIcon_6.dds";
                case "BuilderIcon_7":
                    return @"Textures#FactionLogo#Builders#BuilderIcon_7.dds";
                case "BuilderIcon_8":
                    return @"Textures#FactionLogo#Builders#BuilderIcon_8.dds";
                case "BuilderIcon_9":
                    return @"Textures#FactionLogo#Builders#BuilderIcon_9.dds";
                case "BuilderIcon_10":
                    return @"Textures#FactionLogo#Builders#BuilderIcon_10.dds";
                case "BuilderIcon_11":
                    return @"Textures#FactionLogo#Builders#BuilderIcon_11.dds";
                case "BuilderIcon_12":
                    return @"Textures#FactionLogo#Builders#BuilderIcon_12.dds";
                case "BuilderIcon_13":
                    return @"Textures#FactionLogo#Builders#BuilderIcon_13.dds";
                case "BuilderIcon_14":
                    return @"Textures#FactionLogo#Builders#BuilderIcon_14.dds";
                case "BuilderIcon_15":
                    return @"Textures#FactionLogo#Builders#BuilderIcon_15.dds";
                case "BuilderIcon_16":
                    return @"Textures#FactionLogo#Builders#BuilderIcon_16.dds";
                case "Empty":
                    return @"Textures#FactionLogo#Empty.dds";
                case "Factorum":
                    return @"Textures#FactionLogo#Factorum.dds";
                case "MinerIcon_1":
                    return @"Textures#FactionLogo#Miners#MinerIcon_1.dds";
                case "MinerIcon_2":
                    return @"Textures#FactionLogo#Miners#MinerIcon_2.dds";
                case "MinerIcon_3":
                    return @"Textures#FactionLogo#Miners#MinerIcon_3.dds";
                case "MinerIcon_4":
                    return @"Textures#FactionLogo#Miners#MinerIcon_4.dds";
                case "OtherIcon_1":
                    return @"Textures#FactionLogo#Others#OtherIcon_1.dds";
                case "OtherIcon_2":
                    return @"Textures#FactionLogo#Others#OtherIcon_2.dds";
                case "OtherIcon_3":
                    return @"Textures#FactionLogo#Others#OtherIcon_3.dds";
                case "OtherIcon_4":
                    return @"Textures#FactionLogo#Others#OtherIcon_4.dds";
                case "OtherIcon_5":
                    return @"Textures#FactionLogo#Others#OtherIcon_5.dds";
                case "OtherIcon_6":
                    return @"Textures#FactionLogo#Others#OtherIcon_6.dds";
                case "OtherIcon_7":
                    return @"Textures#FactionLogo#Others#OtherIcon_7.dds";
                case "OtherIcon_8":
                    return @"Textures#FactionLogo#Others#OtherIcon_8.dds";
                case "OtherIcon_9":
                    return @"Textures#FactionLogo#Others#OtherIcon_9.dds";
                case "OtherIcon_10":
                    return @"Textures#FactionLogo#Others#OtherIcon_10.dds";
                case "OtherIcon_11":
                    return @"Textures#FactionLogo#Others#OtherIcon_11.dds";
                case "OtherIcon_12":
                    return @"Textures#FactionLogo#Others#OtherIcon_12.dds";
                case "OtherIcon_13":
                    return @"Textures#FactionLogo#Others#OtherIcon_13.dds";
                case "OtherIcon_14":
                    return @"Textures#FactionLogo#Others#OtherIcon_14.dds";
                case "OtherIcon_15":
                    return @"Textures#FactionLogo#Others#OtherIcon_15.dds";
                case "OtherIcon_16":
                    return @"Textures#FactionLogo#Others#OtherIcon_16.dds";
                case "OtherIcon_17":
                    return @"Textures#FactionLogo#Others#OtherIcon_17.dds";
                case "OtherIcon_18":
                    return @"Textures#FactionLogo#Others#OtherIcon_18.dds";
                case "OtherIcon_19":
                    return @"Textures#FactionLogo#Others#OtherIcon_19.dds";
                case "OtherIcon_20":
                    return @"Textures#FactionLogo#Others#OtherIcon_20.dds";
                case "OtherIcon_21":
                    return @"Textures#FactionLogo#Others#OtherIcon_21.dds";
                case "OtherIcon_22":
                    return @"Textures#FactionLogo#Others#OtherIcon_22.dds";
                case "OtherIcon_23":
                    return @"Textures#FactionLogo#Others#OtherIcon_23.dds";
                case "OtherIcon_24":
                    return @"Textures#FactionLogo#Others#OtherIcon_24.dds";
                case "OtherIcon_25":
                    return @"Textures#FactionLogo#Others#OtherIcon_25.dds";
                case "OtherIcon_26":
                    return @"Textures#FactionLogo#Others#OtherIcon_26.dds";
                case "OtherIcon_27":
                    return @"Textures#FactionLogo#Others#OtherIcon_27.dds";
                case "OtherIcon_28":
                    return @"Textures#FactionLogo#Others#OtherIcon_28.dds";
                case "OtherIcon_29":
                    return @"Textures#FactionLogo#Others#OtherIcon_29.dds";
                case "OtherIcon_30":
                    return @"Textures#FactionLogo#Others#OtherIcon_30.dds";
                case "OtherIcon_31":
                    return @"Textures#FactionLogo#Others#OtherIcon_31.dds";
                case "OtherIcon_32":
                    return @"Textures#FactionLogo#Others#OtherIcon_32.dds";
                case "OtherIcon_33":
                    return @"Textures#FactionLogo#Others#OtherIcon_33.dds";
                case "PirateIcon":
                    return @"Textures#FactionLogo#PirateIcon.dds";
                case "Spiders":
                    return @"Textures#FactionLogo#Spiders.dds";
                case "TraderIcon_1":
                    return @"Textures#FactionLogo#Traders#TraderIcon_1.dds";
                case "TraderIcon_2":
                    return @"Textures#FactionLogo#Traders#TraderIcon_2.dds";
                case "TraderIcon_3":
                    return @"Textures#FactionLogo#Traders#TraderIcon_3.dds";
                case "TraderIcon_4":
                    return @"Textures#FactionLogo#Traders#TraderIcon_4.dds";
                case "TraderIcon_5":
                    return @"Textures#FactionLogo#Traders#TraderIcon_5.dds";
                case "Unknown":
                    return @"Textures#FactionLogo#Unknown.dds";
                //case "WhiteSprite":
                   //return "White Screen";
                default:
                    return filename;
            }
        }

        public void Dispose()
        {
            // TODO: Save state and close resources here, called when the game exits (not guaranteed!)
            // IMPORTANT: Do NOT call harmony.UnpatchAll() here! It may break other plugins.

            Instance = null;
        }

        public void Update()
        {
            // TODO: Put your update code here. It is called on every simulation frame!
            if (MyAPIGateway.Multiplayer == null || MySession.Static?.LocalCharacter == null || MyAPIGateway.Session == null)
            {
                return;
            }

            if (!Instance.WindowOpen) return;
            if (Instance._counter == 5)
            {
                Instance._counter = 0;
                if (IsControlled)
                {
                    Instance._prevControlled = true;
                    SecondWindowInter.UpdateDisplayInter();
                    //send the call to update the second window output
                }
                else if (Instance._prevControlled && !IsControlled)
                {
                    Instance._prevControlled = false;
                    SecondWindowInter.ClearDisplayListInter();
                    //send the call to clear the second window output
                }
            }
            else Instance._counter++;

        }
    

        // ReSharper disable once UnusedMember.Global
        public void OpenConfigDialog()
        {
            Instance._settingsGenerator.SetLayout<Simple>();
            MyGuiSandbox.AddScreen(Instance._settingsGenerator.Dialog);
        }

        //TODO: Uncomment and use this method to load asset files
        /*public void LoadAssets(string folder)
        {

        }*/
    }
}