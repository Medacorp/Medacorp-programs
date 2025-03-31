using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Mono.Cecil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TreeEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.WSA;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using static UnityEditor.Progress;
using static UnityEngine.Rendering.DebugUI;

[Serializable]
public class MinecraftModel
{
    public static Dictionary<string, MinecraftItemsFile> itemsFiles = new();
    public static List<string> modelFiles = new();
    public static List<string> textureFiles = new();
    public static List<ParsedMinecraftModel> parsedModels = new();
    public static void ParseModels(string path)
    {
        int index = 0;
        Dictionary<int,MinecraftModel> models = new();
        foreach (string model in modelFiles)
        {
            string[] split = { "minecraft", "" };
            if (model.Contains(":")) split = model.Split(':');
            else split[1] = model;
            if (File.Exists(path + "assets/" + split[0] + "/models/" + split[1] + ".json"))
            {
                MinecraftModel minecraftModel = JsonConvert.DeserializeObject<MinecraftModel>(File.ReadAllText(path + "assets/" + split[0] + "/models/" + split[1] + ".json"));
                minecraftModel.name = model;
                List<string> knownParents = new() { model };
                if (minecraftModel.parent != null) minecraftModel = GetParent(minecraftModel, path, knownParents);

                if (minecraftModel.textures != null && minecraftModel.textures.Count != 0)
                {
                    minecraftModel.textures = updateTextureReferences(minecraftModel.textures);
                    foreach (KeyValuePair<string, string> texture in minecraftModel.textures)
                    {
                        if (!textureFiles.Contains(texture.Value)) textureFiles.Add(texture.Value);
                    }
                }
                models.Add(index,minecraftModel);
            }

            index++;
        }
        foreach (KeyValuePair<int,MinecraftModel> model in models)
        {
            ParsedMinecraftModel parsedModel = parsedModels[model.Key];
            //Generate meshes
        }
    }
    private static MinecraftModel GetParent(MinecraftModel model, string path, List<string> knownParents)
    {
        List<string> newKnownParents = knownParents;
        string[] split = model.parent.Split(":");
        MinecraftModel parentModel = null;
        MinecraftModel newModel = model;
        if (!knownParents.Contains(model.parent))
        {
            newKnownParents.Add(model.parent);
            if (File.Exists(path + "assets/" + split[0] + "/models/" + split[1] + ".json"))
            {
                parentModel = JsonConvert.DeserializeObject<MinecraftModel>(File.ReadAllText(path + "assets/" + split[0] + "/models/" + split[1] + ".json"));
            }
            newModel.parent = null;
            if (parentModel != null)
            {
                if (parentModel.parent != null) newModel.parent = parentModel.parent;
                if (parentModel.textures != null)
                {
                    if (newModel.textures == null) newModel.textures = new();
                    foreach (KeyValuePair<string, string> texture in parentModel.textures)
                    {
                        try
                        {
                            newModel.textures.Add(texture.Key, texture.Value);
                        }
                        catch
                        {
                            //Provided by child
                        }
                    }
                }
                if (parentModel.elements != null && newModel.elements == null) newModel.elements = parentModel.elements;
                if (parentModel.display != null)
                {
                    if (newModel.display == null) newModel.display = new();
                    foreach (KeyValuePair<string, MinecraftModelDisplay> displaysetting in parentModel.display)
                    {
                        try
                        {
                            newModel.display.Add(displaysetting.Key, displaysetting.Value);
                        }
                        catch
                        {
                            //Provided by child
                        }
                    }
                }
            }
            if (newModel.parent != null) return GetParent(newModel, path, newKnownParents);
            return newModel;
        }
        else
        {
            return newModel;
            //Bad model, infinite parent loop
        }
    }
    private static Dictionary<string, string> updateTextureReferences(Dictionary<string, string> textures)
    {
        Dictionary<string, string> newTextures = new();
        foreach (KeyValuePair<string, string> texture in textures)
        {
            if (texture.Value.StartsWith("#"))
            {
                string key = texture.Key;
                if (textures.TryGetValue(texture.Value.Remove(0, 1), out string value))
                {
                    newTextures.Add(key, value);
                }
            }
            else
            {
                newTextures.Add(texture.Key, texture.Value);
            }
        }
        if (newTextures.Values.Where(value => value.StartsWith("#")).ToList().Count != 0) return updateTextureReferences(newTextures);
        return newTextures;

    }
    public static void ClearMemory()
    {
        itemsFiles.Clear();
        modelFiles.Clear();
        textureFiles.Clear();
        parsedModels.Clear();
    }

    public string name;
    public string parent;

    public Dictionary<string, string> textures;

    public List<MinecraftModelElement> elements;

    public Dictionary<string, MinecraftModelDisplay> display;

}
public class ParsedMinecraftModel
{
    public string name;
    public List<Mesh> tintedMeshes = new();
    public List<bool> tintedTransparent = new();
    public List<MinecraftItemTint> tints = new();
    public Mesh untintedMesh;
    public bool untintedTransparent;
    public List<Color> GetTints(MinecraftItem item)
    {
        List<Color> colors = new();
        foreach (MinecraftItemTint tint in tints)
        {
            int value;
            if (tint.type == "custom_model_data")
            {
                MinecraftItemTintCustomData customTint = (MinecraftItemTintCustomData)tint;
                if (item.colors.Count >= customTint.index + 1) value = item.colors[customTint.index];
                else value = customTint.value;
            }
            else
            {
                value = tint.value;
            }
            colors.Add(new(((value >> 16) & 0xFF) / 255, ((value >> 8) & 0xFF) / 255, ((value) & 0xFF) / 255, 1));
        }
        return colors;
    } 
}

[Serializable]
public class MinecraftModelElement {

    public float[] from;
    public float[] to;
    public MinecraftModelRotation rotation;
    public Dictionary<string, MinecraftModelFace> faces;

