using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

[Serializable]
public class MinecraftAtlas {
    public static List<object> vanillaSources = new();
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
                    }
                    else if (p != null && Regex.Match(split[1], p).Success)
                    {
                        Textures.RemoveAt(i);
                        MCMetas.RemoveAt(i);
                        textureFiles.RemoveAt(i);
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
                    string textureID = file.Replace(path + "assets/" + n + "/textures/", n + ":").Replace(".png", "").Replace("\\","/");
                    if (fileData != null && !textureFiles.Contains(textureID.Replace(source + "/", prefix)) && TextureAtlas.texturesInUse.Contains(textureID.Replace(source + "/", prefix)))
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
                    string textureID = file.Replace(minecraftPath + n + "/textures/", n + ":").Replace(".png", "").Replace("\\","/");
                    if (fileData != null && !textureFiles.Contains(textureID.Replace(source + "/", prefix)) && TextureAtlas.texturesInUse.Contains(textureID.Replace(source + "/", prefix)))
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
        string textureID = split[0] + ":" + split[1];
        if (File.Exists(path + "assets/" + value))
        {
            byte[] fileData = File.ReadAllBytes(path + "assets/" + value);
            if (fileData != null && !textureFiles.Contains(sprite) && TextureAtlas.texturesInUse.Contains(sprite))
            {
                texture.LoadImage(fileData);
                texture.name = textureID;
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
            if (fileData != null && !textureFiles.Contains(sprite) && TextureAtlas.texturesInUse.Contains(sprite))
            {
                texture.LoadImage(fileData);
                texture.name = textureID;
                mcmeta = TextureAtlas.LoadTextureMcmeta(minecraftPath + value, texture);
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
                    if (!textureFiles.Contains(region.sprite) && TextureAtlas.texturesInUse.Contains(region.sprite))
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
        }
        else if (File.Exists(minecraftPath + value))
        {
            byte[] fileData = File.ReadAllBytes(minecraftPath + value);
            if (fileData != null)
            {
                foreach (MinecraftAtlasSourceUnstitchRegion region in regions)
                {
                    if (!textureFiles.Contains(region.sprite) && TextureAtlas.texturesInUse.Contains(region.sprite))
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
                    string textureID = textureEntry + separator + permutation.Key;
                    if (!textureFiles.Contains(textureID) && TextureAtlas.texturesInUse.Contains(textureID))
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
                            textureFiles.Add(textureID);
                            Textures.Add(modifiedTexture);
                            MCMetas.Add(newMcmeta);
                        }
                        //else MissingNo
                    }
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
                // If the frame is an integer, create a FrameData with default time
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