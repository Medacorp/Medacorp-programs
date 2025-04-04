using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEngine;

public static class TextureAtlas
{
    public static Texture2D atlasTexture = new(2, 2);
    public static Rect[] rectangles = { };
    public static List<string> textures = new();
    private static List<string> texturesInUse = new();
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
    }
    private static bool InUse(int textureIndex)
    {
        return texturesInUse.Contains(textures[textureIndex]);
    }
    private static void NextFrame(MinecraftMcmetaAnimation animation, Rect rectangle, int textureIndex)
    {
        animation.currentFrame += 1;
        if (animation.currentFrame >= animation.parsedFrames.Count) animation.currentFrame = 0;
        if (InUse(textureIndex))
        {
            Texture2D newTexture = animation.GetFrame(animation.currentFrame);
            atlasTexture.SetPixels(Mathf.FloorToInt(rectangle.position.x * atlasTexture.width), Mathf.FloorToInt(rectangle.position.y * atlasTexture.height), Mathf.FloorToInt(rectangle.width * atlasTexture.width), Mathf.FloorToInt(rectangle.height * atlasTexture.height), newTexture.GetPixels());
            UpdateTexture();
        }
        animation.currentFrameTime += animation.parsedFrames[animation.currentFrame].time;
        if (animation.currentFrameTime <= 0) NextFrame(animation, rectangle, textureIndex);
    }
    private static void NextInterpolatedFrame(MinecraftMcmetaAnimation animation, Rect rectangle, int textureIndex)
    {
        int nextFrame = animation.currentFrame + 1;
        if (nextFrame >= animation.parsedFrames.Count) nextFrame = 0;
        if (InUse(textureIndex))
        {
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
        }
        if (animation.currentFrameTime <= 0)
        {
            animation.currentFrame = nextFrame;
            animation.currentFrameTime += animation.parsedFrames[animation.currentFrame].time;
            if (animation.currentFrameTime <= 0) NextFrame(animation, rectangle, textureIndex);
        }
    }
    public static void ParseAtlas(string path, string settingsPath)
    {
        atlasTexture = new(2, 2);
        atlasTexture.filterMode = FilterMode.Point;
        List<Texture2D> Textures;
        textureAnimations = new();
        textures = new();
        texturesInUse = new();
        textures = GetAtlases(path, out Textures, out textureAnimations, settingsPath);
        rectangles = atlasTexture.PackTextures(Textures.ToArray(), 0, 8192);
    }
    public static List<string> GetAtlases(string path, out List<Texture2D> Textures, out List<MinecraftMcmetaAnimation> MCMetas, string settingsPath)
    {
        List<string> textureNames = new();
        Textures = new();
        MinecraftAtlas atlas = new();
        MCMetas = new();
        string minecraftPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/.minecraft/versions/";
        if (File.Exists(minecraftPath + "version_manifest_v2.json"))
        {
            MinecraftVersionManifest versionManifest = JsonConvert.DeserializeObject<MinecraftVersionManifest>(File.ReadAllText(minecraftPath + "version_manifest_v2.json"));
            string version = "";
            versionManifest.latest.TryGetValue("release", out version);
            minecraftPath = minecraftPath + version + "/" + version + ".jar";
            if (File.Exists(minecraftPath))
            {
                bool extract = true;
                if (File.Exists(settingsPath + "Minecraft Assets.txt")) {
                    if (File.ReadAllText(settingsPath + "Minecraft Assets.txt") == version) extract = false;
                }
                if (extract)
                {
                    if (Directory.Exists(settingsPath + "Minecraft Assets")) Directory.Delete(settingsPath + "Minecraft Assets", true);
                    Directory.CreateDirectory(settingsPath + "Minecraft Assets");
                    if (File.Exists(settingsPath + "Minecraft Assets.txt")) File.Delete(settingsPath + "Minecraft Assets.txt");
                    using (ZipArchive archive = ZipFile.OpenRead(minecraftPath))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            if (entry.FullName == "assets/minecraft/atlases/blocks.json" || entry.FullName.Contains("assets/minecraft/textures/") || entry.FullName.Contains("assets/minecraft/items/") || entry.FullName.Contains("assets/minecraft/models/"))
                            {
                                string folderPath = Regex.Replace(entry.FullName.Replace("assets", ""), "/[^/]+$", "");
                                if (folderPath.Contains("/") || folderPath.Contains("\\"))
                                {
                                    List<string> extractFolders = folderPath.Split("/").ToList();
                                    string folder = settingsPath + "Minecraft Assets";
                                    extractFolders.RemoveAt(0);
                                    foreach (string extractFolder in extractFolders)
                                    {
                                        folder = folder + "/" + extractFolder;
                                        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
                                    }
                                }
                                entry.ExtractToFile(settingsPath + "Minecraft Assets/" + entry.FullName.Replace("assets/", ""));
                            }
                        }
                    }
                }
                atlas = JsonConvert.DeserializeObject<MinecraftAtlas>(File.ReadAllText(settingsPath + "Minecraft Assets/minecraft/atlases/blocks.json"));
                File.WriteAllText(settingsPath + "Minecraft Assets.txt", version);
                minecraftPath = settingsPath + "Minecraft Assets/";
            }
            else
            {
                Debug.Log("Cannot find latest Minecraft release");
                minecraftPath = "";
            }
        }
        else
        {
            Debug.Log("Cannot find Minecraft version manifest");
        }
        if (File.Exists(path + "assets/minecraft/atlases/blocks.json"))
        {
            MinecraftAtlas packAtlas = JsonConvert.DeserializeObject<MinecraftAtlas>(File.ReadAllText(path + "assets/minecraft/atlases/blocks.json"));
            foreach (object source in packAtlas.sources) atlas.sources.Add(source);
        }
        atlas.Parse();
        textureNames = atlas.GetTextures(path, minecraftPath, out Textures, out MCMetas);

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
        texture = mcmeta.GetFrame(0);
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