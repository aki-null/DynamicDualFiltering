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

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fast blur filter with linear blur radius scale parameter.
/// 
/// This implementation uses Dual Filtering (or Dual Kawase Blur) by Marius Bjørge.
/// https://community.arm.com/cfs-file/__key/communityserver-blogs-components-weblogfiles/00-00-00-20-66/siggraph2015_2D00_mmg_2D00_marius_2D00_notes.pdf
/// 
/// However, it is not possible to linearly scale blur radius when using this technique (as far as I know).
/// Therefore, this implementation fixes the number of Dual Filtering passes, and resizes the input texture to
/// simulate blur radius scale. For example, Dual Filtering input texture is halved to double the blur radius.
///
/// Unfortunately, there is still a problem with simulating blur radius scales in a range of (0.0, 1.0).
/// This implementation approximates such range by alpha blending the blur result, which looks good enough for a small
/// iteration configuration (such as 2).
/// </summary>
public class DynamicDualFilteringBlur
{
    private readonly struct Resolution
    {
        public readonly int Width;
        public readonly int Height;

        public float XTexelSize => Width <= 0 ? 0.0f : 1.0f / Width;
        public float YTexelSize => Height <= 0 ? 0.0f : 1.0f / Height;

        public Resolution(float width, float height)
        {
            Width = Mathf.CeilToInt(width);
            Height = Mathf.CeilToInt(height);
        }
    }

    private int _fixedBlurScale;
    private float _referenceHeightRcp;

    private readonly List<Resolution> _baseDownsamplePasses = new List<Resolution>();
    private readonly List<Resolution> _blurDownsamplePasses = new List<Resolution>();

    private static readonly int OpacityPropertyID = Shader.PropertyToID("_Opacity");
    private static readonly int DstTexelSizePropertyID = Shader.PropertyToID("_DstTexelSize");

    private enum BlurShaderPass
    {
        DownSample = 0,
        UpSample = 1,
        UpSampleOpacity = 2
    }

    private const int DefaultReferenceHeight = 1024;
    private const int DefaultIterations = 2;

    private RenderTexture _tmpTex = null;

    public DynamicDualFilteringBlur(int iterations = DefaultIterations, int referenceHeight = DefaultReferenceHeight)
    {
        Configure(iterations, referenceHeight);
    }

    public void Configure(int iterations = DefaultIterations, int referenceHeight = DefaultReferenceHeight)
    {
        // Fallbacks
        if (referenceHeight <= 0)
        {
            referenceHeight = DefaultReferenceHeight;
        }

        if (iterations <= 0)
        {
            iterations = DefaultReferenceHeight;
        }

        _referenceHeightRcp = 1.0f / referenceHeight;
        _fixedBlurScale = 1;
        for (; iterations > 0; --iterations)
        {
            _fixedBlurScale *= 2;
        }
    }

    private void PrepareDownsamplePasses(int width, int height, float scale, ICollection<Resolution> passes)
    {
        passes.Clear();
        var currentWidth = (float)width;
        var currentHeight = (float)height;

        while (scale > 1.0f)
        {
            // Halve the texture size at maximum
            var currentScale = Mathf.Min(scale, 2.0f);
            var currentRatio = 1.0f / currentScale;
            currentWidth *= currentRatio;
            currentHeight *= currentRatio;
            passes.Add(new Resolution(currentWidth, currentHeight));
            scale *= currentRatio;
        }
    }

    private void RenderSwap(ref RenderTexture src, Resolution targetRes, Material mat, int pass = 0)
    {
        var next = RenderTexture.GetTemporary(targetRes.Width, targetRes.Height, 0);
        if (mat == null)
        {
            Graphics.Blit(src, next);
        }
        else
        {
            Graphics.Blit(src, next, mat, pass);
        }

        RenderTexture.ReleaseTemporary(_tmpTex);
        _tmpTex = next;
        src = next;
    }

