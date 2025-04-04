using System;
using System.Collections.Generic;

[Serializable]
public class OLDSavedEntities {
    public List<OLDSavedEntity> entities;
    public OLDSavedEntities() {
        entities = new();
    }
}
[Serializable]
public class OLDSavedEntity {
    public List<OLDModelPart> parts;
    public string name;
    public OLDSavedEntity() {
        
    }
    public OLDSavedEntity(string ID, List<OLDModelPart> parts) {
        this.name = ID;
        this.parts = parts;
    }
    public OLDSavedEntity(string[] ID, List<OLDModelPart> parts) {
        this.name = ID[0] + ":" + ID[1];
        this.parts = parts;
    }
}