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

using Zumero.DataGrid.Core;

using SQLitePCL;
using SQLitePCL.Ugly;

namespace Zumero.DataGrid.SQLite
{
	public abstract class RowList_SQLite_IListish<TRow> : IRowList<TRow>
	{
		protected readonly sqlite3_stmt _stmt;

		public RowList_SQLite_IListish(sqlite3_stmt stmt)
		{
			_stmt = stmt;
		}

		protected abstract TRow get_row();

		public void func_begin_update(CellRange viz)
		{
		}

		public void func_end_update()
		{
		}

		// this implementation of a sqlite rowlist is only appropriate if there is
		// a bind parameter which will return a result for every row.  unless you
		// don't mind empty rows in the grid, gaps where nothing is displayed.
		// however, this will be much faster than the IEnumish one, and doesn't
		// require a cache in front of it.

		public bool get_value(int row, out TRow val)
		{
			// TODO or should the reset be done AFTER?
			_stmt.reset ();

			_stmt.clear_bindings (); // TODO probably not safe to clear bindings here because we only own one of them

			_stmt.bind_int (1, row); // TODO the ndx of the bind param should be a param to the constructor of this class

			int rc = _stmt.step ();
			if (raw.SQLITE_ROW != rc) {
				val = default(TRow);
				return false;
			}

			val = get_row ();
			return true;
		}

		public event EventHandler<CellCoords> changed;
	}

	public abstract class RowList_SQLite_IEnumish<TRow> : IRowList<TRow>
	{
		protected readonly sqlite3_stmt _stmt;
		private int _cur;
		private bool _done;

		public RowList_SQLite_IEnumish(sqlite3_stmt stmt)
		{
			_stmt = stmt;

			Reset ();
		}

		private void Reset()
		{
			_stmt.reset ();
			_cur = -1;
			_done = false;
		}

		private bool Step()
		{
			int rc = _stmt.step ();
			if (raw.SQLITE_ROW == rc) {
				_cur++;
				return true;
			}
			// TODO we've reached the end.  we should notify somebody.

			_done = true;

			return false;
		}

		protected abstract TRow get_row();

		public void func_begin_update(CellRange viz)
		{
		}

		public void func_end_update()
		{
		}

		public bool get_value(int row, out TRow val)
		{
			if (_cur > row) {
				// don't let this happen to you.  use a cache in front of this object.
				Reset ();
			}

			if (_done) {
				val = default(TRow);
				return false;
			}

			while (_cur < row) {
				if (!Step ()) {
					break;
				}
			}

			if (_cur == row) {
				val = get_row ();
				return true;
			} else {
				val = default(TRow);
				return false;
			}
		}

		public event EventHandler<CellCoords> changed;
	}

	public class RowList_SQLite_StringArray : RowList_SQLite_IEnumish<string[]>
	{
		public RowList_SQLite_StringArray(sqlite3_stmt stmt) : base(stmt)
		{
		}

		protected override string[] get_row()
		{
			int count = _stmt.column_count ();
			var row = new string[count];
			for (int i = 0; i < _stmt.column_count(); i++) {
				row [i] = _stmt.column_text (i);
			}
			return row;
		}
	}

	public class RowList_SQLite_Object<TRow> : RowList_SQLite_IEnumish<TRow> where TRow : new()
	{
		public RowList_SQLite_Object(sqlite3_stmt stmt) : base(stmt)
		{
		}

		protected override TRow get_row()
		{
			// TODO if the query has a column which does not match a property name in the given class,
			// ugly.row<T>() will throw.  This might not be the desirable behavior here.

			return ugly.row<TRow> (_stmt);
		}
	}

	public class Dimension_Rows_SQLite : IDimension
	{
		public int? number
		{
			get {
				return null; // TODO we want to change this once the query returns DONE
			}
		}

		public bool wraparound
		{
			get {
				return false;
			}
		}

		public bool variable_sizes
		{
			get {
				return false;
			}
		}

		public double func_size(int n)
		{
			return 50;
		}

		public event EventHandler<int> changed; // TODO the fact that this is OneCoord is kind of a pain since it can't implement IChanged
	}

	public class Dimension_Columns_SQLite : IDimension
	{
		private readonly sqlite3_stmt _stmt;

		public Dimension_Columns_SQLite(sqlite3_stmt stmt)
		{
			_stmt = stmt;
		}

		public int? number
		{
			get {
				return _stmt.column_count ();
			}
		}

		public bool wraparound
		{
			get {
				return false;
			}
		}

		public bool variable_sizes
		{
			get {
				return false;
			}
		}

		public double func_size(int n)
		{
			return 50;
		}

		public event EventHandler<int> changed; // TODO the fact that this is OneCoord is kind of a pain since it can't implement IChanged
	}
}

