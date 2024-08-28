import os
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
    succeed = True
    dimensions = 0
    useNether = False
    useEnd = False
    if selected == "stop":
        break
    elif selected == "luigis mansion" or selected == "lm":
        map = "Luigi's Mansion"
        dimensions = 2
    elif selected == "luigis mansion dark moon" or selected == "dark moon" or selected == "lmdm" or selected == "dm":
        map = "Luigi's Mansion Dark Moon"
        dimensions = 1
    elif selected == "zelda twilight princess" or selected == "twilight princess" or selected == "ztp" or selected == "tp":
        map = "Zelda Twilight Princess"
        dimensions = 1

    if (map != ""):
        path = WorldsPath + map + "/"
        print("Map set to " + map + ".")
        print("\r\nRules:\r\n* No player data remaining\r\n* No save progress remaining\r\n* No additional data packs enabled\r\n* No missing pack.mcmeta-s")
        if useNether == False: 
            print("* No Nether data remaining")
        if useEnd == False: 
            print("* No End data remaining")
        if dimensions == 1: print("* 1 custom dimension")
        else: print("* " + str(dimensions) + " custom dimensions")
        print("\r\nContinue? (Type y or n)")
        while verify == "u":
            verify = readchar.readkey()
            if verify != "y" and verify != "n":
                verify = "u"
            elif verify == "n":
                print("Aborting")
            elif verify == "y":
                print("Continuing")

                #Check level.dat
                nbtfile = nbt.NBTFile(path + map + "/level.dat", "rb")
                nbtfile.name = 'level'
                for tag in nbtfile["Data"].tags:
                    if tag.name == "Player":
                        print("ERROR: Player data is present in level.dat")
                        succeed = False
                tagCount = 0
                for tag in nbtfile["Data"]["DataPacks"]["Enabled"].tags:
                    tagCount += 1
                if tagCount != 2:
                    print("ERROR: Extra data packs are enabled")
                    succeed = False
                tagCount = 0
                for tag in nbtfile["Data"]["WorldGenSettings"]["dimensions"].tags:
                    if tag.name != "minecraft:overworld" and tag.name != "minecraft:the_nether" and tag.name != "minecraft:the_end": tagCount += 1
                if tagCount != dimensions:
                    print("ERROR: Dimension count is incorrect")
                    succeed = False
                
                #Check official add-ons
                officialAddons = [name for name in os.listdir(path + "Official add-ons") if os.path.isdir(os.path.join(path + "Official add-ons", name))]
                for addon in officialAddons:
                    addonName = addon.replace(" add-on","")
                    if os.path.isfile(path + "Official add-ons/" + addon + "/" + addonName + "/pack.mcmeta") == False:
                        print("ERROR: " + addon + " is missing its pack.mcmeta")
                        succeed = False
                
                #Check save data
                if os.path.isdir(os.path.join(path + map + "/generated")) == True:
                    print("ERROR: Unsaved structure files are present")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/playerdata")) == True:
                    print("ERROR: Player data is still in world data")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/stats")) == True:
                    print("ERROR: Statistics data is still in world data")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/advancements")) == True:
                    print("ERROR: Advancement data is still in world data")
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/data/scoreboard.dat")) == True:
                    print("ERROR: Scoreboard is still in world data")
                    succeed = False
                for file in [name for name in os.listdir(path + map + "/data") if os.path.isfile(os.path.join(path + map + "/data", name))]:
                    if file.startswith("command_storage_"):
                        fileName = file.removeprefix("command_storage_").removesuffix(".dat")
                        print("ERROR: Command storage " + fileName + " is still in world data")
                        succeed = False
                if os.path.isfile(os.path.join(path + map + "/data/chunks.dat")) == True:
                    print("ERROR: Dimension minecraft:overworld has forceloaded chunks")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/entities")) == True:
                    print("ERROR: Dimension minecraft:overworld has entity files")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/poi")) == True:
                    print("ERROR: Dimension minecraft:overworld has POI files")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM-1")) == True and useNether == False:
                    print("ERROR: Dimension minecraft:the_nether has files")
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/DIM-1/data/chunks.dat")) == True and useNether == True:
                    print("ERROR: Dimension minecraft:the_nether has forceloaded chunks")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM-1/entities")) == True and useNether == True:
                    print("ERROR: Dimension minecraft:the_nether has entity files")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM-1/poi")) == True and useNether == True:
                    print("ERROR: Dimension minecraft:the_nether has POI files")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM1")) == True and useEnd == False:
                    print("ERROR: Dimension minecraft:the_end has files")
                    succeed = False
                if os.path.isfile(os.path.join(path + map + "/DIM1/data/chunks.dat")) == True and useEnd == True:
                    print("ERROR: Dimension minecraft:the_end has forceloaded chunks")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM1/entities")) == True and useEnd == True:
                    print("ERROR: Dimension minecraft:the_end has entity files")
                    succeed = False
                if os.path.isdir(os.path.join(path + map + "/DIM1/poi")) == True and useEnd == True:
                    print("ERROR: Dimension minecraft:the_end has POI files")
                    succeed = False
                dimensionFolders = [name for name in os.listdir(path + map + "/dimensions") if os.path.isdir(os.path.join(path + map + "/dimensions", name))]
                for d in dimensionFolders:
                    dimensionIDFolders = [name for name in os.listdir(path + map + "/dimensions/" + d) if os.path.isdir(os.path.join(path + map + "/dimensions/" + d, name))]
                    for dID in dimensionIDFolders:
                        if os.path.isfile(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/data/chunks.dat")) == True:
                            print("ERROR: Dimension " + d + ":" + dID + " has forceloaded chunks")
                            succeed = False
                        if os.path.isdir(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/entities")) == True:
                            print("ERROR: Dimension " + d + ":" + dID + " has entity files")
                            succeed = False
                        if os.path.isdir(os.path.join(path + map + "/dimensions/" + d + "/" + dID + "/poi")) == True:
                            print("ERROR: Dimension " + d + ":" + dID + " has POI files")
                            succeed = False
                        
                if succeed:
                    print("\r\nMap can be made into a downloadable build.\r\nDo you want to do that? (Type y or n)")
                    time.sleep(0.5)
                    verify = "u"
                    while verify == "u":
                        verify = readchar.readkey()
                        if verify != "y" and verify != "n":
                            verify = "u"
                        elif verify == "n":
                            print("Alright then")
                        elif verify == "y":
                            print("\r\nWhat is the version number?")
                            versionNumber = input()
                            zipName = map + " (v" + versionNumber + ").zip"
                            print("")

                            if os.path.exists(executionPath + zipName):
                                print("\"" + zipName + "\" already exists")
                            else:
                                print("Creating \"" + zipName + "\"")

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
                                    # Traverse all files in directory
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
                                            # Create filepath of files in directory
                                            if (allowed == True):
                                                # Add files to zip file
                                                zip_object.write(WorldsPath + map + "/" + file_path, file_path)
                                if os.path.exists(executionPath + zipName):
                                    print("Created \"" + zipName + "\"")
                                else:
                                    print("\"" + zipName + "\" could not be created")
                
    else:
        print("Not a valid map to build")
    print("")
