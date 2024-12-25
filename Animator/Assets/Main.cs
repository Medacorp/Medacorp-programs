using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.Text.RegularExpressions;


public class Main : MonoBehaviour
{
    private string executionPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
    private string worldsPath;
    public GameObject worldSelector;
    private string selectedWorld;
    public GameObject addOnSelector;
    private string selectedAddOn;
    public GameObject animationGroupSelector;
    private string selectedAnimationGroup;
    private string[] entityID = {"",""};
    private List<string> validEntities;
    public GameObject animationSelector;
    private string selectedAnimation;
    public GameObject editModelsButton;
    public GameObject editModelsTab;
    public GameObject editModelsPartSelector;
    private string editModelPart;
    public GameObject editModelsVariantSelector;
    private string editModelVariant;
    public GameObject editModelsChangeButton;
    public GameObject editModelsDeleteButton;
    public GameObject editModelsCompositeSelector;
    private int editModelCompositeIndex;
    public GameObject editModelsSelectedText;
    private List<ModelPart> parts;
    private bool singleModelObject = false;
    public GameObject variantNamePopUp;
    public GameObject variantNamePopUpInput;
    public GameObject variantNamePopUpError;
    public GameObject variantNamePopUpButton;
    public GameObject variantNamePopUpButtonText;
    public GameObject entity;
    public GameObject templateModelPart;
    public GameObject templateTagToggle;
    public GameObject entityOffsetInput;
    public List<GameObject> modelParts;
    public List<string> enabledTags = new();
    private List<GameObject> tagToggles = new();
    private List<string> createdTagToggles = new();
    private GameObject selectedModelPart;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    void Start()
    {
        executionPath = executionPath.Remove(executionPath.Length-53);
        if (!Directory.Exists(executionPath + "animator settings/")) {
            Directory.CreateDirectory(executionPath + "animator settings/");
        }
        if (!Directory.Exists(executionPath + "animator settings/models/")) {
            Directory.CreateDirectory(executionPath + "animator settings/models/");
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
            if (Directory.Exists(path + world)) {
                dropdown.options.Add(new TMP_Dropdown.OptionData(world));
            }
        } 
        dropdown.value = 0;
        dropdown.RefreshShownValue();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void WorldSelected() {
        TMP_Dropdown dropdown = worldSelector.GetComponent<TMP_Dropdown>();
        if (dropdown.value == 0) {
            selectedWorld = null;
            addOnSelector.GetComponent<TMP_Dropdown>().interactable = false;
            addOnSelector.GetComponent<TMP_Dropdown>().value = 0;
        }
        else {
            selectedWorld = dropdown.options[dropdown.value].text;
            addOnSelector.GetComponent<TMP_Dropdown>().interactable = true;
            TMP_Dropdown addOnDropdown = addOnSelector.GetComponent<TMP_Dropdown>();
            addOnDropdown.options.Clear();
            addOnDropdown.options.Add(new TMP_Dropdown.OptionData("Select add-on"));
            foreach (string folder in Directory.GetDirectories(worldsPath+selectedWorld+"/"+selectedWorld+"/datapacks/")) {
                string[] split = folder.Split("/");
                string addOn = split[split.Length-1];
                string path = worldsPath+selectedWorld+"/"+selectedWorld+"/datapacks/" + addOn + "/";
                if (Directory.Exists(path + "data")) {
                    addOnDropdown.options.Add(new TMP_Dropdown.OptionData(addOn));
                }
            } 
            addOnDropdown.value = 0;
            addOnDropdown.RefreshShownValue();
            validEntities = new();
            foreach (string addOn in Directory.GetDirectories(worldsPath+selectedWorld+"/"+selectedWorld+"/datapacks/")) {
                string[] split = addOn.Split("/");
                string dataPack = split[split.Length-1];
                string path = worldsPath+selectedWorld+"/"+selectedWorld+"/datapacks/" + dataPack + "/data/";
                foreach (string folder in Directory.GetDirectories(path)) {
                    string[] split2 = folder.Split("/");
                    string animationNamespace = split2[split2.Length-1];
                    string path2 = path + animationNamespace + "/function/animations/";
                    if (Directory.Exists(path2)) {
                        string[] mainFunctions = Directory.GetFiles(path2, "main.mcfunction", SearchOption.AllDirectories);
                        string[] callFunctions = Directory.GetFiles(path2, "call_part_function.mcfunction", SearchOption.AllDirectories);
                        foreach (string function in mainFunctions) {
                            validEntities.Add(animationNamespace + ":" + function.Replace(path2,"").Replace("\\main.mcfunction","") + ":" + dataPack);
                        }
                        foreach (string function in callFunctions) {
                            string trim = function.Replace(path2,"").Replace("\\call_part_function.mcfunction","");
                            if (!validEntities.Contains(animationNamespace + ":" + trim + ":" + dataPack))validEntities.Add(animationNamespace + ":" + trim + ":" + dataPack);
                        }
                    }
                }
            }
            validEntities.Sort();
        }
        animationGroupSelector.GetComponent<TMP_Dropdown>().interactable = false;
        animationGroupSelector.GetComponent<TMP_Dropdown>().value = 0;
        animationSelector.GetComponent<TMP_Dropdown>().interactable = false;
        animationSelector.GetComponent<TMP_Dropdown>().value = 0;
        parts = new();
        SetSelectedModelPart(null);
        ClearTagToggles();
        editModelsButton.GetComponent<Button>().interactable = false;
        editModelsTab.SetActive(false);
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
            animationGroupSelector.GetComponent<TMP_Dropdown>().interactable = true;
            TMP_Dropdown animationGroupDropdown = animationGroupSelector.GetComponent<TMP_Dropdown>();
            animationGroupDropdown.options.Clear();
            animationGroupDropdown.options.Add(new TMP_Dropdown.OptionData("Select animation group"));
                        
            foreach (string folder in Directory.GetDirectories(worldsPath+selectedWorld+"/"+selectedWorld+"/datapacks/" + selectedAddOn + "/data/")) {
                string[] split = folder.Split("/");
                string animationNamespace = split[split.Length-1];
                string path = worldsPath+selectedWorld+"/"+selectedWorld+"/datapacks/" + selectedAddOn + "/data/" + animationNamespace + "/function/animations/";
                foreach (string entity in validEntities) {
                    string[] entityID = entity.Split(":");
                    if (Directory.Exists(path + entityID[1])) {
                        animationGroupSelector.GetComponent<TMP_Dropdown>().options.Add(new TMP_Dropdown.OptionData(entityID[0] + ":" + entityID[1]));
                    }
                }
            }
            animationGroupDropdown.value = 0;
            animationGroupDropdown.RefreshShownValue();
        }
        animationSelector.GetComponent<TMP_Dropdown>().interactable = false;
        animationSelector.GetComponent<TMP_Dropdown>().value = 0;
        parts = new();
        SetSelectedModelPart(null);
        ClearTagToggles();
        editModelsButton.GetComponent<Button>().interactable = false;
        editModelsTab.SetActive(false);
    }
    public void AnimationGroupSelected() {
        TMP_Dropdown dropdown = animationGroupSelector.GetComponent<TMP_Dropdown>();
        if (dropdown.value == 0) {
            selectedAnimationGroup = null;
            animationSelector.GetComponent<TMP_Dropdown>().interactable = false;
            animationSelector.GetComponent<TMP_Dropdown>().value = 0;
            parts = new();
            SetSelectedModelPart(null);
            ClearTagToggles();
            editModelsButton.GetComponent<Button>().interactable = false;
        }
        else {
            selectedAnimationGroup = dropdown.options[dropdown.value].text;
            entityID = selectedAnimationGroup.Split(":");
            string entity = validEntities.Find(e => e.StartsWith(entityID[0] + ":" + entityID[1]));
            entityID = entity.Split(":");
            animationSelector.GetComponent<TMP_Dropdown>().interactable = true;
            editModelsButton.GetComponent<Button>().interactable = true;
            GetListOfParts(worldsPath+selectedWorld+"/"+selectedWorld+"/datapacks/" + entityID[2] + "/data/" + entityID[0] + "/function/animations/" + entityID[1] + "/call_part_function.mcfunction",entityID);
            TMP_Dropdown animationDropdown = animationSelector.GetComponent<TMP_Dropdown>();
            animationDropdown.options.Clear();
            animationDropdown.options.Add(new TMP_Dropdown.OptionData("Select animation"));
            animationDropdown.options.Add(new TMP_Dropdown.OptionData("New animation"));
            foreach (string addOnPath in Directory.GetDirectories(worldsPath+selectedWorld+"/"+selectedWorld+"/datapacks/" + selectedAddOn + "/data/")) {
                string[] split = addOnPath.Split("/");
                string addOnNamespace = split[split.Length-1];
                string path = worldsPath+selectedWorld+"/"+selectedWorld+"/datapacks/" + selectedAddOn + "/data/" + addOnNamespace + "/function/animations/" + entityID[1] + "/";
                if (Directory.Exists(path)) {
                    foreach (string folder in Directory.GetDirectories(path)) {
                        string[] split2 = folder.Split("/");
                        string animationName = split2[split2.Length-1];
                        GetAnimations(path, addOnNamespace, animationName);
                    }
                }
            }
            animationDropdown.value = 0;
            animationDropdown.RefreshShownValue();
        }
        editModelsTab.SetActive(false);
    }
    private void GetListOfParts(string path, string[] ID) {
        parts = new();
        SetSelectedModelPart(null);
        ClearTagToggles();
        if (File.Exists(path)) {
            singleModelObject = false;
            foreach (string line in File.ReadLines(path)) {
                if (line.Contains("/") && line.Contains(" with")) {
                    string[] splitLine = line.Split("/");
                    string[] fullySplitLine = splitLine[splitLine.Length-1].Split(" with");
                    parts.Add(new ModelPart(fullySplitLine[0]));
                }
            }
        }
        else {
            singleModelObject = true;
            parts.Add(new ModelPart(""));
        }
        SavedEntities savedEntities = new();
        if (File.Exists(executionPath + "animator settings/models/" + selectedWorld + ".json")) {
            savedEntities = JsonConvert.DeserializeObject<SavedEntities>(File.ReadAllText(executionPath + "animator settings/models/" + selectedWorld + ".json"));
        }
        bool success = false;
        string entityNamespace = ID[0];
        string entityName = ID[1];
        SavedEntity savedEntity = new(entityNamespace + ":" + entityName, parts);
        foreach (SavedEntity realSavedEntity in savedEntities.entities) {
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
            foreach (ModelPart part in parts) {
                partQueue.Add(part.GetName());
            }
            List<string> selectedFiles = SelectModelFiles(partQueue);
            if (selectedFiles.Count != 0) {
                foreach (ModelPart part in parts) {
                    string[] model = {selectedFiles[0]};
                    part.SetModel(model);
                    selectedFiles.RemoveAt(0);
                }
                savedEntities.entities.Add(savedEntity);
                using (StreamWriter outputFile = new(Path.Combine(executionPath + "animator settings/models/" + selectedWorld + ".json")))
                {
                    outputFile.WriteLine(JsonConvert.SerializeObject(savedEntities));
                }
            }
            else {
                cancel = true;
                animationGroupSelector.GetComponent<TMP_Dropdown>().value = 0;
                animationSelector.GetComponent<TMP_Dropdown>().interactable = false;
                editModelsButton.GetComponent<Button>().interactable = false;
            }
        }
        if (!cancel) {
            if (modelParts == null) modelParts = new();
            if (modelParts.Count != 0) {
                foreach (GameObject modelPart in modelParts) {
                    Destroy(modelPart);
                }
                modelParts.Clear();
            }
            foreach (ModelPart part in savedEntity.parts) {
                if (!singleModelObject) {
                    string newpath = path.Replace("call_part_function",part.GetName());
                    if (File.Exists(newpath)) {
                        foreach (string line in File.ReadLines(newpath)) {
                            if (line.Contains("teleport @s") && line.Contains("^")) {
                                string[] splitLine = line.Split("teleport @s");
                                splitLine = splitLine[splitLine.Length-1].Split(" ~");
                                string[] offsets = splitLine[0].Split(" ^");
                                offsets[0] = offsets[0].Replace("[","").Replace("]","").Replace(" ","");
                                if (offsets[0] == " ") offsets[0] = "";
                                if (offsets[1].Length == 0) offsets[1] = "0";
                                if (offsets[2].Length == 0) offsets[2] = "0";
                                if (offsets[3].Length == 0) offsets[3] = "0";
                                float[] offsetfloats = {(float)Convert.ToDouble(offsets[1]),(float)Convert.ToDouble(offsets[2]),-(float)Convert.ToDouble(offsets[3])};
                                part.addConditionalOffset(offsets[0],offsetfloats);
                                if (offsets[0] != "") CreateTagToggles(offsets[0]);
                            }
                            else if (line.Contains("data modify entity @s") && line.Contains("Pose.Head")) {
                                string[] splitLine = line.Split("data modify entity @s");
                                splitLine = splitLine[splitLine.Length-1].Split(" Pose.Head");
                                string[] strings = splitLine[1].Replace("[","").Replace("]","").Split(" set value ");
                                splitLine[0] = splitLine[0].Replace("[","").Replace("]","").Replace(" ","");
                                strings[1] = strings[1].Replace("f","");
                                if (strings[0] != "") part.addConditionalPose(splitLine[0], Convert.ToSingle(strings[1]), Convert.ToInt32(strings[0]));
                                else {
                                    float[] defaultPose = {0,0,0};
                                    strings = strings[1].Replace("f","").Replace("[","").Replace("f]","").Split(",");
                                    defaultPose[0] = Convert.ToSingle(strings[0]);
                                    defaultPose[1] = Convert.ToSingle(strings[1]);
                                    defaultPose[2] = Convert.ToSingle(strings[2]);
                                    part.addConditionalPose(splitLine[0], defaultPose);
                                }
                                if (splitLine[0] != "") CreateTagToggles(splitLine[0]);
                            }
                        }
                            
                    }
                }
                GameObject modelPart = Instantiate(templateModelPart, new Vector3(0,0,0), new Quaternion(), entity.transform.GetChild(1));
                modelPart.SetActive(true);
                modelPart.name = part.GetName();
                if (modelPart.name == "") modelPart.name = "SingleModelObject";
                modelPart.GetComponent<ModelDisplay>().SetOffsets(part.GetOffsets());
                modelPart.GetComponent<ModelDisplay>().SetPoses(part.GetPoses());
                modelPart.GetComponent<ModelDisplay>().GetState();
                modelPart.GetComponent<ModelDisplay>().SetModel(part.GetMinecraftModel("default"));
                modelParts.Add(modelPart);
            }
        }
    }
    private void ClearTagToggles() {
        foreach (GameObject toggle in tagToggles) {
            Destroy(toggle);
        }
        tagToggles.Clear();
        enabledTags.Clear();
        createdTagToggles.Clear();
    }
    private void CreateTagToggles(string conditions) {
        string[] split = conditions.Replace("!","").Replace("tag=","").Split(",");
        foreach (string con in split) {
            if (!createdTagToggles.Contains(con) && !con.StartsWith("was_")) {
                GameObject toggle = Instantiate(templateTagToggle, templateTagToggle.transform.position, new Quaternion(), templateTagToggle.transform.parent);
                toggle.transform.localPosition = toggle.transform.localPosition + new Vector3(0,-25 * tagToggles.Count,0);
                toggle.SetActive(true);
                createdTagToggles.Add(con);
                toggle.transform.GetChild(1).gameObject.GetComponent<Text>().text = con;
                tagToggles.Add(toggle);
            }
        }
    }
    public void ToggleTag() {
        enabledTags.Clear();
        foreach (GameObject toggle in tagToggles) {
            if (toggle.GetComponent<Toggle>().isOn) enabledTags.Add(toggle.transform.GetChild(1).gameObject.GetComponent<Text>().text);
        }
        foreach (GameObject modelPart in modelParts) {
            modelPart.GetComponent<ModelDisplay>().GetState();
        }
    }
    private void GetAnimations(string path, string animationNamespace, string animationID) {
        if (singleModelObject) {
            foreach (string folder in Directory.GetDirectories(path + animationID + "/")) {
                string[] split = folder.Split("/");
                string animationName = animationID + "/" + split[split.Length-1];
                foreach (string file in Directory.GetFiles(path + animationID + "/")) {
                    string[] split2 = file.Split("/");
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
        SavedEntities savedEntities = new();
        if (File.Exists(executionPath + "animator settings/models/" + selectedWorld + ".json")) {
            savedEntities = JsonConvert.DeserializeObject<SavedEntities>(File.ReadAllText(executionPath + "animator settings/models/" + selectedWorld + ".json"));
        }
        bool success = false;
        foreach (SavedEntity savedEntity in savedEntities.entities) {
            if (savedEntity.name == entityID[0] + ":" + entityID[1]) {
                savedEntity.parts = parts;
                success = true;
                break;
            }
        }
        if (!success) {
            SavedEntity savedEntity = new(entityID, parts);
            savedEntities.entities.Add(savedEntity);
        }
        using (StreamWriter outputFile = new(Path.Combine(executionPath + "animator settings/models/" + selectedWorld + ".json")))
        {
            outputFile.WriteLine(JsonConvert.SerializeObject(savedEntities,Formatting.Indented));
        }
    }

    private string SelectModelFile(string part, bool variant) {
        string modelDirectory = worldsPath+selectedWorld+"/";
        if (Directory.Exists(modelDirectory+selectedWorld+" Resource Pack/assets/")) {
            modelDirectory = modelDirectory+selectedWorld+" Resource Pack/assets/";
            if (Directory.Exists(modelDirectory+entityID[0]+"/models")) modelDirectory = modelDirectory+entityID[0]+"/models";
        }
        string selectedFile;
        string title = "Select Model File";
        if (variant && part == "") title = "Select Model Variant File";
        else if (!variant && part != "") title = "Select Model File For Part \"" + editModelPart + "\"";
        else if (variant && part != "") title = "Select Model Variant File For Part \"" + editModelPart + "\"";
        //while (true) {
            selectedFile = EditorUtility.OpenFilePanel(title, modelDirectory, "json");
            if (selectedFile == "") {
                return selectedFile;
            }
            string[] filePath = selectedFile.Split("/");
            string file = filePath[filePath.Length-1];
            modelDirectory = selectedFile.Remove(selectedFile.Length-file.Length);
            if (selectedFile.Contains("/models/")) return selectedFile;
            throw new Exception();
        //}
    }

    private List<string> SelectModelFiles(List<string> parts) {
        List<string> files = new();
        string modelDirectory = worldsPath+selectedWorld+"/";
        if (Directory.Exists(modelDirectory+selectedWorld+" Resource Pack/assets/")) {
            modelDirectory = modelDirectory+selectedWorld+" Resource Pack/assets/";
            if (Directory.Exists(modelDirectory+entityID[0]+"/models")) modelDirectory = modelDirectory+entityID[0]+"/models";
        }
        string selectedFile;
        foreach (string part in parts) {
            string title = "Select Model File";
            if (part != "") title = "Select Model File For Part \"" + part + "\"";
            while (true) {
                selectedFile = EditorUtility.OpenFilePanel(title, modelDirectory, "json");
                if (selectedFile == "") {
                    break;
                }
                string[] filePath = selectedFile.Split("/");
                string file = filePath[filePath.Length-1];
                modelDirectory = selectedFile.Remove(selectedFile.Length-file.Length);
                if (selectedFile.Contains("/models/")) break;
            }
            if (selectedFile != "") {
                files.Add(selectedFile);
            }
            else {
                files.Clear();
                break;
            }
        }
        return files;
    }

    public void PressEditModelsButton() {
        if (editModelsTab.activeSelf) editModelsTab.SetActive(false);
        else {
            editModelsTab.SetActive(true);
            TMP_Dropdown dropdown = editModelsPartSelector.GetComponent<TMP_Dropdown>();
            editModelPart = null;
            dropdown.value = 0;
            dropdown.options.Clear();
            TMP_Dropdown variantDropdown = editModelsVariantSelector.GetComponent<TMP_Dropdown>();
            editModelVariant = "default";
            variantDropdown.value = 1;
            variantDropdown.options.Clear();
            variantDropdown.options.Add(new TMP_Dropdown.OptionData("Add model variant"));
            TMP_Dropdown compositionDropdown = editModelsCompositeSelector.GetComponent<TMP_Dropdown>();
            editModelCompositeIndex = 0;
            compositionDropdown.value = 1;
            compositionDropdown.options.Clear();
            compositionDropdown.options.Add(new TMP_Dropdown.OptionData("Add another model"));
            if (singleModelObject) {
                dropdown.interactable = false;
                dropdown.options.Add(new TMP_Dropdown.OptionData("Single part model"));
                variantDropdown.options.Add(new TMP_Dropdown.OptionData("Default"));
                editModelPart = "";
                foreach (ModelPart part in parts) {
                    if (part.GetName() == editModelPart) {
                        if (part.variants != null) {
                            foreach (KeyValuePair<string,string[]> variant in part.variants) {
                                variantDropdown.options.Add(new TMP_Dropdown.OptionData(variant.Key));
                            }
                        }
                        for (int i = 0; i < part.GetModel().Length; i++) {
                            compositionDropdown.options.Add(new TMP_Dropdown.OptionData(i.ToString()));
                        }
                        if (compositionDropdown.options.Count == 1) compositionDropdown.options.Add(new TMP_Dropdown.OptionData("No models specified"));
                        break;
                    }
                }
                editModelCompositeIndex = 0;
                editModelsVariantSelector.GetComponent<TMP_Dropdown>().value = 1;
                editModelsCompositeSelector.GetComponent<TMP_Dropdown>().value = 1;
                editModelsChangeButton.GetComponent<Button>().interactable = true;
                editModelsDeleteButton.GetComponent<Button>().interactable = true;
                editModelsVariantSelector.GetComponent<TMP_Dropdown>().interactable = true;
                editModelsCompositeSelector.GetComponent<TMP_Dropdown>().interactable = true;
            }
            else {
                dropdown.interactable = true;
                dropdown.options.Add(new TMP_Dropdown.OptionData("Select model part"));
                foreach (ModelPart part in parts) {
                    dropdown.options.Add(new TMP_Dropdown.OptionData(part.GetName()));
                }
                variantDropdown.options.Add(new TMP_Dropdown.OptionData("Select model variant"));
                editModelsChangeButton.GetComponent<Button>().interactable = false;
                editModelsDeleteButton.GetComponent<Button>().interactable = false;
                editModelsVariantSelector.GetComponent<TMP_Dropdown>().interactable = false;
                editModelsCompositeSelector.GetComponent<TMP_Dropdown>().interactable = false;
            }
            SetSelectedModelName();
        }
    }
    public void EditModelsPartSelected() {
        TMP_Dropdown dropdown = editModelsPartSelector.GetComponent<TMP_Dropdown>();
        if (dropdown.value == 0) {
            if (editModelPart != null) {
                editModelPart = null;
                TMP_Dropdown variantDropdown = editModelsVariantSelector.GetComponent<TMP_Dropdown>();
                variantDropdown.options.Clear();
                variantDropdown.options.Add(new TMP_Dropdown.OptionData("Select model variant"));
                editModelCompositeIndex = 0;
                editModelsChangeButton.GetComponent<Button>().interactable = false;
                editModelsDeleteButton.GetComponent<Button>().interactable = false;
                editModelsVariantSelector.GetComponent<TMP_Dropdown>().interactable = false;
                editModelsCompositeSelector.GetComponent<TMP_Dropdown>().interactable = false;
            }
        }
        else {
            editModelPart = dropdown.options[dropdown.value].text;
            TMP_Dropdown variantDropdown = editModelsVariantSelector.GetComponent<TMP_Dropdown>();
            variantDropdown.options.Clear();
            variantDropdown.options.Add(new TMP_Dropdown.OptionData("Add model variant"));
            variantDropdown.options.Add(new TMP_Dropdown.OptionData("Default"));
            TMP_Dropdown compositionDropdown = editModelsCompositeSelector.GetComponent<TMP_Dropdown>();
            compositionDropdown.options.Clear();
            compositionDropdown.options.Add(new TMP_Dropdown.OptionData("Add another model"));
            foreach (ModelPart part in parts) {
                if (part.GetName() == editModelPart) {
                    editModelsDeleteButton.GetComponent<Button>().interactable = true;
                    if (part.model.Length == 1) editModelsDeleteButton.GetComponent<Button>().interactable = false;
                    if (part.variants != null) {
                        foreach (KeyValuePair<string,string[]> variant in part.variants) {
                            variantDropdown.options.Add(new TMP_Dropdown.OptionData(variant.Key));
                        }
                    }
                    for (int i = 0; i < part.GetModel().Length; i++) {
                        compositionDropdown.options.Add(new TMP_Dropdown.OptionData(i.ToString()));
                    }
                    if (compositionDropdown.options.Count == 1) compositionDropdown.options.Add(new TMP_Dropdown.OptionData("No models specified"));
                    break;
                }
            }
            editModelVariant = "default";
            editModelsVariantSelector.GetComponent<TMP_Dropdown>().value = 1;
            editModelCompositeIndex = 0;
            editModelsCompositeSelector.GetComponent<TMP_Dropdown>().value = 1;
            editModelsChangeButton.GetComponent<Button>().interactable = true;
            editModelsVariantSelector.GetComponent<TMP_Dropdown>().interactable = true;
            editModelsCompositeSelector.GetComponent<TMP_Dropdown>().interactable = true;
        }
        SetSelectedModelName();
    }
    public void EditModelsVariantSelected() {
        TMP_Dropdown dropdown = editModelsVariantSelector.GetComponent<TMP_Dropdown>();
        TMP_Dropdown compositionDropdown = editModelsCompositeSelector.GetComponent<TMP_Dropdown>();
        if (dropdown.value == 0) {
            dropdown.value = 1;
            editModelVariant = "default";
            compositionDropdown.options.Clear();
            compositionDropdown.options.Add(new TMP_Dropdown.OptionData("Add another model"));
            editModelsDeleteButton.GetComponent<Button>().interactable = true;
            foreach (ModelPart part in parts) {
                if (part.GetName() == editModelPart) {
                    if (part.model.Length == 1) editModelsDeleteButton.GetComponent<Button>().interactable = false;
                    for (int i = 0; i < part.GetModel().Length; i++) {
                        compositionDropdown.options.Add(new TMP_Dropdown.OptionData(i.ToString()));
                    }
                    break;
                }
            }
            RefreshModel();
            editModelCompositeIndex = 0;
            editModelsCompositeSelector.GetComponent<TMP_Dropdown>().value = 1;
            variantNamePopUp.SetActive(true);
        }
        else if (dropdown.value == 1) {
            if (editModelVariant != "default") {
                editModelVariant = "default";
                compositionDropdown.options.Clear();
                compositionDropdown.options.Add(new TMP_Dropdown.OptionData("Add another model"));
                editModelsDeleteButton.GetComponent<Button>().interactable = true;
                foreach (ModelPart part in parts) {
                    if (part.GetName() == editModelPart) {
                        if (part.model.Length == 1) editModelsDeleteButton.GetComponent<Button>().interactable = false;
                        for (int i = 0; i < part.GetModel().Length; i++) {
                            compositionDropdown.options.Add(new TMP_Dropdown.OptionData(i.ToString()));
                        }
                        break;
                    }
                }
                RefreshModel();
                editModelCompositeIndex = 0;
                editModelsCompositeSelector.GetComponent<TMP_Dropdown>().value = 1;
            }
        }
        else {
            editModelVariant = dropdown.options[dropdown.value].text;
            compositionDropdown.options.Clear();
            compositionDropdown.options.Add(new TMP_Dropdown.OptionData("Add another model"));
            editModelsDeleteButton.GetComponent<Button>().interactable = true;
            foreach (ModelPart part in parts) {
                if (part.GetName() == editModelPart) {
                    for (int i = 0; i < part.GetModel().Length; i++) {
                        compositionDropdown.options.Add(new TMP_Dropdown.OptionData(i.ToString()));
                    }
                    break;
                }
            }
            editModelCompositeIndex = 0;
            editModelsCompositeSelector.GetComponent<TMP_Dropdown>().value = 1;
        }
        SetSelectedModelName();
    }
    public void EditModelsCompositionSelected() {
        TMP_Dropdown dropdown = editModelsCompositeSelector.GetComponent<TMP_Dropdown>();
        if (dropdown.value == 0) {
            if (dropdown.options.Count >= 2) {
                editModelCompositeIndex = -1;
                string selectedFile = SelectModelFile(editModelPart,true);
                if (selectedFile != "") {
                    editModelsDeleteButton.GetComponent<Button>().interactable = true;
                    foreach (ModelPart part in parts) {
                        if (part.GetName() == editModelPart) {
                            if (editModelVariant == "default") {
                                part.SetModel(selectedFile, part.GetModel().Length + 1);
                            }
                            else {
                                part.SetModel(selectedFile, part.GetModel().Length + 1, editModelVariant);
                            }
                            editModelCompositeIndex = part.GetModel(editModelVariant).Length - 1;
                            dropdown.options.Add(new TMP_Dropdown.OptionData((part.GetModel().Length - 1).ToString()));
                            dropdown.value = part.GetModel(editModelVariant).Length;
                            break;
                        }
                    }
                    RefreshModel();
                    SaveModels();
                }
                else {
                    editModelCompositeIndex = 0;
                    dropdown.value = 1;
                }
            }
        }
        else {
            editModelCompositeIndex = dropdown.value - 1;
        }
        SetSelectedModelName();
    }
    public void PressEditModelsChangeButton() {
        string selectedFile = SelectModelFile(editModelPart,editModelVariant != default);
        if (selectedFile != "") {
            foreach (ModelPart part in parts) {
                if (part.GetName() == editModelPart) {
                    if (editModelVariant == "default") {
                        part.SetModel(selectedFile, editModelCompositeIndex);
                    }
                    else {
                        part.SetModel(selectedFile, editModelCompositeIndex, editModelVariant);
                    }
                    break;
                }
            }
            RefreshModel();
            SaveModels();
        }
        SetSelectedModelName();
    }
    public void PressEditModelsDeleteButton() {
        TMP_Dropdown compositionDropdown = editModelsCompositeSelector.GetComponent<TMP_Dropdown>();
        compositionDropdown.options.Clear();
        compositionDropdown.options.Add(new TMP_Dropdown.OptionData("Add another model"));
        foreach (ModelPart part in parts) {
            if (part.GetName() == editModelPart) {
                if (editModelVariant == "default") {
                    part.DeleteModel(editModelCompositeIndex);
                }
                else {
                    part.DeleteModel(editModelCompositeIndex, editModelVariant);
                    if (!part.VariantExists(editModelVariant)) {
                        TMP_Dropdown variantDropdown = editModelsVariantSelector.GetComponent<TMP_Dropdown>();
                        variantDropdown.options.Clear();
                        variantDropdown.options.Add(new TMP_Dropdown.OptionData("Add model variant"));
                        variantDropdown.options.Add(new TMP_Dropdown.OptionData("Default"));
                        if (part.variants != null) {
                            foreach (KeyValuePair<string,string[]> variant in part.variants) {
                                variantDropdown.options.Add(new TMP_Dropdown.OptionData(variant.Key));
                            }
                        }
                        editModelVariant = "default";
                        editModelsVariantSelector.GetComponent<TMP_Dropdown>().value = 1;
                    }
                }
                for (int i = 0; i < part.GetModel().Length; i++) {
                    compositionDropdown.options.Add(new TMP_Dropdown.OptionData(i.ToString()));
                }
                editModelCompositeIndex = 0;
                editModelsCompositeSelector.GetComponent<TMP_Dropdown>().value = 1;
                if (editModelVariant == "default" && part.model.Length == 1) editModelsDeleteButton.GetComponent<Button>().interactable = false;
                break;
            }
        }
        SaveModels();
        RefreshModel();
        SetSelectedModelName();
    }

    private void RefreshModel() {
        if (singleModelObject) {
            modelParts[0].GetComponent<ModelDisplay>().SetModel(parts[0].GetMinecraftModel(editModelVariant));
        }
        else {
            foreach (GameObject modelPart in modelParts) {
                if (modelPart.name == editModelPart) {
                    foreach (ModelPart part in parts) {
                        if (part.GetName() == editModelPart) {
                            modelPart.GetComponent<ModelDisplay>().SetModel(part.GetMinecraftModel(editModelVariant));
                            break;
                        }
                    }
                    break;
                }
            }
        }
    }
    private void SetSelectedModelName() {
        editModelsSelectedText.GetComponent<TMP_Text>().text = "No model selected";
        if (editModelPart != null) {
            foreach (ModelPart part in parts) {
                if (part.GetName() == editModelPart) {
                    if (editModelVariant == "default") {
                        editModelsSelectedText.GetComponent<TMP_Text>().text = part.GetModel(editModelCompositeIndex);
                    }
                    else {
                        editModelsSelectedText.GetComponent<TMP_Text>().text = part.GetModel(editModelCompositeIndex, editModelVariant);
                    }
                    break;
                }
            }
        }
    }
    public void VariantNamePopUpModify() {
        variantNamePopUpButton.GetComponent<Button>().interactable = true;
        variantNamePopUpError.SetActive(false);
        string name = variantNamePopUpInput.GetComponent<TMP_InputField>().text.ToLower().Replace(" ","_");
        if (name == "") {
            variantNamePopUpButtonText.GetComponent<TMP_Text>().text = "Cancel";
        }
        else {
            variantNamePopUpButtonText.GetComponent<TMP_Text>().text = "Confirm";
            if (name == "default") {
                variantNamePopUpError.SetActive(true);
                variantNamePopUpButton.GetComponent<Button>().interactable = false;
                return;
            }
            foreach (ModelPart part in parts) {
                if (part.GetName() == editModelPart) {
                    if (part.variants != null) {
                        foreach (KeyValuePair<string,string[]> variant in part.variants) {
                            if (variant.Key == name) {
                                variantNamePopUpError.SetActive(true);
                                variantNamePopUpButton.GetComponent<Button>().interactable = false;
                                return;
                            }
                        }
                    }
                    return;
                }
            }
        }
    }
    public void VariantNamePopUpConfirm() {
        if (variantNamePopUpInput.GetComponent<TMP_InputField>().text != "") {
            TMP_Dropdown dropdown = editModelsVariantSelector.GetComponent<TMP_Dropdown>();
            TMP_Dropdown compositionDropdown = editModelsCompositeSelector.GetComponent<TMP_Dropdown>();
            bool fullbreak = false;
            List<string> model = new();
            string name = variantNamePopUpInput.GetComponent<TMP_InputField>().text.ToLower().Replace(" ","_");
            string selectedFile = SelectModelFile(editModelPart,true);
            if (selectedFile == "") {
                fullbreak = true;
            }
            else {
                model.Add(selectedFile);
            }
            if (!fullbreak) {
                compositionDropdown.options.Clear();
                compositionDropdown.options.Add(new TMP_Dropdown.OptionData("Add another model"));
                compositionDropdown.options.Add(new TMP_Dropdown.OptionData("0"));
                foreach (ModelPart part in parts) {
                    if (part.GetName() == editModelPart) {
                        if (part.variants == null) part.variants = new();
                        part.variants.Add(name,model.ToArray());
                        break;
                    }
                }
                SaveModels();
                editModelCompositeIndex = 0;
                editModelsCompositeSelector.GetComponent<TMP_Dropdown>().value = 1;
                dropdown.options.Add(new TMP_Dropdown.OptionData(name));
                dropdown.value = dropdown.options.Count - 1;
            }
        }
        variantNamePopUpInput.GetComponent<TMP_InputField>().text = "";
        variantNamePopUpButtonText.GetComponent<TMP_Text>().text = "Cancel";
        variantNamePopUpError.SetActive(false);
        variantNamePopUp.SetActive(false);
    }
    public void SetSelectedModelPart(GameObject selectedModelPart) {
        this.selectedModelPart = selectedModelPart;
        //Show left menu
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
