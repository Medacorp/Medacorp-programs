using System.Collections.Generic;
using UnityEngine;

public class TextureAnimator : MonoBehaviour
{
    private List<MinecraftMcmetaAnimation> animationMcmetas;
    private List<TextureAnimatorFrame> textureAnimatorFrames;
    private Rect[] rectangles;
    private Texture2D textureAtlas;
    public Mesh unalteredMesh;
    public bool highlight = false;
    private bool wasHighlight = false;
    public float highlightDuration;
    private static bool animationsMayRun = false;
    private bool wasEnabled = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (animationsMayRun) {
            wasEnabled = true;
            int i = 0;
            foreach (MinecraftMcmetaAnimation animation in animationMcmetas) {
                if (animation.parsedFrames.Count > 1) {
                    textureAnimatorFrames[i].frameTime -= Time.deltaTime * 20;
                    if (animation.interpolate) {
                        NextInterpolatedFrame(animation, textureAnimatorFrames[i], rectangles[i]);
                    }
                    else if (textureAnimatorFrames[i].frameTime <= 0) NextFrame(animation, textureAnimatorFrames[i], rectangles[i]);
                }
                i++;
            }
        }
        else {
            if (wasEnabled) {
                int i = 0;
                foreach (Rect rectangle in rectangles) {
                    Texture2D newTexture = animationMcmetas[i].GetFrame(0);
                    textureAtlas.SetPixels(Mathf.FloorToInt(rectangle.position.x * textureAtlas.width), Mathf.FloorToInt(rectangle.position.y * textureAtlas.height), Mathf.FloorToInt(rectangle.width * textureAtlas.width), Mathf.FloorToInt(rectangle.height * textureAtlas.height), newTexture.GetPixels());
                    if (animationMcmetas[i].parsedFrames.Count > 1) textureAnimatorFrames[i].frameTime = animationMcmetas[i].parsedFrames[0].time;
                }
            }
            wasEnabled = false;
        }
        if (highlight) {
            wasHighlight = true;
            highlightDuration += Time.deltaTime;
            while (highlightDuration >= 2) {
                highlightDuration -= 2;
            }
            float value;
            if (highlightDuration < 1) value = highlightDuration/4;
            else value = 0.25f-(highlightDuration-1)/4;
            gameObject.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor",new(value,value,value));
        }
        else if (wasHighlight) {
            wasHighlight = false;
            highlightDuration = 0;
            gameObject.GetComponent<MeshRenderer>().material.SetColor("_EmissionColor",new(0,0,0));

        }
    }
    public static void ToggleAnimations(bool value) {
        animationsMayRun = value;
    }
    public static bool EnabledAnimations() {
        return animationsMayRun;
    }
    public void SetValues(List<MinecraftMcmetaAnimation> animationMcmetas, Texture2D textureAtlas, Rect[] rectangles) {
        this.animationMcmetas = animationMcmetas;
        this.textureAtlas = textureAtlas;
        this.rectangles = rectangles;
        textureAnimatorFrames = new();
        foreach (MinecraftMcmetaAnimation animation in animationMcmetas) {
            TextureAnimatorFrame frame = new();
            if (animation.parsedFrames.Count > 1) frame.frameTime = animation.parsedFrames[0].time;
            textureAnimatorFrames.Add(new());
        }
    }
    public void UpdateTexture() {
        textureAtlas.Apply();
        gameObject.GetComponent<MeshRenderer>().material.mainTexture = textureAtlas;
    }
    public void NextFrame(MinecraftMcmetaAnimation animation, TextureAnimatorFrame animator, Rect rectangle) {
        animator.currentFrame += 1;
        if (animator.currentFrame == animation.parsedFrames.Count) animator.currentFrame = 0;
        Texture2D newTexture = animation.GetFrame(animator.currentFrame);
        textureAtlas.SetPixels(Mathf.FloorToInt(rectangle.position.x * textureAtlas.width), Mathf.FloorToInt(rectangle.position.y * textureAtlas.height), Mathf.FloorToInt(rectangle.width * textureAtlas.width), Mathf.FloorToInt(rectangle.height * textureAtlas.height), newTexture.GetPixels());
        UpdateTexture();
        animator.frameTime += animation.parsedFrames[animator.currentFrame].time;
        if (animator.frameTime <= 0) NextFrame(animation, animator, rectangle);
    }
    public void NextInterpolatedFrame(MinecraftMcmetaAnimation animation, TextureAnimatorFrame animator, Rect rectangle) {
        int nextFrame = animator.currentFrame + 1;
        if (nextFrame == animation.parsedFrames.Count) nextFrame = 0;
        Color[] currentFramePixels = animation.GetFrame(animator.currentFrame).GetPixels(Mathf.FloorToInt(rectangle.position.x * textureAtlas.width), Mathf.FloorToInt(rectangle.position.y * textureAtlas.height), Mathf.FloorToInt(rectangle.width * textureAtlas.width), Mathf.FloorToInt(rectangle.height * textureAtlas.height));
        Color[] nextFramePixels = animation.GetFrame(nextFrame).GetPixels(Mathf.FloorToInt(rectangle.position.x * textureAtlas.width), Mathf.FloorToInt(rectangle.position.y * textureAtlas.height), Mathf.FloorToInt(rectangle.width * textureAtlas.width), Mathf.FloorToInt(rectangle.height * textureAtlas.height));
        List<Color> newPixels = new();
        for (int i = 0; i < currentFramePixels.Length; i++) {
            Color newPixel = new(
                currentFramePixels[i].r - currentFramePixels[i].r * (animation.parsedFrames[animator.currentFrame].time - animator.frameTime) / animation.parsedFrames[animator.currentFrame].time + nextFramePixels[i].r - nextFramePixels[i].r * ((animation.parsedFrames[animator.currentFrame].time - animator.frameTime) / animation.parsedFrames[animator.currentFrame].time * -1 + 1),
                currentFramePixels[i].g - currentFramePixels[i].g * (animation.parsedFrames[animator.currentFrame].time - animator.frameTime) / animation.parsedFrames[animator.currentFrame].time + nextFramePixels[i].g - nextFramePixels[i].g * ((animation.parsedFrames[animator.currentFrame].time - animator.frameTime) / animation.parsedFrames[animator.currentFrame].time * -1 + 1),
                currentFramePixels[i].b - currentFramePixels[i].b * (animation.parsedFrames[animator.currentFrame].time - animator.frameTime) / animation.parsedFrames[animator.currentFrame].time + nextFramePixels[i].b - nextFramePixels[i].b * ((animation.parsedFrames[animator.currentFrame].time - animator.frameTime) / animation.parsedFrames[animator.currentFrame].time * -1 + 1),
                currentFramePixels[i].a - currentFramePixels[i].a * (animation.parsedFrames[animator.currentFrame].time - animator.frameTime) / animation.parsedFrames[animator.currentFrame].time + nextFramePixels[i].a - nextFramePixels[i].a * ((animation.parsedFrames[animator.currentFrame].time - animator.frameTime) / animation.parsedFrames[animator.currentFrame].time * -1 + 1)
            );
            if (currentFramePixels[i].a == 0) {
                newPixel.r = nextFramePixels[i].r;
                newPixel.g = nextFramePixels[i].g;
                newPixel.b = nextFramePixels[i].b;
            }
            else if (nextFramePixels[i].a == 0) {
                newPixel.r = currentFramePixels[i].r;
                newPixel.g = currentFramePixels[i].g;
                newPixel.b = currentFramePixels[i].b;
            }
            newPixels.Add(newPixel);
        }
        textureAtlas.SetPixels(Mathf.FloorToInt(rectangle.position.x * textureAtlas.width), Mathf.FloorToInt(rectangle.position.y * textureAtlas.height), Mathf.FloorToInt(rectangle.width * textureAtlas.width), Mathf.FloorToInt(rectangle.height * textureAtlas.height), newPixels.ToArray());
        UpdateTexture();
        if (animator.frameTime <= 0) {
            animator.currentFrame = nextFrame;
            animator.frameTime += animation.parsedFrames[animator.currentFrame].time;
            if (animator.frameTime <= 0) NextFrame(animation, animator, rectangle);
        }
    }
}
public class TextureAnimatorFrame {
    public int currentFrame = 0;
    public float frameTime = 0;
}