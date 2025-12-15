from MEDACORP import *
import re
import zlib
from zipfile import ZIP_DEFLATED, ZipFile

def verifyDimensions(folder: str, namespace: str, ID: str) -> bool:
    succeed = True
    for dID in [name for name in os.listdir(folder) if os.path.isdir(os.path.join(folder, name))]:
        if ID == "":
            newID = dID
        else:
            newID = ID + "/" + dID
        if os.path.isfile(os.path.join(folder + "/" + dID + "/data/chunks.dat")):
            write("VERIFY: Dimension " + namespace + ":" + newID + " has forceloaded chunks")
            succeed = False
        if os.path.isfile(os.path.join(folder + "/" + dID + "/data/raids.dat")):
            write("VERIFY: Dimension " + namespace + ":" + newID + " has raids")
            succeed = False
        if os.path.isdir(os.path.join(folder + "/" + dID + "/entities")):
            write("VERIFY: Dimension " + namespace + ":" + newID + " has entity files")
            succeed = False
        if os.path.isdir(os.path.join(folder + "/" + dID + "/poi")):
            write("VERIFY: Dimension " + namespace + ":" + newID + " has POI files")
            succeed = False
        subSucceed = verifyDimensions(folder + "/" + dID, namespace, newID)
        if (subSucceed == False): succeed = False
    return succeed
def resetDimensions(folder: str, namespace: str, ID: str):
    for dID in [name for name in os.listdir(folder) if os.path.isdir(os.path.join(folder, name))]:
        if ID == "":
            newID = dID
        else:
            newID = ID + "/" + dID
        if os.path.isfile(os.path.join(folder + "/" + dID + "/data/chunks.dat")):
            write("RESET: Dimension " + namespace + ":" + newID + " had its forceloaded chunks deleted")
            os.remove(os.path.join(folder + "/" + dID + "/data/chunks.dat"))
        if os.path.isfile(os.path.join(folder + "/" + dID + "/data/raids.dat")):
            write("RESET: Dimension " + namespace + ":" + newID + " had its raids deleted")
            os.remove(os.path.join(folder + "/" + dID + "/data/raids.dat"))
        if os.path.isdir(os.path.join(folder + "/" + dID + "/entities")):
            write("RESET: Dimension " + namespace + ":" + newID + " had its entity files deleted")
            shutil.rmtree(os.path.join(folder + "/" + dID + "/entities"))
        if os.path.isdir(os.path.join(folder + "/" + dID + "/poi")):
            write("RESET: Dimension " + namespace + ":" + newID + " had its POI files deleted")
            shutil.rmtree(os.path.join(folder + "/" + dID + "/poi"))
        resetDimensions(folder + "/" + dID, namespace, newID)

WorldsPath= ""
ZipsPath= ""
if os.path.exists(getExecutionPath(True) + "paths.txt"):
    paths = open(getExecutionPath(True) + "paths.txt", "r")
    i = 0
    while i <= 1:
        line = paths.readline()
        if line.startswith("builds="):
            ZipsPath = line.replace("builds=","").replace("\r","").replace("\n","")
            ZipsPath = ZipsPath.replace("\\", "/")
            if os.path.isdir(os.path.join(ZipsPath)) == False:
                ZipsPath = ""
        if line.startswith("worlds="):
            WorldsPath = line.replace("worlds=","").replace("\r","").replace("\n","")
            WorldsPath = WorldsPath.replace("\\", "/")
            if os.path.isdir(os.path.join(WorldsPath)) == False:
                WorldsPath = ""
        i += 1
    paths.close()
if ZipsPath == "":
    write("Please provide the path where the builds should be created (type \"here\" if it's the same folder as where this program is)")
    while ZipsPath == "":
        selected=input().replace("\\", "/")
        if selected.lower() == "here":
            ZipsPath = getExecutionPath()
        else:
            if selected.endswith("/"): selected = selected[slice(0,-1)]
            if os.path.isdir(os.path.join(selected)):
                ZipsPath = selected
            else:
                write("That's not an existing folder")
