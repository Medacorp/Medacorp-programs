if __name__ == "__main__": exit()

import os
import shutil
import sys
import importlib.metadata
import subprocess
from datetime import datetime
from contextlib import redirect_stdout

required = {'readchar','NBT'}
installed = {pkg.metadata['Name'] for pkg in importlib.metadata.distributions()}
missing = required - installed

if missing:
    subprocess.check_call([sys.executable, '-m', 'pip', 'install', '--upgrade', 'pip'])
    subprocess.check_call([sys.executable, '-m', 'pip', 'install', *missing])

import readchar
from nbt import nbt

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
def getExecutionPath(suffixSlash: bool | None = False) -> str:
    if suffixSlash: return executionPath
    else: return executionPath[slice(0,-1)]

program = sys.argv[0].split(".py")[0].split("\\")
write("Running \"" + program[len(program) - 1] + "\" program", True)
