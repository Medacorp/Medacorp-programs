using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;

public class ModelDisplay : MonoBehaviour
{

    public GameObject cameraObject;

    public GameObject Main;
    private GameObject rotationPoint;
    private GameObject animationValues;
    private GameObject defaultOffset;
    private GameObject templateVariant;
    private GameObject display;
    private GameObject templateElement;
    public Material transparentMaterial;
    private List<GameObject> variants;

    private List<ConditionalModelPartOffset> conditionalOffsets;

    private List<ConditionalModelPartPose> conditionalPoses;

    private List<ConditionalModelPartVariant> conditionalVariants;

    private float[] offsets = {0,0,0};
    private static bool generatingModel = false;
    private static int generatingModelThreads = 0;
    private static float heighestPoint = 0;
    private static float lowestPoint = 0;
    public void SetOffsets(List<ConditionalModelPartOffset> conditionalOffsets) {
        this.conditionalOffsets = conditionalOffsets;
    }

    public void SetPoses(List<ConditionalModelPartPose> conditionalPoses) {
        this.conditionalPoses = conditionalPoses;
    }

    public void SetVariants(List<ConditionalModelPartVariant> conditionalVariants) {
        this.conditionalVariants = conditionalVariants;
    }
    public void SetOffsets(float[] values) {
        offsets = values;
        gameObject.transform.localPosition = new(values[0],values[1],values[2]);
    }
    public void SetPoses(float[] values) {
        float pitchrad = values[0] * Mathf.PI / 180;
        float yawrad = values[1] * Mathf.PI / 180;
        float rollrad = values[2] * Mathf.PI / 180;
        float cpitch = Mathf.Cos(pitchrad * 0.5f);
        float spitch = Mathf.Sin(pitchrad * 0.5f);
        float cyaw = Mathf.Cos(yawrad * 0.5f);
        float syaw = Mathf.Sin(yawrad * 0.5f);
        float croll = Mathf.Cos(rollrad * 0.5f);
        float sroll = Mathf.Sin(rollrad * 0.5f);
        float w = cpitch * cyaw * croll + spitch * syaw * sroll;
        float x = spitch * cyaw * croll - cpitch * syaw * sroll;
        float y = cpitch * syaw * croll + spitch * cyaw * sroll;
        float z = cpitch * cyaw * sroll - spitch * syaw * croll;
        gameObject.transform.GetChild(0).GetChild(0).localRotation = new(x,y,z,w);;
    }
    public void GetState() {
        GetOffsets();
        GetPoses();
        GetVariants();
    }
    public void GetOffsets() {
        if (display == null) Start();
        SetOffsets(GetNewOffsets());
    }
    public void GetPoses() {
        if (display == null) Start();
        SetPoses(GetNewPoses());
    }
    public void GetVariants() {
        SetVisibleVariant(GetNewVariant());
    }
    public float[] GetNewOffsets() {
        float[] offsets = {0,0,0};
        Main mainScript = Main.GetComponent<Main>();
        if (conditionalOffsets.Count != 0) {
            foreach (ConditionalModelPartOffset offset in conditionalOffsets) {
                bool success = true;
                if (offset.tags != null && offset.tags.Count != 0){
                    foreach (KeyValuePair<string,bool> tag in offset.tags) {
                        if (mainScript.enabledTags.Contains(tag.Key) != tag.Value) success = false;
                    }
                }
                if (success) {
                    float[] offsets2 = offset.GetOffsets();
                    offsets[0] += offsets2[0];
                    offsets[1] += offsets2[1];
                    offsets[2] += offsets2[2];
                }
            }
        }
        return offsets;
    }
    public float[] GetNewPoses() {
        float[] poses = {0,0,0};
        Main mainScript = Main.GetComponent<Main>();
        if (conditionalPoses.Count != 0) {
            foreach (ConditionalModelPartPose pose in conditionalPoses) {
                bool success = true;
                if (pose.tags != null && pose.tags.Count != 0){
                    foreach (KeyValuePair<string,bool> tag in pose.tags) {
                        if (mainScript.enabledTags.Contains(tag.Key) != tag.Value) success = false;
                    }
                }
                if (success) {
                    float[] poses2 = pose.GetPoses();
                    if (poses2[0] != 9999) poses[0] = poses2[0];
                    if (poses2[1] != 9999) poses[1] = poses2[1];
                    if (poses2[2] != 9999) poses[2] = poses2[2];
                }
            }
        }
        return poses;
    }
    public string GetNewVariant() {
        string value = "default";
        Main mainScript = Main.GetComponent<Main>();
        if (conditionalVariants.Count != 0) {
            foreach (ConditionalModelPartVariant variant in conditionalVariants) {
                bool success = true;
                if (variant.tags != null && variant.tags.Count != 0){
                    foreach (KeyValuePair<string,bool> tag in variant.tags) {
                        if (mainScript.enabledTags.Contains(tag.Key) != tag.Value) success = false;
                    }
                }
                if (success) {
                    value = variant.GetModelVariant();
                }
            }
        }
        return value;
    }
    public void SetVisibleVariant(string variant) {
        if (variants == null) variants = new();
        if (variants.Count != 0) {
            foreach (GameObject var in variants) {
                if (var.name == variant) var.SetActive(true);
                else var.SetActive(false);
            }
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rotationPoint = gameObject.transform.GetChild(0).gameObject;
        animationValues = rotationPoint.transform.GetChild(0).gameObject;
        defaultOffset = animationValues.transform.GetChild(0).gameObject;
        templateVariant = defaultOffset.transform.GetChild(0).gameObject;
        display = templateVariant.transform.GetChild(0).gameObject;
        templateElement = display.transform.GetChild(0).gameObject;
        if (variants == null) variants = new();
    }

    // Update is called once per frame
    void Update()
    {
        if (generatingModel && generatingModelThreads == 0) {
            TextureAnimator.ToggleAnimations(true);
            generatingModel = false;
            cameraObject.GetComponent<CameraBehavior>().SetModelTop(heighestPoint);
            cameraObject.GetComponent<CameraBehavior>().SetModelBottom(lowestPoint);
            print(gameObject.name + " is done");
        }
    }
    public void SetModel(Dictionary<string, string[]> variants, string[] defaultVariant, string visibleVariant) {
        if (display == null) Start();
        if (this.variants.Count != 0) {
            foreach (GameObject variant in this.variants) {
                Destroy(variant);
            }
            this.variants.Clear();
        }
        cameraObject.GetComponent<CameraBehavior>().SetModelTop(0);
        cameraObject.GetComponent<CameraBehavior>().SetModelBottom(0);
        TextureAnimator.ToggleAnimations(false);
        GenerateModels(defaultVariant, "default", visibleVariant == "default");
        //Thread threadedGenerateDefault = new Thread(() => GenerateModels(defaultVariant, "default", visibleVariant == "default"));
        //threadedGenerateDefault.Start();
        generatingModelThreads += 1;
        foreach (KeyValuePair<string,string[]> var in variants) {
            GenerateModels(var.Value, var.Key, visibleVariant == var.Key);
            //Thread threadedGenerateVariant = new Thread(() => GenerateModels(var.Value, var.Key, visibleVariant == var.Key));
            //threadedGenerateVariant.Start();
            generatingModelThreads += 1;
        }
        generatingModel = true;
    }
    public List<MinecraftModel> GetMinecraftModel(string[] models) {
        List<MinecraftModel> compositeModel = new();
        if (models.Length != 0) {
            foreach (string model in models) {
                MinecraftAtlas atlas = null;
                if (File.Exists(Regex.Replace(model,"/assets/[a-z0-9_-]+/models/[a-z0-9/_-]+.json","/assets/minecraft/atlases/blocks.json"))) {
                    atlas = JsonConvert.DeserializeObject<MinecraftAtlas>(File.ReadAllText(Regex.Replace(model,"/assets/[a-z0-9_-]+/models/[a-z0-9/_-]+.json","/assets/minecraft/atlases/blocks.json")));
                }
                MinecraftModel minecraftModel = JsonConvert.DeserializeObject<MinecraftModel>(File.ReadAllText(model));
                minecraftModel.name = model;
                string path = Regex.Replace(model,"/assets/[a-z0-9_-]+/models/[a-z0-9/_-]+.json","/assets/|||/models/");
                List<string> knownParents = new(){model};
                if (minecraftModel.parent != null) GetMinecraftModelParent(minecraftModel, path, knownParents);

                if (minecraftModel.textures != null) {
                    minecraftModel.textures = updateTextureReferences(minecraftModel.textures);
                    minecraftModel.textures = getTextureFiles(minecraftModel.textures, model, atlas);
                }

                compositeModel.Add(minecraftModel);
            }
        }
        return compositeModel;
    }
    private Dictionary<string,string> updateTextureReferences(Dictionary<string,string> textures) {
        Dictionary<string,string> newTextures = new();
        foreach (KeyValuePair<string, string> texture in textures) {
            if (texture.Value.StartsWith("#")) {
                string key = texture.Key;
                if (textures.TryGetValue(texture.Value.Remove(0,1), out string value)) {
                    newTextures.Add(key,value);
                }
            }
            else {
                newTextures.Add(texture.Key,texture.Value);
            }
        }
        if (newTextures.Values.Where( value => value.StartsWith("#")).ToList().Count != 0) return updateTextureReferences(newTextures);
        return newTextures;
        
    }
    private Dictionary<string,string> getTextureFiles(Dictionary<string,string> textures, string modelpath, MinecraftAtlas atlas) {
        Dictionary<string,string> files = new();
        foreach (KeyValuePair<string,string> texture in textures) {
            string[] split = {"minecraft","missingno"};
            if (texture.Value.Contains(":")) split = texture.Value.Split(":");
            else split[1] = texture.Value;
            string filepath = Regex.Replace(modelpath,"/assets/[a-z0-9_-]+/models/[a-z0-9/_-]+.json","/assets/" + split[0] + "/textures/" + split[1] + ".png");
            string newtexture = "missingno";
            if (File.Exists(filepath)) {
                newtexture = filepath;
            }
            else if (atlas != null && atlas.sources.Count != 0) {
                foreach (MinecraftAtlasSource source in atlas.sources) {
                    if (split[1].StartsWith(source.prefix)) {
                        filepath = Regex.Replace(modelpath,"/assets/[a-z0-9_-]+/models/[a-z0-9/_-]+.json","/assets/" + split[0] + "/textures/" + split[1].Replace(source.prefix,source.source + "/") + ".png");
                        break;
                    }
                }
                if (File.Exists(filepath)) {
                    newtexture = filepath;
                }
            }
            files.Add(texture.Key,newtexture);
            
        }
        return files;
    }
    private MinecraftModel GetMinecraftModelParent(MinecraftModel model, string path, List<string> knownParents) {
        List<string> newKnownParents = knownParents;
        string[] split = model.parent.Split(":");
        string filepath = path.Replace("|||",split[0])+split[1]+".json";
        MinecraftModel parentModel = null;
        MinecraftModel newModel = model;
        if(!knownParents.Contains(filepath)) {
            newKnownParents.Add(filepath);
            if (File.Exists(filepath)) {
                parentModel = JsonConvert.DeserializeObject<MinecraftModel>(File.ReadAllText(filepath));
            }
            newModel.parent = null;
            if (parentModel != null) {
                if (parentModel.parent != null) newModel.parent = parentModel.parent;
                if (parentModel.textures != null) {
                    if (newModel.textures == null) newModel.textures = new();
                    foreach (KeyValuePair<string, string> texture in parentModel.textures) {
                        try {
                            newModel.textures.Add(texture.Key, texture.Value);
                        }
                        catch {
                            //Provided by child
                        }
                    }
                }
                if (parentModel.elements != null && newModel.elements == null) newModel.elements = parentModel.elements;
                if (parentModel.display != null) {
                    if (newModel.display == null) newModel.display = new();
                    foreach (KeyValuePair<string, MinecraftModelDisplay> displaysetting in parentModel.display) {
                        try {
                            newModel.display.Add(displaysetting.Key, displaysetting.Value);
                        }
                        catch {
                            //Provided by child
                        }
                    }
                }
            }
            if (newModel.parent != null) return GetMinecraftModelParent(newModel, path, newKnownParents);
            return newModel;
        }
        else {
            return newModel;
            //Bad model, infinite parent loop
        }
    }
    private void GenerateModels(string[] model, string variant, bool visible) {
        GameObject newVariant = Instantiate(templateVariant, new Vector3(0,0,0), new Quaternion(), defaultOffset.transform);
        newVariant.SetActive(visible);
        newVariant.name = variant;
        variants.Add(newVariant);
        StartCoroutine(GenerateModel(GetMinecraftModel(model), newVariant));
    }
    private IEnumerator GenerateModel(List<MinecraftModel> model, GameObject newVariant) {
        int i = 0;
        foreach (MinecraftModel composite in model) {
            if (composite.elements.Count != 0) {
                GameObject newdisplay = Instantiate(display, new Vector3(0,0,0), new Quaternion(), newVariant.transform);
                newdisplay.SetActive(true);
                newdisplay.name = "CompositeModel"+i.ToString();
                MinecraftModelDisplay value = new();
                try {
                    composite.display.TryGetValue("head", out value);
                    newdisplay.transform.localPosition = value.GetTranslation();
                    newdisplay.transform.localScale = value.GetScale();
                    newdisplay.transform.localRotation = value.GetRotation();
                }
                catch {
                    //Doesn't exist
                }
                i++;
                int j = 0;
                foreach (MinecraftModelElement element in composite.elements) {
                    if (element.faces.Count != 0) {
                        GameObject newelement = Instantiate(templateElement, new Vector3(0,0,0), new Quaternion(), newdisplay.transform);
                        newelement.SetActive(true);
                        newelement.name = "ModelElement"+j.ToString();
                        newelement.transform.localScale = element.GetSize();
                        newelement.transform.localPosition = element.GetCenter();
                        newelement.transform.RotateAround(newdisplay.transform.TransformPoint(element.GetRotationPoint()), newelement.transform.InverseTransformDirection(element.GetRotationAxis()), -element.GetRotationAngle());
                        foreach (KeyValuePair<string,MinecraftModelFace> face in element.faces) {
                            GameObject modelFace = null;
                            if (face.Key == "up") modelFace = newelement.transform.GetChild(0).gameObject;
                            else if (face.Key == "down") modelFace = newelement.transform.GetChild(1).gameObject;
                            else if (face.Key == "north") modelFace = newelement.transform.GetChild(2).gameObject;
                            else if (face.Key == "east") modelFace = newelement.transform.GetChild(3).gameObject;
                            else if (face.Key == "south") modelFace = newelement.transform.GetChild(4).gameObject;
                            else if (face.Key == "west") modelFace = newelement.transform.GetChild(5).gameObject;
                            if (modelFace != null) {
                                modelFace.transform.Rotate(modelFace.transform.InverseTransformDirection(modelFace.transform.up), face.Value.rotation, Space.Self);
                                Texture2D texture = LoadTexture(face.Value.texture.Replace("#",""),composite, modelFace);
                                MinecraftMcmetaAnimation animation = modelFace.GetComponent<TextureAnimator>().animationMcmeta;
                                List<float> uv = face.Value.uv.ToList();
                                if (texture.width != texture.height) {
                                    int amountW = texture.width / animation.width;
                                    int amountH = texture.height / animation.height;
                                    uv[0] = uv[0] / amountW;
                                    uv[1] = uv[1] / amountH;
                                    uv[2] = uv[2] / amountW;
                                    uv[3] = uv[3] / amountH;
                                }
                                modelFace.GetComponent<TextureAnimator>().SetUV(uv);
                                if (texture != null)
                                {
                                    if (texture.width == animation.width && texture.height == animation.height) {
                                        if (IsAnyPixelTransparent(texture, new(uv[0] / 16, uv[3] / 16 * -1 + 1), new(uv[2] / 16, uv[1] / 16 * -1 + 1))) {
                                            modelFace.GetComponent<MeshRenderer>().SetMaterials(new(){transparentMaterial});
                                        }
                                    }
                                    else modelFace.GetComponent<MeshRenderer>().SetMaterials(new(){transparentMaterial});
                                    modelFace.GetComponent<MeshRenderer>().material.mainTexture = texture;
                                }
                                modelFace.SetActive(true);
                            }
                        }
                        if (heighestPoint < newelement.transform.position.y + newelement.transform.localScale.y) heighestPoint = newelement.transform.position.y + newelement.transform.localScale.y;
                        if (lowestPoint > newelement.transform.position.y - newelement.transform.localScale.y) lowestPoint = newelement.transform.position.y - newelement.transform.localScale.y;
                    }
                    j++;
                    yield return new WaitForEndOfFrame();
                }
            }
        }
        generatingModelThreads -= 1;
    }
    private Texture2D LoadTexture(string key, MinecraftModel model, GameObject modelFace)
    {
        MinecraftMcmetaAnimation mcmeta = new();
        string path = null;
        if (model.textures.ContainsKey(key)) path = model.textures[key];
        // Load the texture from the file path
        if (path != null && path != "missingno") {
            Texture2D texture = new Texture2D(2, 2); // Create a new texture (replace dimensions as needed)
            texture.filterMode = FilterMode.Point;
            byte[] fileData = File.ReadAllBytes(path); // Read all bytes from the file

            if (fileData != null)
            {
                texture.LoadImage(fileData);
                if (texture != null) {
                    if (File.Exists(path + ".mcmeta")) {
                        MinecraftMcmeta mcmeta2 = JsonConvert.DeserializeObject<MinecraftMcmeta>(File.ReadAllText(path + ".mcmeta"));
                        mcmeta = mcmeta2.animation;
                        mcmeta.ParseFrames();
                        if (mcmeta.width == 0 && mcmeta.height == 0) {
                            mcmeta.width = texture.width;
                            if (mcmeta.width > texture.height) mcmeta.width = texture.height;
                            mcmeta.height = texture.width;
                            if (mcmeta.height > texture.height) mcmeta.height = texture.height;
                        }
                        else {
                            if (mcmeta.width == 0) mcmeta.width = texture.width;
                            if (mcmeta.height == 0) mcmeta.height = texture.height;
                        }
                        if ((float)texture.width % (float)mcmeta.width != 0) return null;
                        if ((float)texture.height % (float)mcmeta.height != 0) return null;
                        modelFace.GetComponent<TextureAnimator>().animationMcmeta = mcmeta;
                        return texture;
                    }
                    else {
                        if (texture.width == texture.height) {
                            mcmeta.ParseFrames();
                            mcmeta.width = texture.width;
                            mcmeta.height = texture.height;
                            modelFace.GetComponent<TextureAnimator>().animationMcmeta = mcmeta;
                            return texture;
                        }
                        return null;
                    }
                }
            }
        }
        Debug.LogError("Couldn't find texture for key: " + key + " in the model " + model.name);
        return null;
    }
    private bool IsAnyPixelTransparent(Texture2D texture, Vector2 uvMin, Vector2 uvMax)
    {

        int minX = Mathf.FloorToInt(Mathf.Min(uvMin.x, uvMax.x) * texture.width);
        int minY = Mathf.FloorToInt(Mathf.Min(uvMin.y, uvMax.y) * texture.height);
        int maxX = Mathf.FloorToInt(Mathf.Max(uvMin.x, uvMax.x) * texture.width);
        int maxY = Mathf.FloorToInt(Mathf.Max(uvMin.y, uvMax.y) * texture.height);

        minX = Mathf.Clamp(minX, 0, texture.width - 1);
        minY = Mathf.Clamp(minY, 0, texture.height - 1);
        maxX = Mathf.Clamp(maxX, 0, texture.width - 1);
        maxY = Mathf.Clamp(maxY, 0, texture.height - 1);

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                Color pixelColor = texture.GetPixel(x, y);
                if (pixelColor.a < 0.9f)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
