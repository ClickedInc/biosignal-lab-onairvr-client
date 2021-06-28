/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using UnityEngine;

public class AirVRProfile : AirVRProfileBase {
    public AirVRProfile(VideoBitrate bitrate) : base(bitrate) {}

	private bool _userPresent;

    public override (int width, int height) eyeTextureSize {
        get {
            var desc = OVRManager.display.GetEyeRenderDesc(UnityEngine.XR.XRNode.LeftEye);

            return ((int)desc.resolution.x, (int)desc.resolution.y);
        }
    }

    public override (int width, int height) defaultVideoResolution {
        get {
            return (3520, 1946);
//#if UNITY_EDITOR || UNITY_STANDALONE
//            return (2048, 1024);
//#else
//            return (3200, 1600);
//#endif
        }
    }

    public override float defaultVideoFrameRate => AXRClientPlugin.GetOptimisticVideoFrameRate();
    public override bool stereoscopy => true;
    public override float ipd => OVRManager.profile.ipd;
    public override bool hasInput => true;
    public override RenderType renderType => RenderType.DirectOnTwoEyeTextures;
    public override bool isUserPresent => OVRManager.instance.isUserPresent;
    public override float delayToResumePlayback => 1.5f;

    public override float[] leftEyeCameraNearPlane { 
        get {
            OVRDisplay.EyeRenderDesc desc = OVRManager.display.GetEyeRenderDesc(UnityEngine.XR.XRNode.LeftEye);

            return new float[] {
                -Mathf.Tan(desc.fullFov.LeftFov / 180.0f * Mathf.PI),
                Mathf.Tan(desc.fullFov.UpFov / 180.0f * Mathf.PI),
                Mathf.Tan(desc.fullFov.RightFov / 180.0f * Mathf.PI),
                -Mathf.Tan(desc.fullFov.DownFov / 180.0f * Mathf.PI),
            };
        }
    }

    public override Vector3 eyeCenterPosition { 
        get {
            return new Vector3(0.0f, OVRManager.profile.eyeHeight - OVRManager.profile.neckHeight, OVRManager.profile.eyeDepth);
        }
    }

	public override int[] leftEyeViewport { 
		get {
			OVRDisplay.EyeRenderDesc desc = OVRManager.display.GetEyeRenderDesc(UnityEngine.XR.XRNode.LeftEye);
			return new int[] { 0, 0, (int)desc.resolution.x, (int)desc.resolution.y };
		}
	}

	public override int[] rightEyeViewport { 
		get {
			return leftEyeViewport;
		}
	}

	public override float[] videoScale {
		get {
            //OVRDisplay.EyeRenderDesc desc = OVRManager.display.GetEyeRenderDesc(UnityEngine.XR.XRNode.LeftEye);
            //return new float[] { (float)videoWidth / 2 / desc.resolution.x, (float)videoHeight / desc.resolution.y };
            return new float[] { 1.0f, 1.0f };
		}
	}
}