if ZipsPath.endswith("/") == False: ZipsPath = ZipsPath + "/"
if WorldsPath == "":
    write("Please provide the path where the worlds are all located in their github repository state (type \"here\" if it's the same folder as where this program is)")
    while WorldsPath == "":
        selected=input().replace("\\", "/")
        if selected.lower() == "here":
            WorldsPath = getExecutionPath()
        else:
            if selected.endswith("/"): selected = selected[slice(0,-1)]
            if os.path.isdir(os.path.join(selected)):
                WorldsPath = selected
            else:
                write("That's not an existing folder")
if WorldsPath.endswith("/") == False: WorldsPath = WorldsPath + "/"
paths = open(getExecutionPath(True) + "paths.txt", "w")
paths.write("builds=" + ZipsPath + "\nworlds=" + WorldsPath)
paths.close()
write("File path where builds get created: " + ZipsPath[slice(0,-1)])
write("File path where worlds get searched for: " + WorldsPath[slice(0,-1)])
write("You can also find the logs and a file to modify the above paths here: " + getExecutionPath())
print("")

while True:
    write("What world do you wish to build? (Type \"!\" to close program)")
    validMaps = ""
    for subfolder in [name for name in os.listdir(WorldsPath) if os.path.isfile(os.path.join(WorldsPath, name, ".gitignore"))]:
        for subfolder2 in [name for name in os.listdir(os.path.join(WorldsPath, subfolder)) if os.path.isfile(os.path.join(WorldsPath, subfolder, name, "level.dat")) if os.path.isdir(os.path.join(WorldsPath, subfolder, name + " Resource Pack")) if os.path.isfile(os.path.join(WorldsPath, subfolder, name + " Resource Pack", "pack.mcmeta"))]:
            if validMaps == "": validMaps = subfolder2
            else: validMaps = validMaps + ", " + subfolder2
    write("Valid worlds: " + validMaps)
    selected=input().lower().replace("'", "").replace(":", "")
    verify = "u"
    rootFolder = ""
    map = ""
    path = ""
    dimensionIDs = []
    useNether = False
    useEnd = False
    unlocked = False
    verifySucceeded = False
    alreadyBuild = False
    mapSet = 0
    if selected == "!":
        break
    else:
        for subfolder in [name for name in os.listdir(WorldsPath) if os.path.isfile(os.path.join(WorldsPath, name, ".gitignore"))]:
            for subfolder2 in [name for name in os.listdir(os.path.join(WorldsPath, subfolder)) if os.path.isfile(os.path.join(WorldsPath, subfolder, name, "level.dat")) if os.path.isdir(os.path.join(WorldsPath, subfolder, name + " Resource Pack")) if os.path.isfile(os.path.join(WorldsPath, subfolder, name + " Resource Pack", "pack.mcmeta"))]:
                folder = subfolder2.lower().replace("'", "").replace(":", "").replace("-", "")
                words = folder.split(" ")
                truncated = ""
                for word in words: truncated = truncated + word[0]
                if selected == folder or selected == truncated:
                    rootFolder = subfolder
                    map = subfolder2
                    mapSet = 1
                if mapSet != 1:
                    if re.search(".*" + selected + ".*", folder):
                        rootFolder = subfolder
                        map = subfolder2
                        if mapSet == 2 or mapSet == 3: mapSet = 3
                        else: mapSet = 2
                    elif re.search(".*" + selected + ".*", truncated):
                        rootFolder = subfolder
                        map = subfolder2
                        if mapSet == 2 or mapSet == 3: mapSet = 3
                        else: mapSet = 2
        if mapSet == 0: write("Matched no world")
        elif mapSet == 3:
            map = ""
            write("Matched several worlds, please be more specific")

    if (map != ""):
        path = WorldsPath + rootFolder + "/"
        write("World set to " + map + ".")
        dimensionIDs = []
        
        if os.path.isdir(os.path.join(path + map + "/datapacks/MEDACORP/data")):
            for subfolder in [name for name in os.listdir(os.path.join(path + map + "/datapacks/MEDACORP/data")) if os.path.isdir(os.path.join(path + map + "/datapacks/MEDACORP/data/" + name)) if os.path.isdir(os.path.join(path + map + "/datapacks/MEDACORP/data/" + name + "/dimension"))]:
                for folder_name, sub_folders2, file_names2 in os.walk(path + map + "/datapacks/MEDACORP/data/" + subfolder + "/dimension"):
                    for filename in file_names2:
                        if filename.endswith(".json"):
                            file_path = os.path.join(folder_name)
                            file_path = file_path.replace("\\","/")
                            file_path = file_path.replace(path + map + "/datapacks/MEDACORP/data/" + subfolder + "/dimension", "")
                            if file_path.startswith("/"): file_path = file_path[slice(1,-1)] + file_path[-1]
                            if file_path != "": file_path = file_path + "/"
                            dimensionID = subfolder + ":" + file_path + filename[slice(0,-5)]
                            if dimensionID == "minecraft:the_nether":
                                useNether = True
                            elif dimensionID == "minecraft:the_end":
                                useEnd = True
                            elif dimensionID != "minecraft:overworld":
                                dimensionIDs.append(dimensionID)
        
        while True:
            print("")
            write("What would you like to do? Type one of the following:")
            write("* \"stop\"")
            write("* \"rules\"")
            if unlocked == False: write("* \"unlock\"")
            if verifySucceeded == False: 
                write("* \"verify\"")
                write("* \"reset\"")
            if alreadyBuild == False: write("* \"build\"")
            selected = input().lower()
            if selected == "stop":
                write("Stopped checking " + map)
                break
            elif selected == "rules":
                write("Rules:")
                write("* Game rule sendCommandFeedback set to false")
                write("* Cheats disabled")
                write("* No player data remaining")
                write("* No save progress remaining")
                write("* No additional data packs enabled")
                write("* No missing pack.mcmeta-s")
                if useNether == False: 
                    write("* No Nether data remaining")
                if useEnd == False: 
                    write("* No End data remaining")
                if len(dimensionIDs) == 1: write("* 1 custom dimension: " + str(dimensionIDs[0]))
                else: 
                    tempString = ""
                    first = True
                    for dimension in dimensionIDs:
                        if first: 
                            tempString = dimension
                            first = False
                        else:
                            tempString = tempString + ", " + dimension
                    write("* " + str(len(dimensionIDs)) + " custom dimensions: " + tempString)
            elif selected == "verify" and verifySucceeded == False:
                succeed = True
                write("Running verification")

                #Check level.dat
                nbtfile = nbt.NBTFile(path + map + "/level.dat", "rb")
                nbtfile.name = 'level'
                for tag in nbtfile["Data"]["game_rules"].tags:
                    if tag.name == "minecraft:send_command_feedback" and tag.value == 1:
                        write("VERIFY: Game rule \"minecraft:send_command_feedback\" is set to true")
                        succeed = False
                for tag in nbtfile["Data"].tags:
                    if tag.name == "allowCommands" and tag.value == 1:
                        write("VERIFY: Cheats are enabled")
                        succeed = False
                    if tag.name == "Player":
                        write("VERIFY: Player data is present in level.dat")
                        succeed = False
                tagCount = 0
                for tag in nbtfile["Data"]["DataPacks"]["Enabled"].tags:
                    tagCount += 1
                if tagCount != 2:
                    write("VERIFY: Extra data packs are enabled")
                    succeed = False
                tagCount = 0
                for tag in nbtfile["Data"]["CustomBossEvents"].tags:
                    tagCount += 1
                if tagCount != 0:
                    write("VERIFY: Custom boss bars are stored")
                    succeed = False
                tagCount = 0
                for tag in nbtfile["Data"]["WorldGenSettings"]["dimensions"].tags:
                    if tag.name != "minecraft:overworld" and tag.name != "minecraft:the_nether" and tag.name != "minecraft:the_end": tagCount += 1
                if tagCount != len(dimensionIDs):
                    write("VERIFY: Dimension count is incorrect")
                    succeed = False
                
                #Check official add-ons
                if os.path.isdir(os.path.join(path + "Official add-ons")):
                    officialAddons = [name for name in os.listdir(path + "Official add-ons") if os.path.isdir(os.path.join(path + "Official add-ons", name))]
                    for addon in officialAddons:
                        addonName = addon.replace(" add-on","")
                        if os.path.isdir(path + "Official add-ons/" + addon + "/" + addonName):
                            if os.path.isfile(path + "Official add-ons/" + addon + "/" + addonName + "/pack.mcmeta") == False:
                                write("VERIFY: " + addon + " is missing its pack.mcmeta")
                                succeed = False
                        else:
                            write("VERIFY: " + addon + " is missing")
                            succeed = False
                
                #Check save data
                if os.path.isfile(os.path.join(path + map + "/session.lock")):
                    write("VERIFY: Level session.lock is present")
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/level.dat_old")):
                    write("VERIFY: Backup level.dat is present")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/generated")):
                    write("VERIFY: Unsaved structure files are present")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/playerdata")):
                    write("VERIFY: Player data is still in world data")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/stats")):
                    write("VERIFY: Statistics data is still in world data")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/advancements")):
                    write("VERIFY: Advancement data is still in world data")
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/data/random_sequences.dat")):
                    write("VERIFY: Random sequence data is still in world data")
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/data/scoreboard.dat")):
                    write("VERIFY: Scoreboard data is still in world data")
                    succeed = False
                for file in [name for name in os.listdir(path + map + "/data") if os.path.isfile(os.path.join(path + map + "/data", name))]:
                    if file.startswith("command_storage_"):
                        fileName = file.removeprefix("command_storage_").removesuffix(".dat")
                        write("VERIFY: Command storage \"" + fileName + "\" is still in world data")
                        succeed = False
                if os.path.isfile(os.path.join(path + map + "/data/chunks.dat")):
                    write("VERIFY: Dimension minecraft:overworld has forceloaded chunks")
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/data/raids.dat")):
                    write("VERIFY: Dimension minecraft:overworld has raids")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/entities")):
                    write("VERIFY: Dimension minecraft:overworld has entity files")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/poi")):
                    write("VERIFY: Dimension minecraft:overworld has POI files")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM-1")) and useNether == False:
                    write("VERIFY: Dimension minecraft:the_nether has files")
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/DIM-1/data/chunks.dat")) and useNether:
                    write("VERIFY: Dimension minecraft:the_nether has forceloaded chunks")
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/DIM-1/data/raids.dat")) and useNether:
                    write("VERIFY: Dimension minecraft:the_nether has raids")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM-1/entities")) and useNether:
                    write("VERIFY: Dimension minecraft:the_nether has entity files")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM-1/poi")) and useNether:
                    write("VERIFY: Dimension minecraft:the_nether has POI files")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM1")) and useEnd == False:
                    write("VERIFY: Dimension minecraft:the_end has files")
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/DIM1/data/chunks.dat")) and useEnd:
                    write("VERIFY: Dimension minecraft:the_end has forceloaded chunks")
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/DIM1/data/raids.dat")) and useEnd:
                    write("VERIFY: Dimension minecraft:the_end has raids")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM1/entities")) and useEnd:
                    write("VERIFY: Dimension minecraft:the_end has entity files")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM1/poi")) and useEnd:
                    write("VERIFY: Dimension minecraft:the_end has POI files")
                    succeed = False
                for d in [name for name in os.listdir(path + map + "/dimensions") if os.path.isdir(os.path.join(path + map + "/dimensions", name))]:
                    subSucceed = verifyDimensions(path + map + "/dimensions/" + d, d, "")
                    if (subSucceed == False): succeed = False
                if succeed:
                    write("VERIFY: Map is properly reset")
                    verifySucceeded = True
                else:
                    write("VERIFY: Map is not reset")
            elif selected == "verify" and verifySucceeded:
                write("Verify already confirmed the map is reset")
            elif selected == "unlock":
                write("Running unlock")
                if unlocked == False:
                    nbtfile = nbt.NBTFile(path + map + "/level.dat", "rb")
                    nbtfile.name = 'level'
                    modify = False
                    for tag in nbtfile["Data"]["game_rules"].tags:
                        if tag.name == "minecraft:send_command_feedback" and tag.value == 0:
                            tag.value = 1
                            write("UNLOCK: Game rule \"minecraft:send_command_feedback\" set to true")
                            modify = True
                    for tag in nbtfile["Data"].tags:
                        if tag.name == "allowCommands" and tag.value == 0:
                            write("UNLOCK: Enabled cheats")
                            tag.value = 1
                            modify = True
                if modify: 
                    nbtfile.write_file(path + map + "/level.dat")
                    write("UNLOCK: Unlocked map for development")
                else:
                    write("UNLOCK: Map was already unlocked")
                unlocked = True
                verifySucceeded = False
            elif selected == "reset" and verifySucceeded == False:
                succeed = True
                structureDelete = False
                missingPackMcmeta = False
                write("Running reset")

                #Check level.dat
                nbtfile = nbt.NBTFile(path + map + "/level.dat", "rb")
                nbtfile.name = 'level'
                modify = False
                for tag in nbtfile["Data"]["game_rules"].tags:
                    if tag.name == "minecraft:send_command_feedback" and tag.value == 1:
                        tag.value = 0
                        write("RESET: Game rule \"minecraft:send_command_feedback\" set to false")
                        modify = True
                for tag in nbtfile["Data"].tags:
                    if tag.name == "allowCommands" and tag.value == 1:
                        write("RESET: Disabled cheats")
                        tag.value = 0
                        modify = True
                    if tag.name == "Player": 
                        write("RESET: Deleted player data in level.dat")
                        del nbtfile["Data"]["Player"]
                        modify = True
                tagCount = 0
                for tag in nbtfile["Data"]["DataPacks"]["Enabled"].tags:
                    tagCount += 1
                if tagCount != 2:
                    write("RESET: Deleted extra enabled data packs")
                    del nbtfile["Data"]["DataPacks"]["Enabled"]
                    endabledList = nbt.TAG_List(name="Enabled",type=nbt.TAG_String)
                    endabledList.tags.append(nbt.TAG_String(value="vanilla"))
                    endabledList.tags.append(nbt.TAG_String(value="file/MEDACORP"))
                    nbtfile["Data"]["DataPacks"].tags.append(endabledList)
                    modify = True
                tagCount = 0
                for tag in nbtfile["Data"]["CustomBossEvents"].tags:
                    tagCount += 1
                if tagCount != 0:
                    write("RESET: Deleted custom boss bars")
                    del nbtfile["Data"]["CustomBossEvents"]
                    bossBars = nbt.TAG_Compound(name="CustomBossEvents")
                    nbtfile["Data"].tags.append(bossBars)
                    modify = True
                dimensionsFound = []
                dimensionList = nbt.TAG_Compound(name="dimensions")
                for tag in nbtfile["Data"]["WorldGenSettings"]["dimensions"].tags:
                    if tag.name == "minecraft:overworld" or tag.name == "minecraft:the_nether" or tag.name == "minecraft:the_end" or tag.name in dimensionIDs:
                        dimensionList.tags.append(tag)
                        if tag.name in dimensionIDs: dimensionsFound.append(tag.name)
                    else:
                        write("RESET: Dimension \"" + tag.name + "\" has been deleted from generation data")
                        modify = True
                del nbtfile["Data"]["WorldGenSettings"]["dimensions"]
                nbtfile["Data"]["WorldGenSettings"].tags.append(dimensionList)
                missingDimension = False
                if len(dimensionsFound) != len(dimensionIDs):
                    for dimension in dimensionIDs:
                        missing = True
                        for foundDimension in dimensionsFound:
                            if dimension == foundDimension: missing = False
                        if missing: 
                            write("RESET ERROR: Dimension " + dimension + " is missing from generation data")
                            succeed = False
                            missingDimension = True
                if modify: nbtfile.write_file(path + map + "/level.dat")
                
                #Check official add-ons
                if os.path.isdir(os.path.join(path + "Official add-ons")):
                    officialAddons = [name for name in os.listdir(path + "Official add-ons") if os.path.isdir(os.path.join(path + "Official add-ons", name))]
                    for addon in officialAddons:
                        addonName = addon.replace(" add-on","")
                        if os.path.isdir(path + "Official add-ons/" + addon + "/" + addonName):
                            foundMcmeta = True
                            if os.path.isfile(path + "Official add-ons/" + addon + "/" + addonName + "/pack.mcmeta") == False:
                                foundMcmeta = False
                                for file in [name for name in os.listdir(path + "Official add-ons/" + addon + "/" + addonName) if os.path.isfile(os.path.join(path + "Official add-ons/" + addon + "/" + addonName, name))]:
                                    if file.endswith(".mcmeta"):
                                        write("RESET: " + addon + "'s \"" + file + "\" renamed to \"pack.mcmeta\"")
                                        os.rename(os.path.join(path + "Official add-ons/" + addon + "/" + addonName + "/" + file), os.path.join(path + "Official add-ons/" + addon + "/" + addonName + "/pack.mcmeta"))
                                        foundMcmeta = True
                            if foundMcmeta == False: 
                                write("RESET ERROR: " + addon + " is missing its pack.mcmeta; this needs manual fixing")
                                succeed = False
                                missingPackMcmeta = True
                        else:
                            write("RESET WARN: " + addon + " is missing altogether; this needs manual fixing")
                            succeed = False
                            missingPackMcmeta = True
                
                #Check save data
                if os.path.isfile(os.path.join(path + map + "/session.lock")):
                    write("RESET: Level session.lock deleted")
                    os.remove(os.path.join(path + map + "/session.lock"))
                if os.path.isfile(os.path.join(path + map + "/level.dat_old")):
                    write("RESET: Backup level.dat deleted")
                    os.remove(os.path.join(path + map + "/level.dat_old"))
                if os.path.isdir(os.path.join(path + map + "/generated")):
                    for folder_name, sub_folders, file_names in os.walk(path + map + "/generated"):
                        for subfolder in sub_folders:
                            file_path = os.path.join(folder_name, subfolder)
                            file_path = file_path.replace("\\","/")
                            file_path = file_path.replace(path + map + "/generated/", "")
                            file_path = file_path.replace("/structures/","/structure/")
                            if os.path.isdir(os.path.join(path + map + "/datapacks/MEDACORP/data/" + file_path)) == False and subfolder != "structures":
                                write("RESET: Created directory \"" + map + "/datapacks/MEDACORP/data/" + file_path + "\"", True)
                                os.makedirs(os.path.join(path + map + "/datapacks/MEDACORP/data/" + file_path))
                        for filename in file_names:
                            file_path = os.path.join(folder_name, filename)
                            file_path = file_path.replace("\\","/")
                            file_path = file_path.replace(path + map + "/generated/", "")
                            if os.path.isfile(os.path.join(path + map + "/datapacks/MEDACORP/data/" + file_path.replace("/structures/","/structure/"))):
                                write("RESET ERROR: Generated file \"" + file_path + "\" cannot be moved to \"MEDACORP\" data pack because the file already exists")
                                structureDelete = True
                                succeed = False
                            else:
                                write("RESET: Generated file \"" + file_path + "\" moved to \"MEDACORP\" data pack")
                                os.rename(os.path.join(path + map + "/generated/" + file_path), os.path.join(path + map + "/datapacks/MEDACORP/data/" + file_path.replace("/structures/","/structure/")))
                    if structureDelete == False:
                        shutil.rmtree(os.path.join(path + map + "/generated"))
                if os.path.isdir(os.path.join(path + map + "/playerdata")):
                    write("RESET: Player data has been deleted")
                    shutil.rmtree(os.path.join(path + map + "/playerdata"))
                if os.path.isdir(os.path.join(path + map + "/stats")):
                    write("RESET: Statistics data has been deleted")
                    shutil.rmtree(os.path.join(path + map + "/stats"))
                if os.path.isdir(os.path.join(path + map + "/advancements")):
                    write("RESET: Advancement data has been deleted")
                    shutil.rmtree(os.path.join(path + map + "/advancements"))
                if os.path.isfile(os.path.join(path + map + "/data/random_sequences.dat")):
                    write("RESET: Random sequence data has been deleted")
                    os.remove(os.path.join(path + map + "/data/random_sequences.dat"))
                if os.path.isfile(os.path.join(path + map + "/data/scoreboard.dat")):
                    write("RESET: Scoreboard data has been deleted")
                    os.remove(os.path.join(path + map + "/data/scoreboard.dat"))
                for file in [name for name in os.listdir(path + map + "/data") if os.path.isfile(os.path.join(path + map + "/data", name))]:
                    if file.startswith("command_storage_"):
                        fileName = file.removeprefix("command_storage_").removesuffix(".dat")
                        write("RESET: Command storage \"" + fileName + "\" has been deleted")
                        os.remove(os.path.join(path + map + "/data/" + file))
                if os.path.isfile(os.path.join(path + map + "/data/chunks.dat")):
                    write("RESET: Dimension minecraft:overworld had its forceloaded chunks deleted")
                    os.remove(os.path.join(path + map + "/data/chunks.dat"))
                if os.path.isfile(os.path.join(path + map + "/data/raids.dat")):
                    write("RESET: Dimension minecraft:overworld had its raids deleted")
                    os.remove(os.path.join(path + map + "/data/raids.dat"))
                if os.path.isdir(os.path.join(path + map + "/entities")):
                    write("RESET: Dimension minecraft:overworld had its entity files deleted")
                    shutil.rmtree(os.path.join(path + map + "/entities"))
                if os.path.isdir(os.path.join(path + map + "/poi")):
                    write("RESET: Dimension minecraft:overworld had its POI files deleted")
                    shutil.rmtree(os.path.join(path + map + "/poi"))
                if os.path.isdir(os.path.join(path + map + "/DIM-1")) and useNether == False:
                    write("RESET: Dimension minecraft:the_nether had its files deleted")
                    shutil.rmtree(os.path.join(path + map + "/DIM-1"))
                if os.path.isfile(os.path.join(path + map + "/DIM-1/data/chunks.dat")) and useNether:
                    write("RESET: Dimension minecraft:the_nether had its forceloaded chunks deleted")
                    os.remove(os.path.join(path + map + "/DIM-1/data/chunks.dat"))
                if os.path.isfile(os.path.join(path + map + "/DIM-1/data/raids.dat")) and useNether:
                    write("RESET: Dimension minecraft:the_nether had its raids deleted")
                    os.remove(os.path.join(path + map + "/DIM-1/data/raids.dat"))
                if os.path.isdir(os.path.join(path + map + "/DIM-1/entities")) and useNether:
                    write("RESET: Dimension minecraft:the_nether had its entity files deleted")
                    shutil.rmtree(os.path.join(path + map + "/DIM-1/entities"))
                if os.path.isdir(os.path.join(path + map + "/DIM-1/poi")) and useNether:
                    write("RESET: Dimension minecraft:the_nether had its POI files deleted")
                    shutil.rmtree(os.path.join(path + map + "/DIM-1/poi"))
                if os.path.isdir(os.path.join(path + map + "/DIM1")) and useEnd == False:
                    write("RESET: Dimension minecraft:the_end had its files deleted")
                    shutil.rmtree(os.path.join(path + map + "/DIM1"))
                if os.path.isfile(os.path.join(path + map + "/DIM1/data/chunks.dat")) and useEnd:
                    write("RESET: Dimension minecraft:the_end had its forceloaded chunks deleted")
                    os.remove(os.path.join(path + map + "/DIM1/data/chunks.dat"))
                if os.path.isfile(os.path.join(path + map + "/DIM1/data/raids.dat")) and useEnd:
                    write("RESET: Dimension minecraft:the_end had its raids deleted")
                    os.remove(os.path.join(path + map + "/DIM1/data/raids.dat"))
                if os.path.isdir(os.path.join(path + map + "/DIM1/entities")) and useEnd:
                    write("RESET: Dimension minecraft:the_end had its entity files deleted")
                    shutil.rmtree(os.path.join(path + map + "/DIM1/entities"))
                if os.path.isdir(os.path.join(path + map + "/DIM1/poi")) and useEnd:
                    write("RESET: Dimension minecraft:the_end had its POI files deleted")
                    shutil.rmtree(os.path.join(path + map + "/DIM1/poi"))
                for d in [name for name in os.listdir(path + map + "/dimensions") if os.path.isdir(os.path.join(path + map + "/dimensions", name))]:
                    resetDimensions(path + map + "/dimensions/" + d, d, "")
                if succeed:
                    write("RESET: Map is properly reset")
                    verifySucceeded = True
                else:
                    write("RESET: Map is not fully reset, some manual work is still needed")
                    if missingDimension: write("RESET: Some dimensions are missing generation data")
                    if missingPackMcmeta: write("RESET: Some official add-ons are missing their pack.mcmeta")
                    if structureDelete: write("RESET: Some structure files couldn't be moved to MEDACORP data pack")
                    if structureDelete:
                        print()
                        write("RESET: Do you want to force-delete the unsaved structure files? (Type \"y\" or \"n\")")
                        verify = "u"
                        while verify == "u":
                            verify = readchar.readkey()
                            if verify != "n" and verify != "y":
                                verify = "u"
                            elif verify == "y":
                                write("RESET: Deleted unsaved structure files")
                                shutil.rmtree(os.path.join(path + map + "/generated"))
                                structureDelete = False
                            elif verify == "n":
                                write("RESET: Left unsaved structure files alone")
                    if missingPackMcmeta == False and structureDelete == False and missingDimension == False:
                        verifySucceeded = True
                unlocked = False
            elif selected == "reset" and verifySucceeded:
                write("Reset already reset the map")
            elif selected == "build" and alreadyBuild == False:
                write("Running download building")
                continueBuilding = "y"
                if verifySucceeded == False:
                    write("BUILD: Please verify or reset the map first; if it's not properly reset, building is not allowed")
                    continueBuilding = "n"
                if continueBuilding == "y":
                    write("BUILD: What is the version number?")
                    versionNumber = input()
                    zipName = map + " (v" + versionNumber + ").zip"
                    print("")
                    if os.path.exists(ZipsPath + zipName):
                        write("BUILD: The file \"" + zipName + "\" already exists, building aborted")
                    else:
                        write("BUILD: Creating \"" + zipName + "\", please be patient")
                        ignoreFile = open(path + ".gitignore", "r")
                        allowFiles = []
                        disallowFiles = []
                        lineCount = sum(1 for _ in ignoreFile)
                        ignoreFile.close()
                        ignoreFile = open(path + ".gitignore", "r")
                        i = 0
                        while i <= lineCount:
                            line = ignoreFile.readline()
                            line = line.replace("*","[A-Za-z0-9_ -]+")
                            line = line.replace("[A-Za-z0-9_ -]+\n",".*")
                            line = line.replace("\r","")
                            line = line.replace("\n","")
                            if line.endswith("/") == False and line.endswith("+") == False and line.endswith("*") == False : line = line + "$"
                            if line.startswith("!"):
                                line = line.replace("!","")
                                if line.startswith("/"): line = "^" + line
                                allowFiles.append(line)
                            elif len(line) >= 1:
                                if line.startswith("/") == False and line.startswith(".*") == False:
                                    line = ".*/" + line
                                if line.startswith("/"): line = "^" + line
                                disallowFiles.append(line)
                            i += 1
                        ignoreFile.close()
                        disallowFiles.append("/.git/.*")
                        filesIncluded = 0
                        filesExcluded = 0
                        with ZipFile(ZipsPath + zipName, "w", compression=ZIP_DEFLATED) as zip_object:
                            for folder_name, sub_folders, file_names in os.walk(WorldsPath + rootFolder):
                                for filename in file_names:
                                    file_path = os.path.join(folder_name, filename)
                                    file_path = file_path.replace("\\","/")
                                    file_path = file_path.replace(WorldsPath + rootFolder, "")
                                    allowed = True
                                    for disallow in disallowFiles:
                                        if re.search(disallow, file_path):
                                            allowed = False
                                            for allow in allowFiles:
                                                if re.search(allow, file_path):
                                                    allowed = True
                                            if allowed == False: 
                                                filesExcluded += 1
                                            else: 
                                                filesIncluded += 1
                                    if allowed:
                                        zip_object.write(WorldsPath + rootFolder + "/" + file_path, file_path)
                                        write("BUILD: \"" + file_path + "\" added to zip", True)
                        if os.path.exists(ZipsPath + zipName):
                            write("BUILD: Created \"" + zipName + "\" with " + str(filesIncluded) + " files; " + str(filesExcluded) + " files were excluded")
                            alreadyBuild = True
                        else:
                            write("BUILD: The file \"" + zipName + "\" could not be created")
                elif continueBuilding == "n":
                    write("BUILD: Building aborted")
            elif selected == "build" and alreadyBuild:
                write("Build was already created")
    print("")
