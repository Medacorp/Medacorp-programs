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

executionPath, executionFileName = os.path.split(os.path.abspath(sys.argv[0]))
executionPath = executionPath.replace("\\", "/") + "/"
WorldsPath = executionPath.replace("Download builds", "server")
if os.path.exists(executionPath + "paths.txt"):
    paths = open(executionPath + "paths.txt", "r")
    i = 0
    while i <= 1:
        line = paths.readline()
        if line.startswith("execution="):
            executionPath = line.replace("execution=","").replace("\r","").replace("\n","")
        if line.startswith("worlds="):
            WorldsPath = line.replace("worlds=","").replace("\r","").replace("\n","")
        i += 1
    paths.close()
executionPath = executionPath.replace("\\", "/")
WorldsPath = WorldsPath.replace("\\", "/")
if executionPath.endswith("/") == False: executionPath = executionPath + "/"
if WorldsPath.endswith("/") == False: WorldsPath = WorldsPath + "/"
paths = open(executionPath + "paths.txt", "w")
paths.write("execution=" + executionPath + "\nworlds=" + WorldsPath)
paths.close()
if os.path.exists(executionPath + "logs/latest.log"): os.remove(executionPath + "logs/latest.log")
dateNow = datetime.now()
dateFormat = dateNow.strftime("%Y-%m-%d")
def write(string: str, debug: bool):
    timeNow = datetime.now()
    timeFormat = timeNow.strftime("%H:%M:%S")
    if debug == False: print(string)
    with open(executionPath + "logs/" + dateFormat + ".log", "a") as logFile:
        with redirect_stdout(logFile):
            if debug == False: print("[" + timeFormat + "]" + string)
            else: print("[" + timeFormat + "] [DEBUGGER]" + string)
    with open(executionPath + "logs/latest.log", "a") as logFile:
        with redirect_stdout(logFile):
            if debug == False: print("[" + timeFormat + "] " + string)
            else: print("[" + timeFormat + "] [DEBUGGER] " + string)

write("Path zips get created in: " + executionPath, False)
write("Path worlds get searched for: " + WorldsPath, False)
write("To change, modify \"paths.txt\" in the same folder as this program is located", False)
print("")

