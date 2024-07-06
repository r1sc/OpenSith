using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Sith.Content
{
    public enum ColorMode : UInt32
    {
        Indexed = 0,
        RGB     = 1,
        RGBA    = 2
    }

    public enum MatType : UInt32
    {
        Color   = 0,
        Texture = 2
    };

    public class MAT : ISithAsset
    {
        const int ColorTexWidth  = 64;
        const int ColorTexHeight = 64;
        const int ColorTexSize   = ColorTexWidth * ColorTexHeight;

        public string Name { get; set; }
        public ColorMode ColorMode { get; set; }
        public Texture2D[] Textures { get; set; }

        public void Load(string name, Stream dataStream, CMP cmp)
        {
            Name = name;
            using (var br = new BinaryReader(dataStream))
            {
                var matHdr = br.ReadInt32();
                if (matHdr != 0x2054414D) // 'MAT '
                {
                    throw new Exception("Unknown or invalid MAT file");
                }

                var version = br.ReadInt32();
                var type    = (MatType)br.ReadInt32();
                if (type != MatType.Color && type != MatType.Texture)
                {
                    throw new Exception($"Unsupported MAT file type: {type}");
                }

                var celCount = br.ReadInt32();
                var texCount = br.ReadInt32();

                var cf = new ColorFormat
                {
                    Mode     = (ColorMode)br.ReadUInt32(),
                    BPP      = br.ReadUInt32(),
                    RedBPP   = br.ReadUInt32(),
                    GreenBPP = br.ReadUInt32(),
                    BlueBPP  = br.ReadUInt32(),
                    RedShl   = br.ReadUInt32(),
                    GreenShl = br.ReadUInt32(),
                    BlueShl  = br.ReadUInt32(),
                    RedShr   = br.ReadUInt32(),
                    GreenShr = br.ReadUInt32(),
                    BlueShr  = br.ReadUInt32(),
                    AlphaBPP = br.ReadUInt32(),
                    AlphaShl = br.ReadUInt32(),
                    AlphaShr = br.ReadUInt32()
                };

                if (cf.BPP % 8 != 0 && cf.BPP <= 32) 
                {
                    throw new Exception("MAT file contains invalid BPP size");
                }

                if (cf.Mode < ColorMode.Indexed || cf.Mode > ColorMode.RGBA)
                {
                    throw new Exception("MAT file contains invalid color mode");
                }
                ColorMode = cf.Mode;

                Textures = new Texture2D[celCount];
                for (int i = 0; i < celCount; i++)
                {
                    var texType  = br.ReadUInt32();
                    var colorNum = br.ReadUInt32();
                    for (int j = 0; j < 4; j++)
                    {
                        var unk = br.ReadUInt32(); //0x3F800000
                    }

                    if (type == MatType.Color)
                    {
                        Textures[i] = new Texture2D(ColorTexWidth, ColorTexHeight);
                        var pixels  = new Color32[ColorTexSize];
                        var color   = cmp.GetColor((byte)colorNum);
                        for (int xy = 0; xy < ColorTexSize; xy++)
                        {
                            pixels[xy] = color;
                        }
                        Textures[i].SetPixels32(pixels);
                    }
                    else if (type == MatType.Texture)
                    {
                        for (int j = 0; j <= 1; j++)
                        {
                            var unk = br.ReadUInt32(); //unknown
                        }
                        var longint = br.ReadUInt32(); // 0xBFF78482
                        var currentTxNum = br.ReadUInt32();
                    }
                }

                if (type == MatType.Texture)
                {
                    var transparentColor = new Color32(0, 0, 0, 0);
                    for (int i = 0; i < texCount; i++)
                    {
                        var width       = br.ReadUInt32();
                        var height      = br.ReadUInt32();
                        var transparent = br.ReadUInt32(); //1 = color 0 is transparent, else 0
                        br.ReadUInt32(); // Could be alpha color num in case of transparent texture
                        br.ReadUInt32(); // pad2
                        var mipmapLevels = br.ReadUInt32();
                        int pixSize = (int)cf.BPP /8;

                        for (int j = 0; j < mipmapLevels; j++)
                        {
                            int texSize = (int)(width * height * pixSize);
                            if (j == 0)
                            {
                                var tex = new Texture2D((int)width, (int)height);
                                Textures[i] = tex;
                                tex.alphaIsTransparency = (cf.Mode == ColorMode.RGBA);
                                if (cf.Mode == ColorMode.Indexed)
                                {
                                    var data = br.ReadBytes(texSize);
                                    tex.SetPixels32(data.Select(x => (x == 0 && transparent == 1) ? transparentColor : cmp.GetColor(x)).ToArray());
                                }
                                else
                                {
                                    int padSize = 0;
                                    if (pixSize < 4)
                                    {
                                        padSize = 4 - pixSize;
                                    }
                                    var data = new byte[texSize + padSize];
                                    br.Read(data, 0, texSize);

                                    var pixdata = new Color32[width * height];
                                    var stride  = pixSize * width;

                                    double invPixSize = 1 / (double)pixSize;
                                    Parallel.For(0, height, y =>  // Note, parallel for loop is not much faster than sequential for loop (1 sec diff, loading 03_shs.ndy)
                                    {
                                        var strideRow = y * stride;
                                        var row = y * width;
                                        for (int x = 0; x < stride; x += pixSize)
                                        {
                                            var pix = BitConverter.ToInt32(data, (int)(strideRow + x));
                                            pixdata[((int)(x * invPixSize) + row)] = DecodePixel(pix, cf);
                                        }
                                    });

                                    tex.SetPixels32(pixdata);
                                }
                                tex.Apply();
                            }
                            else
                            {
                                br.BaseStream.Position += texSize;
                            }

                            width  >>= 1;
                            height >>= 1;
                            if (width == 0 || height == 0)
                                break;
                        }
                    }
                }
            }
        }

        struct ColorFormat
        {
            public ColorMode Mode { get; set; }
            public uint BPP { get; set; } // bits per pixel

            public uint RedBPP { get; set; }
            public uint GreenBPP { get; set; }
            public uint BlueBPP { get; set; }

            public uint RedShl { get; set; }
            public uint GreenShl { get; set; }
            public uint BlueShl { get; set; }

            public uint RedShr { get; set; }
            public uint GreenShr { get; set; }
            public uint BlueShr { get; set; }

            public uint AlphaBPP { get; set; }
            public uint AlphaShl { get; set; }
            public uint AlphaShr { get; set; }
        }

        static uint GetColorMask(uint bpc)
        {
            return 0xFFFFFFFF >> (32 - (int)bpc);
        }

        static Color32 DecodePixel(int p, ColorFormat cf)
        {
            int r = ((p >> (int)cf.RedShl) & (int)GetColorMask(cf.RedBPP)) << (int)cf.RedShr;
            int g = ((p >> (int)cf.GreenShl) & (int)GetColorMask(cf.GreenBPP)) << (int)cf.GreenShr;
            int b = ((p >> (int)cf.BlueShl) & (int)GetColorMask(cf.BlueBPP)) << (int)cf.BlueShr;
            int a = 255;
            if (cf.AlphaBPP > 0)
            {
                a = ((p >> (int)cf.AlphaShl) & (int)GetColorMask(cf.AlphaBPP)) << (int)cf.AlphaShr;
                if (cf.AlphaBPP == 1)
                {
                    a = a > 0 ? 255 : 0;
                }
            }
            return new Color32((byte)r, (byte)g, (byte)b, (byte)a); // TODO: clamp to 0-255 before casting to byte
        }
    }

    public class CMP : ISithAsset
    {
        public string Name { get; set; }
        public Color32[] Palette { get; set; }
        public List<byte[]> LightLevels { get; set; }
        public List<byte[]> Transparency { get; set; }

        public void Load(string name, Stream dataStream)
        {
            Name = name;
            using var br = new BinaryReader(dataStream);
            var cmpHdr   = br.ReadChars(4);
            var version  = br.ReadInt32();
            var flags    = br.ReadInt32();
            br.ReadBytes(52); // Skip Tint and unknown parts

            Palette = new Color32[256];
            for (int i = 0; i < 256; i++)
            {
                Palette[i] = new Color32(br.ReadByte(), br.ReadByte(), br.ReadByte(), 255);
            }

            LightLevels = new List<byte[]>(64);
            for (int i = 0; i < 64; i++)
                LightLevels.Add(br.ReadBytes(256));

            if ((flags & 0x04) != 0)
            {
                // Don't know how to parse
            }

            if ((flags & 0x01) != 0)
            {
                Transparency = new List<byte[]>(256);
                for (int i = 0; i < 256; i++)
                    Transparency.Add(br.ReadBytes(256));

            }
        }

        public Color32 GetColor(byte val)
        {
            var col = Palette[val];
            return col;
        }
    }
}
