using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace execnc
{
    public class Utils
    {
        //Look for baseFilename in a bunch of locations. Return the first one or null if none were found
        public static string findFile(string baseFilename, string[] locations)
        {
            foreach (string dir in locations)
            {
                string filename = dir + "\\" + baseFilename;
                Utils.Log("Looking for: " + filename);
                if (File.Exists(filename))
                {
                    Utils.Log("Found file");
                    return filename;
                }
            }

            return null;
        }

        //try creating and deleting a file in directories to find a writable directory
        //Returns the dir path or null if none was found
        public static string findWritableDirectory(string[] locations)
        {
            foreach (string dir in locations)
            {
                string filename = dir + "\\testfile.txt";
                Utils.Log("Trying: " + dir);
                try
                {
                    FileStream file = File.Open(filename, FileMode.Append);
                    Utils.Log($"Dir {dir} is writable");
                    file.Close();
                    File.ReadAllText(filename);
                    Utils.Log($"Dir {dir} is readable");
                    File.Delete(filename);
                    return dir;
                }
                catch (Exception ex)
                {
                    Utils.Log($"Dir failed: {dir}: {ex.Message}");
                }
            }

            return null;
        }

        public static void Log(string message)
        {
            DateTime dt = DateTime.Now;
            string dtMessage = dt.ToString("yyyy/MM/dd H:mm:ss zzz");
            Trace.WriteLine($"[{dtMessage}] {message}");
        }
    }
}
