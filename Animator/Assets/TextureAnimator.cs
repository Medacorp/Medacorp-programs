using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor.Rendering;
using UnityEngine;

public class TextureAnimator : MonoBehaviour
{
    public MinecraftMcmetaAnimation animationMcmeta;
    private List<float> uv;

    private int width = 0;
    private int height = 0;

    private List<TextureAnimatorFrame> frames;
    private float frameTime = 0;
    private int currentFrame = -1;
    private Vector2[] originalUVs;
    private static bool animationsMayRun = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Texture texture = gameObject.GetComponent<MeshRenderer>().material.mainTexture;
        width = texture.width;
        height = texture.height;
        originalUVs = gameObject.GetComponent<MeshFilter>().mesh.uv;
        if (uv != null && uv.Count != 0) SetFrames();
    }

    // Update is called once per frame
    void Update()
    {
        if (animationsMayRun) {
            if (frames != null && frames.Count > 1 ) {
                frameTime = frameTime - Time.deltaTime * 20;
                if (frameTime <= 0) GetNextFrame();
            }
        }
        else {
            currentFrame = -1;
            frameTime = 0;
        }
    }
    public static void ToggleAnimations(bool value) {
        animationsMayRun = value;
    }
    public static bool EnabledAnimations() {
        return animationsMayRun;
    }
    public void SetUV(List<float> newUVs) {
        uv = new(){
            newUVs[0] / 16,
            newUVs[1] / 16,
            newUVs[2] / 16,
            newUVs[3] / 16
        };
        if (width != 0) SetFrames();
    }
    private void SetFrames() {
        frames = new();
        int textureHeight = height / animationMcmeta.height;
        int textureWidth = width / animationMcmeta.width;
        float frameHeight = 1 / (float)textureHeight;
        float frameWidth = 1 / (float)textureWidth;
        if (animationMcmeta.frames != null && animationMcmeta.frames.Count != 0) {
            foreach (MinecraftMcmetaAnimationFrame frame in animationMcmeta.frames) {
                TextureAnimatorFrame newframe = new();
                int horizontalFrame = frame.index / textureHeight - 1;
                int verticalFrame = frame.index % textureHeight - 1;
                newframe.time = frame.time;
                newframe.uv = new(){
                    uv[0] + frameWidth * horizontalFrame,
                    uv[1] + frameHeight * verticalFrame,
                    uv[2] + frameWidth * horizontalFrame,
                    uv[3] + frameHeight * verticalFrame
                };
                frames.Add(newframe);
            }
        }
        else {
            int time = 1;
            if (animationMcmeta.frametime != 0) time = animationMcmeta.frametime;
            for (int horizontalFrame = 0; horizontalFrame != textureWidth; horizontalFrame++) {
                for (int verticalFrame = 0; verticalFrame != textureHeight; verticalFrame++) {
                    TextureAnimatorFrame newframe = new();
                    newframe.time = time;
                    newframe.uv = new(){
                        uv[0] + frameWidth * horizontalFrame,
                        uv[1] + frameHeight * verticalFrame,
                        uv[2] + frameWidth * horizontalFrame,
                        uv[3] + frameHeight * verticalFrame
                    };
                    frames.Add(newframe);
                }
            }
        }
        SetFrame(0);
    }
    private void GetNextFrame() {
        int index = currentFrame + 1;
        if (index == frames.Count) index = 0;
        SetFrame(index);
    }
    private void SetFrame(int index) {
        Mesh mesh = gameObject.GetComponent<MeshFilter>().mesh;
        Vector2[] uvs = mesh.uv;
        Vector2 uvStart = new Vector2(frames[index].uv[0], frames[index].uv[1] * -1 + 1);
        Vector2 uvEnd = new Vector2(frames[index].uv[2], frames[index].uv[3] * -1 + 1);
        for (int l = 0; l < uvs.Length; l++)
        {
            
            uvs[l] = new Vector2(
                Mathf.Lerp(uvStart.x, uvEnd.x, originalUVs[l].x), 
                Mathf.Lerp(uvStart.y, uvEnd.y, originalUVs[l].y)
            );
        }
        mesh.uv = uvs;
        gameObject.GetComponent<MeshFilter>().mesh = mesh;
        currentFrame = index;
        frameTime += frames[index].time;
    }
}
public class TextureAnimatorFrame {
    public List<float> uv;
    public float time;
}
