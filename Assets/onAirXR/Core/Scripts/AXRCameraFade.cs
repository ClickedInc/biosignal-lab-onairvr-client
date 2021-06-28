/***********************************************************

  Copyright (c) 2017-present Clicked, Inc.

  Licensed under the license found in the LICENSE file 
  in the Docs folder of the distributed package.

 ***********************************************************/

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AXRCameraFade : MonoBehaviour {
    private static Dictionary<string, List<AXRCameraFade>> _cameraFades = new Dictionary<string, List<AXRCameraFade>>();

    private static void addCameraFade(AXRCameraFade fade) {
        if (_cameraFades.ContainsKey(fade._tag) == false) {
            _cameraFades.Add(fade._tag, new List<AXRCameraFade>());
        }
        _cameraFades[fade._tag].Add(fade);
    }

    private static void removeCameraFade(AXRCameraFade fade) {
        if (_cameraFades.ContainsKey(fade._tag) == false) { return; }

        _cameraFades[fade._tag].Remove(fade);

        if (_cameraFades[fade._tag].Count == 0) {
            _cameraFades.Remove(fade._tag);
        }
    }

    public static async Task FadeAllCameras(string tag, int layer, Color from, Color to, float duration) {
        if (_cameraFades.ContainsKey(tag) == false) { return; }

        await Task.WhenAll(_cameraFades[tag].Select(fade => fade.fadeTask(layer, from, to, duration)));
    }

    public static void FadeAllCamerasImmediately(string tag, int layer, Color color) {
        if (_cameraFades.ContainsKey(tag) == false) { return; }

        foreach (var cameraFade in _cameraFades[tag]) {
            cameraFade.FadeImmediately(layer, color);
        }
    }

    private Material _fadeMaterial;
    private Dictionary<int, Fader> _faders = new Dictionary<int, Fader>();

    [SerializeField] string _tag = "default";

    public bool isFading {
        get {
            foreach (var fader in _faders.Values) {
                if (fader.fading) {
                    return true;
                }
            }
            return false;
        }
    }

    public async void Fade(int layer, Color from, Color to, float duration) {
        await fadeTask(layer, from, to, duration);
    }

    public void FadeImmediately(int layer, Color color) {
        var shouldUpdateImmediately = isFading == false;

        _faders[layer] = new Fader(color);

        if (shouldUpdateImmediately) {
            _fadeMaterial.color = updateFadeColors(Time.realtimeSinceStartup);
        }
    }

    private void Awake() {
        _fadeMaterial = new Material(Shader.Find("onAirXR/Unlit transparent color"));
        _fadeMaterial.color = Color.clear;

        addCameraFade(this);
    }

    private void OnDestroy() {
        foreach (var fader in _faders.Values) {
            fader.Cancel();
        }
        _faders.Clear();

        removeCameraFade(this);
    }

    private void OnPostRender() {
        if (_fadeMaterial.color != Color.clear) {
            _fadeMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadOrtho();
            GL.Color(_fadeMaterial.color);
            GL.Begin(GL.QUADS);
            GL.Vertex3(0.0f, 0.0f, -1.0f);
            GL.Vertex3(0.0f, 1.0f, -1.0f);
            GL.Vertex3(1.0f, 1.0f, -1.0f);
            GL.Vertex3(1.0f, 0.0f, -1.0f);
            GL.End();
            GL.PopMatrix();
        }
    }

    private async Task fadeTask(int layer, Color from, Color to, float duration) {
        if (isFading) {
            _faders[layer] = new Fader(from, to, duration);
            return;
        }

        _faders[layer] = new Fader(from, to, duration);

        do {
            _fadeMaterial.color = updateFadeColors(Time.realtimeSinceStartup);

            await Task.Yield();
        } while (isFading);
    }

    private Color updateFadeColors(float time) {
        var color = Color.clear;

        var layers = new List<int>(_faders.Keys);
        layers.Sort();

        foreach (var key in layers) {
            _faders[key].Update(time);

            var over = _faders[key].color;
            var col = over + color * (1 - over.a);
            var alpha = over.a + color.a * (1 - over.a);

            color = col;
            color.a = alpha;
        }
        return color;
    }

    private class Fader {
        private State _state = State.Ready;
        private Color _colorFrom;
        private Color _colorTo;
        private float _startTime;
        private float _duration;

        public Color color { get; private set; }
        public bool fading => _state != State.Done;

        public Fader(Color from, Color to, float duration) {
            _colorFrom = from;
            _colorTo = to;
            _duration = duration;
        }

        public Fader(Color color) {
            _colorFrom = _colorTo = color;
            _duration = 0;
    }

        public void Update(float realtime) {
            switch (_state) {
                case State.Ready:
                    color = _colorFrom;
                    _startTime = realtime;
                    _state = _duration > 0 ? State.Fading : State.Done;
                    break;
                case State.Fading:
                    color = Color.Lerp(_colorFrom, _colorTo, (realtime - _startTime) / _duration);

                    if (color == _colorTo) {
                        _state = State.Done;
                    }
                    break;
            }
        }

        public void Cancel() {
            _state = State.Done;
            }

        private enum State {
            Ready,
            Fading,
            Done
        }
    }
}
