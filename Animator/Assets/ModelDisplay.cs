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
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.InputSystem;

public class ModelDisplay : MonoBehaviour
{

    public GameObject cameraObject;

    public GameObject Main;
    private GameObject rotationPoint;
    private GameObject animationValues;
    private GameObject defaultOffset;
    private GameObject templateVariant;
    private GameObject display;
    public Material transparentMaterial;
    public Texture2D missingNo;
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
            print("model is done");
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
        generatingModelThreads += 1;
        foreach (KeyValuePair<string,string[]> var in variants) {
            GenerateModels(var.Value, var.Key, visibleVariant == var.Key);
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
        newVariant.transform.localPosition = new(0,0,0);
        newVariant.SetActive(visible);
        newVariant.name = variant;
        variants.Add(newVariant);
        GenerateMesh(GetMinecraftModel(model), newVariant);
    }
    private void GenerateMesh(List<MinecraftModel> model, GameObject newVariant) {
        int i = 0;
        foreach (MinecraftModel minecraftModel in model) 
        {
            GameObject newdisplay = Instantiate(display, new Vector3(0,0,0), new Quaternion(), newVariant.transform);
            newdisplay.name = "CompositeModel"+i.ToString();
            Texture2D textureAtlas = new(2,2);
            textureAtlas.filterMode = FilterMode.Point;
            List<Texture2D> textures = new();
            List<MinecraftMcmetaAnimation> texturesAnimations = new();
            List<string> loadedTextures = new();
            Dictionary<string,int> textureAtlasPositions = new();
            foreach (KeyValuePair<string,string> textureReference in minecraftModel.textures) {
                Texture2D newtexture = LoadTexture(textureReference.Value);
                MinecraftMcmetaAnimation textureMcmeta = LoadTextureMcmeta(textureReference.Value, newtexture);
                string textureName = textureReference.Value;
                if (!(newtexture != null && textureMcmeta != null)) {
                    newtexture = missingNo;
                    textureMcmeta = new(newtexture.width, newtexture.height);
                    textureName = "missingno";
                }
                textureMcmeta.SetTexture(newtexture);
                newtexture = textureMcmeta.GetFrame(0);
                if (loadedTextures.Contains(textureName)) {
                    textureAtlasPositions.Add(textureReference.Key,loadedTextures.FindIndex(valueIndex => valueIndex == textureName));
                }
                else {
                    textures.Add(newtexture);
                    texturesAnimations.Add(textureMcmeta);
                    textureAtlasPositions.Add(textureReference.Key,loadedTextures.Count - 1);
                    loadedTextures.Add(textureName);
                }
            }
            Rect[] rectangles = textureAtlas.PackTextures(textures.ToArray(), 0);
            Dictionary<string,float> uvScaleX = new();
            Dictionary<string,float> uvScaleY = new();
            Dictionary<string,float> uvOffsetX = new();
            Dictionary<string,float> uvOffsetY = new();
            foreach (KeyValuePair<string,string> textureReference in minecraftModel.textures) {
                int valueIndex = loadedTextures.FindIndex(valueIndex => valueIndex == textureReference.Value);
                uvScaleX.Add(textureReference.Key, rectangles[valueIndex].width);
                uvScaleY.Add(textureReference.Key, rectangles[valueIndex].height);
                uvOffsetX.Add(textureReference.Key, rectangles[valueIndex].position.x);
                uvOffsetY.Add(textureReference.Key, rectangles[valueIndex].position.y);
            }
            newdisplay.GetComponent<MeshRenderer>().material.mainTexture = textureAtlas;
            newdisplay.GetComponent<TextureAnimator>().SetValues(texturesAnimations, textureAtlas, rectangles);
            i++;
            Mesh mesh = new();
            mesh.name = gameObject.name + ":" + newVariant.name + ":" + newdisplay.name;
            List<Vector3> vertices = new();
            List<Vector2> uv = new();
            List<int> triangles = new();
            foreach (MinecraftModelElement element in minecraftModel.elements) {
                float[] from = {-(element.from[0] - 8) / 16, (element.from[1] - 8) / 16, (element.from[2] - 8) / 16};
                float[] to = {-(element.to[0] - 8) / 16, (element.to[1] - 8) / 16, (element.to[2] - 8) / 16};
                List<Vector3> points = new(){
                    new(from[0],from[1],from[2]),
                    new(from[0],from[1],to[2]),
                    new(from[0],to[1],from[2]),
                    new(from[0],to[1],to[2]),
                    new(to[0],from[1],from[2]),
                    new(to[0],from[1],to[2]),
                    new(to[0],to[1],from[2]),
                    new(to[0],to[1],to[2])
                };
                if (element.GetRotationAngle() != 0) {
                    List<Vector3> newpoints = new();
                    float[] angles = element.GetRotationEulerAngle();
                    Quaternion rotation = Quaternion.Euler(angles[0], -angles[1], -angles[2]);
                    Vector3 rotationPoint = element.GetRotationPoint();
                    foreach (Vector3 point in points) {
                        newpoints.Add(rotation * (point - rotationPoint) + rotationPoint);
                    }
                    points = newpoints;
                }
                foreach (KeyValuePair<string,MinecraftModelFace> face in element.faces) {
                    float[] uvs = {face.Value.uv[0] / 16, face.Value.uv[1] / 16, face.Value.uv[2] / 16, face.Value.uv[3] / 16};
                    float xScale = 1;
                    float yScale = 1;
                    float xOffset = 0;
                    float yOffset = 0;
                    string textureReference = face.Value.texture.Replace("#","");
                    if (!minecraftModel.textures.ContainsKey(textureReference)) {
                        textureReference = "missingno";
                    }
                    uvScaleX.TryGetValue(textureReference, out xScale);
                    uvScaleY.TryGetValue(textureReference, out yScale);
                    uvOffsetX.TryGetValue(textureReference, out xOffset);
                    uvOffsetY.TryGetValue(textureReference, out yOffset);
                    uvs[0] = uvs[0] * xScale + xOffset;
                    uvs[1] = (uvs[1] * yScale + yOffset) * -1 + 1;
                    uvs[2] = uvs[2] * xScale + xOffset;
                    uvs[3] = (uvs[3] * yScale + yOffset) * -1 + 1;
                    int index = vertices.Count;
                    bool validKey = false;
                    if (face.Key == "east") {
                        vertices.Add(points[5]);
                        vertices.Add(points[7]);
                        vertices.Add(points[6]);
                        vertices.Add(points[4]);
                        validKey = true;
                    }
                    else if (face.Key == "down") {
                        vertices.Add(points[0]);
                        vertices.Add(points[1]);
                        vertices.Add(points[5]);
                        vertices.Add(points[4]);
                        validKey = true;
                    }
                    else if (face.Key == "north") {
                        vertices.Add(points[4]);
                        vertices.Add(points[6]);
                        vertices.Add(points[2]);
                        vertices.Add(points[0]);
                        validKey = true;
                    }
                    else if (face.Key == "south") {
                        vertices.Add(points[1]);
                        vertices.Add(points[3]);
                        vertices.Add(points[7]);
                        vertices.Add(points[5]);
                        validKey = true;
                    }
                    else if (face.Key == "up") {
                        vertices.Add(points[3]);
                        vertices.Add(points[2]);
                        vertices.Add(points[6]);
                        vertices.Add(points[7]);
                        validKey = true;
                    }
                    else if (face.Key == "west") {
                        vertices.Add(points[0]);
                        vertices.Add(points[2]);
                        vertices.Add(points[3]);
                        vertices.Add(points[1]);
                        validKey = true;
                    }
                    if (validKey) {
                        triangles.Add(index);
                        triangles.Add(index+1);
                        triangles.Add(index+2);
                        triangles.Add(index);
                        triangles.Add(index+2);
                        triangles.Add(index+3);
                        uv.Add(new(uvs[0], uvs[3]));
                        uv.Add(new(uvs[0], uvs[1]));
                        uv.Add(new(uvs[2], uvs[1]));
                        uv.Add(new(uvs[2], uvs[3]));
                        if (face.Value.rotation == 90 || face.Value.rotation == 180 || face.Value.rotation == 270) {
                            uv.Add(uv[index]);
                            uv.RemoveAt(index);
                            if (face.Value.rotation == 180 || face.Value.rotation == 270) {
                                uv.Add(uv[index]);
                                uv.RemoveAt(index);
                                if (face.Value.rotation == 270) {
                                    uv.Add(uv[index]);
                                    uv.RemoveAt(index);
                                }
                            }
                        }
                    }
                }
            }
            mesh.vertices = vertices.ToArray();
            mesh.uv = uv.ToArray();
            mesh.triangles = triangles.ToArray();
            newdisplay.transform.SetParent(null);
            Vector3 trueScale = newdisplay.transform.localScale;
            Vector3 truePosition = newdisplay.transform.localPosition;
            Quaternion trueRotation = newdisplay.transform.localRotation;
            newdisplay.transform.localScale = new (1,1,1);
            newdisplay.transform.localPosition = new (0,0,0);
            newdisplay.transform.localRotation = Quaternion.Euler(0,0,0);
            newdisplay.GetComponent<MeshFilter>().sharedMesh = mesh;
            newdisplay.GetComponent<MeshCollider>().sharedMesh = mesh;
            newdisplay.transform.localScale = trueScale;
            newdisplay.transform.localPosition = truePosition;
            newdisplay.transform.localRotation = trueRotation;
            newdisplay.transform.SetParent(newVariant.transform);
            MinecraftModelDisplay value = new();
            try {
                minecraftModel.display.TryGetValue("head", out value);
                newdisplay.transform.localPosition = value.GetTranslation();
                newdisplay.transform.localScale = value.GetScale();
                newdisplay.transform.localRotation = value.GetRotation();
            }
            catch {
                //Doesn't exist, use defaults
            }
            Bounds bounds = mesh.bounds;
            newdisplay.SetActive(true);
            if (heighestPoint < bounds.max.y) heighestPoint = bounds.max.y;
            if (lowestPoint > bounds.min.y) lowestPoint = bounds.min.y;
        }
        generatingModelThreads -= 1;
    }
    //private IEnumerator GenerateModel(List<MinecraftModel> model, GameObject newVariant) {
    //    int i = 0;
    //    foreach (MinecraftModel composite in model) {
    //        if (composite.elements.Count != 0) {
    //            GameObject newdisplay = Instantiate(display, new Vector3(0,0,0), new Quaternion(), newVariant.transform);
    //            newdisplay.SetActive(true);
    //            newdisplay.name = "CompositeModel"+i.ToString();
    //            i++;
    //            int j = 0;
    //            string textureComposite = null;
    //            bool canMergeComposite = true;
    //            bool transparentComposite = false;
    //            foreach (MinecraftModelElement element in composite.elements) {
    //                if (element.faces.Count != 0) {
    //                    GameObject newelement = Instantiate(templateElement, new Vector3(0,0,0), new Quaternion(), newdisplay.transform);
    //                    newelement.SetActive(true);
    //                    newelement.name = "ModelElement"+j.ToString();
    //                    newelement.transform.localScale = element.GetSize();
    //                    newelement.transform.localPosition = element.GetCenter();
    //                    string textureElement = null;
    //                    bool canMergeElement = true;
    //                    bool transparentElement = false;
    //                    foreach (KeyValuePair<string,MinecraftModelFace> face in element.faces) {
    //                        GameObject modelFace = null;
    //                        if (face.Key == "up") modelFace = newelement.transform.GetChild(0).gameObject;
    //                        else if (face.Key == "down") modelFace = newelement.transform.GetChild(1).gameObject;
    //                        else if (face.Key == "north") modelFace = newelement.transform.GetChild(2).gameObject;
    //                        else if (face.Key == "east") modelFace = newelement.transform.GetChild(3).gameObject;
    //                        else if (face.Key == "south") modelFace = newelement.transform.GetChild(4).gameObject;
    //                        else if (face.Key == "west") modelFace = newelement.transform.GetChild(5).gameObject;
    //                        if (modelFace != null) {
    //                            modelFace.transform.Rotate(modelFace.transform.InverseTransformDirection(modelFace.transform.up), face.Value.rotation, Space.Self);
    //                            Texture2D texture = LoadTexture(face.Value.texture.Replace("#",""),composite, modelFace);
    //                            if (textureElement == null) textureElement = GetTextureName(face.Value.texture.Replace("#",""),composite);
    //                            if (textureElement != GetTextureName(face.Value.texture.Replace("#",""),composite)) canMergeElement = false;
    //                            MinecraftMcmetaAnimation animation = modelFace.GetComponent<TextureAnimator>().animationMcmeta;
    //                            List<float> uv = face.Value.uv.ToList();
    //                            if (texture.width != texture.height) {
    //                                int amountW = texture.width / animation.width;
    //                                int amountH = texture.height / animation.height;
    //                                uv[0] = uv[0] / amountW;
    //                                uv[1] = uv[1] / amountH;
    //                                uv[2] = uv[2] / amountW;
    //                                uv[3] = uv[3] / amountH;
    //                            }
    //                            modelFace.GetComponent<TextureAnimator>().SetUV(uv);
    //                            Mesh mesh = modelFace.GetComponent<MeshFilter>().mesh;
    //                            Vector2[] uvs = mesh.uv;
    //                            modelFace.GetComponent<TextureAnimator>().SetOriginalUV(uvs);
    //                            Vector2 uvStart = new Vector2(uv[0] / 16, uv[1] / 16 * -1 + 1);
    //                            Vector2 uvEnd = new Vector2(uv[2] / 16, uv[3] / 16 * -1 + 1);
    //                            for (int l = 0; l < uvs.Length; l++)
    //                            {
    //                                
    //                                uvs[l] = new Vector2(
    //                                    Mathf.Lerp(uvStart.x, uvEnd.x, uvs[l].x), 
    //                                    Mathf.Lerp(uvStart.y, uvEnd.y, uvs[l].y)
    //                                );
    //                            }
    //                            mesh.uv = uvs;
    //                            mesh.name = modelFace.transform.position.z.ToString() + "_" + (-modelFace.transform.position.x).ToString() + "_" + modelFace.transform.position.y.ToString();
    //                            modelFace.GetComponent<MeshFilter>().mesh = mesh;
    //                            if (texture != null)
    //                            {
    //                                if (texture.width == animation.width && texture.height == animation.height) {
    //                                    if (IsAnyPixelTransparent(texture, new(uv[0] / 16, uv[3] / 16 * -1 + 1), new(uv[2] / 16, uv[1] / 16 * -1 + 1))) {
    //                                        modelFace.GetComponent<MeshRenderer>().SetMaterials(new(){transparentMaterial});
    //                                        transparentElement = true;
    //                                    }
    //                                }
    //                                else {
    //                                    modelFace.GetComponent<MeshRenderer>().SetMaterials(new(){transparentMaterial});
    //                                    canMergeElement = false;
    //                                }
    //                                modelFace.GetComponent<MeshRenderer>().material.mainTexture = texture;
    //                            }
    //                            modelFace.SetActive(true);
    //                        }
    //                    }
    //                    if (!newelement.transform.GetChild(5).gameObject.activeSelf) {
    //                        GameObject childToDelete = newelement.transform.GetChild(5).gameObject;
    //                        childToDelete.transform.SetParent(null);
    //                        Destroy(childToDelete);
    //                    }
    //                    if (!newelement.transform.GetChild(4).gameObject.activeSelf) {
    //                        GameObject childToDelete = newelement.transform.GetChild(4).gameObject;
    //                        childToDelete.transform.SetParent(null);
    //                        Destroy(childToDelete);
    //                    }
    //                    if (!newelement.transform.GetChild(3).gameObject.activeSelf) {
    //                        GameObject childToDelete = newelement.transform.GetChild(3).gameObject;
    //                        childToDelete.transform.SetParent(null);
    //                        Destroy(childToDelete);
    //                    }
    //                    if (!newelement.transform.GetChild(2).gameObject.activeSelf) {
    //                        GameObject childToDelete = newelement.transform.GetChild(2).gameObject;
    //                        childToDelete.transform.SetParent(null);
    //                        Destroy(childToDelete);
    //                    }
    //                    if (!newelement.transform.GetChild(1).gameObject.activeSelf) {
    //                        GameObject childToDelete = newelement.transform.GetChild(1).gameObject;
    //                        childToDelete.transform.SetParent(null);
    //                        Destroy(childToDelete);
    //                    }
    //                    if (!newelement.transform.GetChild(0).gameObject.activeSelf) {
    //                        GameObject childToDelete = newelement.transform.GetChild(0).gameObject;
    //                        childToDelete.transform.SetParent(null);
    //                        Destroy(childToDelete);
    //                    }
    //                    if (canMergeElement) {
    //                        newelement.transform.SetParent(null);
    //                        Vector3 trueScale = newelement.transform.localScale;
    //                        Vector3 truePosition = newelement.transform.localPosition;
    //                        newelement.transform.localScale = new (1,1,1);
    //                        newelement.transform.localPosition = new (0,0,0);
    //                        MeshFilter[] meshFilters = newelement.GetComponentsInChildren<MeshFilter>();
    //                        CombineInstance[] combine = new CombineInstance[meshFilters.Length];
//
    //                        for (int k = 0; k < meshFilters.Length; k++)
    //                        {
    //                            if (meshFilters[k].transform == newelement.transform || meshFilters[k].sharedMesh == null) {    
    //                                continue;      
    //                            }
    //                            combine[k - 1].subMeshIndex = 0;     
    //                            combine[k - 1].mesh = meshFilters[k].sharedMesh;       
    //                            combine[k - 1].transform = meshFilters[k].transform.localToWorldMatrix;
    //                        }
//
    //                        Mesh mesh = new Mesh();
    //                        mesh.CombineMeshes(combine);
    //                        mesh.name = truePosition.z.ToString() + "_" + (-truePosition.x).ToString() + "_" + truePosition.y.ToString();
    //                        mesh.RecalculateTangents();
    //                        newelement.GetComponent<MeshFilter>().sharedMesh = mesh;
    //                        newelement.GetComponent<MeshCollider>().sharedMesh = mesh;
    //                        if (transparentElement) newelement.GetComponent<MeshRenderer>().SetMaterials(new(){transparentMaterial});
    //                        newelement.GetComponent<MeshRenderer>().material.mainTexture = newelement.transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.mainTexture;
    //                        for (int l = newelement.transform.childCount - 1; l >= 0; l-=1) {
    //                            Destroy(newelement.transform.GetChild(l).gameObject);
    //                        }
    //                        newelement.transform.localScale = trueScale;
    //                        newelement.transform.localPosition = truePosition;
    //                        newelement.transform.SetParent(newdisplay.transform);
    //                        if (transparentElement) transparentComposite = true;
    //                        if (textureComposite == null) textureComposite = textureElement;
    //                        if (textureComposite != textureElement) canMergeComposite = false;
    //                    }
    //                    else canMergeComposite = false;
    //                    newelement.transform.RotateAround(newdisplay.transform.TransformPoint(element.GetRotationPoint()), newelement.transform.InverseTransformDirection(element.GetRotationAxis()), -element.GetRotationAngle());
    //                    if (heighestPoint < newelement.transform.position.y + newelement.transform.localScale.y) heighestPoint = newelement.transform.position.y + newelement.transform.localScale.y;
    //                    if (lowestPoint > newelement.transform.position.y - newelement.transform.localScale.y) lowestPoint = newelement.transform.position.y - newelement.transform.localScale.y;
    //                }
    //                j++;
    //                yield return new WaitForEndOfFrame();
    //            }
    //            GameObject templateToDelete = newdisplay.transform.GetChild(0).gameObject;
    //            templateToDelete.transform.SetParent(null);
    //            Destroy(templateToDelete);
    //            if (canMergeComposite) {
    //                newdisplay.transform.SetParent(null);
    //                Vector3 trueScale = newdisplay.transform.localScale;
    //                Vector3 truePosition = newdisplay.transform.localPosition;
    //                newdisplay.transform.localScale = new (1,1,1);
    //                newdisplay.transform.localPosition = new (0,0,0);
    //                MeshFilter[] meshFilters = newdisplay.GetComponentsInChildren<MeshFilter>();
    //                CombineInstance[] combine = new CombineInstance[meshFilters.Length];
//
    //                for (int k = 0; k < meshFilters.Length; k++)
    //                {
    //                    if (meshFilters[k].transform == newdisplay.transform || meshFilters[k].sharedMesh == null) {    
    //                        continue;      
    //                    }
    //                    combine[k].subMeshIndex = 0;
    //                    combine[k].mesh = meshFilters[k].sharedMesh;
    //                    combine[k].transform = meshFilters[k].transform.localToWorldMatrix;
    //                }
    //                
    //                Mesh mesh = new Mesh();
    //                mesh.CombineMeshes(combine);
    //                mesh.RecalculateTangents();
    //                newdisplay.GetComponent<MeshFilter>().sharedMesh = mesh;
    //                newdisplay.GetComponent<MeshCollider>().sharedMesh = mesh;
    //                if (transparentComposite) newdisplay.GetComponent<MeshRenderer>().SetMaterials(new(){transparentMaterial});
    //                newdisplay.GetComponent<MeshRenderer>().material.mainTexture = newdisplay.transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.mainTexture;
    //                for (int l = newdisplay.transform.childCount - 1; l >= 0; l-=1) {
    //                    Destroy(newdisplay.transform.GetChild(l).gameObject);
    //                }
    //                newdisplay.transform.localScale = trueScale;
    //                newdisplay.transform.localPosition = truePosition;
    //                newdisplay.transform.SetParent(newVariant.transform);
    //            }
    //            MinecraftModelDisplay value = new();
    //            try {
    //                composite.display.TryGetValue("head", out value);
    //                newdisplay.transform.localPosition = value.GetTranslation();
    //                newdisplay.transform.localScale = value.GetScale();
    //                newdisplay.transform.localRotation = value.GetRotation();
    //            }
    //            catch {
    //                //Doesn't exist
    //            }
    //        }
    //    }
    //    generatingModelThreads -= 1;
    //}
    private Texture2D LoadTexture(string value)
    {
        // Load the texture from the file path
        if (value != null && value != "missingno") {
            Texture2D texture = new Texture2D(2, 2); // Create a new texture (replace dimensions as needed)
            texture.filterMode = FilterMode.Point;
            byte[] fileData = File.ReadAllBytes(value); // Read all bytes from the file

            if (fileData != null)
            {
                texture.LoadImage(fileData);
                if (texture != null && LoadTextureMcmeta(value,texture) != null) return texture;
            }
        }
        return null;
    }
    private MinecraftMcmetaAnimation LoadTextureMcmeta(string value, Texture2D texture)
    {
        MinecraftMcmetaAnimation mcmeta = new(texture.width, texture.height);
        if (File.Exists(value + ".mcmeta")) {
            MinecraftMcmeta mcmeta2 = JsonConvert.DeserializeObject<MinecraftMcmeta>(File.ReadAllText(value + ".mcmeta"));
            mcmeta = mcmeta2.animation;
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
            return mcmeta;
        }
        else {
            if (texture.width == texture.height) {
                return mcmeta;
            }
            return null;
        }
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
