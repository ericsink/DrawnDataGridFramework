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
using System.Reflection;

namespace Zumero.DataGrid.Core
{
	public interface IRowList<T> : IPerCell
	{
		bool get_value(int r, out T val);
	}

	// note that this class only returns values of a single type, which
	// may not be true for the various properties of an object.
	public class ValuePerCell_RowList_Properties<TVal,TRow> : IValuePerCell<TVal> where TRow : class
	{
		private readonly IRowList<TRow> _rows;
		private readonly Dictionary<int,string> _map;

		public ValuePerCell_RowList_Properties(IRowList<TRow> rows, Dictionary<int,string> map)
		{
			_rows = rows;
			_map = map;

			rows.changed += (object sender, CellCoords e) => {
				if (changed != null) {
					changed(this, e);
				}
			};
		}

		public bool get_value(int col, int row, out TVal val)
		{
            TRow r;
            if (!_rows.get_value(row, out r))
            {
				val = default(TVal); 
                return false;
            }

			string propname = _map[col];
            if (propname == null)
            {
                // TODO what if there is no propname for the given column?  return value not found.
				val = default(TVal); 
                return false;
            }

			var typ = typeof(TRow);
			var ti = typ.GetTypeInfo();
			var p = ti.GetDeclaredProperty(propname);

			if (p == null) {
				// TODO what?  throw?  return value not found?
				val = default(TVal); 
				return false;
			}

			TVal v = (TVal) p.GetValue(r); // TODO is this cast safe?  does this function work for anything but strings?
            val = v;

            return true;
		}

		public void func_begin_update(CellRange viz)
		{
			_rows.func_begin_update(viz);
		}

		public void func_end_update()
		{
			_rows.func_end_update();
		}

		public event EventHandler<CellCoords> changed;
	}

	// TODO do we need the TRow type param here?
	public class ValuePerCell_RowList_Indexed<TVal,TRow> : IValuePerCell<TVal> where TRow : IList<TVal>
	{
		private readonly IRowList<TRow> _rows;

		public ValuePerCell_RowList_Indexed(IRowList<TRow> rows)
		{
			_rows = rows;

			// TODO listen
		}

		public bool get_value(int col, int row, out TVal val)
		{
            TRow r;
            if (!_rows.get_value(row, out r)) {
                val = default(TVal);
                return false;
            }
            else {
                val = r[col];
                return true;
            }
		}

		public void func_begin_update(CellRange viz)
		{
			_rows.func_begin_update(viz);
		}

		public void func_end_update()
		{
			_rows.func_end_update();
		}

		public event EventHandler<CellCoords> changed;
	}

	public class RowList_IList<T> : IRowList<T>
	{
		private readonly IList<T> _rows;

		public RowList_IList(IList<T> rows)
		{
			_rows = rows;
			// for a plain IList, there is nothing to listen to
		}

		public void func_begin_update(CellRange viz)
		{
		}

		public void func_end_update()
		{
		}

		public bool get_value(int row, out T val) {
            val = _rows[row];
            return true;
		}

		public event EventHandler<CellCoords> changed;
	}

	public class RowList_Cache<T> : IRowList<T>
	{
		private readonly IRowList<T> _cd;
		private readonly Dictionary<int,T> _cache = new Dictionary<int, T> ();

		public RowList_Cache(IRowList<T> cd)
		{
			_cd = cd;

			_cd.changed += (object sender, CellCoords e) => {
				_cache.Remove(e.Row);
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

		public bool get_value(int row, out T val) {
			if (_cache.TryGetValue (row, out val)) {
                return true;
            } else {
                if (_cd.get_value(row, out val)) {
                    _cache [row] = val;
                    return true;
                } else {
                    return false;
                }
			}
		}

		public event EventHandler<CellCoords> changed;
	}

}