    public Vector3 GetCenter() {
        Vector3 size = new(0,0,0);
        size[0] = -((from[0] + to[0]) / 2 - 8) / 16;
        size[1] = ((from[1] + to[1]) / 2 - 8) / 16;
        size[2] = ((from[2] + to[2]) / 2 - 8) / 16;
        return size;
    }

    public Vector3 GetSize() {
        Vector3 size = new(0,0,0);
        size[0] = (to[0] - from[0]) / 16;
        size[1] = (to[1] - from[1]) / 16;
        size[2] = (to[2] - from[2]) / 16;
        if (size[0] == 0) size[0] = 0.0001f;
        if (size[1] == 0) size[1] = 0.0001f;
        if (size[2] == 0) size[2] = 0.0001f;
        return size;
    }
    public Vector3 GetRotationPoint() {
        Vector3 origin = new(0,0,0);
        if (rotation != null) {
            origin.x = -(rotation.origin[0] - 8) / 16;
            origin.y = (rotation.origin[1] - 8) / 16;
            origin.z = (rotation.origin[2] - 8) / 16;
        }
        return origin;
    }
    public float GetRotationAngle() {
        float angle = 0;
        if (rotation != null) {
            angle = rotation.angle;
        }
        return angle;
    }
    public float[] GetRotationEulerAngle() {
        float[] angles = {0,0,0};
        if (rotation != null) {
            if (rotation.axis == 'x') angles[0] = rotation.angle;
            else if (rotation.axis == 'y') angles[1] = rotation.angle;
            else angles[2] = rotation.angle;
        }
        return angles;
    }

}

[Serializable]

public class MinecraftModelRotation {

    public float angle;
    public char axis;
    public float[] origin;

}


[Serializable]
public class MinecraftModelFace {

    public float rotation;
    public string texture;
    public float[] uv;

}

[Serializable]
public class MinecraftModelDisplay {

    public float[] translation;
    public float[] rotation;
    public float[] scale;

    public MinecraftModelDisplay() {
        
    }

    public Vector3 GetTranslation() {
        if (translation != null) return new(translation[0] / 16,translation[1] / 16,translation[2] / 16);
        return new(0,0,0);
    }

    public Quaternion GetRotation() {
        if (rotation != null) return Quaternion.Euler(new(rotation[0],-rotation[1],rotation[2]));
        return new();
    }

    public Vector3 GetScale() {
        if (scale != null) return new(scale[0],scale[1],scale[2]);
        return new(1,1,1);
    }

}

public static class TextureAtlas
{
    public static Texture2D atlasTexture = new(2, 2);
    public static Rect[] rectangles = { };
    //uvScaleX = rectangles[i].width
    //uvScaleY = rectangles[i].height
    //uvOffsetX = rectangles[i].position.x
    //uvOffsetY = rectangles[i].position.y
    public static List<string> textures = new();
    public static List<string> texturesInUse = new();
    private static bool animationsMayRun = true;
    private static List<MinecraftMcmetaAnimation> textureAnimations = new();
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
        List<MinecraftAtlas> atlasFiles = new();
        List<Texture2D> Textures = new();
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

[Serializable]
public class MinecraftAtlas {
    public List<object> sources = new();
    public List<MinecraftAtlasSourceBase> parsedSources;

