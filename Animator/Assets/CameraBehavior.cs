using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.EventSystems;

public class CameraBehavior : MonoBehaviour
{
    private Vector3 focusHeight = new();
    public GameObject entityModel;
    public GameObject Main;
    float cameraDistance = 5;
    private bool mouseWasDown;
    private int UILayer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameObject.transform.position = gameObject.GetComponentInParent<Transform>().transform.localPosition;
        gameObject.transform.position = focusHeight + entityModel.transform.position - transform.forward * cameraDistance;
        UILayer = LayerMask.NameToLayer("UI");
    }

    // Update is called once per frame
    void Update()
    {
        List<float> moveDirection = MoveDirection();
        if (!(moveDirection[0] == 0 && moveDirection[1] == 0 && moveDirection[2] == 0)) {
            gameObject.transform.position = focusHeight + entityModel.transform.position + transform.forward * cameraDistance;
            if (moveDirection[2] != 0) {
                cameraDistance -= moveDirection[2];
                float focusHeight = this.focusHeight.y;
                if (focusHeight < 1) focusHeight = 1;
                if (cameraDistance < 0.2) cameraDistance = 0.3f;
                if (cameraDistance > 10 * focusHeight) cameraDistance = 10 * focusHeight;
            }
            if (!(moveDirection[0] == 0 && moveDirection[1] == 0)) {
                Vector3 rotation = gameObject.transform.rotation.eulerAngles;
                float xRotation = rotation.x += moveDirection[1];
                if (xRotation > 90 && xRotation < 100) xRotation = 90;
                if (xRotation < 270 && xRotation > 100) xRotation = -90;
                gameObject.transform.rotation = Quaternion.Euler(new Vector3(xRotation,rotation.y += moveDirection[0],0));
            }
            gameObject.transform.position = focusHeight + entityModel.transform.position - transform.forward * cameraDistance;
        }
    }
    private List<float> MoveDirection() {
        List<float> floats = new();
        Ray ray = Camera.main.ScreenPointToRay (Input.mousePosition);
        RaycastHit hit;
        if (Input.GetKey(KeyCode.Mouse0)) {
            if (IsPointerOverUIElement(GetEventSystemRaycastResults())) {
                floats.Add(0);
                floats.Add(0);
                mouseWasDown = true;
            }
            else {
                if (mouseWasDown == false && Physics.Raycast (ray, out hit, 100)) {
                    Main.GetComponent<Main>().SetSelectedModelPart(hit.transform.parent.parent.parent.parent.gameObject);
                    floats.Add(0);
                    floats.Add(0);
                }
                else {
                    floats.Add(Input.mousePositionDelta.x);
                    floats.Add(Input.mousePositionDelta.y);
                    mouseWasDown = true;
                }
            }
        }
        else {
            floats.Add(0);
            floats.Add(0);
            mouseWasDown = false;
        }
        if (!IsPointerOverUIElement(GetEventSystemRaycastResults())) {
            floats.Add(Input.mouseScrollDelta.y);
        }
        else floats.Add(0);
        return floats;
    }
    
    //Returns 'true' if we touched or hovering on Unity UI element.
    private bool IsPointerOverUIElement(List<RaycastResult> eventSystemRaysastResults)
    {
        for (int index = 0; index < eventSystemRaysastResults.Count; index++)
        {
            RaycastResult curRaysastResult = eventSystemRaysastResults[index];
            if (curRaysastResult.gameObject.layer == UILayer)
                return true;
        }
        return false;
    }


    //Gets all event system raycast results of current mouse or touch position.
    static List<RaycastResult> GetEventSystemRaycastResults()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raysastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raysastResults);
        return raysastResults;
    }
    public void SetModelHeight(float height) {
        if (height / 2 > focusHeight.y) focusHeight.y = height / 2; 
        if (height == 0) focusHeight.y = 0;
    }
}
