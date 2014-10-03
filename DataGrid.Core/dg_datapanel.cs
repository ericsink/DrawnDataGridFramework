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

//[assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Zumero.DataGrid.XF.iOS")]
//[assembly:System.Runtime.CompilerServices.InternalsVisibleTo("Zumero.DataGrid.XF.Android")]

namespace Zumero.DataGrid.Core
{

	public class CellCoords
	{
		public CellCoords(int c, int r)
		{
			Column = c;
			Row = r;
		}

		public int Column { get; private set; }
		public int Row { get; private set; }
	}

	public interface IChanged
	{
		// TODO -1 means all, but there is no way to specify a range or list
		event EventHandler<CellCoords> changed;
	}

	public interface IEdgeFinder
	{
		void GetEdge (int ndx, out double start, out double length);
	}

	public interface IBoxGetter
	{
		void GetBox (
			int col, 
			int row,
			out double x,
			out double y,
			out double width,
			out double height
		);
	}

	public class BoxGetter : IBoxGetter
	{
		private readonly IEdgeFinder ef_col;
		private readonly IEdgeFinder ef_row;

		public BoxGetter(IEdgeFinder c, IEdgeFinder r)
		{
			ef_col = c;
			ef_row = r;
		}

		public void GetBox(
			int col, 
			int row,
			out double x,
			out double y,
			out double width,
			out double height
		)
		{
			ef_col.GetEdge (col, out x, out width);
			ef_row.GetEdge (row, out y, out height);
		}
	}

	public interface INotifyUserActions
	{
		event EventHandler<CellCoords> SingleTap;
		event EventHandler<CellCoords> DoubleTap;
		event EventHandler<CellCoords> LongPress;
	}

	public interface ISetView
	{
		void SetView(INativeView v);
	}

	public interface ISendUserActions
	{
		bool do_singletap (double x, double y);
		bool do_doubletap (double x, double y);
		bool do_longpress (double x, double y);
	}

	public interface IScrollOffset
	{
		void GetContentOffset(out double x, out double y);
		void SetContentOffset (double x, double y);
	}

	// defines calls from core panel to its native view
	public interface INativeView
	{
		void SetFrame (double x, double y, double width, double height);
		void SetNeedsRedraw();
	}

	public interface ISetFrame
	{
		void SetFrame (double x, double y, double width, double height);
	}

	public interface IFrozenScrollOffset
	{
		void SetContentOffset (double x, double y);
	}

	public interface IGetTotalHeight
	{
		double? GetTotalHeight (); // TODO doesn't need to be nullable
	}

	public interface IGetTotalWidth
	{
		double? GetTotalWidth(); // TODO doesn't need to be nullable
	}

	public interface IFrozenRows : ISetFrame, IGetTotalHeight
	{
	}

	public interface IFrozenColumns : ISetFrame, IGetTotalWidth
	{
	}

	public interface IOnScroll
	{
		void OnScroll (Action<double,double> f);
	}

