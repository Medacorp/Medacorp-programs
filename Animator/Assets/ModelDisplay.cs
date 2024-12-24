using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;

public class ModelDisplay : MonoBehaviour
{
    List<MinecraftModel> model;

    public GameObject cameraObject;

    public GameObject Main;
    GameObject animationValues;
    GameObject autoScale;
    GameObject display;
    GameObject templateElement;
    public Material transparentMaterial;
    List<GameObject> displays;

    List<ConditionalModelPartOffset> conditionalOffsets;

    private float[] offsets = {0,0,0};

    public void SetOffsets(List<ConditionalModelPartOffset> conditionalOffsets) {
        this.conditionalOffsets = conditionalOffsets;
        SetOffsets(GetOffsets());
    }
    public void SetOffsets(float[] values) {
        offsets = values;
        gameObject.transform.localPosition = new(values[0],values[1],values[2]);
    }
    public float[] GetOffsets() {
        Main mainScript = Main.GetComponent<Main>();
        foreach (ConditionalModelPartOffset offset in conditionalOffsets) {
            bool success = true;
            if (offset.tags != null && offset.tags.Count != 0){
                foreach (KeyValuePair<string,bool> tag in offset.tags) {
                    if (mainScript.enabledTags.Contains(tag.Key) != tag.Value) {
                        success = false;
                        break;
                    }
                }
            }
            if (success) {
                offsets = offset.GetOffsets();
                return offsets;
            }
        }
        offsets[0] = 0;
        offsets[1] = 0;
        offsets[2] = 0;
        return offsets;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        animationValues = gameObject.transform.GetChild(0).gameObject;
        autoScale = animationValues.transform.GetChild(0).gameObject;
        display = autoScale.transform.GetChild(0).gameObject;
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
            GameObject newdisplay = Instantiate(display, new Vector3(0,0,0), new Quaternion(), autoScale.transform);
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
                        modelFace.SetActive(true);
                        Mesh mesh = modelFace.GetComponent<MeshFilter>().mesh;
                        List<float> uv = face.Value.uv.ToList();
                        Vector2[] uvs = mesh.uv;
                        Vector2 uvStart = new Vector2(uv[0] / 16, uv[1] / 16 * -1 + 1);
                        Vector2 uvEnd = new Vector2(uv[2] / 16, uv[3] / 16 * -1 + 1);
                        for (int l = 0; l < uvs.Length; l++)
                        {
                            uvs[l] = new Vector2(
                                Mathf.Lerp(uvStart.x, uvEnd.x, uvs[l].x), 
                                Mathf.Lerp(uvStart.y, uvEnd.y, uvs[l].y)
                            );
                        }
                        mesh.uv = uvs;
                        modelFace.GetComponent<MeshFilter>().mesh = mesh;
                        Texture2D texture = LoadTexture(face.Value.texture.Replace("#",""),composite);
                        if (texture != null)
                        {
                            if (IsAnyPixelTransparent(texture, new(uv[0] / 16, uv[3] / 16 * -1 + 1), new(uv[2] / 16, uv[1] / 16 * -1 + 1))) {
                                modelFace.GetComponent<MeshRenderer>().SetMaterials(new(){transparentMaterial});
                            }
                            modelFace.GetComponent<MeshRenderer>().material.mainTexture = texture;
                        }

                    }
                }
                if (heighestPoint < newelement.transform.position.y + newelement.transform.localScale.y) heighestPoint = newelement.transform.position.y + newelement.transform.localScale.y;
                j++;
            }
        }
        cameraObject.GetComponent<CameraBehavior>().SetModelHeight(heighestPoint);
    }
    Texture2D LoadTexture(string key, MinecraftModel model)
    {
        string path = null;
        if (model.textures.ContainsKey(key)) path = model.textures[key];
        // Load the texture from the file path
        if (path != null && path != "missingno") {
            Texture2D texture = new Texture2D(2, 2); // Create a new texture (replace dimensions as needed)
            texture.filterMode = FilterMode.Point;
            byte[] fileData = System.IO.File.ReadAllBytes(path); // Read all bytes from the file

            if (fileData != null)
            {
                // Load the image data into the texture
                texture.LoadImage(fileData);
                if (texture != null) return texture;
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
