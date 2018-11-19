using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Assets.Scripts
{
    class Puppet
    {
        public Dictionary<int, Dictionary<string, PuppetSubMode>> Modes { get; set; }
    }

    class PuppetSubMode
    {
        public string KeyFile { get; set; }
        public int Flags { get; set; }
        public int LoPri { get; set; }
        public int HiPri { get; set; }
    }

    static class PUPPETParser
    {
        public static Puppet Parse(Stream dataStream)
        {
            using (var sr = new StreamReader(dataStream))
            {
                var puppet = new Puppet
                {
                    Modes = new Dictionary<int, Dictionary<string, PuppetSubMode>>()
                };

                Dictionary<string, PuppetSubMode> subModes = null;
                bool joints = false;

                while (!sr.EndOfStream)
                {
                    var args = sr.ReadArgs();
                    if (args == null)
                        break;

                    if (args[0] == "mode")
                    {
                        subModes = new Dictionary<string, PuppetSubMode>();
                        puppet.Modes.Add(int.Parse(args[1]), subModes);
                    }
                    else if(args[0] == "joints")
                    {
                        joints = true;
                    }
                    else if (joints)
                    {

                    }
                    else
                    {
                        if(subModes == null)
                            throw new Exception("Submodelist is null");

                        var submode = new PuppetSubMode
                        {
                            KeyFile = args[1],
                            Flags = int.Parse(args[2].Replace("0x", ""), NumberStyles.AllowHexSpecifier),
                            LoPri = int.Parse(args[3]),
                            HiPri = int.Parse(args[4])
                        };
                        subModes[args[0]] = submode;
                    }
                }

                return puppet;
            }

            
        }

        private static string[] ReadArgs(this StreamReader sr)
        {
            while (!sr.EndOfStream)
            {
                
                var line = sr.ReadLine().ToLower().Trim();
                if (line.StartsWith("#") || line == "")
                    continue;
                var args = line.Split(new[] { ' ', ',', '\t', '=' }, StringSplitOptions.RemoveEmptyEntries);
                return args;
            }

            return null;
        }

    }
}