	public class DataPanelBase : 
		ISetView, 
		ISendUserActions, 
		IScrollOffset, 
		INotifyUserActions, 
		IOnScroll, 
		ISetFrame, 
		IFrozenScrollOffset, 
		IGetTotalWidth, 
		IGetTotalHeight
	{
		// Row and Column info are separate so that it's easier for a DataGrid with
		// frozen panes to share them, but they share the same interface.  Different
		// implementations.

		protected readonly IDimension cb_row;
		protected readonly IDimension cb_col;

		private Action<double,double> _scrollfunc;

		void IOnScroll.OnScroll(Action<double,double> f)
		{
			_scrollfunc = f;
		}

		protected double ContentOffset_X;
		protected double ContentOffset_Y;

		void IScrollOffset.GetContentOffset(out double x, out double y)
		{
			x = ContentOffset_X;
			y = ContentOffset_Y;
		}

		protected double myWidth;
		protected double myHeight;

		private INativeView myView;

		void ISetView.SetView(INativeView v)
		{
			myView = v;
		}

		void ISetFrame.SetFrame(double x, double y, double width, double height)
		{
			myView.SetFrame (x,y,width,height);
			myWidth = width;
			myHeight = height;
		}

		private void my_SetContentOffset(double x, double y)
		{
			if (
				(ContentOffset_X != x) 
				|| (ContentOffset_Y != y)
			)
			{
				ContentOffset_X = x;
				ContentOffset_Y = y;

				this.SetNeedsRedraw ();
			}
		}

		void IFrozenScrollOffset.SetContentOffset(double x, double y)
		{
			my_SetContentOffset (x,y);
		}

		private double? ScrollXMin
		{
			get {
				if (cb_col.number.HasValue) {
					if (cb_col.wraparound) {
						return null;
					} else {
						return 0; 
					}
				} else {
					// no wraparound is possible since we don't know how many cols there are.
					// and we don't support negative column numbers.
					// so there is no point in allowing the scrolling to go further.
					// so we want to stop it at 0.
					return 0;
				}
			}
		}

		private double? ScrollXMax
		{
			get {
				if (cb_col.number.HasValue) {
					if (cb_col.wraparound) {
						return null;
					} else {
						return get_total_size (cb_col);
					}
				} else {
					// infinite number of columns, so allow scrolling to all of them
					return null; 
				}
			}
		}

		private double? ScrollYMin
		{
			get {
				if (cb_row.number.HasValue) {
					if (cb_row.wraparound) {
						return null;
					} else {
						return 0;
					}
				} else {
					// no wraparound is possible since we don't know how many rows there are.
					// and we don't support negative row numbers.
					// so there is no point in allowing the scrolling to go further.
					// so we want to stop it at 0.
					return 0;
				}
			}
		}

		private double? ScrollYMax
		{
			get {
				if (cb_row.number.HasValue) {
					if (cb_row.wraparound) {
						return null;
					} else {
						return get_total_size (cb_row);
					}
				} else {
					// infinite number of rows, so allow scrolling to all of them
					return null;
				}
			}
		}

		void IScrollOffset.SetContentOffset(double x, double y)
		{
			// fix for constrained scroll range

			if (ScrollXMin.HasValue) {
				double xmin = ScrollXMin.Value;
				if (x < xmin) {
					x = xmin;
				}
			}
			if (ScrollXMax.HasValue) {
				double xmax = ScrollXMax.Value - myWidth;
				// TODO should the following check be xmin instead of 0?
				if (xmax < 0) {
					xmax = 0;
				}
				if (x > xmax) {
					x = xmax;
				}
			}

			if (ScrollYMin.HasValue) {
				double ymin = ScrollYMin.Value;
				if (y < ymin) {
					y = ymin;
				}
			}
			if (ScrollYMax.HasValue) {
				double ymax = ScrollYMax.Value - myHeight;
				// TODO should the following check be ymin instead of 0?
				if (ymax < 0) {
					ymax = 0;
				}
				if (y > ymax) {
					y = ymax;
				}
			}

			my_SetContentOffset (x, y);

			if (_scrollfunc != null) {
				_scrollfunc (ContentOffset_X, ContentOffset_Y);
			}
		}

		protected DataPanelBase(IDimension cbcol, IDimension cbrow)
		{
			cb_col = cbcol;
			cb_row = cbrow;

			cb_row.changed += (object sender, int e) => {
				// TODO if we were caching edges/lengths, invalidate the cache here
				SetNeedsRedraw();
			};

			cb_col.changed += (object sender, int e) => {
				// TODO if we were caching edges/lengths, invalidate the cache here
				SetNeedsRedraw();
			};
		}

		private event EventHandler<CellCoords> SingleTap;
		private event EventHandler<CellCoords> DoubleTap;
		private event EventHandler<CellCoords> LongPress;

		event EventHandler<CellCoords> INotifyUserActions.SingleTap
		{
			add {
				SingleTap += value;
			}
			remove {
				SingleTap -= value;
			}
		}

		event EventHandler<CellCoords> INotifyUserActions.DoubleTap
		{
			add {
				DoubleTap += value;
			}
			remove {
				DoubleTap -= value;
			}
		}

		event EventHandler<CellCoords> INotifyUserActions.LongPress
		{
			add {
				LongPress += value;
			}
			remove {
				LongPress -= value;
			}
		}

		protected void SetNeedsRedraw()
		{
			if (myView != null) {
				myView.SetNeedsRedraw ();
			}
		}

		private class FixedEdgeFinder : IEdgeFinder
		{
			private readonly double _len;

			public FixedEdgeFinder(double len)
			{
				_len = len;
			}

			public void GetEdge(int ndx, out double start, out double length)
			{
				length = _len;
				start = ndx * _len;
			}
		}

		private class VariableEdgeFinder : IEdgeFinder
		{
			private readonly Dictionary<int,double> _edges;
			private readonly Dictionary<int,double> _lengths;

			public VariableEdgeFinder(Dictionary<int,double> edges, Dictionary<int,double> lengths)
			{
				_edges = edges;
				_lengths = lengths;
			}

			public void GetEdge(int ndx, out double start, out double length)
			{
				length = _lengths[ndx];
				start = _edges[ndx];
			}
		}

		protected static void calc_visible(
			double range_begin,
			double range_length,
			IDimension rc,
			out int first, 
			out int last, 
			out IEdgeFinder ef
		)
		{
			if (range_length <= 0) {
				first = -1;
				last = -1;
				ef = null;
			}

			// we could do wraparound in here (with a fixed number), but then we
			// could end up needing to return two first/last pairs if the range
			// straddles the boundary.  probably better to handle wraparound
			// entirely in the caller.  it should just adjust the range to be
			// positive and then de-adjust afterward.

			bool variable = rc.variable_sizes;
			Func<int, double> get_dist = rc.func_size;

			if (!variable) {
				double w = get_dist (-1);
				first = (int)(range_begin / w);
				if (first < 0) {
					// we don't support negative row/column numbers
					first = 0;
				}
				last = (int)((range_begin + range_length) / w);
				if (rc.number.HasValue) {
					int num = rc.number.Value;
					if (last > (num - 1)) {
						last = num - 1;
					}
				}
				ef = new FixedEdgeFinder (w);
			} else {
				var edges = new Dictionary<int,double> ();
				var lengths = new Dictionary<int,double> ();

				double cur_begin = 0;
				int i = 0;
				do {
					// assert that cur_begin cannot be > range_begin here
					// TODO but it happened, and the loop goes forever,
					// so we changed the check below to use >= instead of ==

					double cur_length = get_dist (i);

					if (
						(cur_begin >= range_begin)
						|| ((cur_begin < range_begin) && ((cur_begin + cur_length) > range_begin))
					) {
						// column i is going to be first.  add it to the results.
						edges[i] = cur_begin;
						lengths[i] = cur_length;

						// advance the cur_begin edge, but leave i alone for now so it
						// can be assigned after the loop.

						cur_begin += cur_length;
						break;
					}

					// TODO don't overrun num

					cur_begin += cur_length;
					i++;

				} while (true);

				first = i++;
				// TODO first cannot be <0 here, right?

				// cur_begin/i should now be together again, pointing at the first column
				// after first.  which might not exist.

				double range_end = range_begin + range_length;

				if ((edges [first] + lengths [first]) >= range_end) {
					// if width of first is wider than the range, we're done
					last = first;
				} else if (
					(rc.number.HasValue)
					&& (i > (rc.number.Value-1))
				) {
					// if there actually is nothing beyond first, we're done
					last = first;
				} else {
					do {
						// assert that left < right here

						double w = get_dist (i);

						// add the current column
						edges [i] = cur_begin;
						lengths [i] = w;

						if ((cur_begin + w) >= range_end) {
							// column is going to be last.
							break;
						}

						if (rc.number.HasValue) {
							int num = rc.number.Value;
							if ((i+1) > (num - 1)) {
								// there are no more columns.  so this one is last.
								break;
							}
						}

						cur_begin += w;
						i++;

					} while (true);

					last = i;
				}
				ef = new VariableEdgeFinder (edges, lengths);
			}
		}

		protected static double? get_total_size(IDimension rc)
		{
			if (!rc.number.HasValue) {
				return null;
			}

			int cols = rc.number.Value;
			if (rc.variable_sizes) {
				double total = 0;
				for (int i = 0; i < cols; i++) {
					total += rc.func_size(i);
				}
				return total;
			} else {
				return cols * rc.func_size (-1);
			}
		}

		private static int find_rowcolumn(double x, IDimension rc)
		{
			var wrap = rc.number.HasValue && rc.wraparound;

			if (wrap) {
				double len = get_total_size (rc).Value;
				int n = which_window (x, len);
				x -= (n * len);
			} else {
				if (x < 0) {
					return -1;
				}
				// we could check here to see if x > get_total_size, but that
				// would require us to call get_total_size, which duplicates 
				// some of the logic below, so if x is off the far end, we'll
				// just let the loop below figure that out.
			}

			if (rc.variable_sizes) {
				if (rc.number.HasValue) {
					int cols = rc.number.Value;
					double total = 0;
					for (int i = 0; i < cols; i++) {
						total += rc.func_size (i);
						if (total >= x) {
							return i;
						}
					}
				} else {
					// TODO this could loop forever?
					// variable sizes with an infinite number of columns?
					// not a good idea.
					double total = 0;
					int i = 0;
					while (true)
					{
						total += rc.func_size (i);
						if (total >= x) {
							return i;
						}
						i++;
					}
				}
				return -1;
			} else {
				int c = (int) (x / rc.func_size (-1));
				if (c < 0) {
					return -1;
				}
				if (rc.number.HasValue) {
					int num = rc.number.Value;
					if (c > (num - 1)) {
						return -1;
					}
				}
				return c;
			}
		}

		double? IGetTotalWidth.GetTotalWidth()
		{
			return get_total_size (cb_col);
		}

		double? IGetTotalHeight.GetTotalHeight()
		{
			return get_total_size (cb_row);
		}

		private CellCoords find_cell(double x, double y)
		{
			// coords come from the renderer, and have not yet been adjusted
			// for the content offset

			int c = find_rowcolumn (x + ContentOffset_X, cb_col);
			if (c < 0) {
				return null;
			}

			int r = find_rowcolumn (y + ContentOffset_Y, cb_row);
			if (r < 0) {
				return null;
			}

			return new CellCoords (c, r);
		}

		bool ISendUserActions.do_singletap(double x, double y)
		{
			CellCoords cc = find_cell (x, y);
			if (cc != null) {
				if (SingleTap != null) {
					SingleTap (this, cc);
				}
				return true;
			} else {
				return false;
			}
		}

		bool ISendUserActions.do_doubletap(double x, double y)
		{
			CellCoords cc = find_cell (x, y);
			if (cc != null) {
				if (DoubleTap != null) {
					DoubleTap (this, cc);
				}
				return true;
			} else {
				return false;
			}
		}

		bool ISendUserActions.do_longpress(double x, double y)
		{
			CellCoords cc = find_cell (x, y);
			if (cc != null) {
				if (LongPress != null) {
					LongPress (this, cc);
				}
				return true;
			} else {
				return false;
			}
		}

		protected static int which_window(double cur, double len)
		{
			if (cur < 0) {
				return - (1 + (int)(-cur / len));
			} else if (cur >= len) {
				return (int)(cur / len);
			} else {
				return 0;
			}
		}

	}

