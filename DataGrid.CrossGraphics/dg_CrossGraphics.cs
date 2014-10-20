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
using System.Collections.Generic;

using CrossGraphics;

using Zumero.DataGrid.Core;

namespace Zumero.DataGrid.xGraphics // TODO dislike name
{

	public class DrawVisible_Cache : IDrawVisible<IGraphics>
	{
		private readonly IDrawVisible<IGraphics> _next;
		private readonly IDimension _cbcol;
		private readonly IDimension _cbrow;

		private class cimg
		{
			private IImage img;
			private CellRange range;
			private double xorigin;
			private double yorigin;

			public void destroy()
			{
				if (null != img) {
					img.Destroy ();
					img = null;
				}
			}

			public bool is_usable(CellRange viz)
			{
				if (img == null) {
					return false;
				}
				if (null == range) {
					return false;
				}
				if (viz.col_first < range.col_first) {
					return false;
				}
				if (viz.col_last > range.col_last) {
					return false;
				}
				if (viz.row_first < range.row_first) {
					return false;
				}
				if (viz.row_last > range.row_last) {
					return false;
				}
				return true;
			}

			public void draw(double xoff, double yoff, IGraphics gr)
			{
				double x = xoff + xorigin;
				double y = yoff + yorigin;
				gr.DrawImage(img, (float) x, (float) y);
			}

			public void get_image(cimg prev, IDrawVisible<IGraphics> next, CellRange viz, IBoxGetter box, IGraphics gr)
			{
				range = viz;

				double width = 0;
				for (int i = range.col_first; i <= range.col_last; i++) {
					double cx;
					double cy;
					double cwidth;
					double cheight;
					box.GetBox(i, 0,
						out cx,
						out cy,
						out cwidth,
						out cheight
					);
					if (i == range.col_first) {
						xorigin = cx;
					}
					width += cwidth;
				}

				double height = 0;
				for (int i = range.row_first; i <= range.row_last; i++) {
					double cx;
					double cy;
					double cwidth;
					double cheight;
					box.GetBox(0, i,
						out cx,
						out cy,
						out cwidth,
						out cheight
					);
					if (i == range.row_first) {
						yorigin = cy;
					}
					height += cheight;
				}

				//destroy ();
				gr.BeginOffscreen ((float) width, (float) height, img);
				if (
					(prev == null) 
					|| (null == prev.range)
					|| (null == prev.range.Intersect(range))
				)
				{
					next.func_draw (-xorigin, -yorigin, range, box, gr);
				} else {
					prev.draw (-xorigin, -yorigin, gr);
					List<CellRange> missing = range.Subtract (prev.range);
					foreach (CellRange mv in missing) {
						next.func_draw (-xorigin, -yorigin, mv, box, gr);
					}
				}
				img = gr.EndOffscreen ();
			}
		}

		private readonly cimg[] _cache = new cimg[2];
		private int _cur = 0;

		private void get_image(IDrawVisible<IGraphics> next, CellRange viz, IBoxGetter box, IGraphics gr) 
		{
			#if true
			const int extra_cols = 2;
			const int extra_rows = 2;

			// TODO we don't want to alloc here
			CellRange newrange = new CellRange (
				Math.Max(viz.col_first - extra_cols, 0),
				viz.col_last + extra_cols, // TODO constrain
				Math.Max(viz.row_first - extra_rows, 0),
				viz.row_last + extra_rows // TODO constrain
			);
			#endif

			int newcur = (_cur + 1) % 2;
			_cache[newcur].get_image (_cache[_cur], next, newrange, box, gr);
			_cur = newcur;
		}

		private void on_chained_change(object sender, CellCoords e)
		{
			_cache [0].destroy ();
			_cache [1].destroy ();
			if (changed != null) {
				changed (this, e);
			}
		}

		private void on_columnwidth_change(object sender, int n)
		{
			_cache [0].destroy ();
			_cache [1].destroy ();
		}

		private void on_rowheight_change(object sender, int n)
		{
			_cache [0].destroy ();
			_cache [1].destroy ();
		}

		public DrawVisible_Cache(IDrawVisible<IGraphics> next, IDimension cbcol, IDimension cbrow)
		{
			_cache[0] = new cimg();
			_cache[1] = new cimg();

			_next = next;
			_cbcol = cbcol;
			_cbrow = cbrow;

			_next.changed += on_chained_change;
			_cbcol.changed += on_columnwidth_change;
			_cbrow.changed += on_rowheight_change;
		}

		public void func_draw(double xoff, double yoff, CellRange viz, IBoxGetter box, IGraphics gr) 
		{
			if (_cache [0].is_usable (viz)) {
				_cur = 0;
			} else if (_cache [1].is_usable (viz)) {
				_cur = 1;
			} else {
				get_image (_next, viz, box, gr);
			}

			_cache[_cur].draw (xoff, yoff, gr);
		}

		public event EventHandler<CellCoords> changed;
	}

	public class DrawCell_Chain_Padding : DrawCell_Chain_Padding<IGraphics>
	{
		public DrawCell_Chain_Padding(IValuePerCell<Padding?> vals, IDrawCell<IGraphics> cd) : base(vals, cd)
		{
		}
	}

