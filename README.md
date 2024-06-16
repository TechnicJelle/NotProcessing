# Not Processing

A C# template project that makes drawing stuff to the screen
(almost) as simple as [Processing](https://processing.org/).

This combines [Silk.NET](https://dotnet.github.io/Silk.NET/)'s
[High-Level Utilities](https://dotnet.github.io/Silk.NET/docs/hlu/)
with [SkiaSharp](https://github.com/mono/SkiaSharp) to provide a simple way to draw to the screen.

All the OpenGL is abstracted away in the [GPU](NotProcessing/GPU.cs) class,
so you can focus on drawing stuff in the [NotProcessing](NotProcessing/NotProcessing.cs) class.

This is *not* a library, but rather just a template project. You have to edit the NotProcessing.cs file itself to draw things.  
The `Canvas` property there is what you'll mostly use to do the actual drawing on.

Oh, and you'll probably also want to remove the example drawing stuff from there.

This should be cross-platform, but I've only tested it on Linux.