    public class CellRange
    {
        public readonly int col_first;
        public readonly int col_last;
        public readonly int row_first;
        public readonly int row_last;

        public CellRange(int cf, int cl, int rf, int rl)
        {
            col_first = cf;
            col_last = cl;
            row_first = rf;
            row_last = rl;
        }

		public CellRange Intersect(CellRange other)
		{
			var i_col_first = Math.Max(this.col_first, other.col_first);
			var i_row_first = Math.Max(this.row_first, other.row_first);
			var i_col_last = Math.Min(this.col_last, other.col_last);
			var i_row_last = Math.Min(this.row_last, other.row_last);

			if (
				((i_col_last - i_col_first) >= 0)
				&& ((i_row_last - i_row_first) >= 0)) {
				return new CellRange (i_col_first, i_col_last, i_row_first, i_row_last);
			} else {
				return null;
			}
    	}

		// TODO ouch.  this gets called during draw, and it allocs.  a lot.
		public List<CellRange> Subtract(CellRange other)
		{
			var a = new List<CellRange> ();
			CellRange intersection = Intersect (other);
			if (null == intersection) {
				a.Add (this);
			} else {
				if (intersection.row_first > row_first) {
					a.Add (new CellRange (col_first, col_last, row_first, intersection.row_first - 1));
				}
				if (intersection.row_last < row_last) {
					a.Add (new CellRange (col_first, col_last, intersection.row_last + 1, row_last));
				}
				if (intersection.col_first > col_first) {
					a.Add (new CellRange (col_first, intersection.col_first - 1, intersection.row_first, intersection.row_last));
				}
				if (intersection.col_last < col_last) {
					a.Add(new CellRange(intersection.col_last + 1, col_last, intersection.row_first, intersection.row_last));
				}
			}
			return a;
		}
	}

