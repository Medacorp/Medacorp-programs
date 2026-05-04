using System;
using System.Collections.Generic;

[Serializable]
public class AnimatorSettings
{
    public Dictionary<string, AnimatorSettingsEntity> entities;
    public AnimatorSettings()
    {
        entities = new();
    }
}

[Serializable]
public class AnimatorSettingsEntity
{
    public string spawn_function;
    public string animation_root;
}