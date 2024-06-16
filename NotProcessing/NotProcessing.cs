using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using SkiaSharp;
using System.Numerics;

namespace NotProcessing;

public class NotProcessing
{
	private readonly IWindow _window;
	private GPU _gpu = null!;

	private readonly Random _random = new();
	private readonly SKFont _font;
	private readonly SKPaint _paint;

	private int Width => _window.Size.X;
	private int Height => _window.Size.Y;
	private SKCanvas Canvas => _gpu.Canvas;

	public NotProcessing()
	{
		WindowOptions options = WindowOptions.Default with
		{
			Size = new Vector2D<int>(800, 600),
			Title = "NotProcessing",
		};
		_window = Window.Create(options);

		_font = new SKFont(SKTypeface.Default, 24);
		_paint = new SKPaint
		{
			Color = new SKColor(255, 255, 255),
			IsAntialias = true,
			Style = SKPaintStyle.Fill,
		};

		_window.Load += OnLoad;
		_window.Update += OnUpdate;
		_window.Render += OnRender;
		_window.FramebufferResize += newSize => _gpu.OnFramebufferResize(newSize);
		_window.Closing += OnClose;
	}

	public void Run()
	{
		_window.Run();
	}

	private void OnLoad()
	{
		_gpu = new GPU(_window);
		IInputContext input = _window.CreateInput();

		foreach(IKeyboard kb in input.Keyboards)
		{
			kb.KeyDown += OnKeyDown;
			// kb.KeyUp
		}

		foreach(IMouse m in input.Mice)
		{
			m.MouseDown += MouseDown;
			// m.Scroll
			// m.MouseUp
		}
	}

	private void OnUpdate(double deltaTime)
	{
	}

	private void OnRender(double deltaTime)
	{
		_paint.Color = new SKColor(_paint.Color.Red, _paint.Color.Green, _paint.Color.Blue, 100);
		Canvas.DrawCircle(_random.Next(0, Width), _random.Next(0, Height), 5, _paint);

		_gpu.Render();
	}

	private void OnKeyDown(IKeyboard keyboard, Key key, int keyCode)
	{
		Console.WriteLine($"Key Down: {key} ({keyCode})");

		_paint.Color = new SKColor(_paint.Color.Red, _paint.Color.Green, _paint.Color.Blue, 255);
		Canvas.DrawText(key.ToString(), _random.Next(0, Width), _random.Next(0, Height),
			SKTextAlign.Center, _font, _paint);

		switch(key)
		{
			case Key.F1:
				Canvas.Clear();
				break;
			case Key.Escape:
				_window.Close();
				break;
		}
	}

	private void MouseDown(IMouse mouse, MouseButton button)
	{
		Vector2 position = mouse.Position;
		Console.WriteLine($"Mouse Down: {button} at {position}");

		_paint.Color = new SKColor(_paint.Color.Red, _paint.Color.Green, _paint.Color.Blue, 100);
		Canvas.DrawRect(position.X, position.Y, 50, 50, _paint);
	}

	private void OnClose()
	{
		_gpu.Cleanup();
	}
}
