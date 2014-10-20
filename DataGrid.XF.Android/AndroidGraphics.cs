
// TODO this file has moved
//
//
// Copyright (c) 2010-2012 Frank A. Krueger
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
using System;
using System.Collections.Generic;
using Android.Graphics;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

namespace XFGraphics.Android
{
	public class AndroidGraphics : IGraphics
	{
		ColPaints _paints;
		class ColPaints
		{
			public Paint Fill;
			public Paint Stroke;
            public Paint Text;
		}

		public AndroidGraphics (Canvas canvas)
		{
			_c = canvas;
			_offscreen = null;

            _paints = new ColPaints();

			_paints.Fill = new Paint ();
			_paints.Fill.AntiAlias = true;
			_paints.Fill.SetStyle (Paint.Style.Fill);

			_paints.Stroke = new Paint ();
			_paints.Stroke.AntiAlias = true;
			_paints.Stroke.SetStyle (Paint.Style.Stroke);

			_paints.Text = new Paint ();
			_paints.Text.AntiAlias = true;
			_paints.Text.SetStyle (Paint.Style.Fill);

			SetColor (Xamarin.Forms.Color.Black);
		}

		private struct drawcontext
		{
			public Canvas c;
			public AndroidImage img;
		};

		Canvas _c;
		AndroidImage _offscreen;
		private Stack<drawcontext> _prev;

		Xamarin.Forms.Font _font_xf;

		//public Canvas Canvas { get { return _c; } }

		private Paint.FontMetrics TextFontMetrics;

		public void BeginEntity (object entity)
		{
		}

		private static bool equal(Xamarin.Forms.Font c1, Xamarin.Forms.Font c2)
		{
			if (c1 == c2) {
				return true;
			}
			if (c1.UseNamedSize != c2.UseNamedSize) {
				return false;
			}
			if (c1.UseNamedSize) {
				if (c1.NamedSize != c2.NamedSize) {
					return false;
				}
			} else {
				if (c1.FontSize != c2.FontSize) {
					return false;
				}
			}
			if (c1.FontFamily != c2.FontFamily) {
				return false;
			}
			return true;
		}

		public void SetFont (Xamarin.Forms.Font f)
		{
			if (!equal (f, _font_xf)) 
			{
				_font_xf = f;
				_paints.Text.SetTypeface (f.ToTypeface());
				_paints.Text.TextSize = (float) f.FontSize;
				TextFontMetrics = _paints.Text.GetFontMetrics ();
			}
		}

		public void Clear (Xamarin.Forms.Color c)
		{

		}

		private static bool equal(Xamarin.Forms.Color c1, Xamarin.Forms.Color c2)
		{
			if (c1 == c2) {
				return true;
			}
			if (c1.A != c2.A) {
				return false;
			}
			if (c1.R != c2.R) {
				return false;
			}
			if (c1.G != c2.G) {
				return false;
			}
			if (c1.B != c2.B) {
				return false;
			}
			return true;
		}

		Xamarin.Forms.Color _clr_xf;

		public void SetColor (Xamarin.Forms.Color c)
		{
			if (!equal(c, _clr_xf)) 
			{
				_clr_xf = c;

				var ac = c.ToAndroid ();

				_paints.Stroke.Color = ac;
				_paints.Fill.Color = ac;
				_paints.Text.Color = ac;
			}
		}

		Path GetPolyPath (Polygon poly)
		{
			var p = poly.Tag as Path;
			if (p == null) {
				p = new Path ();
				p.MoveTo ((float) poly.Points[0].X, (float) poly.Points[0].Y);
				for (var i = 1; i < poly.Points.Count; i++) {
					var pt = poly.Points[i];
					p.LineTo ((float) pt.X, (float) pt.Y);
				}
				p.LineTo ((float) poly.Points[0].X, (float) poly.Points[0].Y);
				poly.Tag = p;
			}
			return p;
		}

		public void FillPolygon (Polygon poly)
		{
			_c.DrawPath (GetPolyPath (poly), _paints.Fill);
		}

		public void DrawPolygon (Polygon poly, float w)
		{
			_paints.Stroke.StrokeWidth = w;
			_c.DrawPath (GetPolyPath (poly), _paints.Stroke);
		}

		public void FillRoundedRect (float x, float y, float width, float height, float radius)
		{
			_c.DrawRoundRect (new RectF (x, y, x + width, y + height), radius, radius, _paints.Fill);
		}

		public void DrawRoundedRect (float x, float y, float width, float height, float radius, float w)
		{
			_paints.Stroke.StrokeWidth = w;
			_c.DrawRoundRect (new RectF (x, y, x + width, y + height), radius, radius, _paints.Stroke);
		}

		public void FillRect (float x, float y, float width, float height)
		{
			_c.DrawRect (new RectF (x, y, x + width, y + height), _paints.Fill);
		}

		public void DrawRect (float x, float y, float width, float height, float w)
		{
			_paints.Stroke.StrokeWidth = w;
			_c.DrawRect (new RectF (x, y, x + width, y + height), _paints.Stroke);
		}

