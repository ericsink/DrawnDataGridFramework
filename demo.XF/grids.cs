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

using SQLitePCL;
using SQLitePCL.Ugly;
using Zumero.DataGrid.SQLite;

namespace Zumero.DataGrid.Demo.XF
{
	public class SQLiteGrid : Zumero.DataGrid.XF.DataGridBase
	{
		public SQLiteGrid()
		{
			sqlite3 conn = ugly.open (":memory:");
			conn.exec ("BEGIN TRANSACTION");
			conn.exec ("CREATE TABLE foo (a int, b int, c int);");
			for (int i = 0; i < 100; i++) {
				conn.exec ("INSERT INTO foo (a,b,c) VALUES (?,?,?)", i, i * 5 - 3, i * i / 10);
			}
			conn.exec ("COMMIT TRANSACTION");

			var stmt = conn.prepare ("SELECT * FROM foo");

			var colinfo = new Dimension_Columns_SQLite (stmt);
			var rowinfo = new Dimension_Rows_SQLite ();

			var mytextfmt = new MyTextFormat {
				TextFont = this.Font,
				TextColor = Color.Black,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center,
			};

			var fmt = new ValuePerCell_Steady<MyTextFormat> (
				mytextfmt
			);
			PropertyChanged += (object sender, System.ComponentModel.PropertyChangedEventArgs e) => {
				if (e.PropertyName == A1Grid.FontProperty.PropertyName) {
					mytextfmt.TextFont = Font;
					fmt.notify_changed(-1, -1);
				}
			};
			var padding1 = new ValuePerCell_Steady<Padding?> (new Padding (1));
			var padding4 = new ValuePerCell_Steady<Padding?> (new Padding (4));
			var fill_white = new ValuePerCell_Steady<Color?> (Color.White);

			var rowlist = new RowList_SQLite_StringArray (stmt);
			var rowlist_cached = new RowList_Cache<string[]> (rowlist);
			var vpc = new ValuePerCell_RowList_Indexed<string,string[]> (rowlist_cached);
			IDrawCell<IGraphics> dec = new DrawCell_Text (vpc, fmt);

			dec = new DrawCell_Chain_Padding (padding4, dec);
			dec = new DrawCell_Fill (fill_white, dec);
			dec = new DrawCell_Chain_Padding (padding1, dec);
			dec = new DrawCell_Chain_Cache (dec, colinfo, rowinfo);

			var sel = new Selection ();

			var dec_selection = new DrawCell_FillRectIfSelected (sel, Color.FromRgba (0, 255, 0, 120));

			var dh_layers = new DrawVisible_Layers (new IDrawVisible<IGraphics>[] {
				new DrawVisible_Adapter_DrawCell<IGraphics>(dec),
				new DrawVisible_Adapter_DrawCell<IGraphics>(dec_selection)
			});

			Main = new MainPanel(
				colinfo,
				rowinfo,
				dh_layers
			);
		}

		// --------------------------------
		// Font

		public static readonly BindableProperty FontProperty = 
			BindableProperty.Create<A1Grid,Nullable<Xamarin.Forms.Font>>(
				p => p.Font, Xamarin.Forms.Font.SystemFontOfSize(18));

		public Xamarin.Forms.Font Font {
			get { return (Xamarin.Forms.Font)GetValue(FontProperty); }
			set { SetValue(FontProperty, value); }
		}
	}

	internal static class myutil
	{
		public static string GetExcelColumnName(int columnNumber)
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
			val = myutil.GetExcelColumnName (col + 1) + (row + 1).ToString();
			return true;
		}

