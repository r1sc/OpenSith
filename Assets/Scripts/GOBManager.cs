using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts
{
    class GOBRecordEntry
    {
        public GOBStream GOBStream { get; set; }
        public GOBRecord GOBRecord { get; set; }
    }

    public class GOBManager
    {
        private Dictionary<string, GOBRecordEntry> _recordDict;
        private string _extractedPath;

        public GOBManager(string gamePath, string[] gobFiles)
        {
            _extractedPath = Path.Combine(gamePath, "Extracted");

            _recordDict = new Dictionary<string, GOBRecordEntry>();
            foreach (var gobFile in gobFiles)
            {
                var gob = new GOBStream(Path.Combine(gamePath, gobFile));
                foreach (var record in gob.Records)
                {
                    _recordDict.Add(record.Name.ToLower(), new GOBRecordEntry { GOBStream = gob, GOBRecord = record });
                }
            }
        }

        private bool ExistOnDisk(string name)
        {
            return File.Exists(Path.Combine(_extractedPath, name));
        }

        public Stream GetStream(string name)
        {
            var lowerName = name.ToLower();

            if (ExistOnDisk(name))
            {
                Debug.Log("GOBManager [DISK]: Loading " + lowerName);
                return new FileStream(Path.Combine(_extractedPath, name), FileMode.Open);
            }

            if(!_recordDict.ContainsKey(lowerName))
                throw new Exception("Cannot find file in GOB: " + lowerName);

            Debug.Log("GOBManager: Loading " + lowerName);
            var recordEntry = _recordDict[lowerName];
            return recordEntry.GOBStream.GetRecordStream(recordEntry.GOBRecord);
        }

        public bool Exists(string name)
        {
            if (ExistOnDisk(name))
                return true;

            return _recordDict.ContainsKey(name);
        }
    }
}