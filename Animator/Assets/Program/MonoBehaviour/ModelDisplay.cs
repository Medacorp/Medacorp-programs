using System.Collections.Generic;
using UnityEngine;

public class ModelDisplay : MonoBehaviour
{
    private List<GameObject> createdParts = new();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void GenerateModels(List<MapEntityPart> model_parts)
    {
        DeleteModel();
        foreach (MapEntityPart part in model_parts)
        {
            GameObject newPart = Instantiate(gameObject.transform.GetChild(0).gameObject, new Vector3(0, 0, 0), new Quaternion(), gameObject.transform);
            newPart.transform.localPosition = new(0, 0, 0);
            newPart.SetActive(true);
            newPart.GetComponent<ModelDisplayPart>().part = part;
            createdParts.Add(newPart);
        }
    }
    public void DeleteModel()
    {
        foreach (GameObject part in createdParts)
        {
            Destroy(part);
        }
        createdParts = new();
    }
}