# Dynamic Dual Filtering
This is a Unity implementation of Dual Filtering (or Dual Kawase Filtering Blur) with approximated linear radius scale parameter.

The Dual Filtering technique is fast and useful for many scenarios where blurring is needed, but it doesn't work very well when gradually blurring an image. However, this is a common scenario for any game, which makes this technique less versatile.

This is my attempt to approximate linear radius scale adjustments by mixing existing blur techniques.

<video src="https://user-images.githubusercontent.com/40129/139671749-554e902d-8c88-48c5-bd36-1b702599d1e7.mp4" controls="controls" style="max-width: 730px;" loop></video>

<details closed>
<summary>Static Image</summary>
<img src="https://raw.githubusercontent.com/aki-null/DynamicDualFiltering/assets/blur_example.png">
</details>

Usage
---
For Unity built-in pipeline, attach the `CameraDualFilteringBlur` component to a camera, and configure the parameters.

URP support is a work in progress.

License
---
```
MIT License

Copyright (c) 2021 Akihiro Noguchi

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
