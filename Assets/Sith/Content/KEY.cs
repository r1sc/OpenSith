using Assets.Sith;
using Assets.Sith.Utility;
using System;
using System.IO;
using UnityEngine;

namespace Assets.Sith.Content
{
    class KEY
    {
        public string Name { get; private set; }

        private string _line;
        private string[] _args;

        private void ReadLine(in StreamReader sr)
        {
            while (!sr.EndOfStream)
            {
                _line = sr.ReadLine();
                if (_line.StartsWith("#") || _line.Trim() == "")
                    continue;
                _args = _line.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                break;
            }
        }

        public AnimationClip Load(string filename, Stream dataStream, Model3DO model, Transform root)
        {
            var animationClip = new AnimationClip
            {
                name = filename
            };
            Name = filename;// Path.GetFileNameWithoutExtension(filename);

            string section = "";
            int numFrames = 0;
            float fps = 1.0f;
            int currentNodeIdx = 0;
            string currentMeshName = null;
            int numEntries;
            AnimationCurve curveX, curveY, curveZ;
            AnimationCurve curveXR, curveYR, curveZR, curveWR;
            curveX = curveY = curveZ = curveXR = curveYR = curveZR = curveWR = null;

            using (var sr = new StreamReader(dataStream))
            {
                while (!sr.EndOfStream)
                {
                    ReadLine(sr);

                    if (_line == "")
                        continue;
                    else if (_line.StartsWith("SECTION: "))
                    {
                        section = _line;
                    }
                    else if (section == "SECTION: HEADER")
                    {
                        switch (_args[0])
                        {
                            case "FRAMES":
                                numFrames = int.Parse(_args[1]);
                                break;
                            case "FPS":
                                fps = float.Parse(_args[1]);
                                break;
                        }
                    }
                    else if (section == "SECTION: KEYFRAME NODES")
                    {
                        if (_args[0] == "NODES")
                        {

                        }
                        else if (_args[0] == "NODE")
                        {
                            if (currentMeshName != null)
                            {
                                var transformPath = GetFullPathTo(root, model.HierarchyNodes[currentNodeIdx].Transform);
                                //var transformPath = GetFullPathTo(root, root.transform.Find(currentMeshName));
                                animationClip.SetCurve(transformPath, typeof(Transform), "localPosition.x", curveX);
                                animationClip.SetCurve(transformPath, typeof(Transform), "localPosition.y", curveY);
                                animationClip.SetCurve(transformPath, typeof(Transform), "localPosition.z", curveZ);
                                animationClip.SetCurve(transformPath, typeof(Transform), "localRotation.x", curveXR);
                                animationClip.SetCurve(transformPath, typeof(Transform), "localRotation.y", curveYR);
                                animationClip.SetCurve(transformPath, typeof(Transform), "localRotation.z", curveZR);
                                animationClip.SetCurve(transformPath, typeof(Transform), "localRotation.w", curveWR);
                            }

                            currentNodeIdx = int.Parse(_args[1]);
                            curveX = new AnimationCurve();
                            curveY = new AnimationCurve();
                            curveZ = new AnimationCurve();
                            curveXR = new AnimationCurve();
                            curveYR = new AnimationCurve();
                            curveZR = new AnimationCurve();
                            curveWR = new AnimationCurve();

                            curveX.postWrapMode = WrapMode.Loop;
                            curveY.postWrapMode = WrapMode.Loop;
                            curveZ.postWrapMode = WrapMode.Loop;
                            curveXR.postWrapMode = WrapMode.Loop;
                            curveYR.postWrapMode = WrapMode.Loop;
                            curveZR.postWrapMode = WrapMode.Loop;
                        }
                        else if (_args[0] == "MESH" && _args[1] == "NAME")
                        {
                            currentMeshName = _args[2];
                        }
                        else if (_args[0] == "ENTRIES")
                        {
                            numEntries = int.Parse(_args[1]);
                        }
                        else
                        {
                            // var entryNum = int.Parse(_args[0].Replace(":", ""));
                            var frame = int.Parse(_args[1]);
                            var flags = _args[2];
                            var x = float.Parse(_args[3]);
                            var y = float.Parse(_args[4]);
                            var z = float.Parse(_args[5]);
                            var pitch = float.Parse(_args[6]);
                            var yaw   = float.Parse(_args[7]);
                            var roll  = float.Parse(_args[8]);

                            curveX.AddKey(frame / fps, x);
                            curveY.AddKey(frame / fps, z);
                            curveZ.AddKey(frame / fps, y);

                            //if (curveX.length == 1)
                            //{
                            //    curveX.AddKey(numFrames / fps, x);
                            //    curveY.AddKey(numFrames / fps, z);
                            //    curveZ.AddKey(numFrames / fps, y);
                            //}

                            var quaternion = Utils.FromMeshOrientation(pitch, yaw, roll);

                            curveXR.AddKey(frame / fps, quaternion.x);
                            curveYR.AddKey(frame / fps, quaternion.y);
                            curveZR.AddKey(frame / fps, quaternion.z);
                            curveWR.AddKey(frame / fps, quaternion.w);

                            //if (curveXR.length == 1)
                            //{
                            //    curveXR.AddKey(numFrames / fps, quaternion.x);
                            //    curveYR.AddKey(numFrames / fps, quaternion.y);
                            //    curveZR.AddKey(numFrames / fps, quaternion.z);
                            //    curveWR.AddKey(numFrames / fps, quaternion.w);
                            //}

                            ReadLine(sr);

                            // Skip delta frames
                        }
                    }
                }
            }

            if (currentMeshName != null)
            {
                var transformPath = GetFullPathTo(root, model.HierarchyNodes[currentNodeIdx].Transform);
                //var transformPath = GetFullPathTo(root, root.transform.Find(currentMeshName));
                animationClip.SetCurve(transformPath, typeof(Transform), "localPosition.x", curveX);
                animationClip.SetCurve(transformPath, typeof(Transform), "localPosition.y", curveY);
                animationClip.SetCurve(transformPath, typeof(Transform), "localPosition.z", curveZ);
                animationClip.SetCurve(transformPath, typeof(Transform), "localRotation.x", curveXR);
                animationClip.SetCurve(transformPath, typeof(Transform), "localRotation.y", curveYR);
                animationClip.SetCurve(transformPath, typeof(Transform), "localRotation.z", curveZR);
                animationClip.SetCurve(transformPath, typeof(Transform), "localRotation.w", curveWR);
            }

            animationClip.legacy = true;
            animationClip.name = Name;
            animationClip.EnsureQuaternionContinuity();
            animationClip.wrapMode = WrapMode.Loop;
            return animationClip;
        }

        private string GetFullPathTo(Transform root, Transform transformToFind)
        {
            if (transformToFind == root)
                return "";

            var path = transformToFind.name;
            while (transformToFind.parent != root && transformToFind.parent != null)
            {
                path = transformToFind.parent.name + "/" + path;
                transformToFind = transformToFind.parent;
            }
            return path;
        }

        //private string GetFullPathTo(Transform root, string meshName)
        //{
        //    if (root.name == meshName)
        //        return "";

        //    var path = transformToFind.name;
        //    while (transformToFind.parent != root && transformToFind.parent != null)
        //    {
        //        path = transformToFind.parent.name + "/" + path;
        //        transformToFind = transformToFind.parent;
        //    }
        //    return path;
        //}
    }
}