    public void Parse()
    {
        parsedSources = new();
        foreach (object source in sources)
        {
            var jsonParent = JsonConvert.SerializeObject(source);
            MinecraftAtlasSourceBase newSource = JsonConvert.DeserializeObject<MinecraftAtlasSourceBase>(jsonParent);
            newSource.type = newSource.type.Replace("minecraft:","");
            if (newSource.type == "directory") {
                MinecraftAtlasSourceDirectory convertedSource = JsonConvert.DeserializeObject<MinecraftAtlasSourceDirectory>(jsonParent);
                convertedSource.type = newSource.type;
                if (convertedSource.prefix == null) convertedSource.prefix = convertedSource.source + "/";
                parsedSources.Add(convertedSource);
            }
            else if (newSource.type == "single")
            {
                MinecraftAtlasSourceSingle convertedSource = JsonConvert.DeserializeObject<MinecraftAtlasSourceSingle>(jsonParent);
                convertedSource.type = newSource.type;
                if (convertedSource.sprite == null) convertedSource.sprite = convertedSource.resource;
                parsedSources.Add(convertedSource);
            }
            else if (newSource.type == "filter")
            {
                MinecraftAtlasSourceFilter convertedSource = JsonConvert.DeserializeObject<MinecraftAtlasSourceFilter>(jsonParent);
                convertedSource.type = newSource.type;
                parsedSources.Add(convertedSource);
            }
            else if (newSource.type == "unstitch")
            {
                MinecraftAtlasSourceUnstitch convertedSource = JsonConvert.DeserializeObject<MinecraftAtlasSourceUnstitch>(jsonParent);
                convertedSource.type = newSource.type;
                parsedSources.Add(convertedSource);
            }
            else if (newSource.type == "paletted_permutations")
            {
                MinecraftAtlasSourcePalettedPermutations convertedSource = JsonConvert.DeserializeObject<MinecraftAtlasSourcePalettedPermutations>(jsonParent);
                convertedSource.type = newSource.type;
                parsedSources.Add(convertedSource);
            }
            else {
                Debug.Log("Unknown atlas source type \"" + newSource.type + "\"");
            }
        }
    }
    public List<string> GetTextures(string path, string minecraftPath, out List<Texture2D> Textures, out List<MinecraftMcmetaAnimation> MCMetas)
    {
        List<string> textureFiles = new();
        Textures = new();
        MCMetas = new();

        foreach (MinecraftAtlasSourceBase source in parsedSources)
        {
            List<string> newSprites = new();
            List<Texture2D> newTextures = new();
            List<MinecraftMcmetaAnimation> newMCMetas = new();
            newSprites = source.GetTextures(path, minecraftPath, out newTextures, out newMCMetas);
            int i = 0;
            if (newSprites == null)
            {
                //Only filter type returns null, this is to differenciate with no sprites found
                MinecraftAtlasSourceFilter filter = (MinecraftAtlasSourceFilter)source;
                string n;
                string p;
                if (!filter.pattern.TryGetValue("namespace", out n)) n = null;
                if (!filter.pattern.TryGetValue("path", out p)) p = null;
                for (i = Textures.Count - 1; i >= 0; i-- )
                {
                    string[] split = textureFiles[i].Split(':');
                    if (n != null && Regex.Match(split[0], n).Success)
                    {
                        Textures.RemoveAt(i);
                        MCMetas.RemoveAt(i);
                        textureFiles.RemoveAt(i);
                        break;
                    }
                    if (p != null && Regex.Match(split[0], p).Success)
                    {
                        Textures.RemoveAt(i);
                        MCMetas.RemoveAt(i);
                        textureFiles.RemoveAt(i);
                        break;
                    }
                }
            }
            else
            {
                foreach (string sprite in newSprites)
                {
                    if (!textureFiles.Contains(sprite))
                    {
                        textureFiles.Add(sprite);
                        Textures.Add(newTextures[i]);
                        MCMetas.Add(newMCMetas[i]);
                    }
                    i++;
                }
            }
        }

        return textureFiles;
    }
}

[Serializable]
public class MinecraftAtlasSourceBase
{
    public string type;
    public virtual List<string> GetTextures(string path, string minecraftPath, out List<Texture2D> Textures, out List<MinecraftMcmetaAnimation> MCMetas)
    {
        Debug.Log("MinecraftAtlasSourceBase GetTextures triggered! This should never happen!");
        List<string> textureFiles = new();
        Textures = new();
        MCMetas = new();
        return textureFiles;
    }
}

[Serializable]
public class MinecraftAtlasSourceDirectory : MinecraftAtlasSourceBase
{
    public string source;
    public string prefix;
    public override List<string> GetTextures(string path, string minecraftPath, out List<Texture2D> Textures, out List<MinecraftMcmetaAnimation> MCMetas)
    {
        List<string> textureFiles = new();
        Textures = new();
        MCMetas = new();
        foreach (string folder in Directory.GetDirectories(path + "assets/"))
        {
            string[] splitFolder = folder.Split('/');
            string n = splitFolder[splitFolder.Length - 1];
            if (Directory.Exists(path + "assets/" + n + "/textures/" + source + "/"))
            {
                foreach (string file in Directory.GetFiles(path + "assets/" + n + "/textures/" + source + "/", "*.png", SearchOption.AllDirectories))
                {
                    Texture2D texture = new Texture2D(2, 2);
                    texture.filterMode = FilterMode.Point;
                    MinecraftMcmetaAnimation mcmeta = new(2, 2);
                    byte[] fileData = File.ReadAllBytes(file);
                    string textureID = file.Replace(path + "assets/" + n + "/textures/", n + ":").Replace(".png", "");
                    if (fileData != null)
                    {
                        texture.LoadImage(fileData);
                        texture.name = textureID;
                        mcmeta = TextureAtlas.LoadTextureMcmeta(file, texture);
                        if (texture != null && mcmeta != null)
                        {
                            mcmeta.SetTexture(texture);
                            texture = mcmeta.GetFrame(0);
                            textureFiles.Add(textureID.Replace(source + "/", prefix));
                            Textures.Add(texture);
                            MCMetas.Add(mcmeta);
                        }
                    }
                }
            }
        }
        foreach (string folder in Directory.GetDirectories(minecraftPath))
        {
            string[] splitFolder = folder.Split('/');
            string n = splitFolder[splitFolder.Length - 1];
            if (Directory.Exists(minecraftPath + n + "/textures/" + source + "/"))
            {
                foreach (string file in Directory.GetFiles(minecraftPath + n + "/textures/" + source + "/", "*.png", SearchOption.AllDirectories))
                {
                    Texture2D texture = new Texture2D(2, 2);
                    texture.filterMode = FilterMode.Point;
                    MinecraftMcmetaAnimation mcmeta = new(2, 2);
                    byte[] fileData = File.ReadAllBytes(file);
                    string textureID = file.Replace(minecraftPath + n + "/textures/", n + ":").Replace(".png", "");
                    if (fileData != null && !textureFiles.Contains(textureID))
                    {
                        texture.LoadImage(fileData);
                        texture.name = textureID;
                        mcmeta = TextureAtlas.LoadTextureMcmeta(file, texture);
                        if (texture != null && mcmeta != null)
                        {
                            mcmeta.SetTexture(texture);
                            texture = mcmeta.GetFrame(0);
                            textureFiles.Add(textureID.Replace(source + "/", prefix));
                            Textures.Add(texture);
                            MCMetas.Add(mcmeta);
                        }
                    }
                }
            }
        }
        return textureFiles;
    }
}

[Serializable]
public class MinecraftAtlasSourceSingle : MinecraftAtlasSourceBase
{
    public string resource;
    public string sprite;
    public override List<string> GetTextures(string path, string minecraftPath, out List<Texture2D> Textures, out List<MinecraftMcmetaAnimation> MCMetas)
    {
        List<string> textureFiles = new();
        Textures = new();
        MCMetas = new();
        Texture2D texture = new Texture2D(2, 2);
        texture.filterMode = FilterMode.Point;
        MinecraftMcmetaAnimation mcmeta = new(2, 2);
        string[] split = { "minecraft", "" };
        if (resource.Contains(":")) split = resource.Split(':');
        else split[1] = resource;
        string value = split[0] + "/textures/" + split[1] + ".png";
        if (File.Exists(path + "assets/" + value))
        {
            byte[] fileData = File.ReadAllBytes(path + "assets/" + value);
            if (fileData != null)
            {
                texture.LoadImage(fileData);
                texture.name = split[0] + ":" + split[1];
                mcmeta = TextureAtlas.LoadTextureMcmeta(path + "assets/" + value, texture);
                if (texture != null && mcmeta != null)
                {
                    mcmeta.SetTexture(texture);
                    texture = mcmeta.GetFrame(0);
                    textureFiles.Add(sprite);
                    Textures.Add(texture);
                    MCMetas.Add(mcmeta);
                }
                //else MissingNo
            }
        }
        else if (File.Exists(minecraftPath + value))
        {
            byte[] fileData = File.ReadAllBytes(minecraftPath + value);
            if (fileData != null)
            {
                texture.LoadImage(fileData);
                texture.name = split[0] + ":" + split[1];
                mcmeta = TextureAtlas.LoadTextureMcmeta(minecraftPath + value, texture);
                if (texture != null && mcmeta != null) { 
                    mcmeta.SetTexture(texture);
                    texture = mcmeta.GetFrame(0);
                    textureFiles.Add(sprite);
                    Textures.Add(texture);
                    MCMetas.Add(mcmeta);
                
                }
                //else MissingNo
            }
        }
        //else MissingNo
        return textureFiles;
    }
}

[Serializable]
public class MinecraftAtlasSourceFilter : MinecraftAtlasSourceBase
{
    public Dictionary<string,string> pattern;
    public override List<string> GetTextures(string path, string minecraftPath, out List<Texture2D> Textures, out List<MinecraftMcmetaAnimation> MCMetas)
    {
        List<string> textureFiles = new();
        Textures = new();
        MCMetas = new();
        return null;
    }
}

[Serializable]
public class MinecraftAtlasSourceUnstitch : MinecraftAtlasSourceBase
{
    public string resource;
    public double divisor_x;
    public double divisor_y;
    public List<MinecraftAtlasSourceUnstitchRegion> regions;
    public override List<string> GetTextures(string path, string minecraftPath, out List<Texture2D> Textures, out List<MinecraftMcmetaAnimation> MCMetas)
    {
        //TODO: fix this to handle it correctly; animated unstitched textures don't work correctly.
        //      The whole area is animated as one texture, but due to unstitching, the animation breaks down, taking other sprites' textures instead.
        //      Additionally, the height and width of animation frames are not considered at unstitching
        List<string> textureFiles = new();
        Textures = new();
        MCMetas = new();
        string[] split = { "minecraft", "" };
        if (resource.Contains(":")) split = resource.Split(':');
        else split[1] = resource;
        string value = split[0] + "/textures/" + split[1] + ".png";
        if (File.Exists(path + "assets/" + value))
        {
            byte[] fileData = File.ReadAllBytes(path + "assets/" + value);
            if (fileData != null)
            {
                foreach (MinecraftAtlasSourceUnstitchRegion region in regions)
                {
                    Texture2D texture = new Texture2D(2, 2);
                    texture.filterMode = FilterMode.Point;
                    MinecraftMcmetaAnimation mcmeta = new(2, 2);
                    texture.LoadImage(fileData);
                    Color[] c = texture.GetPixels(Convert.ToInt32(region.x * divisor_x), Convert.ToInt32((region.y * divisor_y) * -1 + 1 - region.height), Convert.ToInt32(region.width * divisor_x), Convert.ToInt32(region.height * divisor_y));
                    texture = new Texture2D(Convert.ToInt32(region.width * divisor_x), Convert.ToInt32(region.height * divisor_y));
                    texture.SetPixels(c);
                    texture.name = split[0] + ":" + split[1];
                    mcmeta = TextureAtlas.LoadTextureMcmeta(path + "assets/" + value, texture);
                    if (texture != null && mcmeta != null)
                    {
                        mcmeta.SetTexture(texture);
                        texture = mcmeta.GetFrame(0);
                        textureFiles.Add(region.sprite);
                        Textures.Add(texture);
                        MCMetas.Add(mcmeta);
                    }
                    //else MissingNo
                }
            }
        }
        else if (File.Exists(minecraftPath + value))
        {
            byte[] fileData = File.ReadAllBytes(minecraftPath + value);
            if (fileData != null)
            {
                foreach (MinecraftAtlasSourceUnstitchRegion region in regions)
                {
                    Texture2D texture = new Texture2D(2, 2);
                    texture.filterMode = FilterMode.Point;
                    MinecraftMcmetaAnimation mcmeta = new(2, 2);
                    texture.LoadImage(fileData);
                    Color[] c = texture.GetPixels(Convert.ToInt32(region.x * divisor_x), Convert.ToInt32((region.y * divisor_y) * -1 + 1 - region.height), Convert.ToInt32(region.width * divisor_x), Convert.ToInt32(region.height * divisor_y));
                    texture = new Texture2D(Convert.ToInt32(region.width * divisor_x), Convert.ToInt32(region.height * divisor_y));
                    texture.SetPixels(c);
                    texture.name = split[0] + ":" + split[1];
                    mcmeta = TextureAtlas.LoadTextureMcmeta(minecraftPath + value, texture);
                    if (texture != null && mcmeta != null)
                    {
                        mcmeta.SetTexture(texture);
                        texture = mcmeta.GetFrame(0);
                        textureFiles.Add(region.sprite);
                        Textures.Add(texture);
                        MCMetas.Add(mcmeta);
                    }
                    //else MissingNo
                }
            }
        }
        //else MissingNo
        return textureFiles;
    }
}
[Serializable]
public class MinecraftAtlasSourceUnstitchRegion
{
    public string sprite;
    public double x;
    public double y;
    public double width;
    public double height;
}
[Serializable]
public class MinecraftAtlasSourcePalettedPermutations : MinecraftAtlasSourceBase
{
    public List<string> textures;
    public string palette_key;
    public string separator;
    public Dictionary<string,string> permutations;
    public override List<string> GetTextures(string path, string minecraftPath, out List<Texture2D> Textures, out List<MinecraftMcmetaAnimation> MCMetas)
    {
        List<string> textureFiles = new();
        Textures = new();
        MCMetas = new();
        Texture2D palette = new(2, 2);
        palette.filterMode = FilterMode.Point;
        string[] split = { "minecraft", "" };
        if (palette_key.Contains(":")) split = palette_key.Split(':');
        else split[1] = palette_key;
        //Load base palette
        if (File.Exists(path + "assets/" + split[0] + "/textures/" + split[1] + ".png"))
        {
            byte[] fileData = File.ReadAllBytes(path + "assets/" + split[0] + "/textures/" + split[1] + ".png");
            if (fileData != null) palette.LoadImage(fileData);
        }
        else if (File.Exists(minecraftPath + split[0] + "/textures/" + split[1] + ".png"))
        {
            byte[] fileData = File.ReadAllBytes(minecraftPath + split[0] + "/textures/" + split[1] + ".png");
            if (fileData != null) palette.LoadImage(fileData);
        }
        else return textureFiles; //Missing palette
        foreach (string textureEntry in textures)
        {
            Texture2D baseTexture = new(2, 2);
            baseTexture.filterMode = FilterMode.Point;
            MinecraftMcmetaAnimation mcmeta = new(2, 2);
            string[] splitTexture = { "minecraft", "" };
            if (textureEntry.Contains(":")) splitTexture = textureEntry.Split(':');
            else splitTexture[1] = textureEntry;
            bool foundTexture = false;
            if (File.Exists(path + "assets/" + splitTexture[0] + "/textures/" + splitTexture[1] + ".png"))
            {
                byte[] fileData = File.ReadAllBytes(path + "assets/" + splitTexture[0] + "/textures/" + splitTexture[1] + ".png");
                if (fileData != null)
                {
                    baseTexture.LoadImage(fileData);
                    mcmeta = TextureAtlas.LoadTextureMcmeta(path + "assets/" + splitTexture[0] + "/textures/" + splitTexture[1] + ".png", baseTexture);
                    if (baseTexture != null && mcmeta != null) foundTexture = true;
                    //else MissingNo
                }
            }
            else if (File.Exists(minecraftPath + splitTexture[0] + "/textures/" + splitTexture[1] + ".png"))
            {
                byte[] fileData = File.ReadAllBytes(minecraftPath + splitTexture[0] + "/textures/" + splitTexture[1] + ".png");
                if (fileData != null)
                {
                    baseTexture.LoadImage(fileData);
                    mcmeta = TextureAtlas.LoadTextureMcmeta(minecraftPath + splitTexture[0] + "/textures/" + splitTexture[1] + ".png", baseTexture);
                    if (baseTexture != null && mcmeta != null) foundTexture = true;
                    //else MissingNo
                }
            }
            if (foundTexture)
            {
                Color[] pixelsPalette = palette.GetPixels();
                foreach (KeyValuePair<string, string> permutation in permutations)
                {
                    bool foundPalette = false;
                    Texture2D modifiedTexture = new(baseTexture.width, baseTexture.height);
                    modifiedTexture.filterMode = FilterMode.Point;
                    //Deep copy mcmeta
                    MinecraftMcmetaAnimation newMcmeta = JsonConvert.DeserializeObject<MinecraftMcmetaAnimation>(JsonConvert.SerializeObject(mcmeta));
                    Color[] pixels = baseTexture.GetPixels();
                    Color[] pixelsReplace = { };
                    Texture2D paletteTexture = new(2, 2);
                    paletteTexture.filterMode = FilterMode.Point;
                    string[] splitPermutation = { "minecraft", "" };
                    if (permutation.Value.Contains(":")) splitPermutation = permutation.Value.Split(':');
                    else splitPermutation[1] = permutation.Value;
                    if (File.Exists(path + "assets/" + splitPermutation[0] + "/textures/" + splitPermutation[1] + ".png"))
                    {
                        byte[] fileData = File.ReadAllBytes(path + "assets/" + splitPermutation[0] + "/textures/" + splitPermutation[1] + ".png");
                        if (fileData != null)
                        {
                            paletteTexture.LoadImage(fileData);
                            foundPalette = true;
                            pixelsReplace = paletteTexture.GetPixels();
                        }
                    }
                    else if (File.Exists(minecraftPath + splitPermutation[0] + "/textures/" + splitPermutation[1] + ".png"))
                    {
                        byte[] fileData = File.ReadAllBytes(minecraftPath + splitPermutation[0] + "/textures/" + splitPermutation[1] + ".png");
                        if (fileData != null)
                        {
                            paletteTexture.LoadImage(fileData);
                            foundPalette = true;
                            pixelsReplace = paletteTexture.GetPixels();
                        }
                    }
                    if (foundPalette && pixelsPalette.Length == pixelsReplace.Length)
                    {
                        List<Color> colors = new();
                        foreach (Color pixel in pixels)
                        {
                            Color c = new(pixel.r, pixel.g, pixel.b, pixel.a);
                            int i = 0;
                            foreach (Color pal in pixelsPalette)
                            {
                                if (pixel.r == pal.r && pixel.g == pal.g && pixel.b == pal.b)
                                {
                                    c.r = pixelsReplace[i].r;
                                    c.g = pixelsReplace[i].g;
                                    c.b = pixelsReplace[i].b;
                                    c.a = c.a * pixelsReplace[i].a;
                                }
                                i++;
                            }
                            colors.Add(c);
                        }
                        modifiedTexture.SetPixels(colors.ToArray());
                        newMcmeta.SetTexture(modifiedTexture);
                        modifiedTexture = newMcmeta.GetFrame(0);
                        textureFiles.Add(textureEntry + separator + permutation.Key);
                        Textures.Add(modifiedTexture);
                        MCMetas.Add(newMcmeta);
                    }
                    //else MissingNo
                }
            }
        }
        return textureFiles;
    }
}

[Serializable]
public class MinecraftMcmeta {
    public MinecraftMcmetaAnimation animation;
}

[Serializable]
public class MinecraftMcmetaAnimation {
    public bool interpolate;
    public int width;
    public int height;
    public int frametime;
    public float currentFrameTime;
    public int currentFrame;
    public List<object> frames;
    public List<MinecraftMcmetaAnimationFrame> parsedFrames = new List<MinecraftMcmetaAnimationFrame>();
    public List<Texture2D> textures;

