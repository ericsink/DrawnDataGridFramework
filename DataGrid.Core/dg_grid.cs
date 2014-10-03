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

namespace Zumero.DataGrid.Core
{
	public interface IRegularPanel<TGraphics> : ISetView, ISendUserActions, IDrawablePanel<TGraphics>
	{
	}

	public interface IScrollablePanel<TGraphics> : IRegularPanel<TGraphics>, IScrollOffset
	{
	}

	public interface IDataGrid<TGraphics>
	{
		void Layout(double width, double height);
		void Setup(Action<IScrollablePanel<TGraphics>> f_main, Action<IRegularPanel<TGraphics>> f_frozen);
	}

	public static class FivePanels
	{
		public static void layout (
			double container_width, 
			double container_height,
			ISetFrame Main,
			IFrozenRows Top,
			IFrozenColumns Left,
			IFrozenColumns Right,
			IFrozenRows Bottom
		)
		{
			double top_height;
			if (Top == null) {
				top_height = 0;
			} else {
				top_height = Top.GetTotalHeight ().Value;
			}

			double bottom_height;
			if (Bottom == null) {
				bottom_height = 0;
			} else {
				bottom_height = Bottom.GetTotalHeight ().Value;
			}

			double left_width;
			if (Left == null) {
				left_width = 0;
			} else {
				left_width = Left.GetTotalWidth ().Value;
			}

			double right_width;
			if (Right == null) {
				right_width = 0;
			} else {
				right_width = Right.GetTotalWidth ().Value;
			}

			if (Top != null) {
				Top.SetFrame (left_width, 
					0, 
					container_width - left_width - right_width, 
					top_height
				);
			}
			if (Left != null) {
				Left.SetFrame (0, 
					top_height, 
					left_width, 
					container_height - top_height - bottom_height
				);
			}
			if (Right != null) {
				Right.SetFrame (container_width - right_width, 
					top_height, 
					right_width, 
					container_height - top_height - bottom_height
				);
			}
			if (Bottom != null) {
				Bottom.SetFrame (left_width, 
					container_height - bottom_height,
					container_width - left_width - right_width, 
					bottom_height
				);
			}
			if (Main != null) {
				Main.SetFrame (left_width, 
					top_height, 
					container_width - left_width - right_width, 
					container_height - top_height - bottom_height
				);
			}
		}
	}
}

