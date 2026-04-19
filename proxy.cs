using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace DicomProxyLauncher
{
    class Program
    {
        // Simple INI Parser
        static Dictionary<string, string> ReadIniFile(string path)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(path)) return result;

            foreach (var line in File.ReadAllLines(path))
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#") || trimmed.StartsWith("["))
                    continue;

                int equalIndex = trimmed.IndexOf('=');
                if (equalIndex > 0)
                {
                    string key = trimmed.Substring(0, equalIndex).Trim();
                    string value = trimmed.Substring(equalIndex + 1).Trim();
                    if (!result.ContainsKey(key))
                    {
                        result[key] = value;
                    }
                }
            }
            return result;
        }

        static void Log(string message, string type = "INFO")
        {
             // Logging disabled
        }

        static void Main(string[] args)
        {
            try
            {
                // 1. Parse Arguments to find Accession Number
                string accessionNumber = null;
                string aeTitle = "MAC"; // Default

                foreach (string arg in args)
                {
                    if (arg.StartsWith("-qr="))
                    {
                        string value = arg.Substring(4);
                        string[] parts = value.Split(new char[] { ',', ';' });
                        
                        // Expected: DS_WS01,0008;0050;5444638
                        // Index 3 is Accession Number
                        if (parts.Length >= 4)
                        {
                            aeTitle = parts[0];
                            accessionNumber = parts[3];
                        }
                    }
                }

                if (string.IsNullOrEmpty(accessionNumber))
                {
                    Log("No Accession Number found in args: " + string.Join(" ", args), "ERROR");
                    return;
                }

                // 2. Read Configuration
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string iniPath = Path.Combine(Path.GetDirectoryName(exePath), "config.ini");
                
                var config = ReadIniFile(iniPath);

                // Defaults
                string viewer = config.ContainsKey("viewer") ? config["viewer"].ToLower() : "osirix";
                
                // 3. Dispatch based on Viewer
                if (viewer == "osirix" || viewer == "horos")
                {
                    LaunchOsirix(accessionNumber);
                }
                else 
                {
                    // Assume RadiAnt or Default to RadiAnt logic if specified
                    LaunchRadiant(accessionNumber, config);
                }

            }
            catch (Exception ex)
            {
                Log("Critical Error: " + ex.ToString(), "CRITICAL");
            }
        }

        static void LaunchOsirix(string accessionNumber)
        {
             string url = string.Format("osirix://?methodName=displayStudy&AccessionNumber={0}", accessionNumber);
             try
             {
                 Process.Start(url);
                 Log("OsiriX Launched for AN: " + accessionNumber);
             }
             catch (Exception ex)
             {
                 Log("Failed to launch OsiriX: " + ex.Message, "ERROR");
             }
        }

        static void LaunchRadiant(string accessionNumber, Dictionary<string, string> config)
        {
            string radiantExe = config.ContainsKey("radiant_exe") ? config["radiant_exe"] : @"C:\Program Files\RadiAntViewer\RadiAntViewer.exe";
            string radiantDicom = config.ContainsKey("radiant_dicom") ? config["radiant_dicom"] : @"C:\DICOM";

            // Construct the path for the specific study. 
            // Assumption: The study is stored in a folder named after the Accession Number inside the radiant_dicom directory.
            string studyPath = Path.Combine(radiantDicom, accessionNumber);

            // Verify if executable exists
            if (!File.Exists(radiantExe))
            {
                Log("RadiAnt executable not found at: " + radiantExe, "ERROR");
                // Fallback -> Open Folder?
                OpenFolder(studyPath);
                return;
            }

            try
            {
                // Logic: radiant_exe -cl -d path
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = radiantExe;
                psi.Arguments = string.Format("-cl -d \"{0}\"", studyPath);
                psi.UseShellExecute = false; // Required for CreateNoWindow but we want to launch generic process

                Process.Start(psi);
                Log("RadiAnt Launched for path: " + studyPath);
            }
            catch (Exception ex)
            {
                Log("Failed to launch RadiAnt: " + ex.Message, "ERROR");
                // Fallback
                OpenFolder(studyPath);
            }
        }

        static void OpenFolder(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Process.Start("explorer.exe", path);
                    Log("Opened folder fallback: " + path, "WARN");
                }
                else
                {
                    Log("Study folder not found: " + path, "ERROR");
                }
            }
            catch(Exception ex)
            {
                Log("Failed to open folder: " + ex.Message, "ERROR");
            }
        }
    }
}
