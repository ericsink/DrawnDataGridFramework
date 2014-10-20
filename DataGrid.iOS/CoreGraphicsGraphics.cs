//
// Copyright (c) 2010-2014 Frank A. Krueger
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
using System.Drawing; // TODO do we need this?
using System.Collections.Generic;

#if MONOMAC
using MonoMac.CoreGraphics;
using MonoMac.AppKit;
#else
using MonoTouch.CoreGraphics;
using MonoTouch.UIKit;
#endif
using MonoTouch.CoreText;
using MonoTouch.Foundation;

namespace CrossGraphics.CoreGraphics
{
	public class CoreGraphicsGraphics : IGraphics
	{
		CGContext _c;

		//bool _highQuality = false;

		static CoreGraphicsGraphics ()
		{
			//foreach (var f in UIFont.FamilyNames) {
			//	Console.WriteLine (f);
			//	var fs = UIFont.FontNamesForFamilyName (f);
			//	foreach (var ff in fs) {
			//		Console.WriteLine ("  " + ff);
			//	}
			//}
			
			_textMatrix = CGAffineTransform.MakeScale (1, -1); // TODO what is this for?
		}

		private float _overall_height;
		private Stack<float> _prev_overall_height;

		public CoreGraphicsGraphics (CGContext c, bool highQuality, float overall_height)
		{
			if (c == null) throw new ArgumentNullException ("c");

			_overall_height = (float) overall_height;

			_c = c;
			//_highQuality = highQuality;

			if (highQuality) {
				c.SetLineCap (CGLineCap.Round);
			}
			else {
				c.SetLineCap (CGLineCap.Butt);
			}

			SetColor (Colors.Black);
		}

		public void SetColor (Color c)
		{
			var cgcol = c.GetCGColor ();
#if MONOMAC
			_c.SetFillColorWithColor (cgcol);
			_c.SetStrokeColorWithColor (cgcol);
#else
			_c.SetFillColor (cgcol);
			_c.SetStrokeColor (cgcol);
#endif
		}

		public void FillRoundedRect (float x, float y, float width, float height, float radius)
		{
			_c.AddRoundedRect (new RectangleF (x, y, width, height), radius);
			_c.FillPath ();
		}

		public void DrawRoundedRect (float x, float y, float width, float height, float radius, float w)
		{
			_c.SetLineWidth (w);
			_c.AddRoundedRect (new RectangleF (x, y, width, height), radius);
			_c.StrokePath ();
		}

		public void FillRect (float x, float y, float width, float height)
		{
			_c.FillRect (new RectangleF (x, y, width, height));
		}

		public void FillOval (float x, float y, float width, float height)
		{
			_c.FillEllipseInRect (new RectangleF (x, y, width, height));
		}

		public void DrawOval (float x, float y, float width, float height, float w)
		{
			_c.SetLineWidth (w);
			_c.StrokeEllipseInRect (new RectangleF (x, y, width, height));
		}

		public void DrawRect (float x, float y, float width, float height, float w)
		{
			_c.SetLineWidth (w);
			_c.StrokeRect (new RectangleF (x, y, width, height));
		}
		
		public void DrawArc (float cx, float cy, float radius, float startAngle, float endAngle, float w)
		{
			_c.SetLineWidth (w);
			_c.AddArc (cx, cy, radius, -startAngle, -endAngle, true);
			_c.StrokePath ();
		}

		public void FillArc (float cx, float cy, float radius, float startAngle, float endAngle)
		{
			_c.AddArc (cx, cy, radius, -startAngle, -endAngle, true);
			_c.FillPath ();
		}

		const int _linePointsCount = 1024;
		PointF[] _linePoints = new PointF[_linePointsCount];
		bool _linesBegun = false;
		int _numLinePoints = 0;
		float _lineWidth = 1;
		bool _lineRounded = false;

		public void BeginLines (bool rounded)
		{
			_linesBegun = true;
			_lineRounded = rounded;
			_numLinePoints = 0;
		}

		public void DrawLine (float sx, float sy, float ex, float ey, float w)
		{
#if not
			#if DEBUG
			if (float.IsNaN (sx) || float.IsNaN (sy) || float.IsNaN (ex) || float.IsNaN (ey) || float.IsNaN (w)) {
				System.Diagnostics.Debug.WriteLine ("NaN in CoreGraphicsGraphics.DrawLine");
			}
			#endif

#endif
			if (_linesBegun) {
				
				_lineWidth = w;
				if (_numLinePoints < _linePointsCount) {
					if (_numLinePoints == 0) {
						_linePoints[_numLinePoints].X = sx;
						_linePoints[_numLinePoints].Y = sy;
						_numLinePoints++;
					}
					_linePoints[_numLinePoints].X = ex;
					_linePoints[_numLinePoints].Y = ey;
					_numLinePoints++;
				}
				
			} else {
				_c.MoveTo (sx, sy);
				_c.AddLineToPoint (ex, ey);
				_c.SetLineWidth (w);
				_c.StrokePath ();
			}
		}

