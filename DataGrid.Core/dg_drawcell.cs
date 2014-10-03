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

namespace Zumero.DataGrid.Core
{
	public delegate void DrawCellFunc<TGraphics>(int col, int row, double x,double y,double width,double height, TGraphics gr);

	public interface IDrawCell<TGraphics> : IPerCell
	{
		void func_cell_draw(int col, int row, double x,double y,double width,double height, TGraphics gr);
	}

	// TODO avoid using this?  it seems to be slow.  doing layers at the per-cell level actually seems to be faster.
	public class DrawVisible_Layers<TGraphics> : IDrawVisible<TGraphics>
	{
		private IEnumerable<IDrawVisible<TGraphics>> draw_handlers;

		public DrawVisible_Layers(IEnumerable<IDrawVisible<TGraphics>> h)
		{
			draw_handlers = h;

			foreach (var dh in draw_handlers) {
				dh.changed += (object sender, CellCoords e) => {
					if (changed != null) {
						changed(this, e);
					}
				};
			}
		}

		public void func_draw(double xoff, double yoff, CellRange viz, IBoxGetter box, TGraphics gr) 
		{
			foreach (var dh in draw_handlers)
			{
				// TODO could do xoff/yoff here and pass zeroes down to the layers

				dh.func_draw(xoff, yoff, viz, box, gr);
			}
		}

		public event EventHandler<CellCoords> changed;
	}

	public class DrawCell_Chain_Layers<TGraphics> : DrawCell<TGraphics>
	{
		public DrawCell_Chain_Layers(IEnumerable<IDrawCell<TGraphics>> h) : base(h)
		{
		}
	}
		
	public abstract class DrawCellBase<TGraphics> : IDrawCell<TGraphics>
	{
		public event EventHandler<CellCoords> changed;

		protected readonly IEnumerable<IDrawCell<TGraphics>> _chain;

		protected readonly IPerCell _v1;
		protected readonly IPerCell _v2;

		protected DrawCellBase(IEnumerable<IDrawCell<TGraphics>> chain, IPerCell v1)
		{
			_chain = chain;
			_v1 = v1;

			listen ();
		}

		protected DrawCellBase(IEnumerable<IDrawCell<TGraphics>> chain)
		{
			_chain = chain;

			listen ();
		}

		protected DrawCellBase(IEnumerable<IDrawCell<TGraphics>> chain, IPerCell v1, IPerCell v2)
		{
			_chain = chain;
			_v1 = v1;
			_v2 = v2;

			listen ();
		}

		protected DrawCellBase(IDrawCell<TGraphics> chain1, IPerCell v1)
		{
            if (chain1 != null) {
				_chain = new IDrawCell<TGraphics>[] { chain1 }; // TODO no alloc
            }
			_v1 = v1;

			listen ();
		}

		protected DrawCellBase(IDrawCell<TGraphics> chain1)
		{
            if (chain1 != null) {
				_chain = new IDrawCell<TGraphics>[] { chain1 }; // TODO no alloc
            }

			listen ();
		}

		private void listen()
		{
			listen (_v1);
			listen (_v2);
			listen (_chain);
		}

		private void notify_changed(object sender, CellCoords e)
		{
			if (changed != null)
			{
				changed(this, e);
			}
		}

		private static void begin_update(CellRange viz, IBeginEndUpdate c)
		{
			if (c != null) {
				c.func_begin_update (viz);
			}
		}

		private static void begin_update(CellRange viz, IEnumerable<IBeginEndUpdate> a)
		{
			if (a != null) {
				foreach (IBeginEndUpdate c in a) {
					begin_update (viz, c);
				}
			}
		}

		private static void end_update(IBeginEndUpdate c)
		{
			if (c != null) {
				c.func_end_update ();
			}
		}

		private static void end_update(IEnumerable<IBeginEndUpdate> a)
		{
			if (a != null) {
				foreach (IBeginEndUpdate c in a) {
					end_update (c);
				}
			}
		}

		protected void listen(IChanged c)
		{
			if (c != null) {
				c.changed += notify_changed;
			}
		}

