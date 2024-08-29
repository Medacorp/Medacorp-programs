import os
import shutil
import sys
import readchar
import time
import re
from nbt import nbt
from zipfile import ZIP_DEFLATED, ZipFile
import zlib

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
print("Path zips get created in: " + executionPath + "\r\nPath worlds get searched for: " + WorldsPath + "\r\nTo change, modify \"paths.txt\" in the same folder as this program is located\r\n")

while True:
    print("What map do you wish to build? (Type \"stop\" to close program)")
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
        print("Map set to " + map + ".")
        while True:
            print("\r\nWhat would you like to do? Type one of the following:\r\n* \"stop\"\r\n* \"rules\"")
            if verifySucceeded == False: print("* \"verify\"")
            if alreadyReset == False: print("* \"reset\"")
            if alreadyBuild == False: print("* \"build\"")
            selected = input().lower()
            if selected == "stop":
                print("Aborting")
                break
            elif selected == "rules":
                print("\r\nRules:\r\n* No player data remaining\r\n* No save progress remaining\r\n* No additional data packs enabled\r\n* No missing pack.mcmeta-s")
                if useNether == False: 
                    print("* No Nether data remaining")
                if useEnd == False: 
                    print("* No End data remaining")
                if len(dimensionIDs) == 1: print("* 1 custom dimension")
                else: print("* " + str(len(dimensionIDs)) + " custom dimensions")
            elif selected == "verify" and verifySucceeded == False:
                succeed = True
                print("Running verification")

                #Check level.dat
                nbtfile = nbt.NBTFile(path + map + "/level.dat", "rb")
                nbtfile.name = 'level'
                for tag in nbtfile["Data"].tags:
                    if tag.name == "Player":
                        print("VERIFY: Player data is present in level.dat")
                        succeed = False
                tagCount = 0
                for tag in nbtfile["Data"]["DataPacks"]["Enabled"].tags:
                    tagCount += 1
                if tagCount != 2:
                    print("VERIFY: Extra data packs are enabled")
                    succeed = False
                tagCount = 0
                for tag in nbtfile["Data"]["WorldGenSettings"]["dimensions"].tags:
                    if tag.name != "minecraft:overworld" and tag.name != "minecraft:the_nether" and tag.name != "minecraft:the_end": tagCount += 1
                if tagCount != len(dimensionIDs):
                    print("VERIFY: Dimension count is incorrect")
                    succeed = False
                
                #Check official add-ons
                officialAddons = [name for name in os.listdir(path + "Official add-ons") if os.path.isdir(os.path.join(path + "Official add-ons", name))]
                for addon in officialAddons:
                    addonName = addon.replace(" add-on","")
                    if os.path.isfile(path + "Official add-ons/" + addon + "/" + addonName + "/pack.mcmeta") == False:
                        print("VERIFY: " + addon + " is missing its pack.mcmeta")
                        succeed = False
                
                #Check save data
                if os.path.isfile(os.path.join(path + map + "/session.lock")) == True:
                    print("VERIFY: Level session.lock is present")
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/level.dat_old")) == True:
                    print("VERIFY: Backup level.dat is present")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/generated")) == True:
                    print("VERIFY: Unsaved structure files are present")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/playerdata")) == True:
                    print("VERIFY: Player data is still in world data")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/stats")) == True:
                    print("VERIFY: Statistics data is still in world data")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/advancements")) == True:
                    print("VERIFY: Advancement data is still in world data")
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/data/scoreboard.dat")) == True:
                    print("VERIFY: Scoreboard is still in world data")
                    succeed = False
                for file in [name for name in os.listdir(path + map + "/data") if os.path.isfile(os.path.join(path + map + "/data", name))]:
                    if file.startswith("command_storage_"):
                        fileName = file.removeprefix("command_storage_").removesuffix(".dat")
                        print("VERIFY: Command storage \"" + fileName + "\" is still in world data")
                        succeed = False
                if os.path.isfile(os.path.join(path + map + "/data/chunks.dat")) == True:
                    print("VERIFY: Dimension minecraft:overworld has forceloaded chunks")
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/data/raids.dat")) == True:
                    print("VERIFY: Dimension minecraft:overworld has raids")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/entities")) == True:
                    print("VERIFY: Dimension minecraft:overworld has entity files")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/poi")) == True:
                    print("VERIFY: Dimension minecraft:overworld has POI files")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM-1")) == True and useNether == False:
                    print("VERIFY: Dimension minecraft:the_nether has files")
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/DIM-1/data/chunks.dat")) == True and useNether == True:
                    print("VERIFY: Dimension minecraft:the_nether has forceloaded chunks")
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/DIM-1/data/raids.dat")) == True and useNether == True:
                    print("VERIFY: Dimension minecraft:the_nether has raids")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM-1/entities")) == True and useNether == True:
                    print("VERIFY: Dimension minecraft:the_nether has entity files")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM-1/poi")) == True and useNether == True:
                    print("VERIFY: Dimension minecraft:the_nether has POI files")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM1")) == True and useEnd == False:
                    print("VERIFY: Dimension minecraft:the_end has files")
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/DIM1/data/chunks.dat")) == True and useEnd == True:
                    print("VERIFY: Dimension minecraft:the_end has forceloaded chunks")
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/DIM1/data/raids.dat")) == True and useEnd == True:
                    print("VERIFY: Dimension minecraft:the_end has raids")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM1/entities")) == True and useEnd == True:
                    print("VERIFY: Dimension minecraft:the_end has entity files")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM1/poi")) == True and useEnd == True:
                    print("VERIFY: Dimension minecraft:the_end has POI files")
                    succeed = False
                for d in [name for name in os.listdir(path + map + "/dimensions") if os.path.isdir(os.path.join(path + map + "/dimensions", name))]:
                    for dID in [name for name in os.listdir(path + map + "/dimensions/" + d) if os.path.isdir(os.path.join(path + map + "/dimensions/" + d, name))]:
                        if os.path.isfile(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/data/chunks.dat")) == True:
                            print("VERIFY: Dimension " + d + ":" + dID + " has forceloaded chunks")
                            succeed = False
                        if os.path.isfile(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/data/raids.dat")) == True:
                            print("VERIFY: Dimension " + d + ":" + dID + " has raids")
                            succeed = False
                        if os.path.isdir(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/entities")) == True:
                            print("VERIFY: Dimension " + d + ":" + dID + " has entity files")
                            succeed = False
                        if os.path.isdir(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/poi")) == True:
                            print("VERIFY: Dimension " + d + ":" + dID + " has POI files")
                            succeed = False
                if succeed:
                    print("VERIFY: map is properly reset")
                    verifySucceeded = True
                else:
                    print("VERIFY: map is not reset")
            elif selected == "verify" and verifySucceeded == True:
                print("Verify already confirmed the map is reset")
            elif selected == "reset" and alreadyReset == False:
                succeed = True
                structureDelete = False
                missingPackMcmeta = False
                print("Running reset")

                #Check level.dat
                nbtfile = nbt.NBTFile(path + map + "/level.dat", "rb")
                nbtfile.name = 'level'
                modify = False
                for tag in nbtfile["Data"].tags:
                    if tag.name == "Player": 
                        print("RESET: Removing player data in level.dat")
                        del nbtfile["Data"]["Player"]
                        modify = True
                tagCount = 0
                for tag in nbtfile["Data"]["DataPacks"]["Enabled"].tags:
                    tagCount += 1
                if tagCount != 2:
                    print("RESET: Removing extra enabled data packs")
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
                        print("RESET: Dimension \"" + tag.name + "\" has been removed")
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
                            print("RESET: " + addon + "'s packy.mcmeta renamed to pack.mcmeta")
                            os.rename(path + "Official add-ons/" + addon + "/" + addonName + "/packy.mcmeta", path + "Official add-ons/" + addon + "/" + addonName + "/pack.mcmeta")
                        else: 
                            print("ERROR: " + addon + " is missing its pack.mcmeta; this needs manual fixing")
                            succeed = False
                            missingPackMcmeta = True
                
                #Check save data
                if os.path.isfile(os.path.join(path + map + "/session.lock")) == True:
                    print("RESET: Level session.lock deleted")
                    os.remove(os.path.join(path + map + "/session.lock"))
                if os.path.isfile(os.path.join(path + map + "/level.dat_old")) == True:
                    print("RESET: Backup level.dat deleted")
                    os.remove(os.path.join(path + map + "/level.dat_old"))
                if os.path.isdir(os.path.join(path + map + "/generated")) == True:
                    print("ERROR: Unsaved structure files are present; move these to a data pack and delete the folder")
                    succeed = False
                    structureDelete = True
                if os.path.isdir(os.path.join(path + map + "/playerdata")) == True:
                    print("RESET: Player data has been deleted")
                    shutil.rmtree(os.path.join(path + map + "/playerdata"))
                if os.path.isdir(os.path.join(path + map + "/stats")) == True:
                    print("RESET: Statistics data has been deleted")
                    shutil.rmtree(os.path.join(path + map + "/stats"))
                if os.path.isdir(os.path.join(path + map + "/advancements")) == True:
                    print("RESET: Advancement data has been deleted")
                    shutil.rmtree(os.path.join(path + map + "/advancements"))
                if os.path.isfile(os.path.join(path + map + "/data/scoreboard.dat")) == True:
                    print("RESET: Scoreboard has been deleted")
                    os.remove(os.path.join(path + map + "/data/scoreboard.dat"))
                for file in [name for name in os.listdir(path + map + "/data") if os.path.isfile(os.path.join(path + map + "/data", name))]:
                    if file.startswith("command_storage_"):
                        fileName = file.removeprefix("command_storage_").removesuffix(".dat")
                        print("RESET: Command storage \"" + fileName + "\" has been deleted")
                        os.remove(os.path.join(path + map + "/data/" + file))
                if os.path.isfile(os.path.join(path + map + "/data/chunks.dat")) == True:
                    print("RESET: Dimension minecraft:overworld had its forceloaded chunks deleted")
                    os.remove(os.path.join(path + map + "/data/chunks.dat"))
                if os.path.isfile(os.path.join(path + map + "/data/raids.dat")) == True:
                    print("RESET: Dimension minecraft:overworld had its raids deleted")
                    os.remove(os.path.join(path + map + "/data/raids.dat"))
                if os.path.isdir(os.path.join(path + map + "/entities")) == True:
                    print("RESET: Dimension minecraft:overworld had its entity files deleted")
                    shutil.rmtree(os.path.join(path + map + "/entities"))
                if os.path.isdir(os.path.join(path + map + "/poi")) == True:
                    print("RESET: Dimension minecraft:overworld had its POI files deleted")
                    shutil.rmtree(os.path.join(path + map + "/poi"))
                if os.path.isdir(os.path.join(path + map + "/DIM-1")) == True and useNether == False:
                    print("RESET: Dimension minecraft:the_nether ad its files deleted")
                    shutil.rmtree(os.path.join(path + map + "/DIM-1"))
                if os.path.isfile(os.path.join(path + map + "/DIM-1/data/chunks.dat")) == True and useNether == True:
                    print("RESET: Dimension minecraft:the_nether had its forceloaded chunks deleted")
                    os.remove(os.path.join(path + map + "/DIM-1/data/chunks.dat"))
                if os.path.isfile(os.path.join(path + map + "/DIM-1/data/raids.dat")) == True and useNether == True:
                    print("RESET: Dimension minecraft:the_nether had its raids deleted")
                    os.remove(os.path.join(path + map + "/DIM-1/data/raids.dat"))
                if os.path.isdir(os.path.join(path + map + "/DIM-1/entities")) == True and useNether == True:
                    print("RESET: Dimension minecraft:the_nether had its entity files deleted")
                    shutil.rmtree(os.path.join(path + map + "/DIM-1/entities"))
                if os.path.isdir(os.path.join(path + map + "/DIM-1/poi")) == True and useNether == True:
                    print("RESET: Dimension minecraft:the_nether had its POI files deleted")
                    shutil.rmtree(os.path.join(path + map + "/DIM-1/poi"))
                if os.path.isdir(os.path.join(path + map + "/DIM1")) == True and useEnd == False:
                    print("RESET: Dimension minecraft:the_end had its files deleted")
                    shutil.rmtree(os.path.join(path + map + "/DIM1"))
                if os.path.isfile(os.path.join(path + map + "/DIM1/data/chunks.dat")) == True and useEnd == True:
                    print("RESET: Dimension minecraft:the_end had its forceloaded chunks deleted")
                    os.remove(os.path.join(path + map + "/DIM1/data/chunks.dat"))
                if os.path.isfile(os.path.join(path + map + "/DIM1/data/raids.dat")) == True and useEnd == True:
                    print("RESET: Dimension minecraft:the_end had its raids deleted")
                    os.remove(os.path.join(path + map + "/DIM1/data/raids.dat"))
                if os.path.isdir(os.path.join(path + map + "/DIM1/entities")) == True and useEnd == True:
                    print("RESET: Dimension minecraft:the_end had its entity files deleted")
                    shutil.rmtree(os.path.join(path + map + "/DIM1/entities"))
                if os.path.isdir(os.path.join(path + map + "/DIM1/poi")) == True and useEnd == True:
                    print("RESET: Dimension minecraft:the_end had its POI files deleted")
                    shutil.rmtree(os.path.join(path + map + "/DIM1/poi"))
                for d in [name for name in os.listdir(path + map + "/dimensions") if os.path.isdir(os.path.join(path + map + "/dimensions", name))]:
                    for dID in [name for name in os.listdir(path + map + "/dimensions/" + d) if os.path.isdir(os.path.join(path + map + "/dimensions/" + d, name))]:
                        if os.path.isfile(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/data/chunks.dat")) == True:
                            print("RESET: Dimension " + d + ":" + dID + " had its forceloaded chunks deleted")
                            os.remove(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/data/chunks.dat"))
                        if os.path.isfile(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/data/raids.dat")) == True:
                            print("RESET: Dimension " + d + ":" + dID + " had its raids deleted")
                            os.remove(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/data/raids.dat"))
                        if os.path.isdir(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/entities")) == True:
                            print("RESET: Dimension " + d + ":" + dID + " had its entity files deleted")
                            shutil.rmtree(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/entities"))
                        if os.path.isdir(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/poi")) == True:
                            print("RESET: Dimension " + d + ":" + dID + " had its POI files deleted")
                            shutil.rmtree(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/poi"))
                if succeed:
                    print("RESET: map is properly reset")
                    alreadyReset = True
                else:
                    print("RESET: map is not fully reset, some manual work is still needed")
                    if structureDelete == True:
                        print("\r\nVERIFY: do you want to force-delete the unsaved structure files? (Type \"y\" or \"n\")")
                        verify = "u"
                        while verify == "u":
                            verify = readchar.readkey()
                            if verify != "n" and verify != "y":
                                verify = "u"
                            elif verify == "y":
                                print("VERIFY: Deleted unsaved structure files")
                                shutil.rmtree(os.path.join(path + map + "/generated"))
                                structureDelete = False
                            elif verify == "n":
                                print("VERIFY: Left unsaved structure files alone")
                    if missingPackMcmeta == False and structureDelete == False:
                        alreadyReset = True
            elif selected == "reset" and alreadyReset == False:
                print("Reset already reset the map")
            elif selected == "build" and alreadyBuild == False:
                print("Running download building")
                print("\r\nBUILD: did you make sure to verify before building? (Type \"y\" or \"n\")")
                verify = "u"
                while verify == "u":
                    verify = readchar.readkey()
                    if verify != "n" and verify != "y":
                        verify = "u"
                    elif verify == "y":
                        print("BUILD: What is the version number?")
                        versionNumber = input()
                        zipName = map + " (v" + versionNumber + ").zip"
                        print("")

                        if os.path.exists(executionPath + zipName):
                            print("BUILD: \"" + zipName + "\" already exists, building aborted")
                        else:
                            print("BUILD: Creating \"" + zipName + "\", please be patient")

                            ignoreFile = open(path + ".gitignore", "r")
                            allowFiles = []
                            disallowFiles = []
                            lineCount = sum(1 for _ in ignoreFile)
                            ignoreFile.close()
                            ignoreFile = open(path + ".gitignore", "r")
                            i = 0
                            while i <= lineCount:
                                line = ignoreFile.readline()
                                line = line.replace("*",".*")
                                line = line.replace("\r","")
                                line = line.replace(".*\n",".*")
                                line = line.replace("\n",".*")
                                if line.startswith("!"):
                                    line = line.replace("!","")
                                    allowFiles.append(line)
                                elif len(line) >= 1:
                                    if line.startswith("/") == False and line.startswith(".*") == False:
                                        line = ".*/" + line
                                    disallowFiles.append(line)
                                i += 1
                            ignoreFile.close()
                            disallowFiles.append("/.git/.*")
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
                                        if (allowed == True):
                                            zip_object.write(WorldsPath + map + "/" + file_path, file_path)
                            if os.path.exists(executionPath + zipName):
                                print("BUILD: Created \"" + zipName + "\"")
                                alreadyBuild = True
                            else:
                                print("BUILD: \"" + zipName + "\" could not be created")
                    elif verify == "n":
                        print("BUILD: building aborted")
            elif selected == "build" and alreadyBuild == True:
                print("Build was already created")
    else:
        print("Not a valid map to build")
    print("")
