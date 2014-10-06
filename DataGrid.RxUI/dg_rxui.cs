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

using Xamarin.Forms;

namespace Zumero.DataGrid.RxUI
{
	public class RowList_Bindable_ReactiveList<T> : IRowList<T>
	{
		private readonly BindableObject _obj;
		private readonly BindableProperty _prop;
		private IRowList<T> _next;

		public RowList_Bindable_ReactiveList(BindableObject obj, BindableProperty prop)
		{
			_obj = obj;
			_prop = prop;

			// this only listens for changes to the entire property.  like
			// when the entire IList<T> gets replaced.  it is primarily helpful
			// when we want to bind from a constructor but the property isn't set yet.
			obj.PropertyChanged += (object sender, System.ComponentModel.PropertyChangedEventArgs e) => {
				if (e.PropertyName == prop.PropertyName)
				{
					ReactiveList<T> lst = (ReactiveList<T>)obj.GetValue(prop);
					_next = new RowList_ReactiveList<T>(lst);
					if (changed != null) {
						changed(this, null);
					}
					_next.changed += (object s2, CellCoords e2) => {;
						if (changed != null) {
							changed(this, e2);
						}
					};
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

	public class RowList_ReactiveList<T> : IRowList<T>
	{
		private readonly ReactiveList<T> _rx;

		public RowList_ReactiveList(ReactiveList<T> rx)
		{
			_rx = rx;

			_rx.ChangeTrackingEnabled = true;
			_rx.ItemChanged.Subscribe (x => {
				if (changed != null) {
					int pos = _rx.IndexOf(x.Sender);
					changed(this, new CellCoords(0, pos));
				}
			});
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