		public void EndLines ()
		{
			if (!_linesBegun)
				return;
			_c.SaveState ();
			_c.SetLineJoin (_lineRounded ? CGLineJoin.Round : CGLineJoin.Miter);
			_c.SetLineWidth (_lineWidth);
			for (var i = 0; i < _numLinePoints; i++) {
				var p = _linePoints[i];
				if (i == 0) {
					_c.MoveTo (p.X, p.Y);
				} else {
					_c.AddLineToPoint (p.X, p.Y);
				}
			}
			_c.StrokePath ();
			_c.RestoreState ();
			_linesBegun = false;
		}
		
		static CGAffineTransform _textMatrix;

		Font _lastFont = null;

		UIFont _font_ui = null;
		CTFont _font_ct = null;

		CTStringAttributes _ct_attr = null;

		public void SetFont (Font f)
		{
			if (f != _lastFont) {
				_lastFont = f;

				// We have a Xamarin.Forms.Font object.  We need a CTFont.  X.F.Font has
				// ToUIFont() but not a Core Text version of same.  So we make a UIFont first.

				//_font_ui = _lastFont.ToUIFont ();
				// TODO temporary hack
				_font_ui = UIFont.SystemFontOfSize (_lastFont.Size);

				_font_ct = new CTFont (_font_ui.Name, _font_ui.PointSize);

				// With Core Text, it is important that we use a CTStringAttributes() with the
				// NSAttributedString constructor.

				// TODO maybe we should have the text color be separate, have it be an arg on SetFont?

				_ct_attr = new CTStringAttributes {
					Font = _font_ct,
					ForegroundColorFromContext = true,
					//ForegroundColor = _clr_cg,
					// ParagraphStyle = new CTParagraphStyle(new CTParagraphStyleSettings() { Alignment = CTTextAlignment.Center })
				};
			}
		}
		
		
		public void SetClippingRect (float x, float y, float width, float height)
		{
			_c.ClipToRect (new RectangleF (x, y, width, height));
		}

		// TODO using Core Text here is going to be a huge problem because we have to
		// change the coordinate system for every call to DrawString rather than changing
		// it once for all the calls.  Not even sure we have enough info (the height of
		// the coord system or UIView) to do the transform?

		// TODO if we know the text is one-line, we can skip the big transform and just use
		// the textmatrix thing.

		public void DrawString(string s, 
			float box_x,
			float box_y,
			float box_width,
			float box_height,
			LineBreakMode lineBreak, 
			TextAlignment horizontal_align, 
			TextAlignment vertical_align
		)
		{
			_c.SaveState(); 
			// TODO _c.SetTextDrawingMode (CGTextDrawingMode.Fill);

			// Core Text uses a different coordinate system.

			_c.TranslateCTM (0, _overall_height);
			_c.ScaleCTM (1, -1);

			var attributedString = new NSAttributedString (s, _ct_attr);

			float text_width;
			float text_height;

			if (
				(horizontal_align != TextAlignment.Start)
				|| (vertical_align != TextAlignment.Start)) {

				// not all of the alignments need the bounding rect.  don't
				// calculate it if not needed.

				var sizeAttr = attributedString.GetBoundingRect (
					new SizeF (
						(float)box_width, 
						int.MaxValue), 
					NSStringDrawingOptions.UsesLineFragmentOrigin | NSStringDrawingOptions.UsesFontLeading, 
					null
				);

				text_width = sizeAttr.Width;
				//text_height = sizeAttr.Height;
				//text_height = sizeAttr.Height + uif.Descender; // descender is negative
				text_height = _font_ui.CapHeight;

			} else {
				text_width = 0;
				text_height = 0;
			}

			//Console.WriteLine ("width: {0}    height: {1}", text_width, text_height);

			float x;

			switch (horizontal_align) {
			case TextAlignment.End:
				x = (box_x + box_width) - text_width;
				break;
			case TextAlignment.Center:
				x = box_x + (box_width - text_width) / 2;
				break;
			case TextAlignment.Start:
			default:
				x = box_x;
				break;
			}

			float y;

			switch (vertical_align) {
			case TextAlignment.End:
				y = box_y + text_height;
				break;
			case TextAlignment.Center:
				y = (box_y + box_height) - (box_height - text_height) / 2;
				break;
			case TextAlignment.Start:
			default:
				y = (box_y + box_height);
				break;
			}

			_c.TextPosition = new PointF ((float) x, (float) (_overall_height - y));

			// I think that by using CTLine() below, we are preventing multi-line text.  :-(

			using (var textLine = new CTLine (attributedString)) {
				textLine.Draw (_c);
			}

			//gctx.StrokeRect (new RectangleF(x, height - y, text_width, text_height));

			_c.RestoreState ();
		}
		public void DrawImage (IImage img, float x, float y)
		{
			if (img is UIKitImage) {
				var uiImg = ((UIKitImage)img).Image;
				uiImg.Draw (new PointF ((float) x, (float) y));
			}
		}

