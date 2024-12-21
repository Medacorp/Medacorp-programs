import os
import shutil
import sys
import readchar
import time
import re
from nbt import nbt
from zipfile import ZIP_DEFLATED, ZipFile
import zlib
from contextlib import redirect_stdout
from datetime import datetime

def write(string: str, debug: bool):
    timeNow = datetime.now()
    timeFormat = timeNow.strftime("%H:%M:%S")
    if debug == False: print(string)
    with open(executionPath + "logs/" + dateFormat + ".log", "a") as logFile:
        with redirect_stdout(logFile):
            if debug == False: print("[" + timeFormat + "] " + string)
            else: print("[" + timeFormat + "] [DEBUGGER] " + string)
    with open(executionPath + "logs/latest.log", "a") as logFile:
        with redirect_stdout(logFile):
            if debug == False: print("[" + timeFormat + "] " + string)
            else: print("[" + timeFormat + "] [DEBUGGER] " + string)

WorldsPath= ""
ZipsPath= ""
executionPath, executionFileName = os.path.split(os.path.abspath(sys.argv[0]))
executionPath = executionPath.replace("\\", "/") + "/"
dateNow = datetime.now()
dateFormat = dateNow.strftime("%Y-%m-%d")
if os.path.exists(executionPath + "logs/" + dateFormat + ".log"): 
    with open(executionPath + "logs/" + dateFormat + ".log", "a") as logFile:
        with redirect_stdout(logFile):
            print("\n\n")
if os.path.exists(executionPath + "logs/latest.log"): os.remove(executionPath + "logs/latest.log")
write("Running \"verify and assemble\" program", True)
if os.path.exists(executionPath + "paths.txt"):
    paths = open(executionPath + "paths.txt", "r")
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
    write("Please provide the path where the builds should be created (type \"here\" if it's the same folder as where this program is)", False)
    while ZipsPath == "":
        selected=input().replace("\\", "/")
        if selected.lower() == "here":
            ZipsPath = executionPath
        else:
            if selected.endswith("/"): selected = selected[slice(0,-1)]
            if os.path.isdir(os.path.join(selected)):
                ZipsPath = selected
            else:
                write("That's not an existing folder", False)
if ZipsPath.endswith("/") == False: ZipsPath = ZipsPath + "/"
if WorldsPath == "":
    write("Please provide the path where the worlds are all located in their github repository state (type \"here\" if it's the same folder as where this program is)", False)
    while WorldsPath == "":
        selected=input().replace("\\", "/")
        if selected.lower() == "here":
            WorldsPath = executionPath
        else:
            if selected.endswith("/"): selected = selected[slice(0,-1)]
            if os.path.isdir(os.path.join(selected)):
                WorldsPath = selected
            else:
                write("That's not an existing folder", False)
if WorldsPath.endswith("/") == False: WorldsPath = WorldsPath + "/"
paths = open(executionPath + "paths.txt", "w")
paths.write("builds=" + ZipsPath + "\nworlds=" + WorldsPath)
paths.close()
write("File path where builds get created: " + ZipsPath[slice(0,-1)], False)
write("File path where worlds get searched for: " + WorldsPath[slice(0,-1)], False)
write("You can also find the logs and a file to modify the above paths here: " + executionPath[slice(0,-1)], False)
print("")

