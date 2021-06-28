/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using System;
using System.IO;
using UnityEngine;

#pragma warning disable 0649

[Serializable]
public class AXRClientConfig {
    [Serializable]
    public struct VideoBitrate {
        public int min;
        public int start;
        public int max;
    }

    [Serializable]
    public struct VideoResolution {
        public int width;
        public int height;
    }

    [Serializable]
    public struct Profiler {
        public int flags;
        public string tag;

        public string filename {
            get {
                if (string.IsNullOrEmpty(tag)) { return ""; }

                return Path.Combine(Application.persistentDataPath, 
                                    string.Format("{0}_{1}.frames", tag, DateTime.Now.ToString("yyyyMMddHHmmss")));
            }
        }
    }

    public string address;
    public int port;
    public string userID;
    public VideoBitrate videoBitrate;
    public VideoResolution videoResolution;
    public float videoFramerate;
    public Profiler profiler;
}

#pragma warning restore 0649
