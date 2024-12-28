using System;
using System.Collections.Generic;

[Serializable]
public class SavedEntities {
    public List<SavedEntity> entities;
    public SavedEntities() {
        entities = new();
    }
}
[Serializable]
public class SavedEntity {
    public List<ModelPart> parts;
    public string name;
    public SavedEntity() {
        
    }
    public SavedEntity(string ID, List<ModelPart> parts) {
        this.name = ID;
        this.parts = parts;
    }
    public SavedEntity(string[] ID, List<ModelPart> parts) {
        this.name = ID[0] + ":" + ID[1];
        this.parts = parts;
    }
}