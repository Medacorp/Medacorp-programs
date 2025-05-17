using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

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
            convertedModel.Parse();
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
        if (tints != null && tints.Count != 0)
        {
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
        }
        parsedTints = newtints;
        if (!MinecraftModel.modelFiles.Contains(model))
        {
            modelIndex = MinecraftModel.modelFiles.Count;
            MinecraftModel.modelFiles.Add(model);
            ParsedMinecraftModel parsedModel = new();
            parsedModel.name = model;
            parsedModel.tints = newtints;
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