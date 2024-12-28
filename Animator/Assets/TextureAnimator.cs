using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;

public class TextureAnimator : MonoBehaviour
{
    public List<MinecraftMcmetaAnimation> animationMcmetas;
    public List<TextureAnimatorFrame> textureAnimatorFrames;
    public Rect[] rectangles;
    public Texture2D textureAtlas;
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
            gameObject.GetComponent<MeshRenderer>().material.mainTexture = textureAtlas;
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
    public void NextFrame(MinecraftMcmetaAnimation animation, TextureAnimatorFrame animator, Rect rectangle) {
        animator.currentFrame += 1;
        if (animator.currentFrame == animation.parsedFrames.Count) animator.currentFrame = 0;
        Texture2D newTexture = animation.GetFrame(animator.currentFrame);
        textureAtlas.SetPixels(Mathf.FloorToInt(rectangle.position.x * textureAtlas.width), Mathf.FloorToInt(rectangle.position.y * textureAtlas.height), Mathf.FloorToInt(rectangle.width * textureAtlas.width), Mathf.FloorToInt(rectangle.height * textureAtlas.height), newTexture.GetPixels());
        animator.frameTime += animation.parsedFrames[animator.currentFrame].time;
        if (animator.frameTime <= 0) NextFrame(animation, animator, rectangle);
    }
    public void NextInterpolatedFrame(MinecraftMcmetaAnimation animation, TextureAnimatorFrame animator, Rect rectangle) {
        int nextFrame = animator.currentFrame + 1;
        if (nextFrame == animation.parsedFrames.Count) nextFrame = 0;
        float frameTimeAmount = animation.parsedFrames[animator.currentFrame].time / animator.frameTime;
        Color[] currentFramePixels = animation.GetFrame(animator.currentFrame).GetPixels(Mathf.FloorToInt(rectangle.position.x * textureAtlas.width), Mathf.FloorToInt(rectangle.position.y * textureAtlas.height), Mathf.FloorToInt(rectangle.width * textureAtlas.width), Mathf.FloorToInt(rectangle.height * textureAtlas.height));
        Color[] nextFramePixels = animation.GetFrame(nextFrame).GetPixels(Mathf.FloorToInt(rectangle.position.x * textureAtlas.width), Mathf.FloorToInt(rectangle.position.y * textureAtlas.height), Mathf.FloorToInt(rectangle.width * textureAtlas.width), Mathf.FloorToInt(rectangle.height * textureAtlas.height));
        List<Color> newPixels = new();
        for (int i = 0; i < currentFramePixels.Length; i++) {
            Color newPixel = new(
                (currentFramePixels[i].r - (frameTimeAmount - animation.parsedFrames[0].time) / currentFramePixels[i].r + nextFramePixels[i].r - (frameTimeAmount - animation.parsedFrames[0].time) / nextFramePixels[i].r) / 2,
                (currentFramePixels[i].g - (frameTimeAmount - animation.parsedFrames[0].time) / currentFramePixels[i].g + nextFramePixels[i].g - (frameTimeAmount - animation.parsedFrames[0].time) / nextFramePixels[i].g) / 2,
                (currentFramePixels[i].b - (frameTimeAmount - animation.parsedFrames[0].time) / currentFramePixels[i].b + nextFramePixels[i].b - (frameTimeAmount - animation.parsedFrames[0].time) / nextFramePixels[i].b) / 2,
                (currentFramePixels[i].a - (frameTimeAmount - animation.parsedFrames[0].time) / currentFramePixels[i].a + nextFramePixels[i].a - (frameTimeAmount - animation.parsedFrames[0].time) / nextFramePixels[i].a) / 2
            );
            newPixels.Add(newPixel);
        }
        textureAtlas.SetPixels(Mathf.FloorToInt(rectangle.position.x * textureAtlas.width), Mathf.FloorToInt(rectangle.position.y * textureAtlas.height), Mathf.FloorToInt(rectangle.width * textureAtlas.width), Mathf.FloorToInt(rectangle.height * textureAtlas.height), newPixels.ToArray());
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