		private void listen(IEnumerable<IChanged> a)
		{
			if (a != null) {
				foreach (IChanged c in a) {
					listen (c);
				}
			}
		}

		public virtual void func_begin_update(CellRange viz)
		{
			begin_update (viz, _v1);
			begin_update (viz, _v2);
			begin_update (viz, _chain);
		}

		public virtual void func_end_update()
		{
			end_update (_v1);
			end_update (_v2);
			end_update (_chain);
		}

		// TODO it would be nice to find a way to call chain_draw here, but...
		public abstract void func_cell_draw (int col, int row, double x, double y, double width, double height, TGraphics gr); 
	}

	// no values per cell.
	public class DrawCell<TGraphics> : DrawCellBase<TGraphics>
	{
		// TODO does this need a draw func for THIS?  for cases where we want
		// to draw a cell but we don't need any other info except what is usually
		// passed in to the drawcellfunc?

		protected Action<DrawCellFunc<TGraphics>,int,int,double,double,double,double,TGraphics> _func_chain;

		// TODO IEnumerable<IChanged> _listen_for_changes_on_these
		public DrawCell(
			IDrawCell<TGraphics> chain
		) : base(chain)
		{
		}


		public DrawCell(
			IEnumerable<IDrawCell<TGraphics>> chain
		) : base(chain)
		{
		}

		public override void func_cell_draw(int col, int row, double x, double y, double width, double height, TGraphics gr) 
		{
			if (_chain != null) {
				foreach (IDrawCell<TGraphics> next in _chain) {
					if (_func_chain != null) {
						_func_chain (next.func_cell_draw, col, row, x, y, width, height, gr);
					} else {
						next.func_cell_draw (col, row, x, y, width, height, gr);
					}
				}
			}
		}
	}

	// when only one value is needed for drawing
	public class DrawCell<TGraphics,TVal> : DrawCellBase<TGraphics>
	{
		// TODO do we need to support non-static draw func, settable in the subclass constructor?

		private readonly IValuePerCell<TVal> _vals;
		private readonly Action<TVal,int,int,double,double,double,double,TGraphics> _func;
		protected Action<DrawCellFunc<TGraphics>,TVal,int,int,double,double,double,double,TGraphics> _func_chain;

		// TODO IEnumerable<IChanged> _listen_for_changes_on_these
		public DrawCell(
			IValuePerCell<TVal> vals, 
			Action<TVal,int,int,double,double,double,double,TGraphics> func,
			IDrawCell<TGraphics> chain=null
		) : base(chain, vals)
		{
			_vals = vals;
			_func = func;
		}

		public override void func_cell_draw(int col, int row, double x, double y, double width, double height, TGraphics gr) 
		{
            TVal val;

            if (_vals.get_value(col, row, out val)) {
                if (_func != null) {
                    _func (val, col, row, x, y, width, height, gr);
                }

                if (_chain != null) {
                    foreach (IDrawCell<TGraphics> next in _chain) {
                        if (_func_chain != null) {
                            _func_chain (next.func_cell_draw, val, col, row, x, y, width, height, gr);
                        } else {
                            next.func_cell_draw (col, row, x, y, width, height, gr);
                        }
                    }
                }
            }
            else {
                // TODO if get_value failed and there is a chain without a func_chain, should we call it?
                // TODO return false?
            }
		}
	}

	// two values needed for drawing
	public class DrawCell<TGraphics,TValA,TValB> : DrawCellBase<TGraphics>
	{
		// TODO do we need to support non-static draw func, settable in the subclass constructor?

		private readonly IValuePerCell<TValA> _vals_A;
		private readonly IValuePerCell<TValB> _vals_B;
		private readonly Action<TValA,TValB,int,int,double,double,double,double,TGraphics> _func;
		protected Action<DrawCellFunc<TGraphics>,int,int,double,double,double,double,TGraphics> _func_chain;

		// TODO IEnumerable<IChanged> _listen_for_changes_on_these
		public DrawCell(
			IValuePerCell<TValA> vals_A, 
			IValuePerCell<TValB> vals_B,
			Action<TValA,TValB,int,int,double,double,double,double,TGraphics> func,
			IEnumerable<IDrawCell<TGraphics>> chain = null
		) : base(chain, vals_A, vals_B)
		{
			_vals_A = vals_A;
			_vals_B = vals_B;
			_func = func;
		}

