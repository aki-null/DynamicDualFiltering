// MIT License
//
// Copyright (c) 2021 Akihiro Noguchi
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using UnityEngine;

[ExecuteAlways, ImageEffectAllowedInSceneView]
public class CameraDualFilteringBlur : MonoBehaviour
{
    [HideInInspector, SerializeField] private Shader blurShader;

    [SerializeField, Range(0f, 16.0f)] private float blurSize = 1;

    public float BlueSize
    {
        get => blurSize;
        set => blurSize = Mathf.Max(0.0f, value);
    }

    [SerializeField, Range(1, 4)] private int iterations = 2;

    [SerializeField] private int referenceHeight = 1024;

    private readonly DynamicDualFilteringBlur _filteringBlur = new DynamicDualFilteringBlur();

    private Material _blurMat;

    private void Awake()
    {
        _filteringBlur.Configure(iterations, referenceHeight);
    }

    private void OnPostRender()
    {
        if (_blurMat == null)
        {
            _blurMat = new Material(blurShader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        var src = RenderTexture.active;
        var dst = RenderTexture.active;
#if UNITY_EDITOR
        // For quick parameter adjustments
        _filteringBlur.Configure(iterations, referenceHeight);
#endif
        _filteringBlur.Execute(_blurMat, src, dst, blurSize);
    }

    private void OnDestroy()
    {
        if (_blurMat == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(_blurMat);
            return;
        }
#endif
        Destroy(_blurMat);
    }
}