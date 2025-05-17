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
    public GameObject addOnSelector;
    private string selectedAddOn;
    public GameObject animationGroupSelector;
    private string selectedAnimationGroup;
    private string[] entityID = {"","",""};
    private List<string> validEntities;
    private MapEntity mapEntity;
    public GameObject animationSelector;
    private string selectedAnimation;
    public GameObject changeFunctionButton;
    public GameObject modelDisplay;
    public List<OLDModelPart> parts;
    private bool singleModelObject = false;
    public GameObject entity;
    public GameObject templateModelPart;
    public GameObject templateTagToggle;
    public GameObject templateScoreToggle;
    public GameObject entityOffsetInput;
    public List<GameObject> modelParts;
    public List<string> enabledTags = new();
    public Dictionary<string,int> scoreValues = new();
    private List<GameObject> fieldToggles = new();
    private List<string> createdTagToggles = new();
    private List<string> createdScoreToggles = new();
    private GameObject selectedModelPart;
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
                    loading = "Getting animation groups...";
                    break;
                case "entitiesDone":
                    loading = "Getting animation groups...";
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
            foreach (KeyValuePair<string, string> world in worldMap) {
                if (world.Key == selectedWorld) {
                    selectedWorldRoot = world.Value;
                    break;
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
            validEntities = new();
            Thread threadedCloneAssets = new Thread(() => GetMinecraftAssets());
            threadedCloneAssets.Start();
            Thread threadedGetEntities = new Thread(() => GetEntities());
            threadedGetEntities.Start();
        }
        animationGroupSelector.GetComponent<TMP_Dropdown>().interactable = false;
        animationGroupSelector.GetComponent<TMP_Dropdown>().value = 0;
        animationSelector.GetComponent<TMP_Dropdown>().interactable = false;
        animationSelector.GetComponent<TMP_Dropdown>().value = 0;
        ClearModel();
    }

    private void GetEntities()
    {
        awaiting.Add("entities");
        List<string> newValidEntities = new();
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
                        newValidEntities.Add(animationNamespace + ":" + function.Replace(path2, "").Replace("\\main.mcfunction", "") + ":" + dataPack);
                    }
                    foreach (string function in callFunctions)
                    {
                        string trim = function.Replace(path2, "").Replace("\\call_part_function.mcfunction", "");
                        if (!newValidEntities.Contains(animationNamespace + ":" + trim + ":" + dataPack)) newValidEntities.Add(animationNamespace + ":" + trim + ":" + dataPack);
                    }
                }
            }
        }
        newValidEntities.Sort();
        validEntities = newValidEntities;
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
            animationGroupSelector.GetComponent<TMP_Dropdown>().interactable = false;
            animationGroupSelector.GetComponent<TMP_Dropdown>().value = 0;
        }
        else {
            selectedAddOn = dropdown.options[dropdown.value].text;
            TMP_Dropdown animationGroupDropdown = animationGroupSelector.GetComponent<TMP_Dropdown>();
            animationGroupDropdown.options.Clear();
            if (validEntities.Count != 0) {
                animationGroupSelector.GetComponent<TMP_Dropdown>().interactable = true;
                animationGroupDropdown.options.Add(new TMP_Dropdown.OptionData("Select animation group"));
                foreach (string folder in Directory.GetDirectories(worldsPath+selectedWorldRoot+"/"+selectedWorld+"/datapacks/" + selectedAddOn + "/data/")) {
                    string[] split = folder.Split("/");
                    string animationNamespace = split[split.Length-1];
                    string path = worldsPath+selectedWorldRoot+"/"+selectedWorld+"/datapacks/" + selectedAddOn + "/data/" + animationNamespace + "/function/animations/";
                    foreach (string entity in validEntities) {
                        string[] entityID = entity.Split(":");
                        if (Directory.Exists(path + entityID[1])) {
                            animationGroupSelector.GetComponent<TMP_Dropdown>().options.Add(new TMP_Dropdown.OptionData(entityID[0] + ":" + entityID[1]));
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
    public void AnimationGroupSelected() {
        TMP_Dropdown dropdown = animationGroupSelector.GetComponent<TMP_Dropdown>();
        if (dropdown.value == 0) ClearModel();
        else {
            selectedAnimationGroup = dropdown.options[dropdown.value].text;
            entityID = selectedAnimationGroup.Split(":");
            string entity = validEntities.Find(e => e.StartsWith(entityID[0] + ":" + entityID[1]));
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
        TMP_Dropdown dropdown = animationGroupSelector.GetComponent<TMP_Dropdown>();
        selectedAnimationGroup = dropdown.options[dropdown.value].text;
        entityID = selectedAnimationGroup.Split(":");
        string entity = validEntities.Find(e => e.StartsWith(entityID[0] + ":" + entityID[1]));
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
        foreach (KeyValuePair<string, string> function in animatorSettings.spawn_functions) {
            if (function.Key == entityNamespace + ":" + entityName) {
                spawn_function = function.Value;
                animatorSettings.spawn_functions.Remove(function.Key);
                break;
            }
        }
        if (spawn_function == "" || !File.Exists(path + spawn_function) || force) {
            string title = "Select Spawn Function";
            spawn_function = EditorUtility.OpenFilePanel(title, path, "mcfunction");
            if (spawn_function.Contains(path)) spawn_function = spawn_function.Replace(path,"");
            else spawn_function = "";
        }
        if (spawn_function != "") {
            animatorSettings.spawn_functions.Add(entityNamespace + ":" + entityName, spawn_function);
            using (StreamWriter outputFile = new(Path.Combine(executionPath + "animator settings/" + selectedWorld + ".json")))
            {
                outputFile.WriteLine(JsonConvert.SerializeObject(animatorSettings, Formatting.Indented));
            }
            ReadSpawnFunction(spawn_function);
        }
    }
    private void ReadSpawnFunction(string spawn_function)
    {
        MinecraftModel.ClearMemory();
        mapEntity = new();
        foreach (string line in File.ReadLines(worldsPath + selectedWorldRoot + "/" + selectedWorld + "/datapacks/" + spawn_function))
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
        SetSelectedModelPart(null);
        ClearFieldToggles();
        selectedAnimationGroup = null;
        animationSelector.GetComponent<TMP_Dropdown>().interactable = false;
        animationSelector.GetComponent<TMP_Dropdown>().value = 0;
        changeFunctionButton.GetComponent<Button>().interactable = false;
    }
    private void CreateModel() {
        modelDisplay.GetComponent<ModelDisplay>().GenerateModels(mapEntity.model_parts);
        SetSelectedModelPart(null);
        ClearFieldToggles();
        animationSelector.GetComponent<TMP_Dropdown>().interactable = true;
        animationSelector.GetComponent<TMP_Dropdown>().value = 0;
        changeFunctionButton.GetComponent<Button>().interactable = true;
    }
    private void GetListOfParts(string path, string[] ID) {
        ClearModel();
        if (File.Exists(path)) {
            singleModelObject = false;
            foreach (string line in File.ReadLines(path)) {
                if (line.Contains("/") && line.Contains(" with")) {
                    string[] splitLine = line.Split("/");
                    string[] fullySplitLine = splitLine[splitLine.Length-1].Split(" with");
                    parts.Add(new OLDModelPart(fullySplitLine[0]));
                }
            }
        }
        else {
            singleModelObject = true;
            parts.Add(new OLDModelPart(""));
        }
        OLDSavedEntities savedEntities = new();
        if (File.Exists(executionPath + "animator settings/models/" + selectedWorld + ".json")) {
            savedEntities = JsonConvert.DeserializeObject<OLDSavedEntities>(File.ReadAllText(executionPath + "animator settings/models/" + selectedWorld + ".json"));
        }
        bool success = false;
        string entityNamespace = ID[0];
        string entityName = ID[1];
        OLDSavedEntity savedEntity = new(entityNamespace + ":" + entityName, parts);
        foreach (OLDSavedEntity realSavedEntity in savedEntities.entities) {
            if (realSavedEntity.name == ID[0] + ":" + ID[1]) {
                parts = realSavedEntity.parts;
                savedEntity.parts = parts;
                success = true;
                break;
            }
        }
        bool cancel = false;
        if (!success) {
            List<string> partQueue = new();
            foreach (OLDModelPart part in parts) {
                partQueue.Add(part.GetName());
            }
            List<string> selectedFiles = null;//SelectModelFiles(partQueue);
            if (selectedFiles.Count != 0) {
                foreach (OLDModelPart part in parts) {
                    string[] model = {selectedFiles[0]};
                    part.SetModel(model);
                    selectedFiles.RemoveAt(0);
                }
                savedEntities.entities.Add(savedEntity);
                using (StreamWriter outputFile = new(Path.Combine(executionPath + "animator settings/models/" + selectedWorld + ".json")))
                {
                    outputFile.WriteLine(JsonConvert.SerializeObject(savedEntities, Formatting.Indented));
                }
            }
            else {
                cancel = true;
                animationGroupSelector.GetComponent<TMP_Dropdown>().value = 0;
                animationSelector.GetComponent<TMP_Dropdown>().interactable = false;
                changeFunctionButton.GetComponent<Button>().interactable = false;
            }
        }
        if (!cancel) {
            foreach (OLDModelPart part in savedEntity.parts) {
                if (!singleModelObject) {
                    string newpath = path.Replace("call_part_function",part.GetName());
                    if (File.Exists(newpath)) {
                        foreach (string line in File.ReadLines(newpath)) {
                            if (line.Contains("teleport @s") && line.Contains("^")) {
                                string[] splitLine = line.Split("teleport @s");
                                string scores = "";
                                if (splitLine[0].Contains("if score") || splitLine[0].Contains("unless score")) scores = GetScores(splitLine[0]);
                                string[] split = splitLine[splitLine.Length-1].Split(" ~");
                                string[] offsets = split[0].Split(" ^");
                                offsets[0] = offsets[0].Replace("[","").Replace("]","").Replace(" ","");
                                if (offsets[0] == " ") offsets[0] = "";
                                if (offsets[1].Length == 0) offsets[1] = "0";
                                if (offsets[2].Length == 0) offsets[2] = "0";
                                if (offsets[3].Length == 0) offsets[3] = "0";
                                float[] offsetfloats = {(float)Convert.ToDouble(offsets[1]),(float)Convert.ToDouble(offsets[2]),-(float)Convert.ToDouble(offsets[3])};
                                part.addConditionalOffset(offsets[0],scores,offsetfloats);
                                if (offsets[0] != "") CreateTagToggles(offsets[0]);
                                if (scores != "") CreateScoreToggles(scores);
                            }
                            else if (line.Contains("data modify entity @s") && line.Contains("Pose.Head")) {
                                string[] splitLine = line.Split("data modify entity @s");
                                string scores = "";
                                if (splitLine[0].Contains("if score") || splitLine[0].Contains("unless score")) scores = GetScores(splitLine[0]);
                                string[] split = splitLine[splitLine.Length-1].Split(" Pose.Head");
                                string[] strings = split[1].Replace("[","").Replace("]","").Split(" set value ");
                                split[0] = split[0].Replace("[","").Replace("]","").Replace(" ","");
                                strings[1] = strings[1].Replace("f","");
                                if (strings[0] != "") part.addConditionalPose(split[0], scores,Convert.ToSingle(strings[1]), Convert.ToInt32(strings[0]));
                                else {
                                    float[] defaultPose = {0,0,0};
                                    strings = strings[1].Replace("f","").Replace("[","").Replace("f]","").Split(",");
                                    defaultPose[0] = Convert.ToSingle(strings[0]);
                                    defaultPose[1] = Convert.ToSingle(strings[1]);
                                    defaultPose[2] = Convert.ToSingle(strings[2]);
                                    part.addConditionalPose(split[0], scores,defaultPose);
                                }
                                if (split[0] != "") CreateTagToggles(split[0]);
                                if (scores != "") CreateScoreToggles(scores);
                            }
                            else if (line.Contains("data modify entity @s") && line.Contains("model_data.")) {
                                string[] splitLine = line.Split("data modify entity @s");
                                string scores = "";
                                if (splitLine[0].Contains("if score") || splitLine[0].Contains("unless score")) scores = GetScores(splitLine[0]);
                                string[] split = {};
                                if (line.Contains(" ArmorItems[3]")) split = splitLine[splitLine.Length-1].Split(" ArmorItems[3]");
                                else if (line.Contains(" Item")) split = splitLine[splitLine.Length-1].Split(" Item");
                                split[0] = split[0].Replace("[","").Replace("]","").Replace(" ","");
                                string modelPiece = split[split.Length-1].Split("model_data.")[1];
                                part.addConditionalModelVariant(split[0],scores,modelPiece);
                                if (split[0] != "") CreateTagToggles(split[0]);
                                if (scores != "") CreateScoreToggles(scores);
                                if (!part.VariantExists(modelPiece)) {
                                    string selectedFile = null;//SelectModelFile(part.GetName(), modelPiece);
                                    if (selectedFile != "") {
                                        string[] model = {selectedFile};
                                        part.SetModel(model, modelPiece);
                                    }
                                    else {
                                        cancel = true;
                                        break;
                                    }
                                }
                            }
                        }
                            
                    }
                }
                if (!cancel) {
                    GameObject modelPart = Instantiate(templateModelPart, new Vector3(0,0,0), new Quaternion(), entity.transform.GetChild(1));
                    modelPart.SetActive(true);
                    modelPart.name = part.GetName();
                    if (modelPart.name == "") modelPart.name = "SingleModelObject";
                    modelPart.GetComponent<OLDModelDisplay>().SetOffsets(part.GetOffsets());
                    modelPart.GetComponent<OLDModelDisplay>().SetPoses(part.GetPoses());
                    modelPart.GetComponent<OLDModelDisplay>().SetVariants(part.GetVariants());
                    modelPart.GetComponent<OLDModelDisplay>().SetModel(part.variants, part.model, "default");
                    modelParts.Add(modelPart);
                }
                else {
                    animationGroupSelector.GetComponent<TMP_Dropdown>().value = 0;
                    animationSelector.GetComponent<TMP_Dropdown>().interactable = false;
                    changeFunctionButton.GetComponent<Button>().interactable = false;
                    break;
                }
            }
            if (!cancel) {
                ToggleTag();
                SaveModels();
            }
        }
    }
    private string GetScores(string execute) {
        string scores = "";
        string newexecute = execute.Replace("execute","").Replace(" run ","");
        for (int i = 0; i < newexecute.Length; i++) {
            if (i >= 9 && newexecute[(i-9)..i] == " if score ") {
                string[] newscore = newexecute[(i+1)..].Split(" ");
                scores = scores + "|if " + newscore[0] + " " + newscore[1];
            }
            else if (i >= 13 && newexecute[(i-13)..i] == " unless score ") {
                string[] newscore = newexecute[(i+1)..].Split(" ");
                scores = scores + "|unless " + newscore[0] + " " + newscore[1];
            }
        }
        if (scores.StartsWith("|")) scores = scores[1..];
        return scores;
    }
    private void ClearFieldToggles() {
        foreach (GameObject toggle in fieldToggles) {
            Destroy(toggle);
        }
        fieldToggles.Clear();
        enabledTags.Clear();
        scoreValues.Clear();
        createdTagToggles.Clear();
        createdScoreToggles.Clear();
    }
    private void CreateTagToggles(string conditions) {
        string[] split = conditions.Replace("!","").Replace("tag=","").Split(",");
        foreach (string con in split) {
            if (!createdTagToggles.Contains(con) && !con.StartsWith("was_")) {
                GameObject toggle = Instantiate(templateTagToggle, templateTagToggle.transform.position, new Quaternion(), templateTagToggle.transform.parent);
                toggle.SetActive(true);
                createdTagToggles.Add(con);
                toggle.transform.GetChild(1).gameObject.GetComponent<Text>().text = con;
                fieldToggles.Add(toggle);
            }
        }
    }
    
    private void CreateScoreToggles(string conditions) {
        string[] split = conditions.Split("|");
        foreach (string con in split) {
            if (!createdScoreToggles.Contains(con)) {
                GameObject toggle = Instantiate(templateScoreToggle, templateScoreToggle.transform.position, new Quaternion(), templateScoreToggle.transform.parent);
                toggle.SetActive(true);
                createdScoreToggles.Add(con);
                string[] score = con.Split(" ");
                toggle.transform.GetChild(1).gameObject.GetComponent<Text>().text = score[score.Length-2] + " " + score[score.Length-1];
                fieldToggles.Add(toggle);
            }
        }
    }
    public void ToggleTag() {
        enabledTags.Clear();
        foreach (GameObject toggle in fieldToggles) {
            if (toggle.GetComponent<Toggle>() != null && toggle.GetComponent<Toggle>().isOn) enabledTags.Add(toggle.transform.GetChild(1).gameObject.GetComponent<Text>().text);
        }
        foreach (GameObject modelPart in modelParts) {
            modelPart.GetComponent<OLDModelDisplay>().GetState();
        }
    }
    private void GetAnimations(string path, string animationNamespace, string animationID) {
        //TODO: If there's sub-animations, check if root-animations call their functions, if so, ignore
        if (singleModelObject) {
            foreach (string folder in Directory.GetDirectories(path + animationID + "/")) {
                string[] split = folder.Split("/");
                string animationName = animationID + "/" + split[split.Length-1];
                foreach (string file in Directory.GetFiles(path + animationID + "/")) {
                    string[] split2 = file.Split("/");
                    //Detect sub-entity group; their animations should not be included in this entity group's animation list
                    if (split2[split2.Length-1] == "call_part_function.mcfunction") {
                        return;
                    }
                    else if (split2[split2.Length-1] == "main.mcfunction") {
                        return;
                    }
                }
                GetAnimations(path, animationNamespace, animationName);
            }
            foreach (string file in Directory.GetFiles(path + animationID + "/")) {
                string[] split = file.Split("/");
                string[] fullySplit = split[split.Length-1].Split(".");
                animationSelector.GetComponent<TMP_Dropdown>().options.Add(new TMP_Dropdown.OptionData(animationNamespace + ":" +animationID + "/" + fullySplit[0]));
            }
        }
        else {
            foreach (string file in Directory.GetFiles(path + animationID + "/")) {
                animationSelector.GetComponent<TMP_Dropdown>().options.Add(new TMP_Dropdown.OptionData(animationNamespace + ":" +animationID));
                break;
            }
            foreach (string folder in Directory.GetDirectories(path + animationID + "/")) {
                string[] split = folder.Split("/");
                string animationName = animationID + "/" + split[split.Length-1];
                GetAnimations(path, animationNamespace, animationName);
            }
        }
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

    private void SaveModels() {
        OLDSavedEntities savedEntities = new();
        if (File.Exists(executionPath + "animator settings/models/" + selectedWorld + ".json")) {
            savedEntities = JsonConvert.DeserializeObject<OLDSavedEntities>(File.ReadAllText(executionPath + "animator settings/models/" + selectedWorld + ".json"));
        }
        bool success = false;
        foreach (OLDSavedEntity savedEntity in savedEntities.entities) {
            if (savedEntity.name == entityID[0] + ":" + entityID[1]) {
                savedEntity.parts = parts;
                success = true;
                break;
            }
        }
        if (!success) {
            OLDSavedEntity savedEntity = new(entityID, parts);
            savedEntities.entities.Add(savedEntity);
        }
        using (StreamWriter outputFile = new(Path.Combine(executionPath + "animator settings/models/" + selectedWorld + ".json")))
        {
            outputFile.WriteLine(JsonConvert.SerializeObject(savedEntities,Formatting.Indented));
        }
    }
    public void SetSelectedModelPart(GameObject selectedModelPart) {
        if (this.selectedModelPart != selectedModelPart) {
            if (this.selectedModelPart != null) this.selectedModelPart.GetComponent<OLDModelDisplay>().UnHighlight();
            if (selectedModelPart != null) selectedModelPart.GetComponent<OLDModelDisplay>().Highlight();
            this.selectedModelPart = selectedModelPart;
        }
        if (selectedModelPart != null) {
            //Show left menu
        }
    }

    public void SetModelOffset() {
        try {
            entity.transform.localPosition = new(0,Convert.ToSingle(entityOffsetInput.GetComponent<TMP_InputField>().text),0);
        }
        catch {
            entity.transform.localPosition = new(0,0,0);
        }
    }
    public void SetModelOffsetEnd() {
        try {
            entity.transform.localPosition = new(0,Convert.ToSingle(entityOffsetInput.GetComponent<TMP_InputField>().text),0);
        }
        catch {
            entityOffsetInput.GetComponent<TMP_InputField>().text = "0";
            entity.transform.localPosition = new(0,0,0);
        }
    }
}