		public override void func_cell_draw(int col, int row, double x, double y, double width, double height, TGraphics gr) 
		{
			TValA valA;
			TValB valB;

			if (_vals_A.get_value (col, row, out valA) &&  _vals_B.get_value (col, row, out valB)) {
                _func(valA,valB,col,row,x,y,width,height,gr);

                if (_chain != null) {
                    foreach (IDrawCell<TGraphics> next in _chain) {
                        if (_func_chain != null) {
                            _func_chain (next.func_cell_draw, col, row, x, y, width, height, gr);
                        } else {
                            next.func_cell_draw (col, row, x, y, width, height, gr);
                        }
                    }
                }
            }
            else {
                // TODO if get_value failed and there is a chain without a func_chain, should we call it?
                // TODO return false?
            }
		}

	}
		
	// giving this a name breaks the use of OneValueForEachColumn.
	// so we define OneDisplayTypeFor...
	// not sure this is necessary.  we could just throw this out and make
	// displaytypes simply use <int>.
	// or it could be a generic <T> where T: value type?
	public interface IGetCellDisplayType : IValuePerCell<int>
	{
	}

	public class OneDisplayTypeForAllCells : OneValueForAllCells<int>, IGetCellDisplayType
	{
		public OneDisplayTypeForAllCells(IGetCellDisplayType next) : base(next)
		{
		}
	}

	public class OneDisplayTypeForEachColumn : OneValueForEachColumn<int>, IGetCellDisplayType
	{
		public OneDisplayTypeForEachColumn(IGetCellDisplayType next) : base(next)
		{
		}
	}

	public class OneDisplayTypeForEachRow : OneValueForEachRow<int>, IGetCellDisplayType
	{
		public OneDisplayTypeForEachRow(IGetCellDisplayType next) : base(next)
		{
		}
	}

	public class DisplayTypeMap<TGraphics>
	{
		// TODO the fact that the following is public is rather cheesy.  The class below
		// accesses this member directly.

		public readonly Dictionary<int,IDrawCell<TGraphics>> _types = new Dictionary<int, IDrawCell<TGraphics>>();

		public void Add(int n, IDrawCell<TGraphics> dc)
		{
			_types [n] = dc;
		}
	}

	// TODO can this be rewritten using one of the generics?  probably not, the
	// built in chaining code is all wrong for this case.
	public class DrawCell_Chain_DisplayTypes<TGraphics> : IDrawCell<TGraphics>
	{
		private readonly Dictionary<int,IDrawCell<TGraphics>> _types;
		private readonly IGetCellDisplayType _typeget;

		public DrawCell_Chain_DisplayTypes(IGetCellDisplayType typeget, DisplayTypeMap<TGraphics> map)
		{
			_typeget = typeget;
			_types = map._types;
			// TODO listen
		}

		public void func_begin_update(CellRange viz)
		{
			_typeget.func_begin_update(viz);

            foreach (int display_type in _types.Keys)
            {
                IDrawCell<TGraphics> dec = _types[display_type];
                dec.func_begin_update(viz);
            }
		}

		public void func_end_update()
		{
			_typeget.func_end_update();
			foreach (int display_type in _types.Keys)
			{
				IDrawCell<TGraphics> dec = _types[display_type];
				dec.func_end_update();
			}
		}

		public void func_cell_draw(int col, int row, double x, double y, double width, double height, TGraphics gr) 
		{
			int display_type;
           
            if (_typeget.get_value(col, row, out display_type)) {
                IDrawCell<TGraphics> dec = _types[display_type];

                dec.func_cell_draw(col, row, x,y,width,height, gr);
            }
		}

		public event EventHandler<CellCoords> changed;
	}

	public struct Padding
	{
		public double Left;
		public double Top;
		public double Right;
		public double Bottom;

		public Padding(double v)
		{
			Left = Top = Right = Bottom = v;
		}

