using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;
using UnityEngine.Rendering.Universal;

[Serializable]
public class ModelPart {

    public string name;
    public string[] model;
    public Dictionary<string, string[]> variants;

    private List<ConditionalModelPartOffset> offsetConditions = new();
    private List<ConditionalModelPartPose> poseConditions = new();
    private List<ConditionalModelPartVariant> variantConditions = new();

    public void addConditionalOffset(string conditions, float[] offsets) {
        ConditionalModelPartOffset condition = new();
        condition.conditions = conditions;
        if (offsetConditions.Count != 0) {
            foreach (ConditionalModelPartOffset offset in offsetConditions) {
                if (offset.conditions == conditions) {
                    condition = offset;
                    offsetConditions.Remove(condition);
                    break;
                }
            }
        }
        condition.SetConditions(conditions, offsets);
        offsetConditions.Add(condition);
    }
    public void addConditionalPose(string conditions, float[] defaultState) {
        ConditionalModelPartPose condition = new();
        condition.conditions = conditions;
        if (poseConditions.Count != 0) {
            foreach (ConditionalModelPartPose pose in poseConditions) {
                if (pose.conditions == conditions) {
                    condition = pose;
                    poseConditions.Remove(condition);
                    break;
                }
            }
        }
        condition.SetConditions(conditions, defaultState);
        poseConditions.Add(condition);
    }
    public void addConditionalPose(string conditions, float defaultState, int axis) {
        ConditionalModelPartPose condition = new();
        condition.conditions = conditions;
        if (poseConditions.Count != 0) {
            foreach (ConditionalModelPartPose pose in poseConditions) {
                if (pose.conditions == conditions) {
                    condition = pose;
                    poseConditions.Remove(condition);
                    break;
                }
            }
        }
        condition.SetConditions(conditions, defaultState, axis);
        poseConditions.Add(condition);
    }
    public void addConditionalModelVariant(string conditions, string variant) {
        ConditionalModelPartVariant condition = new();
        condition.conditions = conditions;
        if (variantConditions.Count != 0) {
            foreach (ConditionalModelPartVariant pose in variantConditions) {
                if (pose.conditions == conditions) {
                    condition = pose;
                    variantConditions.Remove(condition);
                    break;
                }
            }
        }
        condition.SetConditions(conditions, variant);
        variantConditions.Add(condition);
    }
    public List<ConditionalModelPartOffset> GetOffsets() {
        return offsetConditions;
    }
    public List<ConditionalModelPartPose> GetPoses() {
        return poseConditions;
    }
    public List<ConditionalModelPartVariant> GetVariants() {
        return variantConditions;
    }
    public ModelPart(string name) {
        this.name = name;
        variants = new();
    }
    public string GetName() {
        return name;
    }
    public string[] GetModel() {
        return model;
    }
    public string[] GetModel(string variant) {
        if (variants != null && variants.Count != 0) {
            foreach (KeyValuePair<string, string[]> var in variants) {
                if (var.Key == variant) {
                    if (var.Value.Length == 0) break;
                    return var.Value;
                }
            }
        }
        return model;
    }
    public string GetModel(int index) {
        return model[index];
    }
    public string GetModel(int index, string variant) {
        if (variants != null && variants.Count != 0) {
            foreach (KeyValuePair<string, string[]> var in variants) {
                if (var.Key == variant) {
                    if (var.Value.Length-1 >= index) return var.Value[index];
                }
            }
        }
        return GetModel(index);
    }
    public bool VariantExists(string variant) {
        if (variants != null && variants.Count != 0) {
            foreach (KeyValuePair<string, string[]> var in variants) {
                if (var.Key == variant) return true;
            }
        }
        return false;
    }
    public void SetModel(string[] model) {
        this.model = model;
    }
    public void SetModel(string[] model, string variant) {
        variants.Remove(variant);
        variants.Add(variant,model);
    }
    public void SetModel(string model, int index) {
        if (this.model.Length-1 >= index) this.model[index] = model;
        else {
            List<string> models = this.model.ToList();
            models.Add(model);
            this.model = models.ToArray();
        }
    }
    public void SetModel(string model, int index, string variant) {
        string[] models = {};
        if (variants != null && variants.Count != 0) {
            foreach (KeyValuePair<string,string[]> var in variants) {
                if (var.Key == variant) {
                    models = var.Value;
                    break;
                }
            }
        }
        variants.Remove(variant);
        models[index] = model;
        variants.Add(variant,models);
    }
    public void DeleteModel(int index) {
        if (model.Length-1 >= index) {
            var models = new List<string>(model);
            models.RemoveAt(index);
            model = models.ToArray();
        }
    }
    public void DeleteModel(int index, string variant) {
        string[] models;
        string[] model = {};
        if (variants != null && variants.Count != 0) {
            foreach (KeyValuePair<string,string[]> var in variants) {
                if (var.Key == variant) {
                    model = var.Value;
                    break;
                }
            }
        }
        variants.Remove(variant);
        var newmodels = new List<string>(model);
        newmodels.RemoveAt(index);
        models = newmodels.ToArray();
        if (models.Length != 0) variants.Add(variant,models);
    }
}
public class ConditionalModelPartPose {
    private float defaultStateX = 9999;
    private float defaultStateY = 9999;
    private float defaultStateZ = 9999;
    public string conditions;
    public Dictionary<string,bool> tags;