while True:
    write("What world do you wish to build? (Type \"!\" to close program)", False)
    validMaps = ""
    for subfolder in [name for name in os.listdir(WorldsPath) if os.path.isdir(os.path.join(WorldsPath, name)) if os.path.isdir(os.path.join(WorldsPath, name, name)) if os.path.isfile(os.path.join(WorldsPath, name, ".gitignore"))]:
        if validMaps == "": validMaps = subfolder
        else: validMaps = validMaps + ", " + subfolder
    write("Valid worlds: " + validMaps, False)
    selected=input().lower().replace("'", "").replace(":", "")
    verify = "u"
    map = ""
    path = ""
    dimensionIDs = []
    useNether = False
    useEnd = False
    verifySucceeded = False
    alreadyBuild = False
    mapSet = 0
    if selected == "!":
        break
    else:
        for subfolder in [name for name in os.listdir(WorldsPath) if os.path.isdir(os.path.join(WorldsPath, name)) if os.path.isdir(os.path.join(WorldsPath, name, name)) if os.path.isfile(os.path.join(WorldsPath, name, ".gitignore"))]:
            folder = subfolder.lower().replace("'", "").replace(":", "")
            words = folder.split(" ")
            truncated = ""
            for word in words: truncated = truncated + word[0]
            if selected == folder or selected == truncated:
                map = subfolder
                mapSet = 1
            if mapSet != 1:
                if re.search(".*" + selected + ".*", folder):
                    map = subfolder
                    if mapSet == 2 or mapSet == 3: mapSet = 3
                    else: mapSet = 2
                elif re.search(".*" + selected + ".*", truncated):
                    map = subfolder
                    if mapSet == 2 or mapSet == 3: mapSet = 3
                    else: mapSet = 2
        if mapSet == 0: write("Matched no world", False)
        elif mapSet == 3:
            map = ""
            write("Matched several worlds, please be more specific", False)

    if (map != ""):
        path = WorldsPath + map + "/"
        write("World set to " + map + ".", False)
        dimensionIDs = []
        
        if os.path.isdir(os.path.join(path + map + "/datapacks/MEDACORP/data")):
            for subfolder in [name for name in os.listdir(os.path.join(path + map + "/datapacks/MEDACORP/data")) if os.path.isdir(os.path.join(path + map + "/datapacks/MEDACORP/data/" + name)) if os.path.isdir(os.path.join(path + map + "/datapacks/MEDACORP/data/" + name + "/dimension"))]:
                for folder_name2, sub_folders2, file_names2 in os.walk(path + map + "/datapacks/MEDACORP/data/" + subfolder + "/dimension"):
                    for filename in file_names2:
                        if filename.endswith(".json"):
                            file_path = os.path.join(folder_name2)
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
            write("What would you like to do? Type one of the following:", False)
            write("* \"stop\"", False)
            write("* \"rules\"", False)
            if verifySucceeded == False: 
                write("* \"verify\"", False)
                write("* \"reset\"", False)
            if alreadyBuild == False: write("* \"build\"", False)
            selected = input().lower()
            if selected == "stop":
                write("Aborting", False)
                break
            elif selected == "rules":
                write("Rules:", False)
                write("* Game rule sendCommandFeedback set to false", False)
                write("* No player data remaining", False)
                write("* No save progress remaining", False)
                write("* No additional data packs enabled", False)
                write("* No missing pack.mcmeta-s", False)
                if useNether == False: 
                    write("* No Nether data remaining", False)
                if useEnd == False: 
                    write("* No End data remaining", False)
                if len(dimensionIDs) == 1: write("* 1 custom dimension: " + str(dimensionIDs[0]), False)
                else: 
                    tempString = ""
                    first = True
                    for dimension in dimensionIDs:
                        if first: 
                            tempString = dimension
                            first = False
                        else:
                            tempString = tempString + ", " + dimension
                    write("* " + str(len(dimensionIDs)) + " custom dimensions: " + tempString, False)
            elif selected == "verify" and verifySucceeded == False:
                succeed = True
                write("Running verification", False)

                #Check level.dat
                nbtfile = nbt.NBTFile(path + map + "/level.dat", "rb")
                nbtfile.name = 'level'
                for tag in nbtfile["Data"]["GameRules"].tags:
                    if tag.name == "sendCommandFeedback" and tag.value == "true":
                        write("VERIFY: Game rule sendCommandFeedback is set to true", False)
                        succeed = False
                for tag in nbtfile["Data"].tags:
                    if tag.name == "Player":
                        write("VERIFY: Player data is present in level.dat", False)
                        succeed = False
                tagCount = 0
                for tag in nbtfile["Data"]["DataPacks"]["Enabled"].tags:
                    tagCount += 1
                if tagCount != 2:
                    write("VERIFY: Extra data packs are enabled", False)
                    succeed = False
                tagCount = 0
                for tag in nbtfile["Data"]["CustomBossEvents"].tags:
                    tagCount += 1
                if tagCount != 0:
                    write("VERIFY: Custom boss bars are stored", False)
                    succeed = False
                tagCount = 0
                for tag in nbtfile["Data"]["WorldGenSettings"]["dimensions"].tags:
                    if tag.name != "minecraft:overworld" and tag.name != "minecraft:the_nether" and tag.name != "minecraft:the_end": tagCount += 1
                if tagCount != len(dimensionIDs):
                    write("VERIFY: Dimension count is incorrect", False)
                    succeed = False
                
                #Check official add-ons
                if os.path.isdir(os.path.join(path + "Official add-ons")):
                    officialAddons = [name for name in os.listdir(path + "Official add-ons") if os.path.isdir(os.path.join(path + "Official add-ons", name))]
                    for addon in officialAddons:
                        addonName = addon.replace(" add-on","")
                        if os.path.isfile(path + "Official add-ons/" + addon + "/" + addonName + "/pack.mcmeta") == False:
                            write("VERIFY: " + addon + " is missing its pack.mcmeta", False)
                            succeed = False
                
                #Check save data
                if os.path.isfile(os.path.join(path + map + "/session.lock")):
                    write("VERIFY: Level session.lock is present", False)
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/level.dat_old")):
                    write("VERIFY: Backup level.dat is present", False)
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/generated")):
                    write("VERIFY: Unsaved structure files are present", False)
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/playerdata")):
                    write("VERIFY: Player data is still in world data", False)
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/stats")):
                    write("VERIFY: Statistics data is still in world data", False)
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/advancements")):
                    write("VERIFY: Advancement data is still in world data", False)
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/data/random_sequences.dat")):
                    write("VERIFY: Random sequence data is still in world data", False)
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/data/scoreboard.dat")):
                    write("VERIFY: Scoreboard data is still in world data", False)
                    succeed = False
                for file in [name for name in os.listdir(path + map + "/data") if os.path.isfile(os.path.join(path + map + "/data", name))]:
                    if file.startswith("command_storage_"):
                        fileName = file.removeprefix("command_storage_").removesuffix(".dat")
                        write("VERIFY: Command storage \"" + fileName + "\" is still in world data", False)
                        succeed = False
                if os.path.isfile(os.path.join(path + map + "/data/chunks.dat")):
                    write("VERIFY: Dimension minecraft:overworld has forceloaded chunks", False)
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/data/raids.dat")):
                    write("VERIFY: Dimension minecraft:overworld has raids", False)
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/entities")):
                    write("VERIFY: Dimension minecraft:overworld has entity files", False)
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/poi")):
                    write("VERIFY: Dimension minecraft:overworld has POI files", False)
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM-1")) and useNether == False:
                    write("VERIFY: Dimension minecraft:the_nether has files", False)
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/DIM-1/data/chunks.dat")) and useNether:
                    write("VERIFY: Dimension minecraft:the_nether has forceloaded chunks", False)
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/DIM-1/data/raids.dat")) and useNether:
                    write("VERIFY: Dimension minecraft:the_nether has raids", False)
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM-1/entities")) and useNether:
                    write("VERIFY: Dimension minecraft:the_nether has entity files", False)
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM-1/poi")) and useNether:
                    write("VERIFY: Dimension minecraft:the_nether has POI files", False)
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM1")) and useEnd == False:
                    write("VERIFY: Dimension minecraft:the_end has files", False)
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/DIM1/data/chunks.dat")) and useEnd:
                    write("VERIFY: Dimension minecraft:the_end has forceloaded chunks", False)
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/DIM1/data/raids.dat")) and useEnd:
                    write("VERIFY: Dimension minecraft:the_end has raids", False)
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM1/entities")) and useEnd:
                    write("VERIFY: Dimension minecraft:the_end has entity files", False)
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM1/poi")) and useEnd:
                    write("VERIFY: Dimension minecraft:the_end has POI files", False)
                    succeed = False
                for d in [name for name in os.listdir(path + map + "/dimensions") if os.path.isdir(os.path.join(path + map + "/dimensions", name))]:
                    #This needs to be recursive until it runs out of folders; all folders with a region subfolder are dimensions (Fix #1)
                    for dID in [name for name in os.listdir(path + map + "/dimensions/" + d) if os.path.isdir(os.path.join(path + map + "/dimensions/" + d, name))]:
                        if os.path.isfile(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/data/chunks.dat")):
                            write("VERIFY: Dimension " + d + ":" + dID + " has forceloaded chunks", False)
                            succeed = False
                        if os.path.isfile(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/data/raids.dat")):
                            write("VERIFY: Dimension " + d + ":" + dID + " has raids", False)
                            succeed = False
                        if os.path.isdir(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/entities")):
                            write("VERIFY: Dimension " + d + ":" + dID + " has entity files", False)
                            succeed = False
                        if os.path.isdir(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/poi")):
                            write("VERIFY: Dimension " + d + ":" + dID + " has POI files", False)
                            succeed = False
                if succeed:
                    write("VERIFY: Map is properly reset", False)
                    verifySucceeded = True
                else:
                    write("VERIFY: Map is not reset", False)
            elif selected == "verify" and verifySucceeded:
                write("Verify already confirmed the map is reset", False)
            elif selected == "reset" and verifySucceeded == False:
                succeed = True
                structureDelete = False
                missingPackMcmeta = False
                write("Running reset", False)

                #Check level.dat
                nbtfile = nbt.NBTFile(path + map + "/level.dat", "rb")
                nbtfile.name = 'level'
                modify = False
                for tag in nbtfile["Data"]["GameRules"].tags:
                    if tag.name == "sendCommandFeedback" and tag.value == "true":
                        tag.value = "false"
                        write("RESET: Game rule sendCommandFeedback set to false", False)
                        modify = True
                for tag in nbtfile["Data"].tags:
                    if tag.name == "Player": 
                        write("RESET: Deleted player data in level.dat", False)
                        del nbtfile["Data"]["Player"]
                        modify = True
                tagCount = 0
                for tag in nbtfile["Data"]["DataPacks"]["Enabled"].tags:
                    tagCount += 1
                if tagCount != 2:
                    write("RESET: Deleted extra enabled data packs", False)
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
                    write("RESET: Deleted custom boss bars", False)
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
                        write("RESET: Dimension \"" + tag.name + "\" has been deleted from generation data", False)
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
                            write("RESET ERROR: Dimesnion " + dimension + " is missing from generation data", False)
                            succeed = false
                            missingDimension = True
                if modify: nbtfile.write_file(path + map + "/level.dat")
                
                #Check official add-ons
                if os.path.isdir(os.path.join(path + "Official add-ons")):
                    officialAddons = [name for name in os.listdir(path + "Official add-ons") if os.path.isdir(os.path.join(path + "Official add-ons", name))]
                    for addon in officialAddons:
                        addonName = addon.replace(" add-on","")
                        foundMcmeta = True
                        if os.path.isfile(path + "Official add-ons/" + addon + "/" + addonName + "/pack.mcmeta") == False:
                            foundMcmeta = False
                            for file in [name for name in os.listdir(path + "Official add-ons/" + addon + "/" + addonName) if os.path.isfile(os.path.join(path + "Official add-ons/" + addon + "/" + addonName, name))]:
                                if file.endswith(".mcmeta"):
                                    write("RESET: " + addon + "'s \"" + file + "\" renamed to \"pack.mcmeta\"", False)
                                    os.rename(os.path.join(path + "Official add-ons/" + addon + "/" + addonName + "/" + file), os.path.join(path + "Official add-ons/" + addon + "/" + addonName + "/pack.mcmeta"))
                                    foundMcmeta = True
                        if foundMcmeta == False: 
                            write("RESET ERROR: " + addon + " is missing its pack.mcmeta; this needs manual fixing", False)
                            succeed = False
                            missingPackMcmeta = True
                
                #Check save data
                if os.path.isfile(os.path.join(path + map + "/session.lock")):
                    write("RESET: Level session.lock deleted", False)
                    os.remove(os.path.join(path + map + "/session.lock"))
                if os.path.isfile(os.path.join(path + map + "/level.dat_old")):
                    write("RESET: Backup level.dat deleted", False)
                    os.remove(os.path.join(path + map + "/level.dat_old"))
                if os.path.isdir(os.path.join(path + map + "/generated")):
                    for folder_name, sub_folders, file_names in os.walk(path + map + "/generated"):
                        for subfolder in sub_folders:
                            file_path = os.path.join(folder_name, subfolder)
                            file_path = file_path.replace("\\","/")
                            file_path = file_path.replace(path + map + "/generated/", "")
                            file_path = file_path.replace("/structures/","/structure/")
                            if os.path.isdir(os.path.join(path + map + "/datapacks/MEDACORP/data/" + file_path)) == False and subfolder != "structures":
                                write(file_path, True)
                                os.makedirs(os.path.join(path + map + "/datapacks/MEDACORP/data/" + file_path))
                        for filename in file_names:
                            file_path = os.path.join(folder_name, filename)
                            file_path = file_path.replace("\\","/")
                            file_path = file_path.replace(path + map + "/generated/", "")
                            if os.path.isfile(os.path.join(path + map + "/datapacks/MEDACORP/data/" + file_path.replace("/structures/","/structure/"))):
                                write("RESET ERROR: Generated file \"" + file_path + "\" cannot be moved to \"MEDACORP\" data pack because the file already exists", False)
                                structureDelete = True
                                succeed = False
                            else:
                                write("RESET: Generated file \"" + file_path + "\" moved to \"MEDACORP\" data pack", False)
                                os.rename(os.path.join(path + map + "/generated/" + file_path), os.path.join(path + map + "/datapacks/MEDACORP/data/" + file_path.replace("/structures/","/structure/")))
                    if structureDelete == False:
                        shutil.rmtree(os.path.join(path + map + "/generated"))
                if os.path.isdir(os.path.join(path + map + "/playerdata")):
                    write("RESET: Player data has been deleted", False)
                    shutil.rmtree(os.path.join(path + map + "/playerdata"))
                if os.path.isdir(os.path.join(path + map + "/stats")):
                    write("RESET: Statistics data has been deleted", False)
                    shutil.rmtree(os.path.join(path + map + "/stats"))
                if os.path.isdir(os.path.join(path + map + "/advancements")):
                    write("RESET: Advancement data has been deleted", False)
                    shutil.rmtree(os.path.join(path + map + "/advancements"))
                if os.path.isfile(os.path.join(path + map + "/data/random_sequences.dat")):
                    write("RESET: Random sequence data has been deleted", False)
                    os.remove(os.path.join(path + map + "/data/random_sequences.dat"))
                if os.path.isfile(os.path.join(path + map + "/data/scoreboard.dat")):
                    write("RESET: Scoreboard data has been deleted", False)
                    os.remove(os.path.join(path + map + "/data/scoreboard.dat"))
                for file in [name for name in os.listdir(path + map + "/data") if os.path.isfile(os.path.join(path + map + "/data", name))]:
                    if file.startswith("command_storage_"):
                        fileName = file.removeprefix("command_storage_").removesuffix(".dat")
                        write("RESET: Command storage \"" + fileName + "\" has been deleted", False)
                        os.remove(os.path.join(path + map + "/data/" + file))
                if os.path.isfile(os.path.join(path + map + "/data/chunks.dat")):
                    write("RESET: Dimension minecraft:overworld had its forceloaded chunks deleted", False)
                    os.remove(os.path.join(path + map + "/data/chunks.dat"))
                if os.path.isfile(os.path.join(path + map + "/data/raids.dat")):
                    write("RESET: Dimension minecraft:overworld had its raids deleted", False)
                    os.remove(os.path.join(path + map + "/data/raids.dat"))
                if os.path.isdir(os.path.join(path + map + "/entities")):
                    write("RESET: Dimension minecraft:overworld had its entity files deleted", False)
                    shutil.rmtree(os.path.join(path + map + "/entities"))
                if os.path.isdir(os.path.join(path + map + "/poi")):
                    write("RESET: Dimension minecraft:overworld had its POI files deleted", False)
                    shutil.rmtree(os.path.join(path + map + "/poi"))
                if os.path.isdir(os.path.join(path + map + "/DIM-1")) and useNether == False:
                    write("RESET: Dimension minecraft:the_nether ad its files deleted", False)
                    shutil.rmtree(os.path.join(path + map + "/DIM-1"))
                if os.path.isfile(os.path.join(path + map + "/DIM-1/data/chunks.dat")) and useNether:
                    write("RESET: Dimension minecraft:the_nether had its forceloaded chunks deleted", False)
                    os.remove(os.path.join(path + map + "/DIM-1/data/chunks.dat"))
                if os.path.isfile(os.path.join(path + map + "/DIM-1/data/raids.dat")) and useNether:
                    write("RESET: Dimension minecraft:the_nether had its raids deleted", False)
                    os.remove(os.path.join(path + map + "/DIM-1/data/raids.dat"))
                if os.path.isdir(os.path.join(path + map + "/DIM-1/entities")) and useNether:
                    write("RESET: Dimension minecraft:the_nether had its entity files deleted", False)
                    shutil.rmtree(os.path.join(path + map + "/DIM-1/entities"))
                if os.path.isdir(os.path.join(path + map + "/DIM-1/poi")) and useNether:
                    write("RESET: Dimension minecraft:the_nether had its POI files deleted", False)
                    shutil.rmtree(os.path.join(path + map + "/DIM-1/poi"))
                if os.path.isdir(os.path.join(path + map + "/DIM1")) and useEnd == False:
                    write("RESET: Dimension minecraft:the_end had its files deleted", False)
                    shutil.rmtree(os.path.join(path + map + "/DIM1"))
                if os.path.isfile(os.path.join(path + map + "/DIM1/data/chunks.dat")) and useEnd:
                    write("RESET: Dimension minecraft:the_end had its forceloaded chunks deleted", False)
                    os.remove(os.path.join(path + map + "/DIM1/data/chunks.dat"))
                if os.path.isfile(os.path.join(path + map + "/DIM1/data/raids.dat")) and useEnd:
                    write("RESET: Dimension minecraft:the_end had its raids deleted", False)
                    os.remove(os.path.join(path + map + "/DIM1/data/raids.dat"))
                if os.path.isdir(os.path.join(path + map + "/DIM1/entities")) and useEnd:
                    write("RESET: Dimension minecraft:the_end had its entity files deleted", False)
                    shutil.rmtree(os.path.join(path + map + "/DIM1/entities"))
                if os.path.isdir(os.path.join(path + map + "/DIM1/poi")) and useEnd:
                    write("RESET: Dimension minecraft:the_end had its POI files deleted", False)
                    shutil.rmtree(os.path.join(path + map + "/DIM1/poi"))
                for d in [name for name in os.listdir(path + map + "/dimensions") if os.path.isdir(os.path.join(path + map + "/dimensions", name))]:
                    #This needs to be recursive until it runs out of folders; all folders with a region subfolder are dimensions (Fix #1)
                    for dID in [name for name in os.listdir(path + map + "/dimensions/" + d) if os.path.isdir(os.path.join(path + map + "/dimensions/" + d, name))]:
                        if os.path.isfile(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/data/chunks.dat")):
                            write("RESET: Dimension " + d + ":" + dID + " had its forceloaded chunks deleted", False)
                            os.remove(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/data/chunks.dat"))
                        if os.path.isfile(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/data/raids.dat")):
                            write("RESET: Dimension " + d + ":" + dID + " had its raids deleted", False)
                            os.remove(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/data/raids.dat"))
                        if os.path.isdir(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/entities")):
                            write("RESET: Dimension " + d + ":" + dID + " had its entity files deleted", False)
                            shutil.rmtree(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/entities"))
                        if os.path.isdir(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/poi")):
                            write("RESET: Dimension " + d + ":" + dID + " had its POI files deleted", False)
                            shutil.rmtree(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/poi"))
                if succeed:
                    write("RESET: Map is properly reset", False)
                    verifySucceeded = True
                else:
                    write("RESET: Map is not fully reset, some manual work is still needed", False)
                    if missingDimension: write("RESET: Some dimensions are missing generation data", False)
                    if missingPackMcmeta: write("RESET: Some official add-ons are missing their pack.mcmeta", False)
                    if structureDelete: write("RESET: Some structure files couldn't be moved to MEDACORP data pack", False)
                    if structureDelete:
                        print()
                        write("RESET: Do you want to force-delete the unsaved structure files? (Type \"y\" or \"n\")", False)
                        verify = "u"
                        while verify == "u":
                            verify = readchar.readkey()
                            if verify != "n" and verify != "y":
                                verify = "u"
                            elif verify == "y":
                                write("RESET: Deleted unsaved structure files", False)
                                shutil.rmtree(os.path.join(path + map + "/generated"))
                                structureDelete = False
                            elif verify == "n":
                                write("RESET: Left unsaved structure files alone", False)
                    if missingPackMcmeta == False and structureDelete == False and missingDimension == False:
                        verifySucceeded = True
            elif selected == "reset" and verifySucceeded:
                write("Reset already reset the map", False)
            elif selected == "build" and alreadyBuild == False:
                write("Running download building", False)
                continueBuilding = "y"
                if verifySucceeded == False:
                    write("BUILD: Please verify or reset the map first; if it's not properly reset, building is not allowed", False)
                    continueBuilding = "n"
                if continueBuilding == "y":
                    write("BUILD: What is the version number?", False)
                    versionNumber = input()
                    zipName = map + " (v" + versionNumber + ").zip"
                    print("")
                    if os.path.exists(ZipsPath + zipName):
                        write("BUILD: The file \"" + zipName + "\" already exists, building aborted", False)
                    else:
                        write("BUILD: Creating \"" + zipName + "\", please be patient", False)
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
                            for folder_name, sub_folders, file_names in os.walk(WorldsPath + map):
                                for filename in file_names:
                                    file_path = os.path.join(folder_name, filename)
                                    file_path = file_path.replace("\\","/")
                                    file_path = file_path.replace(WorldsPath + map, "")
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
                                        zip_object.write(WorldsPath + map + "/" + file_path, file_path)
                                        write("BUILD: \"" + file_path + "\" added to zip", True)
                        if os.path.exists(ZipsPath + zipName):
                            write("BUILD: Created \"" + zipName + "\" with " + str(filesIncluded) + " files; " + str(filesExcluded) + " files were excluded", False)
                            alreadyBuild = True
                        else:
                            write("BUILD: The file \"" + zipName + "\" could not be created", False)
                elif continueBuilding == "n":
                    write("BUILD: Building aborted", False)
            elif selected == "build" and alreadyBuild:
                write("Build was already created", False)
    print("")