    public MinecraftMcmetaAnimation(int width, int height) {
        this.width = width;
        this.height = height;
    }
    public void SetTexture(Texture2D texture) {
        textures = new();
        if (!(width == texture.width && height == texture.height)) {
            for (float w = 0; w + width <= texture.width; w = w + width) {
                for (float h = 0; h + height <= texture.height; h = h + height) {
                    Texture2D frame = new(width,height);
                    frame.SetPixels(texture.GetPixels(Mathf.FloorToInt(w), Mathf.FloorToInt(h), width, height));
                    textures.Add(frame);
                }
            }
        }
        else {
            textures.Add(texture);
        }
        ParseFrames();
        currentFrameTime = parsedFrames[0].time;
    }
    public Texture2D GetFrame(int index) {
        return textures[index];
    }

    private void ParseFrames()
    {
        parsedFrames = new List<MinecraftMcmetaAnimationFrame>();

        int frametime = 1;
        if (this.frametime != 0) frametime = this.frametime;

        if (frames != null && frames.Count != 0) {
            foreach (var frame in frames)
            {
                // If the frame is an integer, create a FrameData with default time=1
                if (frame is Int64)
                {
                    parsedFrames.Add(new MinecraftMcmetaAnimationFrame
                    {
                        index = Convert.ToInt32(frame),
                        time = frametime
                    });
                }
                else if (frame is JObject)
                {
                    // If the frame is a JObject (an object), parse it as a FrameData
                    var frameObj = frame as JObject;
                    MinecraftMcmetaAnimationFrame parsedFrame = frameObj.ToObject<MinecraftMcmetaAnimationFrame>();
                    parsedFrames.Add(parsedFrame);
                }
            }
        }
        else {
            int frame = 0;
            foreach (Texture2D texture in textures) {
                parsedFrames.Add(new MinecraftMcmetaAnimationFrame
                {
                    index = frame,
                    time = frametime
                });
                frame++;
            }
        }
    }
}

public class MinecraftMcmetaAnimationFrame
{
    public int index;
    public int time;
}
[Serializable]
public class MinecraftItemsFile
{
    public object model;
    private MinecraftItemModelBase parsedModel;

