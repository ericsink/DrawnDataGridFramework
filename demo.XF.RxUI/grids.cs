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

using XFGraphics;

using Zumero.DataGrid.Core;
using Zumero.DataGrid.XF;

using Xamarin.Forms;

using ReactiveUI;

using Zumero.DataGrid.RxUI;

namespace Zumero.DataGrid.Demo.XF
{
	public class ColumnishGrid<T> : Zumero.DataGrid.XF.DataGridBase where T : class
	{
		public class ColumnInfo // TODO should be a ReactiveObject
		{
			public string Title = "Untitled";
			public double Width = 100;
			public string PropertyName = null;
			public Xamarin.Forms.Font TextFont = Xamarin.Forms.Font.SystemFontOfSize(16);
			public Xamarin.Forms.Color TextColor = Color.Black;
			public Xamarin.Forms.Color FillColor = Color.White;
			public Xamarin.Forms.TextAlignment HorizontalAlignment = TextAlignment.Center;
		}

		private class myRowInfo : IDimension
		{
			private ColumnishGrid<T> _top;

			public myRowInfo(ColumnishGrid<T> top)
			{
				_top = top;
				// TODO listen for change number of rows
				_top.PropertyChanged += (object sender, System.ComponentModel.PropertyChangedEventArgs e) => {
					if (e.PropertyName == ColumnishGrid<T>.RowHeightProperty.PropertyName)
					{
						if (changed != null) {
							changed(this, -1);
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
				return _top.RowHeight;
			}

			public int? number
			{
				get {
					if (_top.Rows == null) {
						return 0;
					}

					return _top.Rows.Count;
				}
			}

			public event EventHandler<int> changed;
		}

		private class myColumnInfo : IDimension
		{
			private ColumnishGrid<T> _top;

			public myColumnInfo(ColumnishGrid<T> top)
			{
				_top = top;
				// TODO hookup both
			}

			public bool variable_sizes {
				get {
					return true;
				}
			}

			public bool wraparound {
				get {
					return false;
				}
			}

			public double func_size(int n) 
			{
				return _top.Columns[n].Width;
			}

			public int? number
			{
				get {
					if (_top.Columns == null) {
						return 0;
					}
					return _top.Columns.Count;
				}
			}

			public event EventHandler<int> changed;
		}
			
		private class myFrozenRowInfo : IDimension
		{
			private ColumnishGrid<T> _top;

			public myFrozenRowInfo(ColumnishGrid<T> top)
			{
				_top = top;
				// TODO hookup
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

			public int? number
			{
				get {
					return 1;
				}
			}

			public double func_size(int n) 
			{
				// TODO might want a separate HeaderHeight
				return _top.RowHeight;
			}

			public event EventHandler<int> changed;
		}

		private class myFrozenGetCellTextValue : IValuePerCell<string>
		{
			private ColumnishGrid<T> _top;

			public myFrozenGetCellTextValue(ColumnishGrid<T> top)
			{
				_top = top;
				// TODO hookup
			}

			public void func_begin_update(CellRange viz)
			{

			}

			public void func_end_update()
			{

			}

			public bool get_value(int col, int row, out string val) 
			{
				val = _top.Columns[col].Title;
                return true;
			}

			public event EventHandler<CellCoords> changed;
		}

		private bool gv_fmt(int col, int row, out MyTextFormat v)
		{
			v = new MyTextFormat {
					TextFont = Columns [col].TextFont,
					TextColor = Columns [col].TextColor,
					HorizontalTextAlignment = Columns [col].HorizontalAlignment,
					VerticalTextAlignment = TextAlignment.Start,
				};
			return true;
		}

		private bool gv_clr(int col, int row, out Color? v)
		{
			v = Columns [col].FillColor;
			return true;
		}

		public ColumnishGrid()
		{
			var colinfo = new myColumnInfo (this);
			var rowinfo = new myRowInfo (this);

			var fmt = new OneValueForEachColumn<MyTextFormat> (new ValuePerCell_FromDelegates<MyTextFormat> (gv_fmt));

			IRowList<T> rowlist = new RowList_Bindable_ReactiveList<T>(this, RowsProperty);

            // TODO it would be better if these propnames were stored separately
            // from the formatting info.
            var propnames = new Dictionary<int,string>();
			this.PropertyChanged += (object sender, System.ComponentModel.PropertyChangedEventArgs e) => {
				if (e.PropertyName == ColumnsProperty.PropertyName)
				{
					propnames.Clear();
					for (int i=0; i<Columns.Count; i++)
					{
						propnames[i] = Columns[i].PropertyName;
					}
				}
			};

            IValuePerCell<string> vals = new ValuePerCell_RowList_Properties<string,T>(rowlist, propnames);

			IDrawCell<IGraphics> dec = new DrawCell_Text (vals, fmt);

			var padding1 = new ValuePerCell_Steady<Padding?> (new Padding (1, 1, 1, 1));
			var padding8 = new ValuePerCell_Steady<Padding?> (new Padding (8,8,8,8));
			IValuePerCell<Color?> bginfo = new ValuePerCell_FromDelegates<Color?> (gv_clr);
			bginfo = new OneValueForEachColumn<Color?> (bginfo);

			dec = new DrawCell_Chain_Padding (padding8, dec);
			dec = new DrawCell_Fill (bginfo, dec);
			dec = new DrawCell_Chain_Padding (padding1, dec);
			dec = new DrawCell_Chain_Cache (dec, colinfo, rowinfo);

			Main = new MainPanel(
				colinfo,
				rowinfo,
				new DrawVisible_Adapter_DrawCell<IGraphics>(dec)
			);
			Main.SingleTap += (object sender, CellCoords e) => {
				// when we get a tap on a cell, append an asterisk to its text.
				// this should automatically trigger a display update because the
				// object is a ReactiveObject which tells its ReactiveList which
				// tells its RowList and so on.
				T r = Rows [e.Row];
				ColumnInfo ci = Columns[e.Column];
				var typ = typeof(T);
				var ti = typ.GetTypeInfo();
				var p = ti.GetDeclaredProperty(ci.PropertyName);
				if (p != null)
				{
					var val = p.GetValue(r);
					p.SetValue(r, val.ToString() + "*");
				}
			};

			var bginfo_gray = new ValuePerCell_Steady<Color?> (Color.Gray);
			Top = new FrozenRowsPanel (
				colinfo,
				new Dimension_Steady(1, 40, false),
				new DrawVisible_Adapter_DrawCell<IGraphics>(
					new DrawCell_Chain_Padding(
						padding1,
						new DrawCell_Fill(
							bginfo_gray,
							new DrawCell_Text(
								new myFrozenGetCellTextValue(this), 
								new ValuePerCell_Steady<MyTextFormat>(
									new MyTextFormat {
										TextFont = Font.SystemFontOfSize(20, Xamarin.Forms.FontAttributes.Bold),
									TextColor = Color.Red,
										HorizontalTextAlignment= TextAlignment.Center,
										VerticalTextAlignment = TextAlignment.Center
									}
								)
							)
						)
					)
				)
			);

		}

		// --------------------------------
		// Rows

		public static readonly BindableProperty RowsProperty = 
			BindableProperty.Create<ColumnishGrid<T>,ReactiveList<T>>(
				p => p.Rows, null);

		public ReactiveList<T> Rows {
			get { return (ReactiveList<T>)GetValue(RowsProperty); }
			set { SetValue(RowsProperty, value); } // TODO disallow invalid values
		}

		// --------------------------------
		// Columns

		public static readonly BindableProperty ColumnsProperty = 
			BindableProperty.Create<ColumnishGrid<T>,IList<ColumnInfo>>(
				p => p.Columns, null);

		public IList<ColumnInfo> Columns {
			get { return (IList<ColumnInfo>)GetValue(ColumnsProperty); }
			set { SetValue(ColumnsProperty, value); } // TODO disallow invalid values
		}

		// --------------------------------
		// RowHeight

		public static readonly BindableProperty RowHeightProperty = 
			BindableProperty.Create<ColumnishGrid<T>,double>(
				p => p.RowHeight, 40);

		public double RowHeight {
			get { return (double)GetValue(RowHeightProperty); }
			set { SetValue(RowHeightProperty, value); } // TODO disallow invalid values
		}

	}

}


