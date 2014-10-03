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
	public enum SelectionChangeOperation
	{
		SELECT,
		UNSELECT,
		TOGGLE
	}

	public enum SelectionMode
	{
		WHOLE_COLUMN,
		WHOLE_ROW,
		CELLS
	}

	public class SelectionChangedEventArgs
	{
		// TODO shouldn't this have a way of doing a range or a list?  Maybe it should
		// just be a cellgroup?
		// TODO maybe this should just use the <0 convention used in the others below

		public SelectionChangedEventArgs(SelectionMode _selmode, int _col, int _row, bool _new_state)
		{
			selmode = _selmode;
			column = _col;
			row = _row;
			new_state = _new_state;
		}

		public SelectionMode selmode { get; private set; }
		public int column { get; private set; } // unused for mode=whole_row
		public int row { get; private set; } // unused for mode=whole_column
		public bool new_state { get; private set; }
	}

	public interface ISelectionInfo
	{
		bool ContainsCell (int col, int row);
		event EventHandler<SelectionChangedEventArgs> SelectionChanged;
	}

	public class Selection : ISelectionInfo
	{
		// only one of the following can be in effect
		private HashSet<int> whole_rows;
		private HashSet<int> whole_columns;
		private Dictionary<int,HashSet<int>> cells_by_col;

		private event EventHandler<SelectionChangedEventArgs> SelectionChanged;

		event EventHandler<SelectionChangedEventArgs> ISelectionInfo.SelectionChanged
		{
			add {
				SelectionChanged += value;
			}
			remove {
				SelectionChanged -= value;
			}
		}

		private void notify_selection_changed(SelectionChangedEventArgs evargs)
		{
			if (SelectionChanged != null)
			{
				SelectionChanged(this, evargs);
			}
		}

		public void Clear()
		{
			if (whole_columns != null) {
				foreach (int col in whole_columns) {
					notify_selection_changed (new SelectionChangedEventArgs(SelectionMode.WHOLE_COLUMN, col, -1, false));
				}
				whole_columns = null;
			} else if (whole_rows != null) {
				foreach (int row in whole_rows) {
					notify_selection_changed (new SelectionChangedEventArgs(SelectionMode.WHOLE_ROW, -1, row, false));
				}
				whole_rows = null;
			} else if (cells_by_col != null) {
				foreach (int col in cells_by_col.Keys) {
					foreach (int row in cells_by_col[col]) {
						notify_selection_changed (new SelectionChangedEventArgs (SelectionMode.CELLS, col, row, false));
					}
				}
				cells_by_col = null;
			}
		}

		private bool contains_cell(int col, int row)
		{
			if (cells_by_col != null) {
				if (cells_by_col.ContainsKey (col)) {
					if (cells_by_col [col].Contains (row)) {
						return true;
					}
				}
			}

			return false;
		}

		private bool contains_col(int col)
		{
			if (whole_columns != null) {
				if (whole_columns.Contains (col)) {
					return true;
				}
			}

			return false;
		}

		private bool contains_row(int row)
		{
			if (whole_rows != null) {
				if (whole_rows.Contains (row)) {
					return true;
				}
			}

			return false;
		}

		private bool no_effect(SelectionChangeOperation op, bool prev_state)
		{
			if (SelectionChangeOperation.TOGGLE == op) {
				return false;
			}

			if ((SelectionChangeOperation.SELECT == op) && prev_state) {
				return true;
			}

			if ((SelectionChangeOperation.UNSELECT == op) && !prev_state) {
				return true;
			}

			return false;
		}

		private void mode_mismatch()
		{
			Clear (); // TODO auto-clear when the mode doesn't match?  or throw?
		}

		public void Change_Cell(SelectionChangeOperation op, int col, int row)
		{
			if ( (whole_rows != null) || (whole_columns != null)) {
				mode_mismatch ();
			}

			bool prev_state = contains_cell (col, row);

			if (no_effect(op, prev_state))
			{
				return;
			}

			if (cells_by_col == null) {
				cells_by_col = new Dictionary<int, HashSet<int>> ();
			}

			if (!cells_by_col.ContainsKey (col)) {
				cells_by_col [col] = new HashSet<int> ();
			}

			if (prev_state) {
				cells_by_col [col].Remove (row);
			} else {
				cells_by_col [col].Add (row);
			}

			notify_selection_changed (new SelectionChangedEventArgs(SelectionMode.CELLS, col, row, !prev_state));
		}

		public void Change_WholeColumn(SelectionChangeOperation op, int col)
		{
			if ( (whole_rows != null) || (cells_by_col != null)) {
				mode_mismatch ();
			}

			bool prev_state = contains_col (col);

			if (no_effect(op, prev_state))
			{
				return;
			}

			if (whole_columns == null) {
				whole_columns = new HashSet<int> ();
			}

			if (prev_state) {
				whole_columns.Remove (col);
			} else {
				whole_columns.Add (col);
			}

			notify_selection_changed (new SelectionChangedEventArgs(SelectionMode.WHOLE_COLUMN, col, -1, !prev_state));
		}

		public void Change_WholeRow(SelectionChangeOperation op, int row)
		{
			if ( (cells_by_col != null) || (whole_columns != null)) {
				mode_mismatch ();
			}

			bool prev_state = contains_row (row);

			if (no_effect(op, prev_state))
			{
				return;
			}

			if (whole_rows == null) {
				whole_rows = new HashSet<int> ();
			}

			if (prev_state) {
				whole_rows.Remove (row);
			} else {
				whole_rows.Add (row);
			}

			notify_selection_changed (new SelectionChangedEventArgs(SelectionMode.WHOLE_ROW, -1, row, !prev_state));
		}

		public bool ContainsCell(int col, int row)
		{
			return contains_col (col) || contains_row (row) || contains_cell (col, row);
		}

	}

}

