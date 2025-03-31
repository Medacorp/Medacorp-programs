using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;
public class MapEntity {
    public List<MapEntityPart> model_parts = new();
    public List<string> GetItemModels()
    {
        List<string> models = new();
        foreach (MapEntityPart part in model_parts)
        {
            List<string> newModels = part.GetItemModels();
            foreach (string model in newModels) if (!models.Contains(model)) models.Add(model);
        }
        return models;
    }
    public List<MinecraftItemModelFetched> GetModels()
    {
        List<MinecraftItemModelFetched> models = new();
        foreach (MapEntityPart part in model_parts)
        {
            List<MinecraftItemModelFetched> newModels = part.GetModels();
            foreach (MinecraftItemModelFetched model in newModels) if (!models.Contains(model)) models.Add(model);
        }
        return models;
    }
}
public class MapEntityPart {
    public string[] tags = {};
    public MinecraftItem minecraft_item = null;
    public void ParseItemModel(string path)
    {
        if (minecraft_item != null) minecraft_item.ParseItemModel(path);
    }
    public List<string> GetItemModels()
    {
        if (minecraft_item != null)
        {
            return minecraft_item.GetItemModels();
        }
        return new();
    }
    public List<MinecraftItemModelFetched> GetModels()
    {
        if (minecraft_item != null)
        {
            return minecraft_item.GetModels();
        }
        return new();
    }
}
public class MinecraftItem {
    public string item_model = null;
    public List<bool> flags = new();
    public List<float> floats = new();
    public List<string> strings = new();
    public List<int> colors = new();
    public int dyed_color = -1;
    public Dictionary<string, MinecraftItem> model_data = new();

    public List<string> GetItemModels() {
        List<string> models = new();
        if (!models.Contains(item_model)) models.Add(item_model);
        foreach (KeyValuePair<string, MinecraftItem> variant in model_data) {
            List<string> newModels = variant.Value.GetItemModels();
            foreach (string model in newModels) if (!models.Contains(model)) models.Add(model);
        }
        return models;
    }

    public MinecraftItem() {}
    public MinecraftItem(MinecraftItem baseItem) {
        item_model = baseItem.item_model;
        flags = baseItem.flags;
        floats = baseItem.floats;
        strings = baseItem.strings;
        colors = baseItem.colors;
        dyed_color = baseItem.dyed_color;
    }
    public List<MinecraftItemModelFetched> GetModels()
    {
        MinecraftItemsFile itemsFile = null;
        MinecraftModel.itemsFiles.TryGetValue(item_model, out itemsFile);
        if (itemsFile != null)
        {
            return itemsFile.GetModels(this);
        }
        return new();
    }

    public void ParseItemModel(string path)
    {
        if (!MinecraftModel.itemsFiles.ContainsKey(item_model))
        {
            string[] split = item_model.Split(':');
            if (split[0] == "") split[0] = "minecraft";
            if (File.Exists(path + "assets/" + split[0] + "/items/" + split[1] + ".json"))
            {
                MinecraftItemsFile itemsFile = JsonConvert.DeserializeObject<MinecraftItemsFile>(File.ReadAllText(path + "assets/" + split[0] + "/items/" + split[1] + ".json").Replace("\"default\"","\"value\""));
                itemsFile.Parse();
                MinecraftModel.itemsFiles.Add(item_model, itemsFile);
            }

        }
    }

    public void Parse(string SNBT) {
        if (SNBT.Contains("\"minecraft:dyed_color\":")) dyed_color = Convert.ToInt32(SNBT.Split("\"minecraft:dyed_color\":")[1].Split(",")[0]);
        if (SNBT.Contains("\"minecraft:item_model\":\"")) item_model = SNBT.Split("\"minecraft:item_model\":\"")[1].Split("\"")[0];
        else if (SNBT.Contains("id:\"")) item_model = SNBT.Split("id:\"")[1].Split("\"")[0];
        if (SNBT.Contains("\"minecraft:custom_model_data\":{")) {
            string cutLine = SNBT.Split("\"minecraft:custom_model_data\":{")[1].Split("}")[0];
            if (cutLine.Contains("flags:[B;")) {
                string tags = SNBT.Split("flags:[B;")[1].Split("b]")[0];
                string[] flags = tags.Split("b,");
                this.flags = new();
                foreach (string flag in flags) {
                    if (flag == "0") this.flags.Add(false);
                    else this.flags.Add(true);
                }
            }
            if (cutLine.Contains("floats:[")) {
                string tags = SNBT.Split("floats:[")[1].Split("f]")[0];
                string[] floats = tags.Split("f,");
                this.floats = new();
                foreach (string f in floats) {
                    this.floats.Add(float.Parse(f));
                }
            }
            if (cutLine.Contains("strings:[")) {
                string tags = SNBT.Split("strings:[\"")[1].Split("\"]")[0];
                string[] strings = tags.Split("\",\"");
                this.strings = new();
                foreach (string s in strings) {
                    this.strings.Add(s);
                }
            }
            if (cutLine.Contains("colors:[I;")) {
                string tags = SNBT.Split("colors:[I;")[1].Split("]")[0];
                string[] colors = tags.Split(",");
                this.colors = new();
                foreach (string c in colors) {
                    this.colors.Add(Convert.ToInt32(c));
                }
            }
        }
        if (SNBT.Contains("model_data:{")) {
            int brackets = 1;
            int characters = 1;
            string cutLine = SNBT.Split("model_data:{")[1];
            while (brackets != 0) {
                if (cutLine[0] == '{') brackets++;
                else if (cutLine[0] == '}') brackets--;
                characters++;
                cutLine = cutLine.Substring(1);
            }
            cutLine = SNBT.Split("model_data:")[1];
            string variants = cutLine.Substring(1,characters-2);
            while (variants.Length > 0) {
                string variantName = variants.Split(":")[0];
                cutLine = variants.Substring(variantName.Length+2);
                brackets = 1;
                characters = 1;
                while (brackets != 0) {
                    if (cutLine[0] == '{') brackets++;
                    else if (cutLine[0] == '}') brackets--;
                    characters++;
                    cutLine = cutLine.Substring(1);
                }
                cutLine = variants.Substring(variantName.Length+1);
                string variantSNBT = cutLine.Substring(0,characters);
                variants = variants.Substring(variantName.Length+2+characters-1);
                if (variants.Length != 0) variants = variants.Substring(1);
                MinecraftItem variantItem = new(this);
                variantItem.Parse(variantSNBT);
                model_data.Add(variantName,variantItem);
            }
        }
    }
}