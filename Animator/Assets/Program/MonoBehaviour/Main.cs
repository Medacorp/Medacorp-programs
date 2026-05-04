using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Linq;


public class Main : MonoBehaviour
{
    private string executionPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
    private string worldsPath;
    public GameObject worldSelector;
    private Dictionary<string,string> worldMap = new();
    private string selectedWorldRoot;
    private string selectedWorld;
    private List<string> SpawnFunctions;
    public GameObject addOnSelector;
    private string selectedAddOn;
    public GameObject entitySelector;
    private string selectedEntity;
    private string selectedAnimationRoot;
    public string commandStorageName;
    public string commandStorageValue;
    public string mainFunction;
    private string[] entityID = {"","",""};
    private MapEntity mapEntity = new();
    public GameObject animationSelector;
    private string selectedAnimation;
    public GameObject changeFunctionButton;
    public GameObject modelDisplay;
    private List<string> awaiting = new();
    public GameObject loadingScreen;
    public GameObject loadingText;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Texture.allowThreadedTextureCreation = true;
        executionPath = executionPath.Remove(executionPath.Length-53);
        if (!Directory.Exists(executionPath + "animator settings/")) {
            Directory.CreateDirectory(executionPath + "animator settings/");
        }
        if (File.Exists(executionPath + "paths.txt")) {
            foreach (string line in File.ReadLines(executionPath + "paths.txt")) {
                string[] splitLine = line.Split("=");
                if (splitLine[0] == "worlds") {
                    worldsPath = splitLine[1];
                    break;
                }
            }
        }
        else {
            worldsPath = EditorUtility.OpenFolderPanel("Select Worlds Directory", "", "") + "/";
            using (StreamWriter outputFile = new(Path.Combine(executionPath + "paths.txt")))
            {
                outputFile.WriteLine("worlds=" + worldsPath);
            }
        }
        TMP_Dropdown dropdown = worldSelector.GetComponent<TMP_Dropdown>();
        dropdown.options.Add(new TMP_Dropdown.OptionData("Select world"));
        foreach (string folder in Directory.GetDirectories(worldsPath)) {
            string[] split = folder.Split("/");
            string world = split[split.Length-1];
            string path = worldsPath + world + "/";
            foreach (string folder2 in Directory.GetDirectories(path)) {
                split = folder2.Split("/");
                string world2 = split[split.Length-1];
                if (File.Exists(path + world2 + "/level.dat") && File.Exists(path + world2 + " Resource Pack/pack.mcmeta")) {
                    dropdown.options.Add(new TMP_Dropdown.OptionData(world2));
                    worldMap.Add(world2, world);
                }
            } 
        } 
        dropdown.value = 0;
        dropdown.RefreshShownValue();
    }

    // Update is called once per frame
    void Update()
    {
        if (awaiting.Count != 0)
        {
            loadingScreen.SetActive(true);
            string loading = "Loading...";
            switch (awaiting[0])
            {
                case "assets":
                    loading = "Extracting Minecraft assets...";
                    break;
                case "entities":
                    loading = "Getting entities...";
                    break;
                case "entitiesDone":
                    loading = "Getting entities...";
                    //This line may not run on another thread
                    if (addOnSelector.GetComponent<TMP_Dropdown>().value != 0) AddOnSelected();
                    awaiting.RemoveAll(i => i == "entitiesDone");
                    break;
            }
            TMP_Text text = loadingText.GetComponent<TMP_Text>();
            text.text = loading;
            Color32 color = text.color;
            if (color.g != 0 && color.r == 0 && color.b == 255) color.g -= 1;
            else if (color.r != 255 && color.g == 0 && color.b == 255) color.r += 1;
            else if (color.b != 0 && color.g == 0 && color.r == 255) color.b -= 1;
            else if (color.g != 255 && color.b == 0 && color.r == 255) color.g += 1;
            else if (color.r != 0 && color.b == 0 && color.g == 255) color.r -= 1;
            else if (color.b != 255 && color.r == 0 && color.g == 255) color.b += 1;
            text.color = color;
        }
        else loadingScreen.SetActive(false);
        TextureAtlas.AdvanceAnimations(Time.deltaTime*20);
    }

    public void WorldSelected() {
        TMP_Dropdown dropdown = worldSelector.GetComponent<TMP_Dropdown>();
        if (dropdown.value == 0) {
            selectedWorld = null;
            selectedWorldRoot = null;
            addOnSelector.GetComponent<TMP_Dropdown>().interactable = false;
            addOnSelector.GetComponent<TMP_Dropdown>().value = 0;
        }
        else {
            selectedWorld = dropdown.options[dropdown.value].text;
            worldMap.TryGetValue(selectedWorld, out selectedWorldRoot);
            if (!File.Exists(executionPath + "animator settings/" + selectedWorld + ".json") && Directory.Exists(worldsPath+selectedWorldRoot+"/"+selectedWorld+"/datapacks/MEDACORP/animator/")) {
                if (File.Exists(worldsPath+selectedWorldRoot+"/"+selectedWorld+"/datapacks/MEDACORP/animator/settings.json")) {
                    File.Copy(worldsPath + selectedWorldRoot + "/" + selectedWorld + "/datapacks/MEDACORP/animator/settings.json", executionPath + "animator settings/" + selectedWorld + ".json");
                }
            }
            addOnSelector.GetComponent<TMP_Dropdown>().interactable = true;
            TMP_Dropdown addOnDropdown = addOnSelector.GetComponent<TMP_Dropdown>();
            addOnDropdown.options.Clear();
            addOnDropdown.options.Add(new TMP_Dropdown.OptionData("Select Add-on"));
            addOnDropdown.value = 0;
            addOnDropdown.RefreshShownValue();
            foreach (string folder in Directory.GetDirectories(worldsPath+selectedWorldRoot+"/"+selectedWorld+"/datapacks/")) {
                string[] split = folder.Split("/");
                string addOn = split[split.Length-1];
                string path = worldsPath+selectedWorldRoot+"/"+selectedWorld+"/datapacks/" + addOn + "/";
                if (Directory.Exists(path + "data")) {
                    addOnSelector.GetComponent<TMP_Dropdown>().options.Add(new TMP_Dropdown.OptionData(addOn));
                }
            }
            SpawnFunctions = new();
            Thread threadedCloneAssets = new Thread(() => GetMinecraftAssets());
            threadedCloneAssets.Start();
            Thread threadedGetEntities = new Thread(() => GetEntities());
            threadedGetEntities.Start();
        }
        entitySelector.GetComponent<TMP_Dropdown>().interactable = false;
        entitySelector.GetComponent<TMP_Dropdown>().value = 0;
        animationSelector.GetComponent<TMP_Dropdown>().interactable = false;
        animationSelector.GetComponent<TMP_Dropdown>().value = 0;
        ClearModel();
    }

    private void GetEntities()
    {
        awaiting.Add("entities");
        List<string> newSpawnFunctions = new();
        foreach (string addOn in Directory.GetDirectories(worldsPath + selectedWorldRoot + "/" + selectedWorld + "/datapacks/"))
        {
            string[] split = addOn.Split("/");
            string dataPack = split[split.Length - 1];
            string path = worldsPath + selectedWorldRoot + "/" + selectedWorld + "/datapacks/" + dataPack + "/data/";
            foreach (string folder in Directory.GetDirectories(path))
            {
                string[] split2 = folder.Split("/");
                string animationNamespace = split2[split2.Length - 1];
                string path2 = path + animationNamespace + "/function/animations/";
                if (Directory.Exists(path2))
                {
                    string[] mainFunctions = Directory.GetFiles(path2, "main.mcfunction", SearchOption.AllDirectories);
                    string[] callFunctions = Directory.GetFiles(path2, "call_part_function.mcfunction", SearchOption.AllDirectories);
                    foreach (string function in mainFunctions)
                    {
                        newSpawnFunctions.Add(animationNamespace + ":" + function.Replace(path2, "").Replace("\\main.mcfunction", "").Replace("\\", "/") + ":" + dataPack);
                    }
                    foreach (string function in callFunctions)
                    {
                        string trim = function.Replace(path2, "").Replace("\\call_part_function.mcfunction", "").Replace("\\", "/");
                        if (!newSpawnFunctions.Contains(animationNamespace + ":" + trim + ":" + dataPack)) newSpawnFunctions.Add(animationNamespace + ":" + trim + ":" + dataPack);
                    }
                }
            }
        }
        newSpawnFunctions.Sort();
        SpawnFunctions = newSpawnFunctions;
        awaiting.RemoveAll(i => i == "entities");
        awaiting.Add("entitiesDone");
    }
    private void GetMinecraftAssets()
    {
        awaiting.Add("assets");
        string settingsPath = executionPath + "animator settings/";
        string minecraftPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/.minecraft/versions/";
        if (File.Exists(minecraftPath + "version_manifest_v2.json"))
        {
            MinecraftVersionManifest versionManifest = JsonConvert.DeserializeObject<MinecraftVersionManifest>(File.ReadAllText(minecraftPath + "version_manifest_v2.json"));
            string version = "";
            versionManifest.latest.TryGetValue("release", out version);
            minecraftPath = minecraftPath + version + "/" + version + ".jar";
            if (File.Exists(minecraftPath))
            {
                bool extract = true;
                if (File.Exists(settingsPath + "Minecraft Assets.txt")) {
                    if (File.ReadAllText(settingsPath + "Minecraft Assets.txt") == version) extract = false;
                }
                if (extract)
                {
                    if (Directory.Exists(settingsPath + "Minecraft Assets")) Directory.Delete(settingsPath + "Minecraft Assets", true);
                    Directory.CreateDirectory(settingsPath + "Minecraft Assets");
                    if (File.Exists(settingsPath + "Minecraft Assets.txt")) File.Delete(settingsPath + "Minecraft Assets.txt");
                    using (ZipArchive archive = ZipFile.OpenRead(minecraftPath))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            if (entry.FullName == "assets/minecraft/atlases/blocks.json" || entry.FullName.Contains("assets/minecraft/textures/") || entry.FullName.Contains("assets/minecraft/items/") || entry.FullName.Contains("assets/minecraft/models/"))
                            {
                                string folderPath = Regex.Replace(entry.FullName.Replace("assets", ""), "/[^/]+$", "");
                                if (folderPath.Contains("/") || folderPath.Contains("\\"))
                                {
                                    List<string> extractFolders = folderPath.Split("/").ToList();
                                    string folder = settingsPath + "Minecraft Assets";
                                    extractFolders.RemoveAt(0);
                                    foreach (string extractFolder in extractFolders)
                                    {
                                        folder = folder + "/" + extractFolder;
                                        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                                    }
                                }
                                entry.ExtractToFile(settingsPath + "Minecraft Assets/" + entry.FullName.Replace("assets/", ""));
                            }
                        }
                    }
                }
                MinecraftAtlas.vanillaSources = JsonConvert.DeserializeObject<MinecraftAtlas>(File.ReadAllText(settingsPath + "Minecraft Assets/minecraft/atlases/blocks.json")).sources;
                File.WriteAllText(settingsPath + "Minecraft Assets.txt", version);
            }
            else
            {
                Debug.Log("Cannot find latest Minecraft release");
            }
        }
        else
        {
            Debug.Log("Cannot find Minecraft version manifest");
        }
        awaiting.RemoveAll(i => i == "assets");
    }
    public void AddOnSelected() {
        TMP_Dropdown dropdown = addOnSelector.GetComponent<TMP_Dropdown>();
        if (dropdown.value == 0) {
            selectedAddOn = null;
            entitySelector.GetComponent<TMP_Dropdown>().interactable = false;
            entitySelector.GetComponent<TMP_Dropdown>().value = 0;
        }
        else {
            selectedAddOn = dropdown.options[dropdown.value].text;
            TMP_Dropdown animationGroupDropdown = entitySelector.GetComponent<TMP_Dropdown>();
            animationGroupDropdown.options.Clear();
            if (SpawnFunctions.Count != 0) {
                entitySelector.GetComponent<TMP_Dropdown>().interactable = true;
                animationGroupDropdown.options.Add(new TMP_Dropdown.OptionData("Select animation group"));
                foreach (string folder in Directory.GetDirectories(worldsPath+selectedWorldRoot+"/"+selectedWorld+"/datapacks/" + selectedAddOn + "/data/")) {
                    string[] split = folder.Split("/");
                    string animationNamespace = split[split.Length-1];
                    string path = worldsPath+selectedWorldRoot+"/"+selectedWorld+"/datapacks/" + selectedAddOn + "/data/" + animationNamespace + "/function/animations/";
                    foreach (string entity in SpawnFunctions) {
                        string[] entityID = entity.Split(":");
                        if (Directory.Exists(path + entityID[1])) {
                            entitySelector.GetComponent<TMP_Dropdown>().options.Add(new TMP_Dropdown.OptionData(entityID[0] + ":" + entityID[1]));
                        }
                    }
                }
                animationGroupDropdown.value = 0;
            }
            else animationGroupDropdown.options.Add(new TMP_Dropdown.OptionData("Loading animation groups"));
            animationGroupDropdown.RefreshShownValue();
        }
        ClearModel();
    }
    public void EntitySelected() {
        TMP_Dropdown dropdown = entitySelector.GetComponent<TMP_Dropdown>();
        if (dropdown.value == 0) ClearModel();
        else {
            selectedEntity = dropdown.options[dropdown.value].text;
            entityID = selectedEntity.Split(":");
            string entity = SpawnFunctions.Find(e => e.StartsWith(entityID[0] + ":" + entityID[1]));
            entityID = entity.Split(":");
            GetSpawnFunction(entityID, false);
            TMP_Dropdown animationDropdown = animationSelector.GetComponent<TMP_Dropdown>();
            animationDropdown.options.Clear();
            animationDropdown.options.Add(new TMP_Dropdown.OptionData("Select animation"));
            animationDropdown.options.Add(new TMP_Dropdown.OptionData("New animation"));
            foreach (string addOnPath in Directory.GetDirectories(worldsPath+selectedWorldRoot+"/"+selectedWorld+"/datapacks/" + selectedAddOn + "/data/")) {
                string[] split = addOnPath.Split("/");
                string addOnNamespace = split[split.Length-1];
                string path = worldsPath+selectedWorldRoot+"/"+selectedWorld+"/datapacks/" + selectedAddOn + "/data/" + addOnNamespace + "/function/animations/" + entityID[1] + "/";
                if (Directory.Exists(path)) {
                    foreach (string folder in Directory.GetDirectories(path)) {
                        string[] split2 = folder.Split("/");
                        string animationName = split2[split2.Length-1];
                        GetAnimations(path, addOnNamespace, animationName);
                    }
                }
            }
            animationSelector.GetComponent<TMP_Dropdown>().interactable = true;
            changeFunctionButton.GetComponent<Button>().interactable = true;
            animationDropdown.value = 0;
            animationDropdown.RefreshShownValue();
        }
    }

    public void PressChangeFunctionButton()
    {
        TMP_Dropdown dropdown = entitySelector.GetComponent<TMP_Dropdown>();
        selectedEntity = dropdown.options[dropdown.value].text;
        entityID = selectedEntity.Split(":");
        string entity = SpawnFunctions.Find(e => e.StartsWith(entityID[0] + ":" + entityID[1]));
        entityID = entity.Split(":");
        GetSpawnFunction(entityID, true);
    }
    private void GetSpawnFunction(string[] ID, bool force) {
        string path = worldsPath + selectedWorldRoot + "/" + selectedWorld + "/datapacks/";
        AnimatorSettings animatorSettings = new();
        if (File.Exists(executionPath + "animator settings/" + selectedWorld + ".json")) {
            animatorSettings = JsonConvert.DeserializeObject<AnimatorSettings>(File.ReadAllText(executionPath + "animator settings/" + selectedWorld + ".json"));
        }
        string entityNamespace = ID[0];
        string entityName = ID[1];
        string spawn_function = "";
        string animation_root = "";
        AnimatorSettingsEntity entity = new();
        bool foundInList = animatorSettings.entities.TryGetValue(entityNamespace + ":" + entityName, out entity);
        if (entity.spawn_function != null)
        {
            spawn_function = entity.spawn_function;
            animation_root = entity.animation_root;
        }
        if (spawn_function == "" || !File.Exists(path + Regex.Replace(spawn_function,":([a-z0-9_-]+):","/data/$1/function/") + ".mcfunction") || force)
        {
            string title = "Select Spawn Function";
            spawn_function = EditorUtility.OpenFilePanel(title, path, "mcfunction");
            if (spawn_function.Contains(path)) spawn_function = Regex.Replace(spawn_function.Replace(path, "").Replace(".mcfunction", ""), "/data/([a-z0-9_-]+)/function/", ":$1:");
            else spawn_function = "";
        }
        if (animation_root == "" || !File.Exists(path + Regex.Replace(animation_root,":([a-z0-9_-]+):","/data/$1/function/") + ".mcfunction") || force)
        {
            string title = "Select Animation Root";
            animation_root = EditorUtility.OpenFilePanel(title, path, "mcfunction");
            if (animation_root.Contains(path)) animation_root = Regex.Replace(animation_root.Replace(path, "").Replace(".mcfunction", ""), "/data/([a-z0-9_-]+)/function/", ":$1:");
            else animation_root = "";
        }
        if (spawn_function != "" && animation_root != "") {
            entity.spawn_function = spawn_function;
            entity.animation_root = animation_root;
            selectedAnimationRoot = path + Regex.Replace(animation_root,":([a-z0-9_-]+):","/data/$1/function/") + ".mcfunction";
            if (!foundInList) animatorSettings.entities.Add(entityNamespace + ":" + entityName, entity);
            using (StreamWriter outputFile = new(Path.Combine(executionPath + "animator settings/" + selectedWorld + ".json")))
            {
                outputFile.WriteLine(JsonConvert.SerializeObject(animatorSettings, Formatting.Indented));
            }
            ReadSpawnFunction(spawn_function);
            ReadAnimationRoot();
        }
    }
    private void ReadSpawnFunction(string spawn_function)
    {
        MinecraftModel.ClearMemory();
        mapEntity.model_parts = new();
        foreach (string line in File.ReadLines(worldsPath + selectedWorldRoot + "/" + selectedWorld + "/datapacks/" + Regex.Replace(spawn_function,":([a-z0-9_-]+):","/data/$1/function/") + ".mcfunction"))
        {
            //TODO: read full entity data, regardless of formatting
            string item = "";
            if (line.Contains("summon minecraft:armor_stand"))
            {
                MapEntityPart entityPart = new();
                if (line.Contains("Tags:["))
                {
                    string tags = line.Split("Tags:[\"")[1].Split("\"]")[0];
                    entityPart.tags = tags.Split("\",\"");
                }
                if (line.Contains("equipment:{head:{"))
                {
                    int brackets = 1;
                    int characters = 1;
                    string cutLine = line.Split("equipment:{head:{")[1];
                    while (brackets != 0)
                    {
                        if (cutLine[0] == '{') brackets++;
                        else if (cutLine[0] == '}') brackets--;
                        characters++;
                        cutLine = cutLine.Substring(1);
                    }
                    cutLine = line.Split("equipment:{head:")[1];
                    item = cutLine.Substring(0, characters);
                }
                else if (line.Contains("equipment:{mainhand:{"))
                {
                    int brackets = 1;
                    int characters = 1;
                    string cutLine = line.Split("equipment:{mainhand:{")[1];
                    while (brackets != 0)
                    {
                        if (cutLine[0] == '{') brackets++;
                        else if (cutLine[0] == '}') brackets--;
                        characters++;
                        cutLine = cutLine.Substring(1);
                    }
                    cutLine = line.Split("equipment:{mainhand:")[1];
                    item = cutLine.Substring(0, characters);
                }
                if (item != "")
                {
                    MinecraftItem minecraftItem = new();
                    minecraftItem.Parse(item);
                    entityPart.minecraft_item = minecraftItem;
                }
                entityPart.ParseItemModel(worldsPath + selectedWorldRoot + "/" + selectedWorld + " Resource Pack/", executionPath + "animator settings/Minecraft Assets/");
                mapEntity.model_parts.Add(entityPart);
            }
            if (line.Contains("summon minecraft:item_display"))
            {
                MapEntityPart entityPart = new();
                item = "";
                if (line.Contains("Tags:["))
                {
                    string tags = line.Split("Tags:[\"")[1].Split("\"]")[0];
                    entityPart.tags = tags.Split("\",\"");
                }
                if (line.Contains("item:{"))
                {
                    int brackets = 1;
                    int characters = 1;
                    string cutLine = line.Split("item:{")[1];
                    while (brackets != 0)
                    {
                        if (cutLine[0] == '{') brackets++;
                        else if (cutLine[0] == '}') brackets--;
                        characters++;
                        cutLine = cutLine.Substring(1);
                    }
                    cutLine = line.Split("item:")[1];
                    MinecraftItem minecraftItem = new();
                    minecraftItem.Parse(cutLine.Substring(0, characters));
                    entityPart.minecraft_item = minecraftItem;
                }
                entityPart.ParseItemModel(worldsPath + selectedWorldRoot + "/" + selectedWorld + " Resource Pack/", executionPath + "animator settings/Minecraft Assets/");
                mapEntity.model_parts.Add(entityPart);
            }
        }
        MinecraftModel.ParseModels(worldsPath + selectedWorldRoot + "/" + selectedWorld + " Resource Pack/", executionPath + "animator settings/Minecraft Assets/");
        CreateModel();
    }
    private void ClearModel() {
        modelDisplay.GetComponent<ModelDisplay>().DeleteModel();
        selectedEntity = null;
        animationSelector.GetComponent<TMP_Dropdown>().interactable = false;
        animationSelector.GetComponent<TMP_Dropdown>().value = 0;
        changeFunctionButton.GetComponent<Button>().interactable = false;
    }
    private void CreateModel() {
        modelDisplay.GetComponent<ModelDisplay>().GenerateModels(mapEntity.model_parts);
        animationSelector.GetComponent<TMP_Dropdown>().interactable = true;
        animationSelector.GetComponent<TMP_Dropdown>().value = 0;
        changeFunctionButton.GetComponent<Button>().interactable = true;
    }
    private void ReadAnimationRoot()
    {
        foreach (string line in File.ReadLines(selectedAnimationRoot))
        {
            if (line.Contains("data modify storage ") && line.Contains(" set value "))
            {
                commandStorageValue = line.Split(" set value ")[1].Replace("Room:0,", "").Replace("mirror:{},", "").Replace("initial_animation_progress:0,", "").Replace("reset_rotation:0b,", "");
                commandStorageName = line.Split(" set value ")[0].Split("data modify storage ")[1];
            }
            if (line.Contains("function ") && line.Contains("/main"))
            {
                mainFunction = line.Split("function ")[1];
            }
        }
    }
    private void GetAnimations(string path, string animationNamespace, string animationID)
    {

    }
    public void AnimationSelected() {
        TMP_Dropdown dropdown = animationSelector.GetComponent<TMP_Dropdown>();
        if (dropdown.value == 0) {
            selectedAnimation = null;
        }
        if (dropdown.value == 1) {
            selectedAnimation = null;
            //Create new animation
        }
        else {
            selectedAnimation = dropdown.options[dropdown.value].text;
        }
    }
}