		public Padding(double l, double t, double r, double b)
		{
			Left = l;
			Top = t;
			Right = r;
			Bottom = b;
		}
	}

	public class DrawCell_Chain_Padding<TGraphics> : DrawCell<TGraphics,Padding?>
	{
		private static void chain_draw(DrawCellFunc<TGraphics> next, Padding? pad, int col, int row, double x, double y, double width, double height, TGraphics gr)
		{
			if (pad.HasValue) {
				Padding t = pad.Value;
				next(
					col, 
					row, 
					x + t.Left, 
					y + t.Top, 
					width - (t.Left + t.Right), 
					height - (t.Top + t.Bottom), 
					gr
				);
			}
			else
			{
				next(col, row, x, y, width, height, gr);
			}
		}

		public DrawCell_Chain_Padding(IValuePerCell<Padding?> vals, IDrawCell<TGraphics> cd) : base(vals, null, cd)
		{
			_func_chain = chain_draw;
		}
	}

	public class DrawVisible_Adapter_DrawCell<TGraphics> : IDrawVisible<TGraphics>
	{
		protected IDrawCell<TGraphics> _cd;

		public DrawVisible_Adapter_DrawCell(IDrawCell<TGraphics> cd)
		{
			_cd = cd;

			_cd.changed += (object sender, CellCoords e) => {
				if (changed != null) {
					changed(this, e);
				}
			};
		}

		public void func_draw(double xoff, double yoff, CellRange viz, IBoxGetter box, TGraphics gr)
		{
			_cd.func_begin_update(viz);
			for (int row=viz.row_first; row<=viz.row_last; row++)
			{
				for (int col=viz.col_first; col<=viz.col_last; col++)
				{
					double cx;
					double cy;
					double cwidth;
					double cheight;
					box.GetBox(col, row,
						out cx,
						out cy,
						out cwidth,
						out cheight
					);
					_cd.func_cell_draw(col, row, xoff + cx,yoff + cy,cwidth,cheight, gr);
				}
			}
			_cd.func_end_update();
		}

		public event EventHandler<CellCoords> changed;
	}

	public abstract class DrawCell_Chain_Cache<TGraphics,TImage> : DrawCell<TGraphics> where TImage : class
	{
		private readonly Cache<TImage> _cache;
		private readonly IDimension _cbcol;
		private readonly IDimension _cbrow;

		protected abstract TImage draw (DrawCellFunc<TGraphics> next, int col, int row, double width, double height, TGraphics gr);
		protected abstract void draw_image (TGraphics gr, TImage img, double x, double y);
		protected abstract void explicit_dispose (TImage img);

		private void chain_draw(DrawCellFunc<TGraphics> next, int col, int row, double x, double y, double width, double height, TGraphics gr)
		{
			TImage img;
			if (!_cache.find(col,row, out img))
			{
				img = draw(next, col, row, width,height, gr);
				_cache.add(col, row, img);
			}

			draw_image (gr, img, x, y);
		}


		public DrawCell_Chain_Cache(IDrawCell<TGraphics> cd, IDimension cbcol, IDimension cbrow) : base(cd)
		{
			_cache = new Cache<TImage> (explicit_dispose);
			_cbcol = cbcol;
			_cbrow = cbrow;
			_func_chain = chain_draw;

			cd.changed += on_chained_change;
			_cbcol.changed += on_columnwidth_change;
			_cbrow.changed += on_rowheight_change;
		}

		public override void func_begin_update (CellRange viz)
		{
			base.func_begin_update (viz);

			// TODO flush some cells out?
		}

		private void on_chained_change(object sender, CellCoords e)
		{
			_cache.do_changed (e);
		}

		private void on_columnwidth_change(object sender, int n)
		{
			if (n < 0) {
				_cache.invalidate_all ();
			} else {
				_cache.invalidate_column (n);
			}
			// TODO note that we don't trigger our own changed event upward.  should we?
		}

		private void on_rowheight_change(object sender, int n)
		{
			if (n < 0) {
				_cache.invalidate_all ();
			} else {
				_cache.invalidate_row (n);
			}
			// TODO note that we don't trigger our own changed event upward.  should we?
		}

	}


}