	public class DrawVisible_Layers : DrawVisible_Layers<IGraphics>
	{
		public DrawVisible_Layers(IEnumerable<IDrawVisible<IGraphics>> h) : base(h)
		{
		}
	}

	public class DrawCell_Chain_Layers : DrawCell_Chain_Layers<IGraphics>
	{
		public DrawCell_Chain_Layers(IEnumerable<IDrawCell<IGraphics>> h) : base(h)
		{
		}
	}

	public class DisplayTypeMap : DisplayTypeMap<IGraphics>
	{
	}

	public class DrawCell_Chain_DisplayTypes : DrawCell_Chain_DisplayTypes<IGraphics>
	{
		public DrawCell_Chain_DisplayTypes(IGetCellDisplayType typeget, DisplayTypeMap map) : base(typeget,map)
		{
		}
	}

	public class DrawCell_Chain_Cache : DrawCell_Chain_Cache<IGraphics,IImage>
	{
		protected override IImage draw(DrawCellFunc<IGraphics> next, int col, int row, double width, double height, IGraphics gr)
		{
			gr.BeginOffscreen ((float) width, (float) height, null);
			next(col, row, 0,0,width,height, gr);
			return gr.EndOffscreen ();
		}

		protected override void draw_image (IGraphics gr, IImage img, double x, double y)
		{
			gr.DrawImage(img, (float) x, (float) y);
		}

		protected override void explicit_dispose(IImage img)
		{
			img.Destroy ();
		}

		public DrawCell_Chain_Cache(IDrawCell<IGraphics> cd, IDimension cbcol, IDimension cbrow) : base(cd, cbcol, cbrow)
		{
		}

	}

	public class DrawCell_Fill : DrawCell<IGraphics,Color>
	{
		private static void draw(Color clr, int col, int row, double x, double y, double width, double height, IGraphics gr)
		{
			if (clr != null)
			{
				gr.SetColor (clr);
				gr.FillRect((float) x,(float) y,(float) width,(float) height);
			}
		}

		public DrawCell_Fill(IValuePerCell<Color> vals, IDrawCell<IGraphics> chain = null) : base(vals, draw, chain)
		{
		}
	}

	public class DrawCell_FillRectIfSelected : IDrawCell<IGraphics>
	{
		private readonly Color _clr; // TODO no way to change clr after
		private readonly ISelectionInfo _sel;

		public DrawCell_FillRectIfSelected(ISelectionInfo sel, Color clr)
		{
			_clr = clr;
			_sel = sel;

			sel.SelectionChanged += (object sender, SelectionChangedEventArgs e) => {
				changed(this, null); // TODO we wouldn't necessary have to pass null here?  more specific info.
			};
		}

		public void func_begin_update(CellRange viz)
		{
		}

		public void func_end_update()
		{
		}

		public void func_cell_draw(int col, int row, double x, double y, double width, double height, IGraphics gr) 
		{
			if (_sel.ContainsCell (col, row)) {
				gr.SetColor (_clr);
				gr.FillRect ((float) x, (float) y, (float) width, (float) height);
			}
		}

		public event EventHandler<CellCoords> changed;
	}

	public class MyTextFormat
	{
		public Font TextFont;
		public Color TextColor;
		public TextAlignment HorizontalTextAlignment;
		public TextAlignment VerticalTextAlignment;
	}

	public class DrawCell_Text : DrawCell<IGraphics,string,MyTextFormat>
	{
		private static void draw(string s, MyTextFormat fmt, int col, int row, double x, double y, double width, double height, IGraphics gr)
		{
			if (s != null)
			{
				gr.SetFont (fmt.TextFont);
				gr.SetColor (fmt.TextColor);

				gr.DrawString (
					s, 
					(float) x,
					(float) y,
					(float) width,
					(float) height, 
					LineBreakMode.None,
					fmt.HorizontalTextAlignment
				);
			}
		}

		public DrawCell_Text(IValuePerCell<string> vals, IValuePerCell<MyTextFormat> fmtinfo) : base(vals, fmtinfo, draw)
		{
		}
	}

	public class DrawCell_Oval : DrawCell<IGraphics,Color>
	{
		private static void draw(Color clr, int col, int row, double x, double y, double width, double height, IGraphics gr)
		{
			gr.SetColor(clr);
			gr.FillOval((float) x,(float) y,(float) width,(float) height);
		}

		public DrawCell_Oval(Color clr) : base(new ValuePerCell_Steady<Color>(clr), draw)
		{
		}
	}		

	public class DrawCell_RoundedRect : DrawCell<IGraphics,Color>
	{
		private static void draw(Color clr, int col, int row, double x, double y, double width, double height, IGraphics gr)
		{
			gr.SetColor(clr);
			gr.FillRoundedRect((float) x,(float) y,(float) width,(float) height, (float) Math.Min(width, height) / 6); 
		}

		public DrawCell_RoundedRect(Color clr) : base(new ValuePerCell_Steady<Color>(clr), draw)
		{
		}
	}		

}

