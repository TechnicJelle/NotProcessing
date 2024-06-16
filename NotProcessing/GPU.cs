using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using SkiaSharp;

namespace NotProcessing;

public class GPU
{
	private GL GL { get; } //Can be made public if you wanna do funky stuff.

	private readonly uint _vao;
	private readonly uint _vbo;
	private readonly uint _ebo;
	private readonly uint _shaderProgram;

	private readonly uint _texture;

	private SKBitmap _bitmap;
	private SKCanvas _canvas;

	private bool _canvasDirty;

	/// <summary>
	/// Any time this is accessed, the canvas will be marked as dirty,
	/// which means the bitmap data will be re-uploaded to the GPU.
	/// </summary>
	public SKCanvas Canvas
	{
		get
		{
			_canvasDirty = true;
			return _canvas;
		}
	}

	private const string VERTEX_CODE = @"
#version 330 core
layout (location = 0) in vec2 aPos;
layout (location = 1) in vec2 aTexCoord;

out vec2 TexCoord;

void main()
{
	gl_Position = vec4(aPos, 0.0, 1.0);
	TexCoord = aTexCoord;
}
";
	private const string FRAGMENT_CODE = @"
#version 330 core
in vec2 TexCoord;

uniform sampler2D uTexture;

out vec4 FragColour;

void main()
{
	FragColour = texture(uTexture, TexCoord);
}
";

	private static readonly float[] Vertices =
	[
		// positions // texture coords
		1f, 1f, 1.0f, 0.0f, // right top
		1f, -1f, 1.0f, 1.0f, // right bottom
		-1f, -1f, 0.0f, 1.0f, // left bottom
		-1f, 1f, 0.0f, 0.0f, // left top
	];

	private static readonly uint[] Indices =
	[
		0, 1, 3, // first triangle
		1, 2, 3, // second triangle
	];

	public unsafe GPU(IWindow window)
	{
		GL = window.CreateOpenGL();

		_bitmap = new SKBitmap(window.Size.X, window.Size.Y);
		_canvas = new SKCanvas(_bitmap);
		_canvasDirty = false;

		// Setup Vertex Data
		{
			// Set up Vertex Data (and Buffer(s)) and Configure Vertex Attributes
			_vao = GL.GenVertexArray();
			_vbo = GL.GenBuffer();
			_ebo = GL.GenBuffer();

			// bind the Vertex Array Object first, then bind and set vertex buffer(s), and then configure vertex attributes(s).
			GL.BindVertexArray(_vao);

			//Set up the VBO
			GL.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
			fixed (float* buf = Vertices)
			{
				GL.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(Vertices.Length * sizeof(float)), buf, BufferUsageARB.StaticDraw);
			}

			//Set up the EBO
			GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
			fixed (uint* buf = Indices)
			{
				GL.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(Indices.Length * sizeof(uint)), buf, BufferUsageARB.StaticDraw);
			}

			const uint stride = (2 * sizeof(float)) + (2 * sizeof(float));

			const uint positionLoc = 0;
			GL.VertexAttribPointer(positionLoc, 2, VertexAttribPointerType.Float, false, stride, (void*)0);
			GL.EnableVertexAttribArray(positionLoc);

			const uint textureLoc = 1;
			GL.VertexAttribPointer(textureLoc, 2, VertexAttribPointerType.Float, false, stride, (void*)(2 * sizeof(float)));
			GL.EnableVertexAttribArray(textureLoc);

			//Unbind
			GL.BindVertexArray(0);
			GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
			GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
		}

		// Setup Shader
		_shaderProgram = CreateShader(VERTEX_CODE, FRAGMENT_CODE);

		// Setup Texture
		{
			_texture = GL.GenTexture();
			GL.ActiveTexture(TextureUnit.Texture0);

			GL.BindTexture(TextureTarget.Texture2D, _texture);
			UploadTexture();

			//set texture filtering parameters
			GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			//set texture wrapping parameters
			GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameterI(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
			GL.BindTexture(TextureTarget.Texture2D, 0);

			//Set the texture uniform in the shader
			int location = GL.GetUniformLocation(_shaderProgram, "uTexture");
			GL.Uniform1(location, 0);
		}

		//Transparent textures
		{
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
		}

		GL.ClearColor(0.0f, 0.0f, 0.0f, 1f);
	}

	private unsafe void UploadTexture()
	{
		GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba,
			(uint)_bitmap.Width, (uint)_bitmap.Height, 0,
			PixelFormat.Bgra, PixelType.UnsignedByte, _bitmap.GetPixels().ToPointer());
	}

	private uint CreateShader(string vertexCode, string fragmentCode)
	{
		uint vertexShader = GL.CreateShader(ShaderType.VertexShader);
		GL.ShaderSource(vertexShader, vertexCode);
		GL.CompileShader(vertexShader);

		string infoLog = GL.GetShaderInfoLog(vertexShader);
		if (!string.IsNullOrWhiteSpace(infoLog))
		{
			Console.WriteLine($"Error compiling vertex shader {infoLog}");
		}

		uint fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
		GL.ShaderSource(fragmentShader, fragmentCode);
		GL.CompileShader(fragmentShader);

		infoLog = GL.GetShaderInfoLog(fragmentShader);
		if (!string.IsNullOrWhiteSpace(infoLog))
		{
			Console.WriteLine($"Error compiling fragment shader {infoLog}");
		}

		uint shaderProgram = GL.CreateProgram();
		GL.AttachShader(shaderProgram, vertexShader);
		GL.AttachShader(shaderProgram, fragmentShader);
		GL.LinkProgram(shaderProgram);

		GL.GetProgram(shaderProgram, GLEnum.LinkStatus, out int status);
		if (status == 0)
		{
			Console.WriteLine($"Error linking shader {GL.GetProgramInfoLog(shaderProgram)}");
		}

		GL.DetachShader(shaderProgram, vertexShader);
		GL.DetachShader(shaderProgram, fragmentShader);
		GL.DeleteShader(vertexShader);
		GL.DeleteShader(fragmentShader);

		return shaderProgram;
	}

	public unsafe void Render()
	{
		GL.Clear(ClearBufferMask.ColorBufferBit);

		GL.BindVertexArray(_vao);
		GL.UseProgram(_shaderProgram);

		GL.ActiveTexture(TextureUnit.Texture0);
		GL.BindTexture(TextureTarget.Texture2D, _texture);

		if (_canvasDirty)
		{
			UploadTexture();
			_canvasDirty = false;
		}

		GL.DrawElements(PrimitiveType.Triangles, (uint)Indices.Length, DrawElementsType.UnsignedInt, null);

		//Unbind
		GL.BindVertexArray(0);
		GL.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
		GL.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
		GL.BindTexture(TextureTarget.Texture2D, 0);
	}

	public void OnFramebufferResize(Vector2D<int> newSize)
	{
		GL.Viewport(newSize);
		_bitmap.Dispose();
		_canvas.Dispose();
		_bitmap = new SKBitmap(newSize.X, newSize.Y);
		_canvas = new SKCanvas(_bitmap);
		_canvasDirty = true;
	}

	public void Cleanup()
	{
		GL.DeleteBuffer(_vbo);
		GL.DeleteBuffer(_ebo);
		GL.DeleteVertexArray(_vao);
		GL.DeleteProgram(_shaderProgram);
	}
}
