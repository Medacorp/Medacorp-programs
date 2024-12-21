using System.Collections.Generic;
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
                Destroy(display);
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
            }
            catch {
                //Doesn't exist
            }
            newdisplay.transform.localPosition = value.GetTranslation();
            newdisplay.transform.localScale = value.GetScale();
            newdisplay.transform.localRotation = value.GetRotation();
            i++;
            displays.Add(newdisplay);
            int j = 0;
            foreach (MinecraftModelElement element in composite.elements) {
                GameObject newelement = Instantiate(templateElement, new Vector3(0,0,0), new Quaternion(), newdisplay.transform);
                newelement.SetActive(true);
                newelement.name = "ModelElement"+j.ToString();
                newelement.transform.localScale = element.GetSize();
                newelement.transform.localPosition = element.GetCenter();
                newelement.transform.RotateAround(newdisplay.transform.TransformPoint(element.GetRotationPoint()), element.GetRotationAxis(), -element.GetRotationAngle());
                if (heighestPoint < newelement.transform.position.y + newelement.transform.localScale.y) heighestPoint = newelement.transform.position.y + newelement.transform.localScale.y;
                j++;
            }
        }
        cameraObject.GetComponent<CameraBehavior>().SetModelHeight(heighestPoint);
    }
}
