import math
import os
import sys
import readchar
from contextlib import redirect_stdout
from datetime import datetime

def write(string: str, debug: bool | None = False):
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

executionPath, executionFileName = os.path.split(os.path.abspath(sys.argv[0]))
executionPath = executionPath.replace("\\", "/") + "/"
dateNow = datetime.now()
dateFormat = dateNow.strftime("%Y-%m-%d")
if os.path.isdir(executionPath + "logs/") == False:
	os.makedirs(os.path.join(executionPath + "logs/"))
if os.path.exists(executionPath + "logs/" + dateFormat + ".log"): 
    with open(executionPath + "logs/" + dateFormat + ".log", "a") as logFile:
        with redirect_stdout(logFile):
            print("\n\n")
if os.path.exists(executionPath + "logs/latest.log"): os.remove(executionPath + "logs/latest.log")
write("Running \"euler to transformation rotation\" program", True)
write("You can find the logs here: " + executionPath[slice(0,-1)])
print("")

while True:
    write("Which rotation type do you want to use? (Q or A; S to close)")
    selected = "u"
    while selected == "u":
        selected = readchar.readkey()
        if selected != "q" and selected != "a" and selected != "s":
            selected = "u"
    if selected == "s":
        break
    write("Input pitch (x) in degrees:")
    pitch = ""
    while pitch == "":
        value = input()
        write(value, True)
        try:
            pitch = float(value)
        except:
            write("Not a valid number")
    write("Input yaw (y) in degrees:")
    yaw = ""
    while yaw == "":
        value = input()
        write(value, True)
        try:
            yaw = float(value)
        except:
            write("Not a valid number")
    write("Input roll (z) in degrees:")
    roll = ""
    while roll == "":
        value = input()
        write(value, True)
        try:
            roll = float(value)
        except:
            write("Not a valid number")
    pitchrad = pitch * math.pi / 180
    yawrad = yaw * math.pi / 180
    rollrad = roll * math.pi / 180
    cpitch = math.cos(pitchrad * 0.5)
    spitch = math.sin(pitchrad * 0.5)
    cyaw = math.cos(yawrad * 0.5)
    syaw = math.sin(yawrad * 0.5)
    croll = math.cos(rollrad * 0.5)
    sroll = math.sin(rollrad * 0.5)
    if selected == "q":
        w = cpitch * cyaw * croll + spitch * syaw * sroll
        x = spitch * cyaw * croll - cpitch * syaw * sroll
        y = cpitch * syaw * croll + spitch * cyaw * sroll
        z = cpitch * cyaw * sroll - spitch * syaw * croll

        write("Quaternion:[" + str(x) + "f," + str(y) + "f," + str(z) + "f," + str(w) + "f]")
    else:
        cyaw = math.cos(yawrad * 0.5)
        syaw = math.sin(yawrad * 0.5)
        cpitch = math.cos(pitchrad * 0.5)
        spitch = math.sin(pitchrad * 0.5)
        croll = math.cos(rollrad * 0.5)
        sroll = math.sin(rollrad * 0.5)
        w = cyaw * cpitch * croll - syaw * spitch * sroll
        x = cyaw * spitch * croll - syaw * cpitch * sroll
        y = syaw * cpitch * croll + cyaw * spitch * sroll
        z = cyaw * cpitch * sroll + syaw * spitch * croll
        angle = 2 * math.acos(w)
        norm = x*x+y*y+z*z
        if (norm < 0.001):
            x = 1
            y = 0
            z = 0
        else:
            norm = math.sqrt(norm)
            x = x / norm
            y = y / norm
            z = z / norm

        write("Axis-Angle:{angle:" + str(angle) + "f,axis:[" + str(x) + "f," + str(y) + "f," + str(z) + "f]}")
    write("")
