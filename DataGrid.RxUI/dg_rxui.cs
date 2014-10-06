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

using ReactiveUI;

namespace Zumero.DataGrid.RxUI
{
	public class RowList_Rx<T> : IRowList<T>
	{
		private readonly ReactiveList<T> _rx;

		public RowList_Rx(ReactiveList<T> rx)
		{
			_rx = rx;

			//_rx.ItemChanged.Subscribe ();

			_rx.CollectionChanged += (object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => {
				if (changed != null) {
					changed(this, null);
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
			val = _rx[row];
			return true;
		}

		public event EventHandler<CellCoords> changed;
	}
		
}