    public List<MinecraftItemModelFetched> GetModels(MinecraftItem item)
    {
        return parsedModel.GetModels(item);
    }
    public void Parse()
    {
        parsedModel = Parse(model);
    }
    public MinecraftItemModelBase Parse(object model)
    {
        MinecraftItemModel newmodel = new();
        var jsonParent = JsonConvert.SerializeObject(model);
        MinecraftItemModelBase GetType = JsonConvert.DeserializeObject<MinecraftItemModelBase>(jsonParent);
        GetType.type = GetType.type.Replace("minecraft:","");
        if (GetType.type == "model") {
            MinecraftItemModel convertedModel = JsonConvert.DeserializeObject<MinecraftItemModel>(jsonParent);
            convertedModel.type = GetType.type;
            if (convertedModel.tints != null && convertedModel.tints.Count != 0) convertedModel.Parse();
            return convertedModel;
        }
        else if (GetType.type == "composite")
        {
            MinecraftItemModelComposite convertedModel = JsonConvert.DeserializeObject<MinecraftItemModelComposite>(jsonParent);
            convertedModel.type = GetType.type;
            if (convertedModel.models != null && convertedModel.models.Count != 0) convertedModel.Parse();
            return convertedModel;
        }
        else if (GetType.type == "condition")
        {
            MinecraftItemModelCondition convertedModel = JsonConvert.DeserializeObject<MinecraftItemModelCondition>(jsonParent);
            convertedModel.type = GetType.type;
            convertedModel.property = convertedModel.property.Replace("minecraft:", "");
            if (convertedModel.property == "broken"
                || convertedModel.property == "bundle/has_selected_item"
                || convertedModel.property == "carried"
                || convertedModel.property == "component"
                || convertedModel.property == "damaged"
                || convertedModel.property == "extended_view"
                || convertedModel.property == "fishing_rod/cast"
                || convertedModel.property == "has_component"
                || convertedModel.property == "keybind_down"
                || convertedModel.property == "selected"
                || convertedModel.property == "using_item"
                || convertedModel.property == "view_entity") { }
            else if (convertedModel.property == "custom_model_data")
            {
                MinecraftItemModelConditionCustomData convertedModel2 = JsonConvert.DeserializeObject<MinecraftItemModelConditionCustomData>(jsonParent);
                convertedModel2.type = GetType.type;
                convertedModel2.property = convertedModel.property;
                convertedModel2.Parse();
                return convertedModel2;
            }
            else
            {
                Debug.Log("Unknown condition property: \"" + convertedModel.property + "\"");
            }
            convertedModel.Parse();
            return convertedModel;
        }
        else if (GetType.type == "select")
        {
            MinecraftItemModelSelect convertedModel = JsonConvert.DeserializeObject<MinecraftItemModelSelect>(jsonParent);
            convertedModel.type = GetType.type;
            convertedModel.property = convertedModel.property.Replace("minecraft:", "");
            if (convertedModel.property == "block_state"
                || convertedModel.property == "charge_type"
                || convertedModel.property == "component"
                || convertedModel.property == "context_dimension"
                || convertedModel.property == "context_entity_type"
                || convertedModel.property == "display_context"
                || convertedModel.property == "local_time"
                || convertedModel.property == "main_hand"
                || convertedModel.property == "trim_material") { }
            else if (convertedModel.property == "custom_model_data")
            {
                MinecraftItemModelSelectCustomData convertedModel2 = JsonConvert.DeserializeObject<MinecraftItemModelSelectCustomData>(jsonParent);
                convertedModel2.type = GetType.type;
                convertedModel2.property = convertedModel.property;
                convertedModel2.Parse();
                return convertedModel2;
            }
            else
            {
                Debug.Log("Unknown select property: \"" + convertedModel.property + "\"");
            }
            convertedModel.Parse();
            return convertedModel;
        }
        else if (GetType.type == "range_dispatch") {
            MinecraftItemModelRangeDispatch convertedModel = JsonConvert.DeserializeObject<MinecraftItemModelRangeDispatch>(jsonParent);
            convertedModel.type = GetType.type;
            convertedModel.property = convertedModel.property.Replace("minecraft:", "");
            if (convertedModel.property == "bundle/fullness"
                || convertedModel.property == "compass"
                || convertedModel.property == "cooldown"
                || convertedModel.property == "count"
                || convertedModel.property == "crossbow/pull"
                || convertedModel.property == "damage"
                || convertedModel.property == "time"
                || convertedModel.property == "use_cycle"
                || convertedModel.property == "use_duration") { }
            else if (convertedModel.property == "custom_model_data")
            {
                MinecraftItemModelRangeDispatchCustomData convertedModel2 = JsonConvert.DeserializeObject<MinecraftItemModelRangeDispatchCustomData>(jsonParent);
                convertedModel2.type = GetType.type;
                convertedModel2.property = convertedModel.property;
                convertedModel2.Parse();
                return convertedModel2;
            }
            else
            {
                Debug.Log("Unknown range dispatch property: \"" + convertedModel.property + "\"");
            }
            convertedModel.Parse();
            return convertedModel;
        }
        else if (GetType.type == "empty"
            || GetType.type == "bundle/selected_item"
            || GetType.type == "special") { }
        else {
            Debug.Log("Unknown model type: \"" + GetType.type + "\"");
        }
        return newmodel;
    }
}
[Serializable]
public class MinecraftItemModelBase
{
    public string type;
    public virtual List<MinecraftItemModelFetched> GetModels(MinecraftItem item)
    {
        Debug.Log("MinecraftItemModelBase GetModels triggered! This should never happen!");
        return new();
    }
}
public class MinecraftItemModelFetched
{
    public int modelIndex;
    public List<MinecraftItemTint> tints;
}
[Serializable]
public class MinecraftItemModel : MinecraftItemModelBase
{
    public string model;
    public int modelIndex;
    public List<object> tints;
    public List<MinecraftItemTint> parsedTints;
    public override List<MinecraftItemModelFetched> GetModels(MinecraftItem item)
    {
        List<MinecraftItemModelFetched> models = new();
        MinecraftItemModelFetched newModel = new();
        newModel.modelIndex = modelIndex;
        newModel.tints = parsedTints;
        if (model != null && model != "") models.Add(newModel);
        return models;
    }
    public void Parse()
    {
        List<MinecraftItemTint> newtints = new();
        foreach (var tint in tints)
        {
            var jsonParent = JsonConvert.SerializeObject(tint);
            MinecraftItemModelBase GetType = JsonConvert.DeserializeObject<MinecraftItemModelBase>(jsonParent);
            GetType.type = GetType.type.Replace("minecraft:", "");
            if (GetType.type == "constant"
                || GetType.type == "dye"
                || GetType.type == "firework"
                || GetType.type == "map_color"
                || GetType.type == "potion"
                || GetType.type == "team")
            {
                MinecraftItemTint convertedTint = JsonConvert.DeserializeObject<MinecraftItemTint>(jsonParent);
                convertedTint.type = GetType.type;
                newtints.Add(convertedTint);
            }
            else if (GetType.type == "grass")
            {
                MinecraftItemTint convertedTint = JsonConvert.DeserializeObject<MinecraftItemTint>(jsonParent);
                convertedTint.type = GetType.type;
                convertedTint.value = 7979098; //#79c05a; color for forest
                newtints.Add(convertedTint);
            }
            else if (GetType.type == "custom_model_data")
            {
                MinecraftItemTintCustomData convertedTint = JsonConvert.DeserializeObject<MinecraftItemTintCustomData>(jsonParent);
                convertedTint.type = GetType.type;
                newtints.Add(convertedTint);
            }
            else
            {
                MinecraftItemTint convertedTint = JsonConvert.DeserializeObject<MinecraftItemTint>(jsonParent);
                convertedTint.type = GetType.type;
                convertedTint.value = 0; //Turn pitch black
                newtints.Add(convertedTint);
                Debug.Log("Unknown tint type: \"" + GetType.type + "\"");
            }
        }
        parsedTints = newtints;
        if (!MinecraftModel.modelFiles.Contains(model))
        {
            modelIndex = MinecraftModel.modelFiles.Count;
            MinecraftModel.modelFiles.Add(model);
            ParsedMinecraftModel parsedModel = new();
            parsedModel.name = model;
            MinecraftModel.parsedModels.Add(parsedModel);
        }
    }
}
[Serializable]
public class MinecraftItemTint : MinecraftItemModelBase
{
    public int value;
    public MinecraftItemTint(string type, int value)
    {
        this.type = type;
        this.value = value;
    }
}
[Serializable]
public class MinecraftItemTintCustomData : MinecraftItemTint
{
    public int index;
    public MinecraftItemTintCustomData(string type, int value) : base(type, value) { }
}
[Serializable]
public class MinecraftItemModelComposite : MinecraftItemModelBase
{
    public List<object> models;
    public override List<MinecraftItemModelFetched> GetModels(MinecraftItem item)
    {
        List<MinecraftItemModelFetched> models = new();
        foreach (var model in this.models)
        {
            MinecraftItemModelBase convertedModel = (MinecraftItemModelBase)model;
            foreach (MinecraftItemModelFetched s in convertedModel.GetModels(item)) models.Add(s);
        }
        return models;
    }
    public void Parse()
    {
        List<object> newmodels = new();
        MinecraftItemsFile temp = new();
        foreach (var model in models) {
            newmodels.Add(temp.Parse(model));
        }
        models = newmodels;
    }
}
[Serializable]
public class MinecraftItemModelCondition : MinecraftItemModelBase
{
    public string property;
    public object on_true;
    public object on_false;
    private MinecraftItemModelBase parsedOnTrue;
    private MinecraftItemModelBase parsedOnFalse;
    public override List<MinecraftItemModelFetched> GetModels(MinecraftItem item)
    {
        List<MinecraftItemModelFetched> models = new();
        MinecraftItemModelBase convertedModel = GetResult(item);
        foreach (MinecraftItemModelFetched s in convertedModel.GetModels(item)) models.Add(s);
        return models;
    }
    public void Parse()
    {
        MinecraftItemsFile temp = new();
        parsedOnFalse = temp.Parse(on_false);
        parsedOnTrue = temp.Parse(on_true);
    }

