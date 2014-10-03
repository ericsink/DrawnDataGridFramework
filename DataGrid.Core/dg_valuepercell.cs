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
	public interface IBeginEndUpdate
	{
		void func_begin_update (CellRange viz);
		void func_end_update ();
	}

	public interface IPerCell : IBeginEndUpdate, IChanged
	{
	}

	public interface IValuePerCell<T> : IPerCell
	{
		bool get_value(int col, int row, out T val);
	}

	public class OneValueForEachColumnOrRow<T> : IValuePerCell<T>
	{
		protected enum WhichWay
		{
			COLUMN,
			ROW,
		}

		private readonly IValuePerCell<T> _next;
		private readonly Dictionary<int,T> _cache;
		private readonly WhichWay _ww;

		protected OneValueForEachColumnOrRow(IValuePerCell<T> next, WhichWay ww)
		{
			_next = next;
			_ww = ww;
			_cache = new Dictionary<int, T> ();
			_next.changed += (object sender, CellCoords e) => {
				_cache.Clear(); // TODO this is too heavy-handed, probably
				changed(this, e);
			};
		}

		private bool next_get_value(int n, out T val)
		{
			if (WhichWay.COLUMN == _ww) {
				return _next.get_value (n, -1, out val);
			} else {
				return _next.get_value (-1, n, out val);
			}
		}

		public bool get_value(int col, int row, out T val)
		{
			int dim;
			if (WhichWay.COLUMN == _ww) {
				dim = col;
			} else {
				dim = row;
			}

			if (_cache.TryGetValue (dim, out val)) {
				return true;
			}
			else {
				if (next_get_value(dim, out val)) {
					_cache [dim] = val;
					return true;
				}
				else {
					return false;
				}
			}
		}

		public void func_begin_update(CellRange viz)
		{
			_next.func_begin_update(viz);
		}

		public void func_end_update()
		{
			_next.func_end_update();
		}

		public event EventHandler<CellCoords> changed;
	}

	public class OneValueForEachColumn<T> : OneValueForEachColumnOrRow<T>
	{
		public OneValueForEachColumn(IValuePerCell<T> next) : base(next, WhichWay.COLUMN)
		{
		}
	}

	public class OneValueForEachRow<T> : OneValueForEachColumnOrRow<T>
	{
		public OneValueForEachRow(IValuePerCell<T> next) : base(next, WhichWay.ROW)
		{
		}
	}

	// if the _next is just returning a constant, this cache is silly.
	public class OneValueForAllCells<T> : IValuePerCell<T>
	{
		private readonly IValuePerCell<T> _next;
        private bool _got;
		private T _val;

		public OneValueForAllCells(IValuePerCell<T> next)
		{
			_next = next;
			_next.changed += (object sender, CellCoords e) => {
				_val = default(T);
				_got = false;
				changed(this, e);
			};
		}

		public bool get_value(int col, int row, out T val)
		{
			if (!_got) {
				_got = _next.get_value(-1, -1, out _val);
			}
			val = _val;
            return _got;
		}

		public void func_begin_update(CellRange viz)
		{
			_next.func_begin_update(viz);
		}

		public void func_end_update()
		{
			_next.func_end_update();
		}

		public event EventHandler<CellCoords> changed;
	}

	public class ValuePerCell_Cache<T> : IValuePerCell<T>
	{
		private readonly IValuePerCell<T> _cd;
		private readonly Cache<T> _cache;

		// TODO need a cache policy of some kind.  pass in on constructor?  max number of cells?

		public ValuePerCell_Cache(IValuePerCell<T> cd)
		{
			_cd = cd;
			_cache = new Cache<T> (null);

			_cd.changed += (object sender, CellCoords e) => {
				_cache.do_changed(e);

				if (changed != null)
				{
					changed(this, e);
				}
			};
		}

		public void func_begin_update(CellRange viz)
		{
			_cd.func_begin_update(viz);

			// TODO flush some cells out of the cache?
		}

		public void func_end_update()
		{
			_cd.func_end_update();
		}

		public bool get_value(int col, int row, out T val) {
			if (_cache.find (col, row, out val)) {
                return true;
            }
            else {
                if (_cd.get_value(col, row, out val)) {
                    _cache.add (col, row, val);
                    return true;
                }
                else {
                    return false;
                }
			}
		}

		public event EventHandler<CellCoords> changed;
	}

	public abstract class ValuePerCell_Base<TVal> : IValuePerCell<TVal>
	{
		public abstract void func_begin_update(CellRange viz);
		public abstract void func_end_update ();
		public abstract bool get_value (int col, int row, out TVal val);

		public event EventHandler<CellCoords> changed;
		public void notify_changed(int col, int row)
		{
			if (changed != null) {
				changed (this, new CellCoords(col, row));
			}
		}
	}

	public class ValuePerCell_Steady<TVal> : ValuePerCell_Base<TVal>
	{
		private TVal _val;

		public ValuePerCell_Steady(TVal v)
		{
			_val = v;
		}

		public override void func_begin_update(CellRange viz)
		{
		}

		public override void func_end_update()
		{
		}

		public override bool get_value(int col, int row, out TVal val) {
			val = _val;
            return true;
		}
	}

    public delegate bool GetCellValueDelegate<T>(int col, int row, out T val);

	public class ValuePerCell_FromDelegates<T> : ValuePerCell_Base<T>
	{
		private readonly Action<CellRange> _begin_update;
		private readonly Action _end_update;
		private readonly GetCellValueDelegate<T> _func;

		// TODO IChanged
		public ValuePerCell_FromDelegates(
			Action<CellRange> begin_update,
			Action end_update,
			GetCellValueDelegate<T> f
		)
		{
			_begin_update = begin_update;
			_end_update = end_update;
			_func = f;
		}

		// TODO IChanged
		public ValuePerCell_FromDelegates(
			GetCellValueDelegate<T> f
		)
		{
			_begin_update = null;
			_end_update = null;
			_func = f;
		}

		// TODO it would be nice to have CellValue(T val)

		public override void func_begin_update(CellRange viz)
		{
			if (_begin_update != null) {
				_begin_update (viz);
			}
		}

		public override void func_end_update()
		{
			if (_end_update != null) {
				_end_update ();
			}
		}

		public override bool get_value(int col, int row, out T val) 
		{
            return _func (col, row, out val);
		}
	}

	public class ValuePerCell_RowNumber : ValuePerCell_FromDelegates<string>
	{
		private static bool gv(int col, int row, out string val)
		{
			val = string.Format("{0}", row + 1);
			return true;
		}

		public ValuePerCell_RowNumber() : base(gv)
		{
		}
	}

	public class ValuePerCell_ColumnNumber : ValuePerCell_FromDelegates<string>
	{
		private static bool gv(int col, int row, out string val)
		{
			val = string.Format("{0}", col + 1);
			return true;
		}

		public ValuePerCell_ColumnNumber() : base(gv)
		{
		}
	}

	public class ValuePerCell_ColumnLetters : ValuePerCell_FromDelegates<string>
	{
		// http://stackoverflow.com/questions/181596/how-to-convert-a-column-number-eg-127-into-an-excel-column-eg-aa
		private static string GetExcelColumnName(int columnNumber)
		{
			int dividend = columnNumber;
			string columnName = String.Empty;
			int modulo;

			while (dividend > 0)
			{
				modulo = (dividend - 1) % 26;
				columnName = Convert.ToChar(65 + modulo).ToString() + columnName;
				dividend = (int)((dividend - modulo) / 26);
			} 

			return columnName;
		}

		private static bool gv(int col, int row, out string val)
		{
			val = GetExcelColumnName (col + 1);
			return true;
		}

		public ValuePerCell_ColumnLetters() : base(gv)
		{
		}
	}

	public class Cache<T>
	{
		private readonly Dictionary<int,Dictionary<int,T>> _cache;
		private readonly Action<T> explicit_dispose;

		public Cache(Action<T> f)
		{
			_cache = new Dictionary<int,Dictionary<int,T>>();
			explicit_dispose = f;
		}

		public void invalidate_cell(int col, int row)
		{
			Dictionary<int, T> rowcache;
			_cache.TryGetValue (col, out rowcache);
			if (rowcache == null) {
				return;
			}
			T img;
			rowcache.TryGetValue (row, out img);
			if (img != null) {
				if (explicit_dispose != null) {
					explicit_dispose (img);
				}
				rowcache.Remove (row);
			}
		}

		public void invalidate_column(int col)
		{
			Dictionary<int, T> rowcache;
			_cache.TryGetValue (col, out rowcache);
			if (rowcache == null) {
				return;
			}

			foreach (int row in rowcache.Keys) {
				T img = rowcache[row];
				if (explicit_dispose != null) {
					explicit_dispose (img);
				}
			}

			_cache.Remove (col);
		}

		public void invalidate_row(int row)
		{
			foreach (int col in _cache.Keys) {
				Dictionary<int, T> rowcache = _cache[col];
				T img;
				rowcache.TryGetValue (row, out img);
				if (img != null) {
					if (explicit_dispose != null) {
						explicit_dispose (img);
					}
					rowcache.Remove (row);
				}
			}
		}

		public void invalidate_all()
		{
			foreach (int col in _cache.Keys) {
				Dictionary<int, T> rowcache = _cache[col];
				foreach (int row in rowcache.Keys) {
					T img = rowcache[row];
					if (explicit_dispose != null) {
						explicit_dispose (img);
					}
				}
			}

			_cache.Clear ();
		}

		public bool find(int col, int row, out T result)
		{
			Dictionary<int, T> rowcache;
			if (!_cache.TryGetValue (col, out rowcache)) {
				result = default(T);
				return false;
			}
			return rowcache.TryGetValue (row, out result);
		}

		public void add(int col, int row, T val)
		{
			if (!_cache.ContainsKey(col))
			{
				_cache[col] = new Dictionary<int, T>();
			}
			_cache[col][row] = val;
		}

		public void do_changed(CellCoords e)
		{
			if (e == null) {
				invalidate_all ();
			} else if (e.Column < 0) {
				if (e.Row < 0) {
					invalidate_all ();
				} else {
					invalidate_row (e.Row);
				}
			} else if (e.Row < 0) {
				invalidate_column (e.Column);
			} else {
				invalidate_cell (e.Column, e.Row);
			}
		}
	}

}
