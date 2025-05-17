using System.Collections.Generic;
using UnityEngine;

public class ModelDisplayPart : MonoBehaviour
{
    private GameObject rotationPoint;
    private GameObject animationValues;
    private GameObject defaultOffset;
    private GameObject templateModel;
    private GameObject templateMesh;
    public Material transparentMaterial;
    public MapEntityPart part;
    private float[] offsets = { 0, 0, 0 };
    public void SetOffsets(float[] values) {
        offsets = values;
        gameObject.transform.localPosition = new(values[0],values[1],values[2]);
    }
    public void SetRotation(float[] values) {
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
        animationValues.transform.localRotation = new(x,y,z,w);
    }
    public void SetRotation(Quaternion value)
    {
        animationValues.transform.localRotation = value;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rotationPoint = gameObject.transform.GetChild(0).gameObject;
        animationValues = rotationPoint.transform.GetChild(0).gameObject;
        defaultOffset = animationValues.transform.GetChild(0).gameObject;
        templateModel = defaultOffset.transform.GetChild(0).gameObject;
        templateMesh = templateModel.transform.GetChild(0).gameObject;
        GenerateModels();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void GenerateModels() {
        for (int j = defaultOffset.transform.childCount; j < 1; j--)
        {
            Destroy(defaultOffset.transform.GetChild(j - 1).gameObject);
        }
        foreach (MinecraftItemModelFetched fetch in part.GetModels())
        {
            ParsedMinecraftModel parsed = MinecraftModel.parsedModels[fetch.modelIndex];
            List<Color> tints = parsed.GetTints(part.minecraft_item);
            GameObject newModel = Instantiate(templateModel, new Vector3(0, 0, 0), new Quaternion(), defaultOffset.transform);
            MinecraftModelDisplay display = new();
            parsed.display.TryGetValue("head", out display);
            newModel.transform.localRotation = display.GetRotation();
            newModel.transform.localScale = display.GetScale();
            newModel.transform.localPosition = display.GetTranslation();
            newModel.SetActive(true);
            //newModel.transform.GetChild(0).gameObject.GetComponent<MeshCollider>().sharedMesh = parsed.untintedMesh;
            newModel.transform.GetChild(0).gameObject.GetComponent<MeshFilter>().sharedMesh = parsed.untintedMesh;
            if (parsed.untintedTransparent) newModel.transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material = transparentMaterial;
            newModel.transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.mainTexture = TextureAtlas.atlasTexture;
            for (int i = 0; i < parsed.tintedMeshes.Count; i++)
            {
                GameObject newTintedMesh = Instantiate(templateMesh, new Vector3(0, 0, 0), new Quaternion(), newModel.transform);
                newTintedMesh.transform.localPosition = new(0, 0, 0);
                newTintedMesh.SetActive(true);
                //newTintedMesh.GetComponent<MeshCollider>().sharedMesh = parsed.tintedMeshes[i];
                newTintedMesh.GetComponent<MeshFilter>().sharedMesh = parsed.tintedMeshes[i];
                if (parsed.tintedTransparent[i]) newTintedMesh.GetComponent<MeshRenderer>().material = transparentMaterial;
                newTintedMesh.GetComponent<MeshRenderer>().material.color = tints[i];
                newTintedMesh.GetComponent<MeshRenderer>().material.mainTexture = TextureAtlas.atlasTexture;
            }
        }
    }
}