    public void SetConditions(string conditions, float[] defaultState) {
        this.conditions = conditions;
        defaultStateX = -defaultState[0];
        defaultStateY = defaultState[1];
        defaultStateZ = defaultState[2];
        tags = new();
        if (conditions != "") {
            string[] entries = conditions.Split(",");
            foreach (string entry in entries) {
                string[] split = entry.Split("=");
                if (split[1].StartsWith("!")) {
                    if (!split[1].Remove(0,1).StartsWith("was_")) tags.Add(split[1].Remove(0,1),false);
                }
                else if (!split[1].StartsWith("was_")) tags.Add(split[1],true);
            }
        }
    }

    public void SetConditions(string conditions, float defaultState, int axis) {
        this.conditions = conditions;
        if (axis == 0) defaultStateX = -defaultState;
        else if (axis == 1) defaultStateY = defaultState;
        else defaultStateZ = defaultState;
        tags = new();
        if (conditions != "") {
            string[] entries = conditions.Split(",");
            foreach (string entry in entries) {
                string[] split = entry.Split("=");
                if (split[1].StartsWith("!")) {
                    if (!split[1].Remove(0,1).StartsWith("was_")) tags.Add(split[1].Remove(0,1),false);
                }
                else if (!split[1].StartsWith("was_")) tags.Add(split[1],true);
            }
        }
    }
    public float[] GetPoses() {
        float[] poses = {defaultStateX, defaultStateY, defaultStateZ};
        return poses;
    }
}
public class ConditionalModelPartOffset {
    private float[] offsets = {0,0,0};
    public string conditions;

    public void SetConditions(string conditions, float[] offsets) {
        this.conditions = conditions;
        this.offsets = offsets;
        tags = new();
        if (conditions != "") {
            string[] entries = conditions.Split(",");
            foreach (string entry in entries) {
                string[] split = entry.Split("=");
                if (split[1].StartsWith("!")) {
                    tags.Add(split[1].Remove(0,1),false);
                }
                else tags.Add(split[1],true);
            }
        }
    }

    public Dictionary<string,bool> tags;
    public void SetOffsets(float[] offsets) {
        this.offsets = offsets;
    }
    public float[] GetOffsets() {
        return offsets;
    }

}
public class ConditionalModelPartVariant {
    private string variant = "default";
    public string conditions;

    public void SetConditions(string conditions, string variant) {
        this.conditions = conditions;
        this.variant = variant;
        tags = new();
        if (conditions != "") {
            string[] entries = conditions.Split(",");
            foreach (string entry in entries) {
                string[] split = entry.Split("=");
                if (split[1].StartsWith("!")) {
                    tags.Add(split[1].Remove(0,1),false);
                }
                else tags.Add(split[1],true);
            }
        }
    }

    public Dictionary<string,bool> tags;
    public void SetModelVariant(string variant) {
        this.variant = variant;
    }
    public string GetModelVariant() {
        return variant;
    }

}