using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

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
                    minecraftModel.textures = UpdateTextureReferences(minecraftModel.textures);
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
            parsedModel.untintedMesh = new();
            List<Vector3> vertices = new();
            List<Vector2> uv = new();
            List<int> triangles = new();
            List<List<Vector3>> tintedvertices = new();
            List<List<Vector2>> tinteduv = new();
            List<List<int>> tintedtriangles = new();
            foreach (MinecraftModelElement element in model.Value.elements) {
                float[] from = {-(element.from[0] - 8) / 16, (element.from[1] - 8) / 16, (element.from[2] - 8) / 16};
                float[] to = {-(element.to[0] - 8) / 16, (element.to[1] - 8) / 16, (element.to[2] - 8) / 16};
                List<Vector3> points = new(){
                    new(to[0],from[1],from[2]),
                    new(to[0],from[1],to[2]),
                    new(to[0],to[1],from[2]),
                    new(to[0],to[1],to[2]),
                    new(from[0],from[1],from[2]),
                    new(from[0],from[1],to[2]),
                    new(from[0],to[1],from[2]),
                    new(from[0],to[1],to[2])
                };
                if (element.GetRotationAngle() != 0) {
                    List<Vector3> newpoints = new();
                    float[] angles = element.GetRotationEulerAngle();
                    Quaternion rotation = Quaternion.Euler(angles[0], -angles[1], -angles[2]);
                    Vector3 rotationPoint = element.GetRotationPoint();
                    foreach (Vector3 point in points) {
                        newpoints.Add(rotation * (point - rotationPoint) + rotationPoint);
                    }
                    points = newpoints;
                }
                foreach (KeyValuePair<string,MinecraftModelFace> face in element.faces) {
                    float[] uvs = {face.Value.uv[0] / 16, face.Value.uv[1] / 16, face.Value.uv[2] / 16, face.Value.uv[3] / 16};
                    Rect rectangle = TextureAtlas.GetRectangleForTexture(face.Value.texture.Replace("#",""));
                    uvs[0] = uvs[0] * rectangle.width + rectangle.position.x;
                    uvs[1] = (uvs[1] * rectangle.height + rectangle.position.y) * -1 + 1;
                    uvs[2] = uvs[2] * rectangle.width + rectangle.position.x;
                    uvs[3] = (uvs[3] * rectangle.height + rectangle.position.y) * -1 + 1;
                    bool validKey = false;
                    if (face.Value.tintindex != -1) {
                        while (parsedModel.tintedMeshes.Count < face.Value.tintindex - 1) {
                            parsedModel.tintedMeshes.Add(new());
                            parsedModel.tintedTransparent.Add(false);
                            tintedvertices.Add(new());
                            tinteduv.Add(new());
                            tintedtriangles.Add(new());
                        }
                        if (TextureAtlas.TextureIsAnimated(face.Value.texture.Replace("#",""))) parsedModel.tintedTransparent[face.Value.tintindex] = true;
                        index = tintedvertices[face.Value.tintindex].Count;
                        if (face.Key == "east") {
                            tintedvertices[face.Value.tintindex].Add(points[1]);
                            tintedvertices[face.Value.tintindex].Add(points[3]);
                            tintedvertices[face.Value.tintindex].Add(points[2]);
                            tintedvertices[face.Value.tintindex].Add(points[0]);
                            validKey = true;
                        }
                        else if (face.Key == "down") {
                            tintedvertices[face.Value.tintindex].Add(points[4]);
                            tintedvertices[face.Value.tintindex].Add(points[5]);
                            tintedvertices[face.Value.tintindex].Add(points[1]);
                            tintedvertices[face.Value.tintindex].Add(points[0]);
                            validKey = true;
                        }
                        else if (face.Key == "north") {
                            tintedvertices[face.Value.tintindex].Add(points[0]);
                            tintedvertices[face.Value.tintindex].Add(points[2]);
                            tintedvertices[face.Value.tintindex].Add(points[6]);
                            tintedvertices[face.Value.tintindex].Add(points[4]);
                            validKey = true;
                        }
                        else if (face.Key == "south") {
                            tintedvertices[face.Value.tintindex].Add(points[5]);
                            tintedvertices[face.Value.tintindex].Add(points[7]);
                            tintedvertices[face.Value.tintindex].Add(points[3]);
                            tintedvertices[face.Value.tintindex].Add(points[1]);
                            validKey = true;
                        }
                        else if (face.Key == "up") {
                            tintedvertices[face.Value.tintindex].Add(points[7]);
                            tintedvertices[face.Value.tintindex].Add(points[6]);
                            tintedvertices[face.Value.tintindex].Add(points[2]);
                            tintedvertices[face.Value.tintindex].Add(points[3]);
                            validKey = true;
                        }
                        else if (face.Key == "west") {
                            tintedvertices[face.Value.tintindex].Add(points[4]);
                            tintedvertices[face.Value.tintindex].Add(points[6]);
                            tintedvertices[face.Value.tintindex].Add(points[7]);
                            tintedvertices[face.Value.tintindex].Add(points[5]);
                            validKey = true;
                        }
                        if (validKey) {
                            tintedtriangles[face.Value.tintindex].Add(index);
                            tintedtriangles[face.Value.tintindex].Add(index+1);
                            tintedtriangles[face.Value.tintindex].Add(index+2);
                            tintedtriangles[face.Value.tintindex].Add(index);
                            tintedtriangles[face.Value.tintindex].Add(index+2);
                            tintedtriangles[face.Value.tintindex].Add(index+3);
                            tinteduv[face.Value.tintindex].Add(new(uvs[0], uvs[3]));
                            tinteduv[face.Value.tintindex].Add(new(uvs[0], uvs[1]));
                            tinteduv[face.Value.tintindex].Add(new(uvs[2], uvs[1]));
                            tinteduv[face.Value.tintindex].Add(new(uvs[2], uvs[3]));
                            if (face.Value.rotation == 90 || face.Value.rotation == 180 || face.Value.rotation == 270) {
                                tinteduv[face.Value.tintindex].Add(uv[index]);
                                tinteduv[face.Value.tintindex].RemoveAt(index);
                                if (face.Value.rotation == 180 || face.Value.rotation == 270) {
                                    tinteduv[face.Value.tintindex].Add(uv[index]);
                                    tinteduv[face.Value.tintindex].RemoveAt(index);
                                    if (face.Value.rotation == 270) {
                                        tinteduv[face.Value.tintindex].Add(uv[index]);
                                        tinteduv[face.Value.tintindex].RemoveAt(index);
                                    }
                                }
                            }
                        }
                    }
                    else {
                        if (TextureAtlas.TextureIsAnimated(face.Value.texture.Replace("#",""))) parsedModel.untintedTransparent = true;
                        index = vertices.Count;
                        if (face.Key == "east") {
                            vertices.Add(points[1]);
                            vertices.Add(points[3]);
                            vertices.Add(points[2]);
                            vertices.Add(points[0]);
                            validKey = true;
                        }
                        else if (face.Key == "down") {
                            vertices.Add(points[4]);
                            vertices.Add(points[5]);
                            vertices.Add(points[1]);
                            vertices.Add(points[0]);
                            validKey = true;
                        }
                        else if (face.Key == "north") {
                            vertices.Add(points[0]);
                            vertices.Add(points[2]);
                            vertices.Add(points[6]);
                            vertices.Add(points[4]);
                            validKey = true;
                        }
                        else if (face.Key == "south") {
                            vertices.Add(points[5]);
                            vertices.Add(points[7]);
                            vertices.Add(points[3]);
                            vertices.Add(points[1]);
                            validKey = true;
                        }
                        else if (face.Key == "up") {
                            vertices.Add(points[7]);
                            vertices.Add(points[6]);
                            vertices.Add(points[2]);
                            vertices.Add(points[3]);
                            validKey = true;
                        }
                        else if (face.Key == "west") {
                            vertices.Add(points[4]);
                            vertices.Add(points[6]);
                            vertices.Add(points[7]);
                            vertices.Add(points[5]);
                            validKey = true;
                        }
                        if (validKey) {
                            triangles.Add(index);
                            triangles.Add(index+1);
                            triangles.Add(index+2);
                            triangles.Add(index);
                            triangles.Add(index+2);
                            triangles.Add(index+3);
                            uv.Add(new(uvs[0], uvs[3]));
                            uv.Add(new(uvs[0], uvs[1]));
                            uv.Add(new(uvs[2], uvs[1]));
                            uv.Add(new(uvs[2], uvs[3]));
                            if (face.Value.rotation == 90 || face.Value.rotation == 180 || face.Value.rotation == 270) {
                                uv.Add(uv[index]);
                                uv.RemoveAt(index);
                                if (face.Value.rotation == 180 || face.Value.rotation == 270) {
                                    uv.Add(uv[index]);
                                    uv.RemoveAt(index);
                                    if (face.Value.rotation == 270) {
                                        uv.Add(uv[index]);
                                        uv.RemoveAt(index);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (!parsedModel.untintedTransparent && TransparentArea(uv)) parsedModel.untintedTransparent = true;
            parsedModel.untintedMesh.vertices = vertices.ToArray();
            parsedModel.untintedMesh.uv = uv.ToArray();
            parsedModel.untintedMesh.triangles = triangles.ToArray();
            //Bounds bounds = parsedModel.untintedMesh.bounds;
            //heighestPoint += (bounds.max.y + thisdisplay.transform.position.y) * thisdisplay.transform.localScale.y;
            //lowestPoint += (bounds.min.y + thisdisplay.transform.position.y) * thisdisplay.transform.localScale.y;
            index = 0;
            foreach (Mesh mesh in parsedModel.tintedMeshes) {
                mesh.vertices = tintedvertices[index].ToArray();
                mesh.uv = tinteduv[index].ToArray();
                mesh.triangles = tintedtriangles[index].ToArray();
                index++;
            }
        }
    }
    private static bool TransparentArea(List<Vector2> uv)
    {
        int totalPixels = 0;
        int transparentPixels = 0;
        for (int i = 0; i < uv.Count; i += 4) {
            int[] uvs = {Mathf.FloorToInt(Mathf.Min(uv[i].x,uv[i+2].x) * TextureAtlas.atlasTexture.width), Mathf.FloorToInt(Mathf.Min(uv[i+1].y,uv[i+3].y) * TextureAtlas.atlasTexture.height), Mathf.FloorToInt(Mathf.Max(uv[i].x,uv[i+2].x) * TextureAtlas.atlasTexture.width), Mathf.FloorToInt(Mathf.Max(uv[i+1].y,uv[i+3].y) * TextureAtlas.atlasTexture.height)};
            if (uvs[0] == uvs[2]) uvs[2] += 1;
            if (uvs[1] == uvs[3]) uvs[3] += 1;
            for (int x = uvs[0]; x < uvs[2]; x++) {
                for (int y = uvs[1]; y < uvs[3]; y++) {
                    totalPixels++;
                    Color pixel = TextureAtlas.atlasTexture.GetPixel(x,y);
                    if (pixel.a <= 0.9f) transparentPixels++;
                }
            }
        }
        return (float)transparentPixels / (float)totalPixels > 0.25;
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
    private static Dictionary<string, string> UpdateTextureReferences(Dictionary<string, string> textures)
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
        if (newTextures.Values.Where(value => value.StartsWith("#")).ToList().Count != 0) return UpdateTextureReferences(newTextures);
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
    public bool untintedTransparent = false;
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
    public int tintindex = -1;

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