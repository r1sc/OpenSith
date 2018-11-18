using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace Assets.Scripts
{
    public class MAT
    {
        public Texture2D[] Textures { get; set; }

        public void ParseMat(CMP cmp, Stream dataStream)
        {
            var transparentColor = new Color32(0, 0, 0, 0);

            using (var br = new BinaryReader(dataStream))
            {
                var rectList = new List<Rect>();

                var matHdr = br.ReadChars(4);
                var version = br.ReadInt32();
                var type = br.ReadInt32();
                var numTextures = br.ReadInt32();
                var numTextures1 = br.ReadInt32();
                var zero = br.ReadInt32();
                var eight = br.ReadInt32();
                for (int i = 0; i <= 11; i++)
                {
                    br.ReadInt32();
                }

                Textures = new Texture2D[numTextures];
                for (int i = 0; i < numTextures; i++)
                {
                    var texType = br.ReadUInt32();
                    var colorNum = br.ReadUInt32();
                    for (int j = 0; j <= 3; j++)
                    {
                        var unk = br.ReadUInt32(); //0x3F800000
                    }
                    if (type == 0)
                    {
                        Textures[i] = new Texture2D(8, 8, TextureFormat.ARGB32, false);
                        var pixels = new Color32[64];
                        var color = cmp.GetColor((byte)colorNum);
                        for (int xy = 0; xy < 64; xy++)
                        {
                            pixels[xy] = color;
                        }
                        Textures[i].SetPixels32(pixels);
                    }
                    else if (type == 2)
                    {
                        for (int j = 0; j <= 1; j++)
                        {
                            var unk = br.ReadUInt32(); //unknown
                        }
                        var longint = br.ReadUInt32(); // 0xBFF78482
                        var currentTxNum = br.ReadUInt32();
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }

                if (type == 2)
                {
                    for (int i = 0; i < numTextures; i++)
                    {
                        var sizeX = br.ReadUInt32();
                        var sizeY = br.ReadUInt32();
                        var transparent = br.ReadUInt32(); //1 = color 0 is transparent, else 0
                        br.ReadUInt32(); // pad1
                        br.ReadUInt32(); // pad2
                        var numMipMaps = br.ReadUInt32();

                        Textures[i] = new Texture2D((int)sizeX, (int)sizeY, TextureFormat.ARGB32, true);

                        for (int j = 0; j < numMipMaps; j++)
                        {
                            var data = br.ReadBytes((int)(sizeX * sizeY));
                            if (j == 0)
                                Textures[i].SetPixels32(data.Select(x => (x == 0 && transparent == 1) ? transparentColor : cmp.GetColor(x)).ToArray());
                            sizeX /= 2;
                            sizeY /= 2;
                            if (sizeX == 0 || sizeY == 0)
                                break;
                        }
                    }
                }
            }
        }
    }

    public class CMP
    {
        public Color32[] Palette { get; set; }
        public byte[] LightLevels { get; set; }
        public byte[] Transparency { get; set; }

        public void ParseCMP(Stream dataStream)
        {
            using (var br = new BinaryReader(dataStream))
            {
                var cmpHdr = br.ReadChars(4);
                var version = br.ReadInt32();
                var transp = br.ReadInt32();
                br.ReadBytes(52);
                Palette = new Color32[256];
                for (int i = 0; i < 256; i++)
                {
                    Palette[i] = new Color32(br.ReadByte(), br.ReadByte(), br.ReadByte(), 255);
                }
                LightLevels = br.ReadBytes(256);
                if (transp != 0)
                    Transparency = br.ReadBytes(256);
            }
        }

        public Color32 GetColor(byte val)
        {
            var col = Palette[val];
            col.a = Transparency[val];
            return col;
        }
    }
}
