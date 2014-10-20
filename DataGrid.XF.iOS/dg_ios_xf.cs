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

using MonoTouch.Foundation;
using MonoTouch.UIKit;

using CrossGraphics;

using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

using Zumero.DataGrid.Core;
using Zumero.DataGrid.iOS.Core;

[assembly: ExportRenderer(typeof(Zumero.DataGrid.XF.DataGridBase), typeof(Zumero.DataGrid.XF.iOS.DataGridRenderer))]

namespace Zumero.DataGrid.XF.iOS
{
	public static class force_unreferenced_assembly_to_load
	{
		public static int call_this_function()
		{
			return 42;
		}
	}

	public class FivePanelsView : UIView
	{
		IDataGrid<IGraphics> _dg;

		public FivePanelsView(IDataGrid<IGraphics> g)
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

	public class DataGridRenderer : ViewRenderer<Zumero.DataGrid.XF.DataGridBase, FivePanelsView>
	{
		private FivePanelsView _container;

		protected override void OnElementChanged(ElementChangedEventArgs<Zumero.DataGrid.XF.DataGridBase> e)
		{
			base.OnElementChanged (e);

			// TODO if (Element)

			_container = new FivePanelsView (Element);

			Func<DataPanelView<IGraphics>,IGraphics> f = (DataPanelView<IGraphics> p) => {
				IGraphics gr = new CrossGraphics.CoreGraphics.CoreGraphicsGraphics (
					UIGraphics.GetCurrentContext (), true, p.Bounds.Height);
				return gr;
			};

			(Element as IDataGrid<IGraphics>).Setup (
				(IScrollablePanel<IGraphics> dg) => {
					if (dg != null)
					{
						var p = new ScrollableDataPanelView<IGraphics> (dg,f);
						p.BackgroundColor = Element.BackgroundColor.ToUIColor ();
						_container.AddSubview (p);
					}
				},
				(IRegularPanel<IGraphics> dg) => {
					if (dg != null)
					{
						var p = new DataPanelView<IGraphics> (dg, f);
						p.BackgroundColor = Element.BackgroundColor.ToUIColor ();
						_container.AddSubview (p);
					}
				}
			);

			if (e.OldElement != null) {
				e.OldElement.PropertyChanged -= OnElementPropertyChanged;
			}
			e.NewElement.PropertyChanged += OnElementPropertyChanged;

			SetNativeControl (_container);
		}
	}
}