		public void DrawImage (IImage img, float x, float y, float width, float height)
		{
			if (img is UIKitImage) {
				var uiImg = ((UIKitImage)img).Image;
				uiImg.Draw (new RectangleF ((float)x, (float)y, (float)width, (float)height));
			}
		}

		public void SaveState ()
		{
			_c.SaveState ();
		}
		
		public void Translate (float dx, float dy)
		{
			_c.TranslateCTM (dx, dy);
		}
		
		public void Scale (float sx, float sy)
		{
			_c.ScaleCTM (sx, sy);
		}
		
		public void RestoreState ()
		{
			_c.RestoreState ();
			// TODO do anything to the font?
		}

		public void BeginOffscreen(float width, float height, IImage img)
		{
			// iOS apparently cannot re-use an image for offscreen draws
			if (img != null) {
				img.Destroy ();
			}

			UIGraphics.BeginImageContextWithOptions (new SizeF ((float)width, (float)height), false, 0);
			if (_prev_overall_height == null) {
				_prev_overall_height = new Stack<float> ();
			}
			_prev_overall_height.Push (_overall_height);
			_overall_height = (float)height;
			_c = UIGraphics.GetCurrentContext ();
		}

		public IImage EndOffscreen()
		{
			UIImage img = UIGraphics.GetImageFromCurrentImageContext ();
			UIGraphics.EndImageContext ();

			_overall_height = _prev_overall_height.Pop ();
			_c = UIGraphics.GetCurrentContext();

			return new UIKitImage(img);
		}

		public void BeginEntity (object entity)
		{
		}
	}

	public static class ColorEx
	{
		class ColorTag {
#if MONOMAC
			public NSColor NSColor;
#else
			public UIColor UIColor;
#endif
			public CGColor CGColor;
		}
		
#if MONOMAC
		public static NSColor GetNSColor (this Color c)
		{
			var t = c.Tag as ColorTag;
			if (t == null) {
				t = new ColorTag ();
				c.Tag = t;
			}
			if (t.NSColor == null) {
				t.NSColor = NSColor.FromDeviceRgba (c.Red / 255.0f, c.Green / 255.0f, c.Blue / 255.0f, c.Alpha / 255.0f);
			}
			return t.NSColor;
		}
#else
		public static UIColor GetUIColor (this Color c)
		{
			var t = c.Tag as ColorTag;
			if (t == null) {
				t = new ColorTag ();
				c.Tag = t;
			}
			if (t.UIColor == null) {
				t.UIColor = UIColor.FromRGBA (c.Red / 255.0f, c.Green / 255.0f, c.Blue / 255.0f, c.Alpha / 255.0f);
			}
			return t.UIColor;
		}
#endif

		public static CGColor GetCGColor (this Color c)
		{
			var t = c.Tag as ColorTag;
			if (t == null) {
				t = new ColorTag ();
				c.Tag = t;
			}
			if (t.CGColor == null) {
				t.CGColor = new CGColor (c.Red / 255.0f, c.Green / 255.0f, c.Blue / 255.0f, c.Alpha / 255.0f);
			}
			return t.CGColor;
		}
	}

	public class UIKitImage : IImage
	{
#if not
		public CGImage Image { get; private set; }
		public UIKitImage (CGImage image)
		{
			Image = image;
		}
#endif	
		public UIImage Image { get; private set; }
		public UIKitImage(UIImage img)
		{
			Image = img;
		}

		public void Destroy()
		{
			Image.Dispose ();
			Image = null;
		}
	}

	public static class CGContextEx
	{
		public static void AddRoundedRect (this CGContext c, RectangleF b, float r)
		{
			c.MoveTo (b.Left, b.Top + r);
			c.AddLineToPoint (b.Left, b.Bottom - r);
			
			c.AddArc (b.Left + r, b.Bottom - r, r, (float)(Math.PI), (float)(Math.PI / 2), true);
			
			c.AddLineToPoint (b.Right - r, b.Bottom);
			
			c.AddArc (b.Right - r, b.Bottom - r, r, (float)(-3 * Math.PI / 2), (float)(0), true);
			
			c.AddLineToPoint (b.Right, b.Top + r);
			
			c.AddArc (b.Right - r, b.Top + r, r, (float)(0), (float)(-Math.PI / 2), true);
			
			c.AddLineToPoint (b.Left + r, b.Top);
			
			c.AddArc (b.Left + r, b.Top + r, r, (float)(-Math.PI / 2), (float)(Math.PI), true);
		}

		public static void AddBottomRoundedRect (this CGContext c, RectangleF b, float r)
		{
			c.MoveTo (b.Left, b.Top + r);
			c.AddLineToPoint (b.Left, b.Bottom - r);
			
			c.AddArc (b.Left + r, b.Bottom - r, r, (float)(Math.PI), (float)(Math.PI / 2), true);
			
			c.AddLineToPoint (b.Right - r, b.Bottom);
			
			c.AddArc (b.Right - r, b.Bottom - r, r, (float)(-3 * Math.PI / 2), (float)(0), true);
			
			c.AddLineToPoint (b.Right, b.Top);
			
			c.AddLineToPoint (b.Left, b.Top);
		}
	}
	
}

