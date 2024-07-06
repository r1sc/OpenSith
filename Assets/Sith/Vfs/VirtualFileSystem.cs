using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Assets.Sith.Vfs
{
    class GOBRecordEntry
    {
        public GOBStream GOBStream { get; set; }
        public GOBRecord GOBRecord { get; set; }
    }

    public class VirtualFileSystem
    {
        private Dictionary<string, GOBRecordEntry> _recordDict = new Dictionary<string, GOBRecordEntry>();
        private List<string> _sysPaths = new List<string>();

        public VirtualFileSystem() {}

        //public VirtualFileSystem(string gamePath, string[] gobFiles)
        //{
        //    _extractedPath = Path.Combine(gamePath, "Resource");

        //    _recordDict = new Dictionary<string, GOBRecordEntry>();
        //    foreach (var gobFile in gobFiles)
        //    {
        //        var gobPath = Path.Combine(gamePath, gobFile);
        //        if (File.Exists(gobPath))
        //        {
        //            var gob = new GOBStream(gobPath);
        //            foreach (var record in gob.Records)
        //            {
        //                var name = record.Name.ToLower();
        //                if (!_recordDict.ContainsKey(name))
        //                    _recordDict.Add(name, new GOBRecordEntry { GOBStream = gob, GOBRecord = record });
        //            }
        //        }
        //    }
        //}

        public bool AddGob(string gobFilePath)
        {
            if (File.Exists(gobFilePath))
            {
                foreach (var e in _recordDict.Values)
                {
                    if (e.GOBStream.Name == gobFilePath) return false;
                }
                var gob = new GOBStream(gobFilePath);
                AddGob(gob);
                return true;
            }
            return false;
        }

        public void AddGob(in GOBStream gob)
        {
            foreach (var record in gob.Records)
            {
                var name = record.Name.ToLower();
                if (!_recordDict.ContainsKey(name))
                    _recordDict.Add(name, new GOBRecordEntry { GOBStream = gob, GOBRecord = record });
            }
        }

        public bool AddSystemPath(string sysFolderPath)
        {
            foreach (var path in _sysPaths)
            {
                if (sysFolderPath == path)
                    return false;
            }
            _sysPaths.Add(sysFolderPath);
            return true;
        }

        private bool ExistOnDisk(string name)
        {
            foreach (var path in _sysPaths)
            {
                if (File.Exists(Path.Combine(path, name)))
                    return true;
            }
            return false;
        }

        #nullable enable
        private Stream? Find(string name)
        {
            //name = name.ToLower();
            foreach (var path in _sysPaths)
            {
                var filepath = Path.Combine(path, name);
                if (File.Exists(filepath))
                {
                    Debug.Log("GOBManager [DISK]: Loading " + name);
                    return new FileStream(filepath, FileMode.Open);
                }
            }

            if (!_recordDict.ContainsKey(name))
                return null;

            Debug.Log("GOBManager: Loading " + name);
            var recordEntry = _recordDict[name];
            return recordEntry.GOBStream.GetRecordStream(recordEntry.GOBRecord);
        }

        public Stream GetStream(string name)
        {
            var s = Find(name);
            if (s == null)
                throw new Exception("Cannot find file in GOB: " + name);
            return s;
            //var lowerName = name.ToLower();

            //if (ExistOnDisk(name))
            //{
            //    Debug.Log("GOBManager [DISK]: Loading " + lowerName);
            //    return new FileStream(Path.Combine(_extractedPath, name), FileMode.Open);
            //}

            //if(!_recordDict.ContainsKey(lowerName))
            //    throw new Exception("Cannot find file in GOB: " + lowerName);

            //Debug.Log("GOBManager: Loading " + lowerName);
            //var recordEntry = _recordDict[lowerName];
            //return recordEntry.GOBStream.GetRecordStream(recordEntry.GOBRecord);
        }

        public bool Exists(string name)
        {
            if (ExistOnDisk(name))
                return true;

            return _recordDict.ContainsKey(name);
        }
    }
}