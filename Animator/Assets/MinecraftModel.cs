using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

[Serializable]
public class MinecraftModel {
    
    public string name;
    public string parent;

    public Dictionary<string, string> textures;

    public List<MinecraftModelElement> elements;

    public Dictionary<string, MinecraftModelDisplay> display;

}

[Serializable]
public class MinecraftModelElement {

    public float[] from;
    public float[] to;
    public MinecraftModelRotation rotation;
    public Dictionary<string, MinecraftModelFace> faces;

    public Vector3 GetCenter() {
        Vector3 size = new(0,0,0);
        //(from + to)/2 gets offset from 0, which is the corner, - 8 gets the offset from the center
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

[Serializable]
public class MinecraftAtlas {
    public List<MinecraftAtlasSource> sources;
}

[Serializable]
public class MinecraftAtlasSource {
    public string type;
    public string source;
    public string prefix;

    //I'm not sure how to code the variants in; for now this'll be fine; I'll just accept all texture paths and just use this for "renamed" paths

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

    public List<Texture2D> textures;

    public MinecraftMcmetaAnimation(int width, int height) {
        this.width = width;
        this.height = height;
    }
    public void SetTexture(Texture2D texture) {
        textures = new();
        if (!(width == texture.width && height == texture.height)) {
            float divWidth = (float)width / (float)texture.width;
            float divHeight = (float)height / (float)texture.height;
            for (float w = 0; w <= 1 - divWidth; w = w + divWidth) {
                for (float h = 0; h <= 1 - divHeight; h = h + divHeight) {
                    Texture2D frame = new(width,height);
                    frame.SetPixels(texture.GetPixels(Mathf.FloorToInt(w * divWidth), Mathf.FloorToInt(h * divHeight), width, height));
                    textures.Add(frame);
                }
            }
        }
        else {
            textures.Add(texture);
        }
        ParseFrames();
    }
    public Texture2D GetFrame(int index) {
        return textures[index];
    }

    public List<object> frames;

    public List<MinecraftMcmetaAnimationFrame> parsedFrames = new List<MinecraftMcmetaAnimationFrame>();
    private void ParseFrames()
    {
        parsedFrames = new List<MinecraftMcmetaAnimationFrame>();

        int frametime = 1;
        if (this.frametime != 0) frametime = this.frametime;

        if (frames != null && frames.Count != 0) {
            foreach (var frame in frames)
            {
                // If the frame is an integer, create a FrameData with default time=1
                if (frame is int)
                {
                    parsedFrames.Add(new MinecraftMcmetaAnimationFrame
                    {
                        index = (int)frame,
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

[Serializable]
public class MinecraftMcmetaAnimationFrame {
    public int index;
    public int time;
}