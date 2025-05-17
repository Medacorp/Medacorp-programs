using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public static class TextureAtlas
{
    public static Texture2D atlasTexture = new(2, 2);
    public static Rect[] rectangles = { };
    public static List<string> textures = new();
    public static List<string> texturesInUse = new();
    private static bool animationsMayRun = true;
    private static List<MinecraftMcmetaAnimation> textureAnimations = new();
    public static void UseTexture(string texture) {
        if (!texturesInUse.Contains(texture)) texturesInUse.Add(texture);
    }
    public static bool TextureIsAnimated(string texture) {
        if (textures.Contains(texture)) return textureAnimations[textures.IndexOf(texture)].parsedFrames.Count > 1;
        return false;
    }
    public static Rect GetRectangleForTexture(string texture) {
        if (textures.Contains(texture)) return rectangles[textures.IndexOf(texture)];
        return rectangles[textures.IndexOf("minecraft:missingno")];
    }
    public static void ToggleAnimations(bool value)
    {
        animationsMayRun = value;
    }
    public static bool EnabledAnimations()
    {
        return animationsMayRun;
    }
    public static void AdvanceAnimations(float time)
    {
        if (animationsMayRun)
        {
            foreach (MinecraftMcmetaAnimation animation in textureAnimations.Where(a => a.parsedFrames.Count > 1))
            {
                int i = textureAnimations.IndexOf(animation);
                animation.currentFrameTime -= time;
                if (animation.interpolate) NextInterpolatedFrame(animation, rectangles[i], i);
                else if (animation.currentFrameTime <= 0) NextFrame(animation, rectangles[i], i);
            }
        }
    }
    private static void UpdateTexture()
    {
        atlasTexture.Apply();
        //string executionPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        //executionPath = executionPath.Remove(executionPath.Length - 53);
        //File.WriteAllBytes(executionPath + "animator settings/atlas.png", atlasTexture.EncodeToPNG());
    }
    private static void NextFrame(MinecraftMcmetaAnimation animation, Rect rectangle, int textureIndex)
    {
        animation.currentFrame += 1;
        if (animation.currentFrame >= animation.parsedFrames.Count) animation.currentFrame = 0;
        Texture2D newTexture = animation.GetFrame(animation.currentFrame);
        atlasTexture.SetPixels(Mathf.FloorToInt(rectangle.position.x * atlasTexture.width), Mathf.FloorToInt(rectangle.position.y * atlasTexture.height), Mathf.FloorToInt(rectangle.width * atlasTexture.width), Mathf.FloorToInt(rectangle.height * atlasTexture.height), newTexture.GetPixels());
        UpdateTexture();
        animation.currentFrameTime += animation.parsedFrames[animation.currentFrame].time;
        if (animation.currentFrameTime <= 0) NextFrame(animation, rectangle, textureIndex);
    }
    private static void NextInterpolatedFrame(MinecraftMcmetaAnimation animation, Rect rectangle, int textureIndex)
    {
        int nextFrame = animation.currentFrame + 1;
        if (nextFrame >= animation.parsedFrames.Count) nextFrame = 0;
        Texture2D currentFrameTexture = animation.GetFrame(animation.currentFrame);
        Texture2D nextFrameTexture = animation.GetFrame(nextFrame);
        Color[] currentFramePixels = currentFrameTexture.GetPixels(0, 0, currentFrameTexture.width, currentFrameTexture.height);
        Color[] nextFramePixels = nextFrameTexture.GetPixels(0, 0, nextFrameTexture.width, nextFrameTexture.height);
        List<Color> newPixels = new();
        for (int i = 0; i < currentFramePixels.Length; i++)
        {
            Color newPixel = new(
                currentFramePixels[i].r - currentFramePixels[i].r * (animation.parsedFrames[animation.currentFrame].time - animation.currentFrameTime) / animation.parsedFrames[animation.currentFrame].time + nextFramePixels[i].r - nextFramePixels[i].r * ((animation.parsedFrames[animation.currentFrame].time - animation.currentFrameTime) / animation.parsedFrames[animation.currentFrame].time * -1 + 1),
                currentFramePixels[i].g - currentFramePixels[i].g * (animation.parsedFrames[animation.currentFrame].time - animation.currentFrameTime) / animation.parsedFrames[animation.currentFrame].time + nextFramePixels[i].g - nextFramePixels[i].g * ((animation.parsedFrames[animation.currentFrame].time - animation.currentFrameTime) / animation.parsedFrames[animation.currentFrame].time * -1 + 1),
                currentFramePixels[i].b - currentFramePixels[i].b * (animation.parsedFrames[animation.currentFrame].time - animation.currentFrameTime) / animation.parsedFrames[animation.currentFrame].time + nextFramePixels[i].b - nextFramePixels[i].b * ((animation.parsedFrames[animation.currentFrame].time - animation.currentFrameTime) / animation.parsedFrames[animation.currentFrame].time * -1 + 1),
                currentFramePixels[i].a - currentFramePixels[i].a * (animation.parsedFrames[animation.currentFrame].time - animation.currentFrameTime) / animation.parsedFrames[animation.currentFrame].time + nextFramePixels[i].a - nextFramePixels[i].a * ((animation.parsedFrames[animation.currentFrame].time - animation.currentFrameTime) / animation.parsedFrames[animation.currentFrame].time * -1 + 1)
            );
            if (currentFramePixels[i].a == 0)
            {
                newPixel.r = nextFramePixels[i].r;
                newPixel.g = nextFramePixels[i].g;
                newPixel.b = nextFramePixels[i].b;
            }
            else if (nextFramePixels[i].a == 0)
            {
                newPixel.r = currentFramePixels[i].r;
                newPixel.g = currentFramePixels[i].g;
                newPixel.b = currentFramePixels[i].b;
            }
            newPixels.Add(newPixel);
        }
        atlasTexture.SetPixels(Mathf.FloorToInt(rectangle.position.x * atlasTexture.width), Mathf.FloorToInt(rectangle.position.y * atlasTexture.height), Mathf.FloorToInt(rectangle.width * atlasTexture.width), Mathf.FloorToInt(rectangle.height * atlasTexture.height), newPixels.ToArray());
        UpdateTexture();
        if (animation.currentFrameTime <= 0)
        {
            animation.currentFrame = nextFrame;
            animation.currentFrameTime += animation.parsedFrames[animation.currentFrame].time;
            if (animation.currentFrameTime <= 0) NextFrame(animation, rectangle, textureIndex);
        }
    }
    public static void ParseAtlas(string path, string minecraftPath)
    {
        atlasTexture = new(2, 2);
        atlasTexture.filterMode = FilterMode.Point;
        List<Texture2D> Textures;
        textureAnimations = new();
        textures = new();
        textures = GetAtlases(path, out Textures, out textureAnimations, minecraftPath);
        rectangles = atlasTexture.PackTextures(Textures.ToArray(), 0, 8192);
    }
    public static List<string> GetAtlases(string path, out List<Texture2D> Textures, out List<MinecraftMcmetaAnimation> MCMetas, string minecraftPath)
    {
        MinecraftAtlas atlas = new();
        atlas.sources = MinecraftAtlas.vanillaSources;
        if (File.Exists(path + "assets/minecraft/atlases/blocks.json"))
        {
            MinecraftAtlas packAtlas = JsonConvert.DeserializeObject<MinecraftAtlas>(File.ReadAllText(path + "assets/minecraft/atlases/blocks.json"));
            foreach (object source in packAtlas.sources) atlas.sources.Add(source);
        }
        atlas.Parse();
        MCMetas = new();
        Textures = new();
        List<string> textureNames = atlas.GetTextures(path, minecraftPath, out Textures, out MCMetas);

        //Add Missingno, which is always present
        Texture2D texture = new Texture2D(16, 16);
        texture.filterMode = FilterMode.Point;
        List<Color> colors = new();
        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 16; y++)
            {
                if ((y < 8 && x < 8) || (y >= 8 && x >= 8)) colors.Add(new Color32(0, 0, 0, 255));
                else colors.Add(new Color32(251, 60, 249, 255));
            }
        }
        texture.SetPixels(colors.ToArray());
        MinecraftMcmetaAnimation mcmeta = new(16, 16);
        mcmeta.SetTexture(texture);
        textureNames.Add("minecraft:missingno");
        Textures.Add(texture);
        MCMetas.Add(mcmeta);

        return textureNames;
    }
    public static MinecraftMcmetaAnimation LoadTextureMcmeta(string value, Texture2D texture)
    {
        MinecraftMcmetaAnimation mcmeta = new(texture.width, texture.height);
        if (File.Exists(value + ".mcmeta"))
        {
            MinecraftMcmeta mcmeta2 = JsonConvert.DeserializeObject<MinecraftMcmeta>(File.ReadAllText(value + ".mcmeta"));
            mcmeta = mcmeta2.animation;
            if (mcmeta.width == 0 && mcmeta.height == 0)
            {
                mcmeta.width = texture.width;
                if (texture.width > texture.height) mcmeta.width = texture.height;
                mcmeta.height = texture.width;
                if (texture.width > texture.height) mcmeta.height = texture.height;
            }
            else
            {
                if (mcmeta.width == 0) mcmeta.width = texture.width;
                if (mcmeta.height == 0) mcmeta.height = texture.height;
            }
            if ((float)texture.width % (float)mcmeta.width != 0) return null;
            if ((float)texture.height % (float)mcmeta.height != 0) return null;
            return mcmeta;
        }
        else
        {
            if (texture.width == texture.height)
            {
                return mcmeta;
            }
            return null;
        }
    }
}
[Serializable]
public class MinecraftVersionManifest
{
    public Dictionary<string, string> latest;
}