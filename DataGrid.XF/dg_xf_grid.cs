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

using Xamarin.Forms;

using Zumero.DataGrid.Core;

namespace Zumero.DataGrid.XF
{
	public static class ex
	{
		public static CrossGraphics.Font ToCrossFont(this Xamarin.Forms.Font f)
		{
			var options = FontOptions.None;
			if (0 != (f.FontAttributes & FontAttributes.Bold)) {
				options = FontOptions.Bold;
			}
			return new CrossGraphics.Font (f.FontFamily, options, (int) f.FontSize);
		}

		public static CrossGraphics.Color ToCrossColor(this Xamarin.Forms.Color c)
		{
			return new CrossGraphics.Color (c.R, c.G, c.B, c.A);
		}

		public static CrossGraphics.TextAlignment ToCrossTextAlignment(this Xamarin.Forms.TextAlignment ta)
		{
			switch (ta) {
			case Xamarin.Forms.TextAlignment.Center:
				return CrossGraphics.TextAlignment.Center;
			case Xamarin.Forms.TextAlignment.End:
				return CrossGraphics.TextAlignment.End;
			default:
				return CrossGraphics.TextAlignment.Start;
			}
		}
	}

	public class DataGridBase : Xamarin.Forms.View, IDataGrid<IGraphics>
	{
		protected static class Notify_DemoToggleSelections
		{
			public static void Listen(INotifyUserActions dp, Selection sel) // TODO IChangeSelection
			{
				dp.SingleTap += (object sender, CellCoords e) => {
					//dp.ClearSelection(0);
					sel.Change_Cell(SelectionChangeOperation.TOGGLE, e.Column, e.Row);
				};
				dp.DoubleTap += (object sender, CellCoords e) => {
					//dp.ClearSelection(0);
					sel.Change_WholeRow(SelectionChangeOperation.TOGGLE, e.Row);
				};
				dp.LongPress += (object sender, CellCoords e) => {
					//dp.ClearSelection(0);
					sel.Change_WholeColumn(SelectionChangeOperation.TOGGLE, e.Column);
				};
			}
		}
			
		public class RowList_Bindable_IList<T> : IRowList<T>
		{
			private readonly BindableObject _obj;
			private readonly BindableProperty _prop;
			private IRowList<T> _next;

			public RowList_Bindable_IList(BindableObject obj, BindableProperty prop)
			{
				_obj = obj;
				_prop = prop;

	            // this only listens for changes to the entire property.  like
	            // when the entire IList<T> gets replaced.  it is primarily helpful
	            // when we want to bind from a constructor but the property isn't set yet.
				obj.PropertyChanged += (object sender, System.ComponentModel.PropertyChangedEventArgs e) => {
					if (e.PropertyName == prop.PropertyName)
					{
						IList<T> lst = (IList<T>)obj.GetValue(prop);
						_next = new RowList_IList<T>(lst);
						if (changed != null) {
							changed(this, null);
						}
					}
				};
			}

			public void func_begin_update(CellRange viz)
			{
			}

			public void func_end_update()
			{
			}

			public bool get_value(int row, out T val) {
				return _next.get_value(row, out val);
			}

			public event EventHandler<CellCoords> changed;
		}

		protected class RowColumnInfo_Steady_BindableProp : IDimension
		{
			BindableObject _obj;
			BindableProperty _prop_number;
			BindableProperty _prop_size;

			public RowColumnInfo_Steady_BindableProp(BindableObject obj, BindableProperty prop_number, BindableProperty prop_size)
			{
				_obj = obj;
				_prop_number = prop_number;
				_prop_size = prop_size;

				obj.PropertyChanged += (object sender, System.ComponentModel.PropertyChangedEventArgs e) => {
					if (
						(e.PropertyName == _prop_number.PropertyName) 
						|| (e.PropertyName == _prop_size.PropertyName)
					)
					{
						if (changed != null) {
							changed(this, -1); // TODO it would be nice to pass args here with more specific info
						}
					}
				};
			}

			public bool variable_sizes {
				get {
					return false;
				}
			}

			public bool wraparound {
				get {
					return false;
				}
			}

			public double func_size(int n) 
			{
				return (double) _obj.GetValue(_prop_size);
			}

			public int? number
			{
				get {
					return (int?) _obj.GetValue(_prop_number);
				}
			}

			public event EventHandler<int> changed;
		}

		protected interface IMainPanel : ISetFrame, IScrollablePanel<IGraphics>, INotifyUserActions, IOnScroll
		{
		}

		protected interface IFrozenRowsPanel : IFrozenRows, IRegularPanel<IGraphics>, INotifyUserActions, IFrozenScrollOffset
		{
		}

		protected interface IFrozenColumnsPanel : IFrozenColumns, IRegularPanel<IGraphics>, INotifyUserActions, IFrozenScrollOffset
		{
		}

		protected IMainPanel Main;

		protected IFrozenRowsPanel Top;
		protected IFrozenRowsPanel Bottom;

		protected IFrozenColumnsPanel Left;
		protected IFrozenColumnsPanel Right;

		protected class MainPanel : DataPanel<IGraphics>, IMainPanel
		{
			public MainPanel(IDimension cbcol, IDimension cbrow, IDrawVisible<IGraphics> cbdraw) : base(cbcol, cbrow, cbdraw)
			{
			}
		}

		protected class FrozenColumnsPanel : DataPanel<IGraphics>, IFrozenColumnsPanel
		{
			public FrozenColumnsPanel(IDimension cbcol, IDimension cbrow, IDrawVisible<IGraphics> cbdraw) : base(cbcol, cbrow, cbdraw)
			{
			}
		}

		protected class FrozenRowsPanel : DataPanel<IGraphics>, IFrozenRowsPanel
		{
			public FrozenRowsPanel(IDimension cbcol, IDimension cbrow, IDrawVisible<IGraphics> cbdraw) : base(cbcol, cbrow, cbdraw)
			{
			}
		}

		void IDataGrid<IGraphics>.Layout (double container_width, double container_height)
		{
			FivePanels.layout (
				container_width, 
				container_height,

				Main,

				Top,
				Left,
				Right,
				Bottom
			);
		}

		void IDataGrid<IGraphics>.Setup (
			Action<IScrollablePanel<IGraphics>> f_main,
			Action<IRegularPanel<IGraphics>> f_frozen
		)
		{
			f_main (Main);

			f_frozen (Left);
			f_frozen (Top);
			f_frozen (Right);
			f_frozen (Bottom);

			Main.OnScroll ((double x, double y) => {
				if (Top != null) {
					Top.SetContentOffset (x, 0);
				}
				if (Left != null) {
					Left.SetContentOffset (0, y);
				}
				if (Bottom != null) {
					Bottom.SetContentOffset (x, 0);
				}
				if (Right != null) {
					Right.SetContentOffset (0, y);
				}
			});
		}
	}

}
