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
public class OLDModelPart {

    public string name;
    public string[] model;
    public Dictionary<string, string[]> variants;

    private List<OLDConditionalModelPartOffset> offsetConditions = new();
    private List<OLDConditionalModelPartPose> poseConditions = new();
    private List<OLDConditionalModelPartVariant> variantConditions = new();

    public void addConditionalOffset(string tags, string scores, float[] offsets) {
        OLDConditionalModelPartOffset condition = new();
        condition.conditions = tags + " " + scores;
        if (offsetConditions.Count != 0) {
            foreach (OLDConditionalModelPartOffset offset in offsetConditions) {
                if (offset.conditions == tags + " " + scores) {
                    condition = offset;
                    offsetConditions.Remove(condition);
                    break;
                }
            }
        }
        condition.SetConditions(tags, scores, offsets);
        offsetConditions.Add(condition);
    }
    public void addConditionalPose(string tags, string scores, float[] defaultState) {
        OLDConditionalModelPartPose condition = new();
        condition.conditions = tags + " " + scores;
        if (poseConditions.Count != 0) {
            foreach (OLDConditionalModelPartPose pose in poseConditions) {
                if (pose.conditions == tags + " " + scores) {
                    condition = pose;
                    poseConditions.Remove(condition);
                    break;
                }
            }
        }
        condition.SetConditions(tags, scores, defaultState);
        poseConditions.Add(condition);
    }
    public void addConditionalPose(string tags, string scores, float defaultState, int axis) {
        OLDConditionalModelPartPose condition = new();
        condition.conditions = tags + " " + scores;
        if (poseConditions.Count != 0) {
            foreach (OLDConditionalModelPartPose pose in poseConditions) {
                if (pose.conditions == tags + " " + scores) {
                    condition = pose;
                    poseConditions.Remove(condition);
                    break;
                }
            }
        }
        condition.SetConditions(tags, scores, defaultState, axis);
        poseConditions.Add(condition);
    }
    public void addConditionalModelVariant(string tags, string scores, string variant) {
        OLDConditionalModelPartVariant condition = new();
        condition.conditions = tags + " " + scores;
        if (variantConditions.Count != 0) {
            foreach (OLDConditionalModelPartVariant pose in variantConditions) {
                if (pose.conditions == tags + " " + scores) {
                    condition = pose;
                    variantConditions.Remove(condition);
                    break;
                }
            }
        }
        condition.SetConditions(tags, scores, variant);
        variantConditions.Add(condition);
    }
    public List<OLDConditionalModelPartOffset> GetOffsets() {
        return offsetConditions;
    }
    public List<OLDConditionalModelPartPose> GetPoses() {
        return poseConditions;
    }
    public List<OLDConditionalModelPartVariant> GetVariants() {
        return variantConditions;
    }
    public OLDModelPart(string name) {
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
public class OLDConditionalModelPartSetting {
    public string conditions;
    public Dictionary<string,bool> tags = new();
    public Dictionary<string,string> scores = new();
    public void SetConditions(string tags, string scores) {
        conditions = tags + " " + scores;
        this.tags = new();
        this.scores = new();
        if (conditions != "") {
            string[] entries = conditions.Split(",");
            foreach (string entry in entries) {
                string[] split = entry.Split("=");
                if (split[1].StartsWith("!")) {
                    if (!split[1].Remove(0,1).StartsWith("was_")) this.tags.Add(split[1].Remove(0,1),false);
                }
                else if (!split[1].StartsWith("was_")) this.tags.Add(split[1],true);
            }
        }
    }
    public bool ConditionsMatch(Main mainScript) {
        if (tags.Count != 0) {
            foreach (KeyValuePair<string,bool> tag in tags) {
                if (mainScript.enabledTags.Contains(tag.Key) != tag.Value) return false;
            }
        }
        if (scores.Count != 0) {
            foreach (KeyValuePair<string,string> score in scores) {
                int scoreValue;
                string[] scoreKeyStrings = score.Key.Split(" ");
                string scoreKey = scoreKeyStrings[scoreKeyStrings.Length-2] + " " + scoreKeyStrings[scoreKeyStrings.Length-1];
                mainScript.scoreValues.TryGetValue(scoreKey, out scoreValue);
                if (score.Key.StartsWith("if")) {
                    if (!score.Value.Contains("..") && scoreValue != Convert.ToInt32(score.Value)) return false;
                    else if (score.Value.StartsWith("..")) {
                        string[] split = score.Value.Split("..");
                        if (split[0] == "" && scoreValue > Convert.ToInt32(split[1])) return false;
                        else if (split[1] == "" && scoreValue < Convert.ToInt32(split[0])) return false;
                        else if (!(scoreValue >= Convert.ToInt32(split[0]) && scoreValue <= Convert.ToInt32(split[1]))) return false;
                    }
                }
                else {
                    if (!score.Value.Contains("..") && scoreValue == Convert.ToInt32(score.Value)) return false;
                    else if (score.Value.StartsWith("..")) {
                        string[] split = score.Value.Split("..");
                        if (split[0] == "" && scoreValue <= Convert.ToInt32(split[1])) return false;
                        else if (split[1] == "" && scoreValue >= Convert.ToInt32(split[0])) return false;
                        else if (scoreValue >= Convert.ToInt32(split[0]) && scoreValue <= Convert.ToInt32(split[1])) return false;
                    }
                }
            }
        }
        return true;
    }

}
public class OLDConditionalModelPartPose : OLDConditionalModelPartSetting {
    private float defaultStateX = 9999;
    private float defaultStateY = 9999;
    private float defaultStateZ = 9999;

    public void SetConditions(string tags, string scores, float[] defaultState) {
        defaultStateX = -defaultState[0];
        defaultStateY = defaultState[1];
        defaultStateZ = defaultState[2];
        SetConditions(tags, scores);
    }

    public void SetConditions(string tags, string scores, float defaultState, int axis) {
        if (axis == 0) defaultStateX = -defaultState;
        else if (axis == 1) defaultStateY = defaultState;
        else defaultStateZ = defaultState;
        SetConditions(tags, scores);
    }
    public float[] GetPoses() {
        float[] poses = {defaultStateX, defaultStateY, defaultStateZ};
        return poses;
    }
}
public class OLDConditionalModelPartOffset : OLDConditionalModelPartSetting {
    private float[] offsets = {0,0,0};

    public void SetConditions(string tags, string scores, float[] offsets) {
        this.offsets = offsets;
        SetConditions(tags, scores);
    }
    public void SetOffsets(float[] offsets) {
        this.offsets = offsets;
    }
    public float[] GetOffsets() {
        return offsets;
    }

}
public class OLDConditionalModelPartVariant : OLDConditionalModelPartSetting {
    private string variant = "default";

    public void SetConditions(string tags, string scores, string variant) {
        this.variant = variant;
        SetConditions(tags, scores);
    }
    public void SetModelVariant(string variant) {
        this.variant = variant;
    }
    public string GetModelVariant() {
        return variant;
    }

}