    public MinecraftItemModelBase GetResult(bool value)
    {
        if (value) return parsedOnTrue;
        return parsedOnFalse;
    }
    public virtual MinecraftItemModelBase GetResult(MinecraftItem item)
    {
        return GetResult(false);
    }

}
[Serializable]
public class MinecraftItemModelConditionCustomData : MinecraftItemModelCondition
{
    public int index;
    public override MinecraftItemModelBase GetResult(MinecraftItem item)
    {
        if (item.flags.Count >= index+1) return GetResult(item.flags[index]);
        return GetResult(false);
    }
}
[Serializable]
public class MinecraftItemModelSelect : MinecraftItemModelBase
{
    public string property;
    public List<MinecraftItemModelSelectCase> cases;
    public object fallback;
    public MinecraftItemModelBase parsedFallback;
    public override List<MinecraftItemModelFetched> GetModels(MinecraftItem item)
    {
        List<MinecraftItemModelFetched> models = new();
        MinecraftItemModelBase convertedModel = GetResult(item);
        foreach (MinecraftItemModelFetched s in convertedModel.GetModels(item)) models.Add(s);
        return models;
    }
    public void Parse()
    {
        MinecraftItemsFile temp = new();
        parsedFallback = temp.Parse(fallback);
        foreach (MinecraftItemModelSelectCase c in cases)
        {
            c.Parse();
        }
    }
    public virtual MinecraftItemModelBase GetResult(MinecraftItem item)
    {
        return parsedFallback;
    }
    public MinecraftItemModelBase GetResult(string value)
    {
        foreach (MinecraftItemModelSelectCase c in cases)
        {
            List<string> when = (List<string>)c.when;
            if (when.Contains(value)) return c.parsedModel;
        }
        return parsedFallback;
    }
}
[Serializable]
public class MinecraftItemModelSelectCustomData : MinecraftItemModelSelect
{
    public int index;
    public override MinecraftItemModelBase GetResult(MinecraftItem item)
    {
        
        if (item.strings.Count >= index + 1)
        {
            return GetResult(item.strings[index]);
        }
        return parsedFallback;
    }
}
[Serializable]
public class MinecraftItemModelSelectCase
{
    public object when;
    public object model;
    public MinecraftItemModelBase parsedModel;
    public void Parse()
    {
        MinecraftItemsFile temp = new();
        parsedModel = temp.Parse(model);
        try {
            when = (List<string>)when;
        }
        catch {
            List<string> newwhen = new();
            newwhen.Add((string)when);
            when = newwhen;
        }
    }
}
[Serializable]
public class MinecraftItemModelRangeDispatch : MinecraftItemModelBase
{
    public string property;
    public float scale;
    public List<MinecraftItemModelRangeDispatchEntry> entries;
    public object fallback;
    public MinecraftItemModelBase parsedFallback;
    public override List<MinecraftItemModelFetched> GetModels(MinecraftItem item)
    {
        List<MinecraftItemModelFetched> models = new();
        MinecraftItemModelBase convertedModel = GetResult(item);
        foreach (MinecraftItemModelFetched s in convertedModel.GetModels(item)) models.Add(s);
        return models;
    }
    public void Parse()
    {
        if (scale == 0) scale = 1;
        MinecraftItemsFile temp = new();
        parsedFallback = temp.Parse(fallback);
        foreach (MinecraftItemModelRangeDispatchEntry entry in entries)
        {
            entry.parsedModel = temp.Parse(entry.model);
        }
        entries.OrderBy(c => c.threshold);
    }
    public virtual MinecraftItemModelBase GetResult(MinecraftItem item)
    {
        return parsedFallback;
    }
    public MinecraftItemModelBase GetResult(float value)
    {
        MinecraftItemModelBase result = parsedFallback;
        foreach (MinecraftItemModelRangeDispatchEntry entry in entries)
        {
            if (entry.threshold <= value) result = entry.parsedModel;
        }
        return result;
    }
}
[Serializable]
public class MinecraftItemModelRangeDispatchCustomData : MinecraftItemModelRangeDispatch
{
    public int index;
    public override MinecraftItemModelBase GetResult(MinecraftItem item)
    {

        if (item.floats.Count >= index + 1)
        {
            return GetResult(item.floats[index]);
        }
        return parsedFallback;
    }
}
[Serializable]
public class MinecraftItemModelRangeDispatchEntry
{
    public float threshold;
    public object model;
    public MinecraftItemModelBase parsedModel;
}