	public interface IDrawVisible<TGraphics> : IChanged
	{
		void func_draw(double xoff, double yoff, CellRange viz, IBoxGetter boxgetter, TGraphics gr); 
	}

	public interface IDrawablePanel<TGraphics>
	{
		void Draw (TGraphics gr);
	}

	public class DataPanel<TGraphics> : DataPanelBase, IDrawablePanel<TGraphics>
	{
		private readonly IDrawVisible<TGraphics> cb_draw;

		protected DataPanel(IDimension cbcol, IDimension cbrow, IDrawVisible<TGraphics> cbdraw) : base(cbcol, cbrow)
		{
			cb_draw = cbdraw;

			cb_draw.changed += (object sender, CellCoords e) => {
				// TODO visible?
				SetNeedsRedraw();
			};
		}

		void IDrawablePanel<TGraphics>.Draw(TGraphics gr)
		{
			draw_maybe_wraparound (
				gr, 
				ContentOffset_X,
				ContentOffset_Y,
				myWidth,
				myHeight
			);
		}

		private void draw_maybe_wraparound(
			TGraphics gr,
			double rect_x,
			double rect_y,
			double rect_width,
			double rect_height
		)
		{
			var wrap_x = cb_col.number.HasValue && cb_col.wraparound;
			var wrap_y = cb_row.number.HasValue && cb_row.wraparound;

			if (wrap_x || wrap_y) {
				int n_x_first;
				int n_x_last;
				int n_y_first;
				int n_y_last;
				double width;
				double height;

				if (wrap_x) {
					width = get_total_size (cb_col).Value;

					n_x_first = which_window (rect_x, width);
					n_x_last = which_window ((rect_x + rect_width), width);
				} else {
					width = 0; // will be unused
					n_x_first = 0;
					n_x_last = 0;
				}

				if (wrap_y) {
					height = get_total_size (cb_row).Value;

					n_y_first = which_window (rect_y, height);
					n_y_last = which_window ((rect_y + rect_height), height);
				} else {
					height = 0; // will be unused
					n_y_first = 0;
					n_y_last = 0;
				}

				for (int n_x = n_x_first; n_x <= n_x_last; n_x++) {
					double offset_x;
					if (wrap_x) {
						offset_x = n_x * width;
					} else {
						offset_x = 0;
					}
					for (int n_y = n_y_first; n_y <= n_y_last; n_y++) {
						double offset_y;
						if (wrap_y) {
							offset_y = n_y * height;
						} else {
							offset_y = 0;
						}

						double all_x = wrap_x ? offset_x : rect_x;
						double all_y = wrap_y ? offset_y : rect_y;
						double all_width = wrap_x ? width : rect_width;
						double all_height = wrap_y ? height : rect_height;

						// the following sets viz to the intersection of all and rect
						// the result might be degenerate, which we check for.
						double viz_x = Math.Max (all_x, rect_x);
						double viz_y = Math.Max (all_y, rect_y);
						double viz_width = Math.Min ((all_x + all_width), (rect_x + rect_width)) - viz_x;
						double viz_height = Math.Min ((all_y + all_height), (rect_y + rect_height)) - viz_y;

						if ((viz_width > 0) && (viz_height > 0)) {
							draw_one_window (gr, 

								viz_x - offset_x,
								viz_y - offset_y,
								viz_width,
								viz_height,

								offset_x,
								offset_y
							);
						}
					}
				}
			} else {
				draw_one_window (gr, rect_x, rect_y, rect_width, rect_height, 0, 0);
			}
		}

		private void draw_one_window(
			TGraphics gr,

			double rect_x,
			double rect_y,
			double rect_width,
			double rect_height,

			double offset_x,
			double offset_y
		)
		{
			int col_first;
			int col_last;
			int row_first;
			int row_last;

			IEdgeFinder ef_col;
			IEdgeFinder ef_row;

			calc_visible (
				rect_x,
				rect_width,
				cb_col,
				out col_first,
				out col_last,
				out ef_col
			);

			calc_visible (
				rect_y,
				rect_height,
				cb_row,
				out row_first,
				out row_last,
				out ef_row
			);

			if ((ef_col != null) && (ef_row != null)) {
                                // TODO consider whether we want to suffer the mem alloc here or just pass four ints
				CellRange viz = new CellRange(col_first, col_last, row_first, row_last);
				BoxGetter box = new BoxGetter (ef_col, ef_row);

				cb_draw.func_draw (
					offset_x-ContentOffset_X,
					offset_y-ContentOffset_Y,
					viz, 
					box, 
					gr
				);
			}
		}
	}

}

