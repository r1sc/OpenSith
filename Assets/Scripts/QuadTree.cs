using System;
using System.Collections.Generic;
using jksharp.jklviewer.JKL;
using UnityEngine;

class QuadTreeNode
{
    const int MaxDepth = 5;

    public List<Sector> Sectors { get; set; } = new List<Sector>();

    public List<QuadTreeNode> Children { get; set; } = new List<QuadTreeNode>();

    public Bounds BoundingBox { get; set; }

    private bool FitsBox(Bounds other)
    {
        return BoundingBox.Contains(other.min) && BoundingBox.Contains(other.max);
    }

    public bool AddSector(Sector sector, int depthCounter)
    {
        if (depthCounter > MaxDepth)
            return false;

        if (!FitsBox(sector.BoundingBox))
        {
            return false;
        }

        if (Children.Count == 0)
        {
            // Subdivide            
            var boxSize = BoundingBox.size / 2.0f;
            var centerStart = BoundingBox.center - (boxSize / 2.0f);

            for (var cz = 0; cz < 2; cz++)
            {
                for (var cy = 0; cy < 2; cy++)
                {
                    for (var cx = 0; cx < 2; cx++)
                    {
                        var center = centerStart + new Vector3(cx * boxSize.x, cy * boxSize.y, cz * boxSize.z);
                        Children.Add(new QuadTreeNode { BoundingBox = new Bounds(center, boxSize) });
                    }
                }
            }
        }


        foreach (var child in Children)
        {
            if (child.AddSector(sector, depthCounter + 1))
                return true;
        }

        Sectors.Add(sector);
        return true;
    }

    public List<Sector> GetSectorsContaining(Vector3 pos)
    {

        var results = new List<Sector>();
        foreach (var child in Children)
        {
            if (child.BoundingBox.Contains(pos))
            {
                var childSectors = child.GetSectorsContaining(pos);
                results.AddRange(childSectors);
            }
        }

        foreach (var sector in Sectors)
        {
            if (sector.BoundingBox.Contains(pos))
                results.Add(sector); // as good as any
        }

        return results; // no sector found
    }

    public void Draw()
    {
        Gizmos.DrawWireCube(BoundingBox.center * 10, BoundingBox.size * 10);
        Gizmos.color = Color.white;
        foreach (var child in Children)
        {
            child.Draw();
        }
    }
}

public class QuadTree
{
    private QuadTreeNode _root;

    public QuadTree(Bounds boundingBox)
    {
        _root = new QuadTreeNode { BoundingBox = boundingBox };
    }

    public void AddSector(Sector sector)
    {
        _root.AddSector(sector, 0);
    }


    public List<Sector> GetSectorsContaining(Vector3 pos)
    {
        return _root.GetSectorsContaining(pos);
    }

    public void Draw()
    {
        Gizmos.color = Color.red;
        _root.Draw();
    }
}