using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;

public class ModelDisplay : MonoBehaviour
{
    private List<MinecraftModel> model;

    public GameObject cameraObject;

    public GameObject Main;
    private GameObject rotationPoint;
    private GameObject animationValues;
    private GameObject defaultOffset;
    private GameObject display;
    private GameObject templateElement;
    public Material transparentMaterial;
    private List<GameObject> displays;

    private List<ConditionalModelPartOffset> conditionalOffsets;

    private List<ConditionalModelPartPose> conditionalPoses;

    private float[] offsets = {0,0,0};

    public void SetOffsets(List<ConditionalModelPartOffset> conditionalOffsets) {
        this.conditionalOffsets = conditionalOffsets;
        GetOffsets();
    }

    public void SetPoses(List<ConditionalModelPartPose> conditionalPoses) {
        this.conditionalPoses = conditionalPoses;
        GetPoses();
    }
    public void SetOffsets(float[] values) {
        offsets = values;
        gameObject.transform.localPosition = new(values[0],values[1],values[2]);
    }
    public void SetPoses(float[] values) {
        offsets = values;
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
    }
    public void GetOffsets() {
        SetOffsets(GetNewOffsets());
    }
    public void GetPoses() {
        SetPoses(GetNewPoses());
    }
    public float[] GetNewOffsets() {
        offsets[0] = 0;
        offsets[1] = 0;
        offsets[2] = 0;
        Main mainScript = Main.GetComponent<Main>();
        foreach (ConditionalModelPartOffset offset in conditionalOffsets) {
            if (offset.tags != null && offset.tags.Count != 0){
                bool success = true;
                foreach (KeyValuePair<string,bool> tag in offset.tags) {
                    if (mainScript.enabledTags.Contains(tag.Key) != tag.Value) success = false;
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
        return poses;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rotationPoint = gameObject.transform.GetChild(0).gameObject;
        animationValues = rotationPoint.transform.GetChild(0).gameObject;
        defaultOffset = animationValues.transform.GetChild(0).gameObject;
        display = defaultOffset.transform.GetChild(0).gameObject;
        templateElement = display.transform.GetChild(0).gameObject;
        displays = new();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void SetModel(List<MinecraftModel> model) {
        this.model = model;
        if (animationValues == null) Start();
        if (displays.Count != 0) {
            foreach (GameObject display in displays) {
                if (display != this.display) Destroy(display);
            }
        }
        displays = new();
        int i = 0;
        float heighestPoint = 0;
        foreach (MinecraftModel composite in model) {
            GameObject newdisplay = Instantiate(display, new Vector3(0,0,0), new Quaternion(), defaultOffset.transform);
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
            displays.Add(newdisplay);
            int j = 0;
            foreach (MinecraftModelElement element in composite.elements) {
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
                    modelFace.transform.Rotate(modelFace.transform.InverseTransformDirection(modelFace.transform.up), face.Value.rotation, Space.Self);
                    if (modelFace != null) {
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
                j++;
            }
        }
        cameraObject.GetComponent<CameraBehavior>().SetModelHeight(heighestPoint);
    }
    Texture2D LoadTexture(string key, MinecraftModel model, GameObject modelFace)
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

        int minX = Mathf.FloorToInt(uvMin.x * texture.width);
        int minY = Mathf.FloorToInt(uvMin.y * texture.height);
        int maxX = Mathf.FloorToInt(uvMax.x * texture.width);
        int maxY = Mathf.FloorToInt(uvMax.y * texture.height);

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
