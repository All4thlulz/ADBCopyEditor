using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace All4thlulz.Editor
{
    [InitializeOnLoad]
    public class ADBEDependencyCheck
    {
        static void Awake()
        {
            Check();
        }

        static ADBEDependencyCheck()
        {
            Check();
        }

        public static void Check()
        {

            string scriptToFind = string.Empty;
            string[] findAnyScripts = Directory.GetFiles(Application.dataPath, "PumkinsAvatarTools.cs", SearchOption.AllDirectories);
            if (findAnyScripts.Length > 0)
            {
                scriptToFind = findAnyScripts[0];
            }
            var scriptToChange= Directory.GetFiles(Application.dataPath, "ADBEditor.cs", SearchOption.AllDirectories)[0];

            if(!string.IsNullOrEmpty(scriptToChange))
            {
                string file = File.ReadAllText(scriptToChange);                
                string s = file.Substring(0, file.IndexOf("using"));

                int index = s.IndexOf("#define EnablePumkinIntegration");

                if(!string.IsNullOrEmpty(scriptToFind))
                {                    
                    if(index == -1)
                    {
                        s = "#define EnablePumkinIntegration\r\n";
                        s += file;
                        File.WriteAllText(scriptToChange, s);
                        AssetDatabase.ImportAsset(RelativePath(scriptToChange));
                    }
                }
                else
                {
                    if(index != -1)
                    {
                        s = file.Substring(file.IndexOf(s) + s.Length);
                        File.WriteAllText(scriptToChange, s);
                        AssetDatabase.ImportAsset(RelativePath(scriptToChange));
                    }
                }
            }
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            Check();
        }

        static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if(type != null)
                return type;
            foreach(var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if(type != null)
                    return type;
            }
            return null;
        }

        static string RelativePath(string path)
        {            
            if(path.StartsWith(Application.dataPath))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
            }
            return path;
        }
    }
}