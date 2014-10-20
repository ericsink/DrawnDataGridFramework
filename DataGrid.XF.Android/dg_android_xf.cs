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

using Android.Views;
using Android.Graphics;
using Android.Content;
using Android.Widget;

using CrossGraphics;

using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

using Zumero.DataGrid.Core;
using Zumero.DataGrid.Android.Core;

[assembly: Xamarin.Forms.ExportRenderer(typeof(Zumero.DataGrid.XF.DataGridBase), typeof(Zumero.DataGrid.XF.Android.DataGridRenderer))]

namespace Zumero.DataGrid.XF.Android
{
	public class FivePanelsView : global::Android.Views.ViewGroup
	{
		private IDataGrid<IGraphics> _dg;

		public FivePanelsView(Context c, IDataGrid<IGraphics> g) : base(c)
		{
			_dg = g;
		}

		protected override void OnLayout (bool changed, int left, int top, int right, int bottom)
		{
			//base.OnLayout (changed, left, top, right, bottom);

			_dg.Layout (right - left, bottom - top);
		}
	}

	public class DataGridRenderer : Xamarin.Forms.Platform.Android.ViewRenderer<Zumero.DataGrid.XF.DataGridBase, FivePanelsView>
	{
		private FivePanelsView _container;

		protected override void OnElementChanged(Xamarin.Forms.Platform.Android.ElementChangedEventArgs<Zumero.DataGrid.XF.DataGridBase> e)
		{
			base.OnElementChanged(e);

			_container = new FivePanelsView (Context, Element);

			Func<Canvas,IGraphics> f = (Canvas cv) => {
				IGraphics gr = new CrossGraphics.Android.AndroidGraphics (cv);
				return gr;
			};

			(Element as IDataGrid<IGraphics>).Setup (
				(IScrollablePanel<IGraphics> p) => {
					if (p != null)
					{
						var pv = new ScrollableDataPanelView<IGraphics> (Context, p, f);
						// no backgrounds.  slow.
						//pv.SetBackgroundColor(Element.BackgroundColor.ToAndroid ());
						_container.AddView (pv);
					}
				},
				(IRegularPanel<IGraphics> p) => {
					if (p != null)
					{
						var pv = new DataPanelView<IGraphics>(Context, p, f);
						// no backgrounds.  slow.
						//pv.SetBackgroundColor(Element.BackgroundColor.ToAndroid ());
						_container.AddView (pv);
					}
				}
			);

			if (e.OldElement == null)
			{
				SetNativeControl(_container);
			}
		}
	}
}

