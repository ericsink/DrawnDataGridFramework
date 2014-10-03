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

using Xamarin.Forms;

namespace XFGraphics
{
	public interface IGraphics
	{
		void BeginEntity(object entity); // TODO what is this?

		void SetFont(Font f);

		void SetColor(Color c);

		void Clear (Color c);

		void FillPolygon(Polygon poly);

		void DrawPolygon(Polygon poly,float w);

		void FillRect(float x,float y,float width, float height);

		void DrawRect(float x, float y, float width, float height, float w);

		void FillRoundedRect(float x, float y, float width, float height, float radius);

		void DrawRoundedRect(float x, float y, float width, float height, float radius, float w);

		void FillOval(float x, float y, float width, float height);

		void DrawOval(float x, float y, float width, float height, float w);

		void BeginLines(bool rounded);

		void DrawLine(float sx, float sy, float ex, float ey, float w);

		void EndLines();

		void FillArc(float cx, float cy, float radius, float startAngle, float endAngle);
		
		void DrawArc(float cx, float cy, float radius, float startAngle, float endAngle, float w);

		void DrawImage(IImage img, float x, float y);
		void DrawImage(IImage img, float x, float y, float width, float height);

		void DrawString(string s, 
			float x,
			float y,
			float width,
			float height,
			Xamarin.Forms.LineBreakMode lineBreak, 
			Xamarin.Forms.TextAlignment horizontal_align, 
			Xamarin.Forms.TextAlignment vertical_align
		);
			
		void SaveState();
		
		void SetClippingRect (float x, float y, float width, float height);
		
		void Translate(float dx, float dy);
		
		void Scale(float sx, float sy);
		
		void RestoreState();

		IImage ImageFromFile(string path);

		void BeginOffscreen (float width, float height, IImage prev);
		IImage EndOffscreen ();
	}

	#if not
	public static class GraphicsEx
	{
		public static void DrawImage(this IGraphics g, IImage img, Xamarin.Forms.Rectangle r)
		{
			g.DrawImage (img, r.X, r.Y, r.Width, r.Height);
		}

		public static void DrawLine(this IGraphics g, Point s, Point e, double w)
		{
			g.DrawLine (s.X, s.Y, e.X, e.Y, w);
		}

		public static void DrawRoundedRect(this IGraphics g, Rectangle r, double radius, double w)
		{
			g.DrawRoundedRect (r.Left, r.Top, r.Width, r.Height, radius, w);
		}

		public static void FillRoundedRect(this IGraphics g, Xamarin.Forms.Rectangle r, double radius)
		{
			g.FillRoundedRect (r.Left, r.Top, r.Width, r.Height, radius);
		}

		public static void FillRect(this IGraphics g, Rectangle r)
		{
			g.FillRect (r.Left, r.Top, r.Width, r.Height);
		}

		public static void DrawRect(this IGraphics g, Rectangle r, double w)
		{
			g.DrawRect (r.Left, r.Top, r.Width, r.Height, w);
		}

		public static void FillOval(this IGraphics g, Rectangle r)
		{
			g.FillOval (r.Left, r.Top, r.Width, r.Height);
		}

		public static void DrawOval(this IGraphics g, Rectangle r, double w)
		{
			g.DrawOval (r.Left, r.Top, r.Width, r.Height, w);
		}

		public static void FillArc(this IGraphics g, Point c, double radius, double startAngle, double endAngle)
		{
			g.FillArc (c.X, c.Y, radius, startAngle, endAngle);
		}

	}
	#endif

	public interface IImage
	{
		void Destroy();
	}

	public class Polygon
	{
		public readonly List<Xamarin.Forms.Point> Points;

		public object Tag { get; set; }

		public int Version { get; set; }

		public Polygon ()
		{
			Points = new List<Xamarin.Forms.Point> ();
		}

		public Polygon (int[] xs, int[] ys, int c)
		{
			Points = new List<Xamarin.Forms.Point> (c);
			for (var i = 0; i < c; i++) {
				Points.Add (new Xamarin.Forms.Point (xs [i], ys [i]));
			}
		}

		public int Count {
			get { return Points.Count; }
		}

		public void Clear()
		{
			Points.Clear ();
			Version++;
		}

		public void AddPoint(Xamarin.Forms.Point p)
		{
			Points.Add (p);
			Version++;
		}

		public void AddPoint(double x, double y)
		{
			Points.Add (new Xamarin.Forms.Point (x, y));
			Version++;
		}
	}

	#if not
	// TODO do we need this?
    public struct Transform2D
    {
        public double M11, M12, M13;
        public double M21, M22, M23;
        //public double M31, M32, M33;

        public void Apply (double x, double y, out double xp, out double yp)
        {
            xp = M11 * x + M12 * y + M13;
            yp = M21 * x + M22 * y + M23;
        }

        public static Transform2D operator * (Transform2D l, Transform2D r)
        {
            var t = new Transform2D ();

			t.M11 = l.M11 * r.M11 + l.M12 * r.M21;// +l.M13 * r.M31;
			t.M12 = l.M11 * r.M12 + l.M12 * r.M22;// +l.M13 * r.M32;
			t.M13 = l.M11 * r.M13 + l.M12 * r.M23 + l.M13;// *r.M33;

			t.M21 = l.M21 * r.M11 + l.M22 * r.M21;// +l.M23 * r.M31;
			t.M22 = l.M21 * r.M12 + l.M22 * r.M22;// +l.M23 * r.M32;
			t.M23 = l.M21 * r.M13 + l.M22 * r.M23 + l.M23;// *r.M33;

            //t.M31 = l.M31 * r.M11 + l.M32 * r.M21 + l.M33 * r.M31;
            //t.M32 = l.M31 * r.M12 + l.M32 * r.M22 + l.M33 * r.M32;
            //t.M33 = l.M31 * r.M13 + l.M32 * r.M23 + l.M33 * r.M33;

            return t;
        }

        public static Transform2D Identity ()
        {
            var t = new Transform2D ();
            t.M11 = 1;
            t.M22 = 1;
            //t.M33 = 1;
            return t;
        }

        public static Transform2D Translate (double x, double y)
        {
            var t = new Transform2D ();
            t.M11 = 1;
            t.M22 = 1;
            //t.M33 = 1;
            t.M13 = x;
            t.M23 = y;
            return t;
        }
        public static Transform2D Scale (double x, double y)
        {
            var t = new Transform2D ();
            t.M11 = x;
            t.M22 = y;
            //t.M33 = 1;
            return t;
        }
    }
	#endif

	public static class RectangleEx
	{
		public static Xamarin.Forms.Point GetCenter (this Xamarin.Forms.Rectangle r)
		{
			return new Xamarin.Forms.Point (r.Left + r.Width / 2,
										r.Top + r.Height / 2);
		}

		public static List<Xamarin.Forms.Rectangle> GetIntersections (this List<Xamarin.Forms.Rectangle> boxes, Xamarin.Forms.Rectangle box)
		{
			var r = new List<Xamarin.Forms.Rectangle> ();
			foreach (var b in boxes) {
				if (b.IntersectsWith (box)) {
					r.Add (b);
				}
			}
			return r;
		}
	}
}

