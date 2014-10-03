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
using System.Drawing;

using MonoTouch.CoreGraphics;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

using System.ComponentModel;

using Zumero.DataGrid.Core;

namespace Zumero.DataGrid.iOS.Core
{
	public class DataPanelView<TGraphics> : UIView, INativeView
	{
		protected IRegularPanel<TGraphics> _dp;
		private Func<DataPanelView<TGraphics>,TGraphics> _func_gr;

		void INativeView.SetFrame(double x, double y, double width, double height)
		{
			this.Frame = new RectangleF ((float)x, (float)y, (float)width, (float)height);
		}

		void INativeView.SetNeedsRedraw()
		{
			this.SetNeedsDisplay ();
		}

		private void on_singletap(UITapGestureRecognizer gr)
		{
			var wh = gr.LocationInView (this);
			_dp.do_singletap (wh.X, wh.Y);
		}

		private void on_doubletap(UITapGestureRecognizer gr)
		{
			var wh = gr.LocationInView (this);
			_dp.do_doubletap (wh.X, wh.Y);
		}

		private void on_longpress(UILongPressGestureRecognizer gr)
		{
			if (gr.State == UIGestureRecognizerState.Ended) {
				var wh = gr.LocationInView (this);
				_dp.do_longpress (wh.X, wh.Y);
			}
		}

		public DataPanelView(IRegularPanel<TGraphics> dp, Func<DataPanelView<TGraphics>,TGraphics> func_gr)
		{
			_dp = dp;
			_func_gr = func_gr;
			_dp.SetView(this);

			this.AutoresizingMask = UIViewAutoresizing.None;

			var gr_singletap_datacells = new UITapGestureRecognizer(on_singletap);
			gr_singletap_datacells.NumberOfTapsRequired = 1;

			var gr_doubletap_datacells = new UITapGestureRecognizer(on_doubletap);
			gr_doubletap_datacells.NumberOfTapsRequired = 2;

			gr_singletap_datacells.RequireGestureRecognizerToFail(gr_doubletap_datacells);

			var gr_longpress_datacells = new UILongPressGestureRecognizer(on_longpress);

			this.AddGestureRecognizer(gr_longpress_datacells);
			this.AddGestureRecognizer(gr_doubletap_datacells);
			this.AddGestureRecognizer(gr_singletap_datacells);
		}

		public override void Draw (RectangleF unused)
		{
			// TODO do we need to call base.Draw()?

			#if not
			IGraphics gr = new XFGraphics.CoreGraphics.CoreGraphicsGraphics (
				UIGraphics.GetCurrentContext (), true, Bounds.Height);
			#else
			TGraphics gr = _func_gr(this);
			#endif

			DateTime t1 = DateTime.Now;
			_dp.Draw (gr);
			DateTime t2 = DateTime.Now;
			TimeSpan elapsed = t2 - t1;
			Console.WriteLine ("elapsed: {0}", elapsed.TotalMilliseconds);
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

		public ScrollableDataPanelView(IScrollablePanel<TGraphics> dp, Func<DataPanelView<TGraphics>,TGraphics> func_gr) : base(dp, func_gr)
		{
			var gr_pan = new pan_gr(this);
			gr_pan.MaximumNumberOfTouches = 1;
			// TODO or maybe two fingers should be scroll but one finger is select?
			this.AddGestureRecognizer(gr_pan);

		}

		private class pan_gr : UIPanGestureRecognizer
		{
			ScrollableDataPanelView<TGraphics> _dg;
			double _began_x;
			double _began_y;

			public pan_gr(ScrollableDataPanelView<TGraphics> dg) : base(on_fire)
			{
				_dg = dg;
			}

			private static void on_fire(UIPanGestureRecognizer gr)
			{
				pan_gr me = gr as pan_gr;
				if (gr.State == UIGestureRecognizerState.Began) {
					me._dg._mine.GetContentOffset (out me._began_x, out me._began_y);
				} else if (gr.State == UIGestureRecognizerState.Changed) {
					var pt = gr.TranslationInView (me._dg);

					double x = me._began_x - pt.X;
					double y = me._began_y - pt.Y;

					me._dg._mine.SetContentOffset (x, y);
				}
			}
		}

	}

}