		public static GetCellValueDelegate<string> get_delegate()
		{
			return new GetCellValueDelegate<string> (gv);
		}
	}

	public class A1Grid : Zumero.DataGrid.XF.DataGridBase
	{
		public A1Grid()
		{
			var colinfo = new RowColumnInfo_Steady_BindableProp (this, ColumnsProperty, ColumnWidthProperty);
			var rowinfo = new RowColumnInfo_Steady_BindableProp (this, RowsProperty, RowHeightProperty);

			var mytextfmt = new MyTextFormat {
				TextFont = this.Font,
				TextColor = Color.Black,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center,
			};

			var fmt = new ValuePerCell_Steady<MyTextFormat> (
				mytextfmt
				);
			PropertyChanged += (object sender, System.ComponentModel.PropertyChangedEventArgs e) => {
				if (e.PropertyName == A1Grid.FontProperty.PropertyName) {
					mytextfmt.TextFont = Font;
					fmt.notify_changed(-1, -1);
				}
			};
			IDrawCell<IGraphics> dec = new DrawCell_Text (
				new ValuePerCell_FromDelegates<string>(myutil.get_delegate()), 
				fmt
			);

			var padding1 = new ValuePerCell_Steady<Padding?> (new Padding (1));
			var padding4 = new ValuePerCell_Steady<Padding?> (new Padding (4));
			var fill_white = new ValuePerCell_Steady<Color?> (Color.White);

			dec = new DrawCell_Chain_Padding (padding4, dec);
			dec = new DrawCell_Fill (fill_white, dec);
			dec = new DrawCell_Chain_Padding (padding1, dec); // TODO probably useless
			//dec = new DrawCell_Chain_Cache (dec, colinfo, rowinfo);

			#if not
			var sel = new Selection ();

			var dec_selection = new DrawCell_FillRectIfSelected (sel, Color.FromRgba (0, 255, 0, 120));

			var dh_layers = new DrawVisible_Layers (new IDrawVisible<IGraphics>[] {
				new DrawVisible_Cache(new DrawVisible_Adapter_DrawCell<IGraphics>(dec), colinfo, rowinfo),
				//new DrawVisible_Adapter_DrawCell<IGraphics>(dec_selection)
			});
			#endif

			Main = new MainPanel(
				colinfo,
				rowinfo,
				new DrawVisible_Cache(
				new DrawVisible_Adapter_DrawCell<IGraphics>(dec)
				, colinfo, rowinfo)
			);
			//Notify_DemoToggleSelections.Listen (Main, sel);

			#if not
			var fill_gray = new ValuePerCell_Steady<Color?> (Color.Gray);
			var frozen_textfmt = new ValuePerCell_Steady<MyTextFormat> (
				new MyTextFormat {
					TextFont = Font.SystemFontOfSize (18, FontAttributes.Bold),
					TextColor = Color.Black,
					HorizontalTextAlignment = TextAlignment.Center,
					VerticalTextAlignment = TextAlignment.Center,
				});

			Left = new FrozenColumnsPanel(
				new Dimension_Steady(1, 80, false),
				rowinfo,
				new DrawVisible_Adapter_DrawCell<IGraphics>(
					new DrawCell_Chain_Padding(
						padding1,
						new DrawCell_Fill(
							fill_gray,
							new DrawCell_Text(
								new ValuePerCell_RowNumber (), 
								frozen_textfmt
							)
						)
					)
				)
			);
			Left.SingleTap += (object sender, CellCoords e) => {
				// hack for testing purposes
				this.Font = Font.SystemFontOfSize(this.Font.FontSize + 1);
			};

			Top = new FrozenRowsPanel (
				colinfo,
				new Dimension_Steady(1, 40, false),
				new DrawVisible_Adapter_DrawCell<IGraphics>(
					new DrawCell_Chain_Padding(
						padding1,
						new DrawCell_Fill(
							fill_gray,
							new DrawCell_Text(
								new ValuePerCell_ColumnLetters (), 
								frozen_textfmt
							)
						)
					)
				)
			);
			Top.SingleTap += (object sender, CellCoords e) => {
				// hack for testing purposes
				this.RowHeight *= 1.1;
			};
			#endif
		}

		// --------------------------------
		// Rows

		public static readonly BindableProperty RowsProperty = 
			BindableProperty.Create<A1Grid,int?>(
				p => p.Rows, null);

		public int? Rows {
			get { return (int?)GetValue(RowsProperty); }
			set { SetValue(RowsProperty, value); } // TODO disallow invalid values
		}

		// --------------------------------
		// Columns

		public static readonly BindableProperty ColumnsProperty = 
			BindableProperty.Create<A1Grid,int?>(
				p => p.Columns, null);

		public int? Columns {
			get { return (int?)GetValue(ColumnsProperty); }
			set { SetValue(ColumnsProperty, value); } // TODO disallow invalid values
		}

		// --------------------------------
		// ColumnWidth

		public static readonly BindableProperty ColumnWidthProperty = 
			BindableProperty.Create<A1Grid,double>(
				p => p.ColumnWidth, 80);

		public double ColumnWidth {
			get { return (double)GetValue(ColumnWidthProperty); }
			set { SetValue(ColumnWidthProperty, value); } // TODO disallow invalid values
		}

		// --------------------------------
		// RowHeight

		public static readonly BindableProperty RowHeightProperty = 
			BindableProperty.Create<A1Grid,double>(
				p => p.RowHeight, 40);

		public double RowHeight {
			get { return (double)GetValue(RowHeightProperty); }
			set { SetValue(RowHeightProperty, value); } // TODO disallow invalid values
		}

		// --------------------------------
		// Font

		public static readonly BindableProperty FontProperty = 
			BindableProperty.Create<A1Grid,Nullable<Xamarin.Forms.Font>>(
				p => p.Font, Xamarin.Forms.Font.SystemFontOfSize(18));

		public Xamarin.Forms.Font Font {
			get { return (Xamarin.Forms.Font)GetValue(FontProperty); }
			set { SetValue(FontProperty, value); }
		}

	}

	public class Demo_CellsGetBigger : Zumero.DataGrid.XF.DataGridBase
	{
		public Demo_CellsGetBigger()
		{
			Func<int,double> f = (int n) => (double) (n+40);
			var colinfo = new Dimension_FromDelegate ((int?) null, f, false);
			var rowinfo = new Dimension_FromDelegate ((int?) null, f, false);

			var bginfo_inset = new ValuePerCell_Steady<Padding?> (new Padding (1));
			var bginfo_white = new ValuePerCell_Steady<Color?> (Color.White);

			IDrawCell<IGraphics> dec = new DrawCell_Fill (bginfo_white);
			dec = new DrawCell_Chain_Padding (bginfo_inset, dec);
			//dec = new DrawEachCell_Cache (dec, colinfo, rowinfo);

			Main = new MainPanel(
				colinfo,
				rowinfo,
				new DrawVisible_Adapter_DrawCell<IGraphics>(dec)
			);
		}
	}

	public class Demo_GradientCells : Zumero.DataGrid.XF.DataGridBase
	{
		private const int COUNT = 100;
		private const double SIZE = 10;

		private static bool get_value(int col, int row, out Color? clr) 
		{
			clr = Xamarin.Forms.Color.FromRgb (
				(col / (double) COUNT),
				(row / (double) COUNT),
				0.5
			);
			return true;
		}

		public Demo_GradientCells()
		{
			var colinfo = new Dimension_Steady(COUNT, SIZE, false);
			var rowinfo = new Dimension_Steady(COUNT, SIZE, false);

			var colors = new ValuePerCell_FromDelegates<Color?> (get_value);

			var dec = new DrawCell_Fill (colors);

			Main = new MainPanel(
				colinfo,
				rowinfo,
				new DrawVisible_Adapter_DrawCell<IGraphics>(dec)
			);

		}

	}

	public class A1Wraparound : Zumero.DataGrid.XF.DataGridBase
	{
		private const double SIZE = 80;

		public A1Wraparound(int cols, int rows, bool wrap_col, bool wrap_row)
		{
			var colinfo = new Dimension_Steady(cols, SIZE, wrap_col);
			var rowinfo = new Dimension_Steady(rows, SIZE, wrap_row);

			var padding1 = new ValuePerCell_Steady<Padding?> (new Padding (1, 1, 1, 1));
			var fill_white = new ValuePerCell_Steady<Color?> (Color.White);

			IDrawCell<IGraphics> dec_text = new DrawCell_Text (
				new ValuePerCell_FromDelegates<string>(myutil.get_delegate()), 
				new ValuePerCell_Steady<MyTextFormat> (
					new MyTextFormat {
						TextFont = Font.SystemFontOfSize (SIZE/3),
						TextColor = Color.Black,
						HorizontalTextAlignment = TextAlignment.Center,
						VerticalTextAlignment = TextAlignment.Center,
					})
			);

			IDrawVisible<IGraphics> dv;

			#if false

			// this approach uses one layer containing a chain
			// that cache-pad-fill-text

			IDrawEachCell dec = new DrawEachCell_Chain_Padding (
			padding1, 
			new DrawEachCell_Fill (
			fill_white,
			dec_text
			)
			);

			dec = new DrawEachCell_Chain_Cache (dec, colinfo, rowinfo);

			dv = new DrawVisibleCells_DrawEachCellAdapter (dec);

			#else

			// this approach uses two layers, one is pad-fill,
			// and the other is cache-text

			IDrawCell<IGraphics> dec_bg = new DrawCell_Chain_Padding (
				padding1, 
				new DrawCell_Fill (
					fill_white
				)
			);

			//dec_bg = new DrawEachCell_Chain_Cache (dec_bg, colinfo, rowinfo);

			//dec_text = new DrawEachCell_Chain_Cache (dec_text, colinfo, rowinfo);

			#if false

			dv = new DrawVisibleCells_Layers (new IDrawVisibleCells[] {
			new DrawVisibleCells_DrawEachCellAdapter(dec_bg),
			new DrawVisibleCells_DrawEachCellAdapter(dec_text),
			});

			#else

			IDrawCell<IGraphics> dec_layers = new DrawCell_Chain_Layers(new IDrawCell<IGraphics>[] {
				dec_bg,
				dec_text,
			});

			dec_layers = new DrawCell_Chain_Cache (dec_layers, colinfo, rowinfo);

			dv = new DrawVisible_Adapter_DrawCell<IGraphics>(dec_layers);

			#endif

			#endif

			Main = new MainPanel(
				colinfo,
				rowinfo,
				dv
			);
			Main.SingleTap += (object sender, CellCoords e) => {
				// TODO trigger a redraw
			};

			var fill_gray = new ValuePerCell_Steady<Color?> (Color.Gray);

			var frozen_textfmt = new ValuePerCell_Steady<MyTextFormat> (
				new MyTextFormat {
					TextFont = Font.SystemFontOfSize (SIZE/3, FontAttributes.Bold),
					TextColor = Color.Black,
					HorizontalTextAlignment = TextAlignment.Center,
					VerticalTextAlignment = TextAlignment.Center,
				});

			Func<FrozenColumnsPanel> f_c = () => 
				new FrozenColumnsPanel(
				new Dimension_Steady(1, SIZE, false),
				rowinfo,
					new DrawVisible_Adapter_DrawCell<IGraphics>(
					new DrawCell_Chain_Padding(
						padding1,
						new DrawCell_Fill(
							fill_gray,
							new DrawCell_Text(
								new ValuePerCell_RowNumber (), 
								frozen_textfmt
							)
						)
					)
				)
			);
			Left = f_c ();
			Right = f_c ();

			Func<FrozenRowsPanel> f_r = () => 
				new FrozenRowsPanel (
				colinfo,
				new Dimension_Steady(1, SIZE, false),
					new DrawVisible_Adapter_DrawCell<IGraphics>(
					new DrawCell_Chain_Padding(
						padding1,
						new DrawCell_Fill(
							fill_gray,
							new DrawCell_Text(
								new ValuePerCell_ColumnLetters (), 
								frozen_textfmt
							)
						)
					)
				)
			);
			Top = f_r ();
			Bottom = f_r ();

		}

	}

	public class ColumnishGrid<T> : Zumero.DataGrid.XF.DataGridBase where T : class
	{
		public class ColumnInfo // TODO INotify...
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

			IRowList<T> rowlist = new RowList_Bindable_IList<T>(this, RowsProperty);

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

			var sel = new Selection ();

			var dec_selection = new DrawCell_FillRectIfSelected (sel, Color.FromRgba (0, 255, 0, 120));

			var dh_layers = new DrawVisible_Layers (new IDrawVisible<IGraphics>[] {
				new DrawVisible_Adapter_DrawCell<IGraphics>(dec),
				new DrawVisible_Adapter_DrawCell<IGraphics>(dec_selection)
			});

			Main = new MainPanel(
				colinfo,
				rowinfo,
				dh_layers
			);
			Notify_DemoToggleSelections.Listen (Main, sel);
			#if not
			// TODO the mod happens, but the notification does not
			_main.SingleTap += (object sender, CellCoords e) => {
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
			#endif

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
									TextFont = Font.SystemFontOfSize(20, FontAttributes.Bold),
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
			BindableProperty.Create<ColumnishGrid<T>,IList<T>>(
				p => p.Rows, null);

		public IList<T> Rows {
			get { return (IList<T>)GetValue(RowsProperty); }
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

	public class TrivialGrid : Zumero.DataGrid.XF.DataGridBase
	{
		private bool gv(int col, int row, out string v)
		{
			v = Cells [row, col];
			return true;
		}

		public TrivialGrid()
		{
			var colinfo = new Dimension_FromDelegate (
				              () => Cells.GetLength (1),
				              (int n) => ColumnWidth,
				              false);
			PropertyChanged += (object sender, System.ComponentModel.PropertyChangedEventArgs e) => {
				if (e.PropertyName == TrivialGrid.ColumnWidthProperty.PropertyName) {
					colinfo.notify_changed(-1);
				}
				// TODO listen for change number
			};
			var rowinfo = new Dimension_FromDelegate (
				() => Cells.GetLength (0),
				(int n) => RowHeight,
				false);
			PropertyChanged += (object sender, System.ComponentModel.PropertyChangedEventArgs e) => {
				if (e.PropertyName == TrivialGrid.RowHeightProperty.PropertyName) {
					rowinfo.notify_changed(-1);
				}
				// TODO listen for change number
			};

			var text = new ValuePerCell_FromDelegates<string> (gv);

			var fmt = new ValuePerCell_Steady<MyTextFormat> (
				new MyTextFormat
				{
					TextFont = this.Font,
					TextColor = Color.Black,
					HorizontalTextAlignment = TextAlignment.Center,
					VerticalTextAlignment = TextAlignment.Center,
				});

			IDrawCell<IGraphics> dec = new DrawCell_Text (text, fmt);

			var padding1 = new ValuePerCell_Steady<Padding?> (new Padding (1, 1, 1, 1));
			var padding4 = new ValuePerCell_Steady<Padding?> (new Padding (4,4,4,4));
			var fill_white = new ValuePerCell_Steady<Color?> (Color.White);

			dec = new DrawCell_Chain_Padding (padding4, dec);
			dec = new DrawCell_Fill (fill_white, dec);
			dec = new DrawCell_Chain_Padding (padding1, dec);
			dec = new DrawCell_Chain_Cache (dec, colinfo, rowinfo);

			Main = new MainPanel(
				colinfo,
				rowinfo,
				new DrawVisible_Adapter_DrawCell<IGraphics>(dec)
			);
			var fill_gray = new ValuePerCell_Steady<Color?> (Color.Gray);
			var frozen_textfmt = new ValuePerCell_Steady<MyTextFormat> (
				                     new MyTextFormat {
					TextFont = Font.SystemFontOfSize (18, FontAttributes.Bold),
					TextColor = Color.Black,
					HorizontalTextAlignment = TextAlignment.Center,
					VerticalTextAlignment = TextAlignment.Center,
				});
			var val_rownum = new ValuePerCell_RowNumber ();
			var val_colnum = new ValuePerCell_ColumnLetters ();

			Left = new FrozenColumnsPanel(
				new Dimension_Steady(1, 80, false),
				rowinfo,
				new DrawVisible_Adapter_DrawCell<IGraphics>(
					new DrawCell_Chain_Padding(
						padding1,
						new DrawCell_Fill(
							fill_gray,
							new DrawCell_Text(
								val_rownum, 
								frozen_textfmt
							)
						)
					)
				)
			);

			Top = new FrozenRowsPanel (
				colinfo,
				new Dimension_Steady(1, 40, false),
				new DrawVisible_Adapter_DrawCell<IGraphics>(
					new DrawCell_Chain_Padding(
						padding1,
						new DrawCell_Fill(
							fill_gray,
							new DrawCell_Text(
								val_colnum, 
								frozen_textfmt
							)
						)
					)
				)
			);

		}

		// --------------------------------
		// Cells

		public static readonly BindableProperty CellsProperty = 
			BindableProperty.Create<TrivialGrid,string[,]>(
				p => p.Cells, null);

		public string[,] Cells {
			get { return (string[,])GetValue(CellsProperty); }
			set { SetValue(CellsProperty, value); } // TODO disallow invalid values
		}

		// --------------------------------
		// ColumnWidth

		public static readonly BindableProperty ColumnWidthProperty = 
			BindableProperty.Create<TrivialGrid,double>(
				p => p.ColumnWidth, 80);

		public double ColumnWidth {
			get { return (double)GetValue(ColumnWidthProperty); }
			set { SetValue(ColumnWidthProperty, value); } // TODO disallow invalid values
		}

		// --------------------------------
		// RowHeight

		public static readonly BindableProperty RowHeightProperty = 
			BindableProperty.Create<TrivialGrid,double>(
				p => p.RowHeight, 40);

		public double RowHeight {
			get { return (double)GetValue(RowHeightProperty); }
			set { SetValue(RowHeightProperty, value); } // TODO disallow invalid values
		}

		// --------------------------------
		// Font

		public static readonly BindableProperty FontProperty = 
			BindableProperty.Create<TrivialGrid,Nullable<Xamarin.Forms.Font>>(
				p => p.Font, Xamarin.Forms.Font.SystemFontOfSize(18));

		public Xamarin.Forms.Font Font {
			get { return (Xamarin.Forms.Font)GetValue(FontProperty); }
			set { SetValue(FontProperty, value); }
		}

	}

	public class DrawGrid : Zumero.DataGrid.XF.DataGridBase
	{
		private class myGetCellDisplayType : IGetCellDisplayType
		{
			public void func_begin_update(CellRange viz)
			{

			}

			public void func_end_update()
			{

			}

			public bool get_value(int col, int row, out int val) 
			{
				val = Math.Abs(col + row) % 3;
                return true;
			}

			public event EventHandler<CellCoords> changed;
		}

		public DrawGrid()
		{
			var colinfo = new RowColumnInfo_Steady_BindableProp (this, NumberOfColumnsProperty, ColumnWidthProperty);
			var rowinfo = new RowColumnInfo_Steady_BindableProp (this, NumberOfRowsProperty, RowHeightProperty);

			var padding4 = new ValuePerCell_Steady<Padding?> (new Padding (4, 4, 4, 4));
			var fill_blue = new ValuePerCell_Steady<Color?> (Color.Blue);
			var fill_white = new ValuePerCell_Steady<Color?> (Color.White); // TODO used to have radius 8

			Func<IDrawCell<IGraphics>,IDrawCell<IGraphics>> f = (IDrawCell<IGraphics> inner) => new DrawCell_Fill (
				fill_blue, 
				new DrawCell_Chain_Padding (
					padding4,
					new DrawCell_Fill (
						fill_white, 
						new DrawCell_Chain_Padding (
							padding4,
							inner
						)
					)
				)
			);

			var dec_oval = f (new DrawCell_Oval (Color.Red));
			var dec_arc = f (new DrawCell_PieWedge (Color.Green));
			var dec_roundedrect = f (new DrawCell_RoundedRect (Color.Yellow));

			var map = new DisplayTypeMap();
			map.Add (0, dec_oval);
			map.Add (1, dec_arc);
			map.Add (2, dec_roundedrect);

			IGetCellDisplayType getter = new myGetCellDisplayType ();
			//getter = new OneDisplayTypeForEachColumn (getter);

			var dec_switch = new DrawCell_Chain_DisplayTypes (getter, map);

			var dec_cache = new DrawCell_Chain_Cache (dec_switch, colinfo, rowinfo);

			var sel = new Selection ();

			var dec_selection = new DrawCell_FillRectIfSelected (sel, Color.FromRgba (0, 255, 0, 120));

			var dh_layers = new DrawVisible_Layers (new IDrawVisible<IGraphics>[] {
				new DrawVisible_Adapter_DrawCell<IGraphics>(dec_cache),
				new DrawVisible_Adapter_DrawCell<IGraphics>(dec_selection)
			});

			Main = new MainPanel(
				colinfo,
				rowinfo,
				dh_layers
			);
			Notify_DemoToggleSelections.Listen (Main, sel);
		}

		// --------------------------------
		// ColumnWidth

		public static readonly BindableProperty ColumnWidthProperty = 
			BindableProperty.Create<DrawGrid,double>(
				p => p.ColumnWidth, 150);

		public double ColumnWidth {
			get { return (double)GetValue(ColumnWidthProperty); }
			set { SetValue(ColumnWidthProperty, value); } // TODO disallow invalid values
		}

		// --------------------------------
		// NumberOfColumns

		public static readonly BindableProperty NumberOfColumnsProperty = 
			BindableProperty.Create<DrawGrid,int>(
				p => p.NumberOfColumns, 8);

		public int NumberOfColumns {
			get { return (int)GetValue(NumberOfColumnsProperty); }
			set { SetValue(NumberOfColumnsProperty, value); } // TODO disallow invalid values
		}

		// --------------------------------
		// RowHeight

		public static readonly BindableProperty RowHeightProperty = 
			BindableProperty.Create<DrawGrid,double>(
				p => p.RowHeight, 80);

		public double RowHeight {
			get { return (double)GetValue(RowHeightProperty); }
			set { SetValue(RowHeightProperty, value); } // TODO disallow invalid values
		}

		// --------------------------------
		// NumberOfRows

		public static readonly BindableProperty NumberOfRowsProperty = 
			BindableProperty.Create<DrawGrid,int>(
				p => p.NumberOfRows, 8);

		public int NumberOfRows {
			get { return (int)GetValue(NumberOfRowsProperty); }
			set { SetValue(NumberOfRowsProperty, value); } // TODO disallow invalid values
		}

	}
}


