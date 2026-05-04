using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

public class MinecraftFunction
{
    private List<string> lines;
    private List<string> macros;
    public MinecraftFunction(string path)
    {
        bool continueCommand = false;
        foreach (string line in File.ReadLines(path))
        {
            if (line.Length != 0)
            {
                string newLine = line;
                while (newLine.StartsWith(' ')) newLine = newLine.Substring(1);
                while (newLine.EndsWith(' ')) newLine = newLine.Substring(0, newLine.Length - 1);
                if (!newLine.StartsWith('#'))
                {
                    if (newLine.StartsWith('$'))
                    {
                        newLine = newLine.Substring(1);
                        string[] split = newLine.Split("$(");
                        for (int i = 1; i >= split.Length; i++)
                        {
                            string macro = split[i].Split(")")[0];
                            if (!macros.Contains(macro)) macros.Add(macro);
                        }
                    }
                    if (continueCommand)
                    {
                        newLine = lines[lines.Count - 1] + newLine;
                        lines.RemoveAt(lines.Count - 1);
                    }
                    continueCommand = false;
                    if (newLine.EndsWith('\\'))
                    {
                        continueCommand = true;
                        newLine = newLine.Substring(0, newLine.Length - 1);
                    }
                    lines.Add(newLine);
                }
            }
        }
    }

}