while True:
    write("What map do you wish to build? (Type \"stop\" to close program)", False)
    selected=input().lower().replace("'", "").replace(":", "")
    verify = "u"
    map = ""
    path = ""
    dimensionIDs = []
    useNether = False
    useEnd = False
    verifySucceeded = False
    alreadyReset = False
    alreadyBuild = False
    if selected == "stop":
        break
    elif selected == "luigis mansion" or selected == "lm":
        map = "Luigi's Mansion"
        dimensionIDs = ["luigis_mansion:normal","luigis_mansion:hidden"]
    elif selected == "luigis mansion dark moon" or selected == "dark moon" or selected == "lmdm" or selected == "dm":
        map = "Luigi's Mansion Dark Moon"
        dimensionIDs = ["dark_moon:paranormal"]
    elif selected == "zelda twilight princess" or selected == "twilight princess" or selected == "ztp" or selected == "tp":
        map = "Zelda Twilight Princess"
        dimensionIDs = ["zelda_twilight_princess:twilight","zelda_twilight_princess:dungeon"]

    if (map != ""):
        path = WorldsPath + map + "/"
        write("Map set to " + map + ".", False)
        while True:
            print("")
            write("What would you like to do? Type one of the following:", False)
            write("* \"stop\"", False)
            write("* \"rules\"", False)
            if verifySucceeded == False: write("* \"verify\"", False)
            if alreadyReset == False: write("* \"reset\"", False)
            if alreadyBuild == False: write("* \"build\"", False)
            selected = input().lower()
            if selected == "stop":
                write("Aborting", False)
                break
            elif selected == "rules":
                write("Rules:", False)
                write("* No player data remaining", False)
                write("* No save progress remaining", False)
                write("* No additional data packs enabled", False)
                write("* No missing pack.mcmeta-s", False)
                if useNether == False: 
                    write("* No Nether data remaining", False)
                if useEnd == False: 
                    write("* No End data remaining", False)
                if len(dimensionIDs) == 1: write("* 1 custom dimension", False)
                else: write("* " + str(len(dimensionIDs)) + " custom dimensions", False)
            elif selected == "verify" and verifySucceeded == False:
                succeed = True
                write("Running verification", False)

                #Check level.dat
                nbtfile = nbt.NBTFile(path + map + "/level.dat", "rb")
                nbtfile.name = 'level'
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
                for tag in nbtfile["Data"]["WorldGenSettings"]["dimensions"].tags:
                    if tag.name != "minecraft:overworld" and tag.name != "minecraft:the_nether" and tag.name != "minecraft:the_end": tagCount += 1
                if tagCount != len(dimensionIDs):
                    write("VERIFY: Dimension count is incorrect", False)
                    succeed = False
                
                #Check official add-ons
                officialAddons = [name for name in os.listdir(path + "Official add-ons") if os.path.isdir(os.path.join(path + "Official add-ons", name))]
                for addon in officialAddons:
                    addonName = addon.replace(" add-on","")
                    if os.path.isfile(path + "Official add-ons/" + addon + "/" + addonName + "/pack.mcmeta") == False:
                        write("VERIFY: " + addon + " is missing its pack.mcmeta", False)
                        succeed = False
                
                #Check save data
                if os.path.isfile(os.path.join(path + map + "/session.lock")) == True:
                    write("VERIFY: Level session.lock is present", False)
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/level.dat_old")) == True:
                    write("VERIFY: Backup level.dat is present", False)
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/generated")) == True:
                    write("VERIFY: Unsaved structure files are present", False)
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/playerdata")) == True:
                    write("VERIFY: Player data is still in world data", False)
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/stats")) == True:
                    write("VERIFY: Statistics data is still in world data", False)
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/advancements")) == True:
                    write("VERIFY: Advancement data is still in world data", False)
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/data/scoreboard.dat")) == True:
                    write("VERIFY: Scoreboard data is still in world data", False)
                    succeed = False
                for file in [name for name in os.listdir(path + map + "/data") if os.path.isfile(os.path.join(path + map + "/data", name))]:
                    if file.startswith("command_storage_"):
                        fileName = file.removeprefix("command_storage_").removesuffix(".dat")
                        write("VERIFY: Command storage \"" + fileName + "\" is still in world data", False)
                        succeed = False
                if os.path.isfile(os.path.join(path + map + "/data/chunks.dat")) == True:
                    write("VERIFY: Dimension minecraft:overworld has forceloaded chunks", False)
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/data/raids.dat")) == True:
                    write("VERIFY: Dimension minecraft:overworld has raids", False)
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/entities")) == True:
                    write("VERIFY: Dimension minecraft:overworld has entity files", False)
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/poi")) == True:
                    write("VERIFY: Dimension minecraft:overworld has POI files", False)
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM-1")) == True and useNether == False:
                    write("VERIFY: Dimension minecraft:the_nether has files", False)
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/DIM-1/data/chunks.dat")) == True and useNether == True:
                    write("VERIFY: Dimension minecraft:the_nether has forceloaded chunks", False)
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/DIM-1/data/raids.dat")) == True and useNether == True:
                    write("VERIFY: Dimension minecraft:the_nether has raids", False)
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM-1/entities")) == True and useNether == True:
                    write("VERIFY: Dimension minecraft:the_nether has entity files", False)
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM-1/poi")) == True and useNether == True:
                    write("VERIFY: Dimension minecraft:the_nether has POI files", False)
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM1")) == True and useEnd == False:
                    write("VERIFY: Dimension minecraft:the_end has files", False)
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/DIM1/data/chunks.dat")) == True and useEnd == True:
                    write("VERIFY: Dimension minecraft:the_end has forceloaded chunks", False)
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/DIM1/data/raids.dat")) == True and useEnd == True:
                    write("VERIFY: Dimension minecraft:the_end has raids", False)
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM1/entities")) == True and useEnd == True:
                    write("VERIFY: Dimension minecraft:the_end has entity files", False)
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM1/poi")) == True and useEnd == True:
                    write("VERIFY: Dimension minecraft:the_end has POI files", False)
                    succeed = False
                for d in [name for name in os.listdir(path + map + "/dimensions") if os.path.isdir(os.path.join(path + map + "/dimensions", name))]:
                    for dID in [name for name in os.listdir(path + map + "/dimensions/" + d) if os.path.isdir(os.path.join(path + map + "/dimensions/" + d, name))]:
                        if os.path.isfile(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/data/chunks.dat")) == True:
                            write("VERIFY: Dimension " + d + ":" + dID + " has forceloaded chunks", False)
                            succeed = False
                        if os.path.isfile(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/data/raids.dat")) == True:
                            write("VERIFY: Dimension " + d + ":" + dID + " has raids", False)
                            succeed = False
                        if os.path.isdir(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/entities")) == True:
                            write("VERIFY: Dimension " + d + ":" + dID + " has entity files", False)
                            succeed = False
                        if os.path.isdir(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/poi")) == True:
                            write("VERIFY: Dimension " + d + ":" + dID + " has POI files", False)
                            succeed = False
                if succeed:
                    write("VERIFY: map is properly reset", False)
                    verifySucceeded = True
                else:
                    write("VERIFY: map is not reset", False)
            elif selected == "verify" and verifySucceeded == True:
                write("Verify already confirmed the map is reset", False)
            elif selected == "reset" and alreadyReset == False:
                succeed = True
                structureDelete = False
                missingPackMcmeta = False
                write("Running reset", False)

                #Check level.dat
                nbtfile = nbt.NBTFile(path + map + "/level.dat", "rb")
                nbtfile.name = 'level'
                modify = False
                for tag in nbtfile["Data"].tags:
                    if tag.name == "Player": 
                        write("RESET: Removing player data in level.dat", False)
                        del nbtfile["Data"]["Player"]
                        modify = True
                tagCount = 0
                for tag in nbtfile["Data"]["DataPacks"]["Enabled"].tags:
                    tagCount += 1
                if tagCount != 2:
                    write("RESET: Removing extra enabled data packs", False)
                    del nbtfile["Data"]["DataPacks"]["Enabled"]
                    endabledList = nbt.TAG_List(name="Enabled",type=nbt.TAG_String)
                    endabledList.tags.append(nbt.TAG_String(value="vanilla"))
                    endabledList.tags.append(nbt.TAG_String(value="file/MEDACORP"))
                    nbtfile["Data"]["DataPacks"].tags.append(endabledList)
                    modify = True
                tagCount = 0
                dimensionList = nbt.TAG_Compound(name="dimensions")
                for tag in nbtfile["Data"]["WorldGenSettings"]["dimensions"].tags:
                    if tag.name == "minecraft:overworld" or tag.name == "minecraft:the_nether" or tag.name == "minecraft:the_end" or tag.name in dimensionIDs:
                        dimensionList.tags.append(tag)
                    else:
                        write("RESET: Dimension \"" + tag.name + "\" has been removed", False)
                        modify = True
                del nbtfile["Data"]["WorldGenSettings"]["dimensions"]
                nbtfile["Data"]["WorldGenSettings"].tags.append(dimensionList)
                if modify: nbtfile.write_file(path + map + "/level.dat")
                
                #Check official add-ons
                officialAddons = [name for name in os.listdir(path + "Official add-ons") if os.path.isdir(os.path.join(path + "Official add-ons", name))]
                for addon in officialAddons:
                    addonName = addon.replace(" add-on","")
                    if os.path.isfile(path + "Official add-ons/" + addon + "/" + addonName + "/pack.mcmeta") == False:
                        if os.path.isfile(path + "Official add-ons/" + addon + "/" + addonName + "/packy.mcmeta") == True:
                            write("RESET: " + addon + "'s packy.mcmeta renamed to pack.mcmeta", False)
                            os.rename(path + "Official add-ons/" + addon + "/" + addonName + "/packy.mcmeta", path + "Official add-ons/" + addon + "/" + addonName + "/pack.mcmeta")
                        else: 
                            write("ERROR: " + addon + " is missing its pack.mcmeta; this needs manual fixing", False)
                            succeed = False
                            missingPackMcmeta = True
                
                #Check save data
                if os.path.isfile(os.path.join(path + map + "/session.lock")) == True:
                    write("RESET: Level session.lock deleted", False)
                    os.remove(os.path.join(path + map + "/session.lock"))
                if os.path.isfile(os.path.join(path + map + "/level.dat_old")) == True:
                    write("RESET: Backup level.dat deleted", False)
                    os.remove(os.path.join(path + map + "/level.dat_old"))
                if os.path.isdir(os.path.join(path + map + "/generated")) == True:
                    write("ERROR: Unsaved structure files are present; move these to a data pack and delete the folder", False)
                    succeed = False
                    structureDelete = True
                if os.path.isdir(os.path.join(path + map + "/playerdata")) == True:
                    write("RESET: Player data has been deleted", False)
                    shutil.rmtree(os.path.join(path + map + "/playerdata"))
                if os.path.isdir(os.path.join(path + map + "/stats")) == True:
                    write("RESET: Statistics data has been deleted", False)
                    shutil.rmtree(os.path.join(path + map + "/stats"))
                if os.path.isdir(os.path.join(path + map + "/advancements")) == True:
                    write("RESET: Advancement data has been deleted", False)
                    shutil.rmtree(os.path.join(path + map + "/advancements"))
                if os.path.isfile(os.path.join(path + map + "/data/scoreboard.dat")) == True:
                    write("RESET: Scoreboard data has been deleted", False)
                    os.remove(os.path.join(path + map + "/data/scoreboard.dat"))
                for file in [name for name in os.listdir(path + map + "/data") if os.path.isfile(os.path.join(path + map + "/data", name))]:
                    if file.startswith("command_storage_"):
                        fileName = file.removeprefix("command_storage_").removesuffix(".dat")
                        write("RESET: Command storage \"" + fileName + "\" has been deleted", False)
                        os.remove(os.path.join(path + map + "/data/" + file))
                if os.path.isfile(os.path.join(path + map + "/data/chunks.dat")) == True:
                    write("RESET: Dimension minecraft:overworld had its forceloaded chunks deleted", False)
                    os.remove(os.path.join(path + map + "/data/chunks.dat"))
                if os.path.isfile(os.path.join(path + map + "/data/raids.dat")) == True:
                    write("RESET: Dimension minecraft:overworld had its raids deleted", False)
                    os.remove(os.path.join(path + map + "/data/raids.dat"))
                if os.path.isdir(os.path.join(path + map + "/entities")) == True:
                    write("RESET: Dimension minecraft:overworld had its entity files deleted", False)
                    shutil.rmtree(os.path.join(path + map + "/entities"))
                if os.path.isdir(os.path.join(path + map + "/poi")) == True:
                    write("RESET: Dimension minecraft:overworld had its POI files deleted", False)
                    shutil.rmtree(os.path.join(path + map + "/poi"))
                if os.path.isdir(os.path.join(path + map + "/DIM-1")) == True and useNether == False:
                    write("RESET: Dimension minecraft:the_nether ad its files deleted", False)
                    shutil.rmtree(os.path.join(path + map + "/DIM-1"))
                if os.path.isfile(os.path.join(path + map + "/DIM-1/data/chunks.dat")) == True and useNether == True:
                    write("RESET: Dimension minecraft:the_nether had its forceloaded chunks deleted", False)
                    os.remove(os.path.join(path + map + "/DIM-1/data/chunks.dat"))
                if os.path.isfile(os.path.join(path + map + "/DIM-1/data/raids.dat")) == True and useNether == True:
                    write("RESET: Dimension minecraft:the_nether had its raids deleted", False)
                    os.remove(os.path.join(path + map + "/DIM-1/data/raids.dat"))
                if os.path.isdir(os.path.join(path + map + "/DIM-1/entities")) == True and useNether == True:
                    write("RESET: Dimension minecraft:the_nether had its entity files deleted", False)
                    shutil.rmtree(os.path.join(path + map + "/DIM-1/entities"))
                if os.path.isdir(os.path.join(path + map + "/DIM-1/poi")) == True and useNether == True:
                    write("RESET: Dimension minecraft:the_nether had its POI files deleted", False)
                    shutil.rmtree(os.path.join(path + map + "/DIM-1/poi"))
                if os.path.isdir(os.path.join(path + map + "/DIM1")) == True and useEnd == False:
                    write("RESET: Dimension minecraft:the_end had its files deleted", False)
                    shutil.rmtree(os.path.join(path + map + "/DIM1"))
                if os.path.isfile(os.path.join(path + map + "/DIM1/data/chunks.dat")) == True and useEnd == True:
                    write("RESET: Dimension minecraft:the_end had its forceloaded chunks deleted", False)
                    os.remove(os.path.join(path + map + "/DIM1/data/chunks.dat"))
                if os.path.isfile(os.path.join(path + map + "/DIM1/data/raids.dat")) == True and useEnd == True:
                    write("RESET: Dimension minecraft:the_end had its raids deleted", False)
                    os.remove(os.path.join(path + map + "/DIM1/data/raids.dat"))
                if os.path.isdir(os.path.join(path + map + "/DIM1/entities")) == True and useEnd == True:
                    write("RESET: Dimension minecraft:the_end had its entity files deleted", False)
                    shutil.rmtree(os.path.join(path + map + "/DIM1/entities"))
                if os.path.isdir(os.path.join(path + map + "/DIM1/poi")) == True and useEnd == True:
                    write("RESET: Dimension minecraft:the_end had its POI files deleted", False)
                    shutil.rmtree(os.path.join(path + map + "/DIM1/poi"))
                for d in [name for name in os.listdir(path + map + "/dimensions") if os.path.isdir(os.path.join(path + map + "/dimensions", name))]:
                    for dID in [name for name in os.listdir(path + map + "/dimensions/" + d) if os.path.isdir(os.path.join(path + map + "/dimensions/" + d, name))]:
                        if os.path.isfile(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/data/chunks.dat")) == True:
                            write("RESET: Dimension " + d + ":" + dID + " had its forceloaded chunks deleted", False)
                            os.remove(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/data/chunks.dat"))
                        if os.path.isfile(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/data/raids.dat")) == True:
                            write("RESET: Dimension " + d + ":" + dID + " had its raids deleted", False)
                            os.remove(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/data/raids.dat"))
                        if os.path.isdir(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/entities")) == True:
                            write("RESET: Dimension " + d + ":" + dID + " had its entity files deleted", False)
                            shutil.rmtree(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/entities"))
                        if os.path.isdir(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/poi")) == True:
                            write("RESET: Dimension " + d + ":" + dID + " had its POI files deleted", False)
                            shutil.rmtree(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/poi"))
                if succeed:
                    write("RESET: map is properly reset", False)
                    alreadyReset = True
                else:
                    write("RESET: map is not fully reset, some manual work is still needed", False)
                    if structureDelete == True:
                        print()
                        write("RESET: do you want to force-delete the unsaved structure files? (Type \"y\" or \"n\")", False)
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
                    if missingPackMcmeta == False and structureDelete == False:
                        alreadyReset = True
            elif selected == "reset" and alreadyReset == False:
                write("Reset already reset the map", False)
            elif selected == "build" and alreadyBuild == False:
                write("Running download building", False)
                if alreadyReset == False and verifySucceeded == False:
                    write("BUILD: did you make sure to verify before building? (Type \"y\" or \"n\")", False)
                verify = "u"
                while verify == "u":
                    if alreadyReset == False and verifySucceeded == False:
                        verify = readchar.readkey()
                    else:
                        verify = "y"
                    if verify != "n" and verify != "y":
                        verify = "u"
                    elif verify == "y":
                        write("BUILD: What is the version number?", False)
                        versionNumber = input()
                        zipName = map + " (v" + versionNumber + ").zip"
                        print("")

                        if os.path.exists(executionPath + zipName):
                            write("BUILD: \"" + zipName + "\" already exists, building aborted", False)
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
                            with ZipFile(executionPath + zipName, "w", compression=ZIP_DEFLATED) as zip_object:
                                for folder_name, sub_folders, file_names in os.walk(WorldsPath + map):
                                    for filename in file_names:
                                        file_path = os.path.join(folder_name, filename)
                                        file_path = file_path.replace(WorldsPath + map, "")
                                        file_path = file_path.replace("\\","/")
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
                                        if allowed == True:
                                            zip_object.write(WorldsPath + map + "/" + file_path, file_path)
                                            write("BUILD: \"" + file_path + "\" added to zip", True)
                            if os.path.exists(executionPath + zipName):
                                write("BUILD: Created \"" + zipName + "\" with " + str(filesIncluded) + " files; " + str(filesExcluded) + " files were excluded", False)
                                alreadyBuild = True
                            else:
                                write("BUILD: \"" + zipName + "\" could not be created", False)
                    elif verify == "n":
                        write("BUILD: building aborted", False)
            elif selected == "build" and alreadyBuild == True:
                write("Build was already created", False)
    else:
        write("Not a valid map to build", False)
    print("")