		public void FillOval (float x, float y, float width, float height)
		{
			_c.DrawOval (new RectF (x, y, x + width, y + height), _paints.Fill);
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
			_paints.Stroke.StrokeWidth = w;
			_c.DrawOval (new RectF (x, y, x + width, y + height), _paints.Stroke);
		}

		const float RadiansToDegrees = (float)(180 / Math.PI);

		public void FillArc (float cx, float cy, float radius, float startAngle, float endAngle)
		{
			var sa = -startAngle * RadiansToDegrees;
			var ea = -endAngle * RadiansToDegrees;
			_c.DrawArc (new RectF (cx - radius, cy - radius, cx + radius, cy + radius), sa, ea - sa, false, _paints.Fill);
		}
		
		public void DrawArc (float cx, float cy, float radius, float startAngle, float endAngle, float w)
		{
			var sa = -startAngle * RadiansToDegrees;
			var ea = -endAngle * RadiansToDegrees;
			_paints.Stroke.StrokeWidth = w;
			_c.DrawArc (new RectF (cx - radius, cy - radius, cx + radius, cy + radius), sa, ea - sa, false, _paints.Stroke);
		}

		bool _inLines = false;
		Path _linesPath = null;
		int _linesCount = 0;
		float _lineWidth = 1;

		public void BeginLines (bool rounded)
		{
			if (!_inLines) {
				_inLines = true;
				_linesPath = new Path ();
				_linesCount = 0;
			}
		}

		public void DrawLine (float sx, float sy, float ex, float ey, float w)
		{
			if (_inLines) {
				if (_linesCount == 0) {
					_linesPath.MoveTo (sx, sy);
				}
				_linesPath.LineTo (ex, ey);
				_lineWidth = w;
				_linesCount++;
			}
			else {
				_paints.Stroke.StrokeWidth = w;
				_c.DrawLine (sx, sy, ex, ey, _paints.Stroke);
			}
		}

		public void EndLines ()
		{
			if (_inLines) {
				_inLines = false;
				_paints.Stroke.StrokeWidth = _lineWidth;
				_paints.Stroke.StrokeJoin = Paint.Join.Round;
				_c.DrawPath (_linesPath, _paints.Stroke);
				_linesPath.Dispose ();
				_linesPath = null;
			}
		}

		public void DrawImage (IImage img, float x, float y)
		{
			var dimg = img as AndroidImage;
			if (dimg != null) {
				//SetColor (Xamarin.Forms.Color.White);
				// TODO what if there is no fill set?
				_c.DrawBitmap (
					dimg.Bitmap,
					x,
					y,
					null); //_paints.Fill);
			}
		}

		public void DrawImage (IImage img, float x, float y, float width, float height)
		{
			var dimg = img as AndroidImage;
			if (dimg != null) {
				SetColor (Xamarin.Forms.Color.White);
				// TODO what if there is no fill set?
				_c.DrawBitmap (
					dimg.Bitmap,
					new Rect (0, 0, dimg.Bitmap.Width, dimg.Bitmap.Height),
					new RectF(x,y,(x+width),(y+height)),
					_paints.Fill);
			}
		}
		
		public void DrawString(string s, 
			float box_x,
			float box_y,
			float box_width,
			float box_height,
			Xamarin.Forms.LineBreakMode lineBreak, 
			Xamarin.Forms.TextAlignment horizontal_align, 
			Xamarin.Forms.TextAlignment vertical_align
		)
		{
			// TODO
			if (string.IsNullOrWhiteSpace (s)) return;

			//SetTextAlign()

			float text_width;
			float text_height;

			if (
				(horizontal_align != TextAlignment.Start)
				|| (vertical_align != TextAlignment.Start)) {

				// not all of the alignments need the bounding rect.  don't
				// calculate it if not needed.

				text_width = _paints.Text.MeasureText (s);

				text_height = - (TextFontMetrics.Ascent);

			} else {
				text_width = 0;
				text_height = 0;
			}

			//Console.WriteLine ("width: {0}    height: {1}", text_width, text_height);

			float x;

			switch (horizontal_align) {
			case Xamarin.Forms.TextAlignment.End:
				x = (box_x + box_width) - text_width;
				break;
			case Xamarin.Forms.TextAlignment.Center:
				x = box_x + (box_width - text_width) / 2;
				break;
			case Xamarin.Forms.TextAlignment.Start:
			default:
				x = box_x;
				break;
			}

			float y;

			switch (vertical_align) {
			case Xamarin.Forms.TextAlignment.End:
				y = box_y + text_height;
				break;
			case Xamarin.Forms.TextAlignment.Center:
				y = (box_y + box_height) - (box_height - text_height) / 2;
				break;
			case Xamarin.Forms.TextAlignment.Start:
			default:
				y = (box_y + box_height);
				break;
			}

			_c.DrawText (s,  x,  y, _paints.Text);
		}

		#if not
		public void DrawString(string s, float x, float y, float width, float height, LineBreakMode lineBreak, TextAlignment align)
		{
			if (string.IsNullOrWhiteSpace (s)) return;
			DrawString (s, x, y);
		}

