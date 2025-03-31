using System;
using System.Collections.Generic;

[Serializable]
public class AnimatorSettings {
    public Dictionary<string, string> spawn_functions;
    public AnimatorSettings() {
        spawn_functions = new();
    }
}