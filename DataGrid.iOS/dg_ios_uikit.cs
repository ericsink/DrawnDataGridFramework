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
using System.Drawing;

using MonoTouch.CoreGraphics;
using MonoTouch.Foundation;
using MonoTouch.UIKit;

using System.ComponentModel;

using Zumero.DataGrid.Core;
using Zumero.DataGrid.iOS.Core;

namespace Zumero.DataGrid.iOS.UIKit
{
	public class DataGridBase : IDataGrid<CGContext>
	{
		protected interface IMainPanel : ISetFrame, IScrollablePanel<CGContext>, INotifyUserActions, IOnScroll
		{
		}

		protected interface IFrozenRowsPanel : IFrozenRows, IRegularPanel<CGContext>, INotifyUserActions, IFrozenScrollOffset
		{
		}

		protected interface IFrozenColumnsPanel : IFrozenColumns, IRegularPanel<CGContext>, INotifyUserActions, IFrozenScrollOffset
		{
		}

		protected IMainPanel Main;

		protected IFrozenRowsPanel Top;
		protected IFrozenRowsPanel Bottom;

		protected IFrozenColumnsPanel Left;
		protected IFrozenColumnsPanel Right;

		protected class MainPanel : DataPanel<CGContext>, IMainPanel
		{
			public MainPanel(IDimension cbcol, IDimension cbrow, IDrawVisible<CGContext> cbdraw) : base(cbcol, cbrow, cbdraw)
			{
			}
		}

		protected class FrozenColumnsPanel : DataPanel<CGContext>, IFrozenColumnsPanel
		{
			public FrozenColumnsPanel(IDimension cbcol, IDimension cbrow, IDrawVisible<CGContext> cbdraw) : base(cbcol, cbrow, cbdraw)
			{
			}
		}

		protected class FrozenRowsPanel : DataPanel<CGContext>, IFrozenRowsPanel
		{
			public FrozenRowsPanel(IDimension cbcol, IDimension cbrow, IDrawVisible<CGContext> cbdraw) : base(cbcol, cbrow, cbdraw)
			{
			}
		}

		// this exists entirely so that the subclass doesn't need to say <TGraphics> for padding
		protected class DrawCell_Chain_Padding : DrawCell_Chain_Padding<CGContext>
		{
			public DrawCell_Chain_Padding(IValuePerCell<Padding?> vals, IDrawCell<CGContext> cd) : base(vals, cd)
			{
			}
		}

		void IDataGrid<CGContext>.Layout (double container_width, double container_height)
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

		void IDataGrid<CGContext>.Setup (
			Action<IScrollablePanel<CGContext>> f_main,
			Action<IRegularPanel<CGContext>> f_frozen
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

		protected class DrawCell_Oval : DrawCell<CGContext,CGColor>
		{
			private static void draw(CGColor clr, int col, int row, double x, double y, double width, double height, CGContext gr)
			{
				gr.SetFillColor (clr);
				gr.FillEllipseInRect (new RectangleF ((float) x, (float) y, (float) width, (float) height));
			}

			public DrawCell_Oval(CGColor clr) : base(new ValuePerCell_Steady<CGColor>(clr), draw)
			{
			}
		}		

		protected class DrawCell_Fill : DrawCell<CGContext,CGColor>
		{
			private static void draw(CGColor clr, int col, int row, double x, double y, double width, double height, CGContext gr)
			{
				gr.SetFillColor (clr);
				gr.FillRect(new RectangleF((float) x,(float) y,(float) width,(float) height));
			}

			public DrawCell_Fill(IValuePerCell<CGColor> vals, IDrawCell<CGContext> chain = null) : base(vals, draw, chain)
			{
			}
		}

	}

	public class OvalGrid : DataGridBase
	{
		public OvalGrid()
		{
			var colinfo = new Dimension_Steady (100000, 120, false);
			var rowinfo = new Dimension_Steady (100000, 80, false);

			var padding4 = new ValuePerCell_Steady<Padding?> (new Padding (5));
			var fill_blue = new ValuePerCell_Steady<CGColor> (UIColor.Blue.CGColor);
			var fill_white = new ValuePerCell_Steady<CGColor> (UIColor.White.CGColor);

			var dec_oval = new DrawCell_Fill (
				fill_blue, 
				new DrawCell_Chain_Padding (
					padding4,
					new DrawCell_Fill (
						fill_white, 
						new DrawCell_Chain_Padding (
							padding4,
							new DrawCell_Oval (UIColor.Purple.CGColor)
						)
					)
				)
			);

			Main = new MainPanel(
				colinfo,
				rowinfo,
				new DrawVisible_Adapter_DrawCell<CGContext>(dec_oval)
			);
		}

	}

	public class FivePanelsView : UIView
	{
		IDataGrid<CGContext> _dg;

		public FivePanelsView(IDataGrid<CGContext> g)
		{
			_dg = g;

			//this.AutosizesSubviews = false;
		}

		public override void LayoutSubviews ()
		{
			base.LayoutSubviews ();

			_dg.Layout (Bounds.Width, Bounds.Height);
		}
	}

	public class DataGridView : FivePanelsView
	{
		public DataGridView(DataGridBase grid) : base(grid)
		{
			Func<DataPanelView<CGContext>,CGContext> f = (DataPanelView<CGContext> p) => {
				return UIGraphics.GetCurrentContext ();
			};

			var g = (IDataGrid<CGContext>) grid;

			g.Setup (
				(IScrollablePanel<CGContext> dg) => {
					if (dg != null)
					{
						var p = new ScrollableDataPanelView<CGContext> (dg,f);
						AddSubview (p);
					}
				},
				(IRegularPanel<CGContext> dg) => {
					if (dg != null)
					{
						var p = new DataPanelView<CGContext> (dg, f);
						AddSubview (p);
					}
				}
			);

			// TODO addview?
		}
	}
}