		static AndroidFontInfo GetFontInfo (Font f)
		{
			var fi = f.Tag as AndroidFontInfo;
			if (fi == null) {
				var tf = f.IsBold ? Typeface.DefaultBold : Typeface.Default;
				fi = new AndroidFontInfo {
					Typeface = tf,
				};
				f.Tag = fi;
			}
			return fi;
		}

		void SetFontOnPaints ()
		{
			var f = _paints.Font;
			if (f == null || f != _font) {
				f = _font;
				_paints.Font = f;
				ApplyFontToPaint (f, _paints.Fill);
			}
		}

		public void DrawString (string s, float x, float y)
		{
			if (string.IsNullOrWhiteSpace (s)) return;

			SetFontOnPaints ();
			var fm = GetFontMetrics ();
			_c.DrawText (s, x, y + fm.Ascent - fm.Descent, _paints.Fill);
		}
		#endif

		#if not
		public IFontMetrics GetFontMetrics ()
		{
			SetFontOnPaints ();
			return ((AndroidFontInfo)_paints.Font.Tag).FontMetrics;
		}

		public static IFontMetrics GetFontMetrics (Font font)
		{
			var fi = GetFontInfo (font);
			if (fi.FontMetrics == null) {
				var paint = new Paint ();
				ApplyFontToPaint (font, paint); // This ensures font metrics
			}
			return fi.FontMetrics;
		}
		#endif

		public IImage ImageFromFile (string path)
		{
			var bmp = BitmapFactory.DecodeFile (path);
			if (bmp == null) return null;
			
			var dimg = new AndroidImage () {
				Bitmap = bmp
			};
			return dimg;
		}
		
		public void BeginOffscreen(float width, float height, IImage img)
		{
			if (_prev == null) {
				_prev = new Stack<drawcontext> ();
			}

			_prev.Push (new drawcontext { c = _c, img = _offscreen });

			_offscreen = null;
			if (img != null) {
				var aimg = img as AndroidImage;
				if (
					(aimg.Bitmap.Width >= width)
					&& (aimg.Bitmap.Height >= height)) 
				{
					_offscreen = img as AndroidImage;
				} else {
					img.Destroy ();
				}
			}

			if (null == _offscreen) {
				_offscreen = new AndroidImage ();
				_offscreen.Bitmap = Bitmap.CreateBitmap ((int)width, (int)height, Bitmap.Config.Rgb565); // TODO what bitmap config?
			}

			_c = new Canvas ();
			_c.SetBitmap (_offscreen.Bitmap);
			#if false
			if (img != null) {
				SetColor (Xamarin.Forms.Color.Yellow);
				FillRect (0, 0, _offscreen.Bitmap.Width, _offscreen.Bitmap.Height);
			}
			#endif
		}

		public IImage EndOffscreen()
		{
			var dimg = _offscreen;

			drawcontext ctx = _prev.Pop ();

			_offscreen = ctx.img;
			_c = ctx.c;

			return dimg;
		}

		public void SaveState()
		{
			_c.Save ();
		}
		
		public void SetClippingRect (float x, float y, float width, float height)
		{
			_c.ClipRect (x, y, x + width, y + height);
		}
		
		public void Translate(float dx, float dy)
		{
			_c.Translate (dx, dy);
		}
		
		public void Scale(float sx, float sy)
		{
			_c.Scale (sx, sy);
		}
		
		public void RestoreState()
		{
			_c.Restore ();
		}
	}

	#if not
	class AndroidFontInfo
	{
		public Typeface Typeface;
		public AndroidFontMetrics FontMetrics;
	}
	#endif

	public class AndroidImage : IImage
	{
		public Bitmap Bitmap;

		public void Destroy()
		{
			Bitmap.Recycle ();
			Bitmap.Dispose ();
			Bitmap = null;
		}
	}

	#if not
	public class AndroidFontMetrics : IFontMetrics
	{
		const int NumWidths = 128;
		float[] _widths;

		static char[] _chars;
		static AndroidFontMetrics ()
		{
			_chars = new char[NumWidths];
			for (var i = 0; i < NumWidths; i++) {
				if (i <= ' ') {
					_chars[i] = ' ';
				}
				else {
					_chars[i] = (char)i;
				}
			}
		}

		public AndroidFontMetrics (Paint paint)
		{
			_widths = new float[NumWidths];
			paint.GetTextWidths (_chars, 0, NumWidths, _widths);
			Ascent = (int)(Math.Abs (paint.Ascent ()) + 0.5f);
			Descent = (int)(paint.Descent ()/2 + 0.5f);
			Height = Ascent;
		}

		public int StringWidth (string s, int startIndex, int length)
		{
			if (string.IsNullOrEmpty (s)) return 0;

			var end = startIndex + length;
			if (end <= 0) return 0;

			var a = 0.0f;
			for (var i = startIndex; i < end; i++) {
				if (s[i] < NumWidths) {
					a += _widths[s[i]];
				}
				else {
					a += _widths[' '];
				}
			}

			return (int)(a + 0.5f);
		}

		public int Height { get; private set; }

		public int Ascent { get; private set; }

		public int Descent { get; private set; }
	}
	#endif

}
