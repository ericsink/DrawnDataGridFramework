/*
    Copyright 2014 Zumero, LLC

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

        http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.
*/

using System;

using Android.Views;
using Android.Graphics;
using Android.Content;
using Android.Widget;

using Zumero.DataGrid.Core;

namespace Zumero.DataGrid.Android.Core
{
	// Original Source: http://csharp-tricks-en.blogspot.com/2014/05/android-draw-on-screen-by-finger.html
	public class DataPanelView<TGraphics> : global::Android.Views.View, GestureDetector.IOnGestureListener, GestureDetector.IOnDoubleTapListener, INativeView
	{
		protected IRegularPanel<TGraphics> _dp;
		private GestureDetector _g;
		private Func<Canvas,TGraphics> _func_gr;

		void INativeView.SetFrame(double x, double y, double width, double height)
		{
			this.Layout ((int) x, (int) y, (int) (x + width), (int) (y + height));
		}

		void INativeView.SetNeedsRedraw()
		{
			//this.Invalidate (0, 0, Width, Height);
			this.Invalidate ();
		}

		public DataPanelView(Context context, IRegularPanel<TGraphics> dg, Func<Canvas,TGraphics> func_gr)
			: base(context)
		{
			_dp = dg;
			_func_gr = func_gr;

			_dp.SetView(this);

			_g = new GestureDetector(this);

			SetWillNotDraw(false); // TODO do we need this?
		}

		public virtual bool OnDown(MotionEvent ev)
		{
			//global::Android.Util.Log.Info ("dp OnDown", ev.ToString ());
			return true;
		}

		public void OnLongPress(MotionEvent ev)
		{
			//global::Android.Util.Log.Info ("dp OnLongPress", ev.ToString ());
			_dp.do_longpress (ev.GetX(), ev.GetY());
		}

		public void OnShowPress(MotionEvent ev)
		{
			//global::Android.Util.Log.Info ("dp OnShowPress", ev.ToString ());
		}

		public bool OnSingleTapUp(MotionEvent ev)
		{
			//global::Android.Util.Log.Info ("dp OnSingleTapUp", ev.ToString ());
			return true;
		}

		public bool OnDoubleTap(MotionEvent ev)
		{
			//global::Android.Util.Log.Info ("dp OnDoubleTap", ev.ToString ());
			return false;
		}

		public bool OnDoubleTapEvent(MotionEvent ev)
		{
			// doing the doubletap stuff here works, but in the OnDoubleTap call above,
			// it does not.

			//global::Android.Util.Log.Info ("dp OnDoubleTapEvent", ev.ToString ());
			return _dp.do_doubletap (ev.GetX (), ev.GetY ());
		}

		public bool OnSingleTapConfirmed(MotionEvent ev)
		{
			//global::Android.Util.Log.Info ("dp OnSingleTapConfirmed", ev.ToString ());
			return _dp.do_singletap (ev.GetX (), ev.GetY ());
		}

		public bool OnFling(MotionEvent ev1, MotionEvent ev2, float vx, float vy)
		{
			//global::Android.Util.Log.Info ("dp OnFling", ev1.ToString ());
			return false;
		}

		public virtual bool OnScroll(MotionEvent ev1, MotionEvent ev2, float dx, float dy)
		{
			//global::Android.Util.Log.Info ("dp OnScroll", ev1.ToString ());
			//global::Android.Util.Log.Info ("dp OnScroll", ev2.ToString ());
			//global::Android.Util.Log.Info ("dp OnScroll", dx.ToString ());
			//global::Android.Util.Log.Info ("dp OnScroll", dy.ToString ());

			return false;
		}

		protected override void OnDraw(Canvas canvas)
		{
			// TODO do we need to call base.Draw()?
			//base.OnDraw(canvas);

			//IGraphics gr = new XFGraphics.Android.AndroidGraphics (canvas);
			TGraphics gr = _func_gr (canvas);

			DateTime t1 = DateTime.Now;
			_dp.Draw (gr);
			DateTime t2 = DateTime.Now;
			TimeSpan elapsed = t2 - t1;
			global::Android.Util.Log.Info ("elapsed: ", elapsed.TotalMilliseconds.ToString ());
		}

		public override bool OnTouchEvent(MotionEvent e)
		{
			bool b = _g.OnTouchEvent (e);
			if (b) {
				return true;
			} else {
				return base.OnTouchEvent (e);
			}
		}
	}

	public class ScrollableDataPanelView<TGraphics> : DataPanelView<TGraphics>
	{
		private IScrollablePanel<TGraphics> _mine
		{
			get {
				return _dp as IScrollablePanel<TGraphics>;
			}
		}

		public ScrollableDataPanelView(Context c, IScrollablePanel<TGraphics> dp, Func<Canvas,TGraphics> func_gr) : base(c, dp, func_gr)
		{
		}

		private double _began_x;
		private double _began_y;

		public override bool OnDown(MotionEvent ev)
		{
			//_began = new Xamarin.Forms.Point (ev.GetX (), ev.GetY ());
			_mine.GetContentOffset (out _began_x, out _began_y);
			//global::Android.Util.Log.Info ("dp OnDown", ev.ToString ());
			return true;
		}

		public override bool OnScroll(MotionEvent ev1, MotionEvent ev2, float dx, float dy)
		{
			//global::Android.Util.Log.Info ("dp OnScroll", ev1.ToString ());
			//global::Android.Util.Log.Info ("dp OnScroll", ev2.ToString ());
			//global::Android.Util.Log.Info ("dp OnScroll", dx.ToString ());
			//global::Android.Util.Log.Info ("dp OnScroll", dy.ToString ());

			double tr_x = ev2.GetX () - ev1.GetX ();
			double tr_y = ev2.GetY () - ev1.GetY ();

			double x = _began_x - tr_x;
			double y = _began_y - tr_y;

			_mine.SetContentOffset (x, y);

			return true;
		}

	}
		
}