    private void DualFilteringBlur(Material blurMat, RenderTexture src, float opacity)
    {
        PrepareDownsamplePasses(src.width, src.height, _fixedBlurScale, _blurDownsamplePasses);

        // Downsample
        for (var i = 0; i < _blurDownsamplePasses.Count; ++i)
        {
            var targetRes = _blurDownsamplePasses[i];
            blurMat.SetVector(DstTexelSizePropertyID,
                new Vector4(targetRes.XTexelSize, targetRes.YTexelSize, 0, 0));
            RenderSwap(ref src, targetRes, blurMat, (int)BlurShaderPass.DownSample);
        }

        // Upsample
        for (var i = _blurDownsamplePasses.Count - 2; i >= 0; --i)
        {
            var targetRes = _blurDownsamplePasses[i];
            blurMat.SetVector(DstTexelSizePropertyID,
                new Vector4(targetRes.XTexelSize, targetRes.YTexelSize, 0, 0));
            RenderSwap(ref src, targetRes, blurMat, (int)BlurShaderPass.UpSample);
        }

        PrepareBlurFinalPassTexture();
        var dst = _currentBlurFinalPassTexture;

        // Final blur pass
        blurMat.SetVector(DstTexelSizePropertyID, new Vector4(1f / dst.width, 1f / dst.height, 0, 0));
        if (opacity < 1.0f)
        {
            blurMat.SetFloat(OpacityPropertyID, opacity);
            Graphics.Blit(src, dst, blurMat, (int)BlurShaderPass.UpSampleOpacity);
        }
        else
        {
            Graphics.Blit(src, dst, blurMat, (int)BlurShaderPass.UpSample);
        }

        RenderTexture.ReleaseTemporary(_tmpTex);
        _tmpTex = null;
    }
    
    private RenderTexture _currentBlurFinalPassTexture;

    private void PrepareBlurFinalPassTexture()
    {
        if (_currentBlurFinalPassTexture != null) return;
        
        var targetSize = _baseDownsamplePasses[_baseDownsamplePasses.Count - 1];
        _currentBlurFinalPassTexture = RenderTexture.GetTemporary(targetSize.Width, targetSize.Height, 0);
    }

    public void Execute(Material blurMat, RenderTexture src, RenderTexture dst, float blurSize)
    {
        if (blurMat == null) return;

        var currentSize = Mathf.Max(0.0f, blurSize);
        if (currentSize == 0.0f) return;

        currentSize *= src.height * _referenceHeightRcp;

        if (currentSize <= 1.0f)
        {
            // Size less than 1 is handled by fading the result of fixed Dual Filtering
            _currentBlurFinalPassTexture = dst;
            DualFilteringBlur(blurMat, src, currentSize);
            _currentBlurFinalPassTexture  = null;
        }
        else
        {
            // Downsample to scale, apply, upscale
            PrepareDownsamplePasses(src.width, src.height, currentSize, _baseDownsamplePasses);

            // Downsample
            for (var i = 0; i < _baseDownsamplePasses.Count; ++i)
            {
                RenderSwap(ref src, _baseDownsamplePasses[i], null);
            }

            // We don't allocate the final pass texture just yet.
            // The source texture will be released after the first pass in the blur process, making it available
            // to be reused for the final pass.
            // It is important to delay temporary render texture allocation until we really need it.
            _currentBlurFinalPassTexture = null;
            DualFilteringBlur(blurMat, src, 1);
            // The final pass texture was automatically created by the blur process
            src = _tmpTex = _currentBlurFinalPassTexture;
            _currentBlurFinalPassTexture  = null;

            // Upscale
            for (var i = _baseDownsamplePasses.Count - 2; i >= 0; --i)
            {
                RenderSwap(ref src, _baseDownsamplePasses[i], null);
            }

            Graphics.Blit(src, dst);
            RenderTexture.ReleaseTemporary(_tmpTex);
            _tmpTex = null;
        }
    }
}