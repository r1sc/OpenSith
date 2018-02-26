using System;
using System.IO;
using UnityEngine;

namespace jksharp.jklviewer
{
    public class GOBRecord
    {
        public int Offset { get; set; }
        public int Length { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class GOBStream : IDisposable
    {
        public byte Version { get; private set; }
        public GOBRecord[] Records { get; private set; }

        private Stream _stream;
        public GOBStream(string path)
        {
            _stream = new FileStream(path, FileMode.Open);
            var br = new BinaryReader(_stream);
            br.ReadChars(3);
            Version = br.ReadByte();
            br.ReadInt32();
            br.ReadInt32();
            var numItems = br.ReadInt32();
            Records = new GOBRecord[numItems];
            for (int i = 0; i < numItems; i++)
            {
                Records[i] = new GOBRecord
                {
                    Offset = br.ReadInt32(),
                    Length = br.ReadInt32(),
                    Name = new string(br.ReadChars(128)).Trim('\0')
                };
            }
        }

        public Stream GetRecordStream(GOBRecord record)
        {
            _stream.Position = record.Offset;
            return new PartStream(_stream, record.Length);
        }

        public void Extract(string extractFolder)
        {
            foreach (var gobRecord in Records)
            {
                var path = Path.Combine(extractFolder, gobRecord.Name);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var outFile = new FileStream(path, FileMode.Create))
                {
                    Debug.Log("Extracting " + gobRecord.Name);
                    var inFile = GetRecordStream(gobRecord);
                    var buffer = new byte[gobRecord.Length];
                    inFile.Read(buffer, 0, buffer.Length);
                    outFile.Write(buffer, 0, buffer.Length);
                }
            }
        }

        public void Dispose()
        {
            _stream.Dispose();
        }
    }
}
