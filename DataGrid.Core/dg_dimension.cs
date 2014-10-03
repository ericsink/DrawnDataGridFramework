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
	public interface IDimension
	{
		// if number is null, then there are an infinite number of rows/columns
		int? number {
			get;
		}

		// only allowed to be true if number is not null
		bool wraparound {
			get;
		}

		bool variable_sizes {
			get;
		}

		double func_size(int n) ;

		event EventHandler<int> changed; // TODO the fact that this is OneCoord is kind of a pain since it can't implement IChanged
	}

	public abstract class Dimension_Base : IDimension
	{
		public abstract bool variable_sizes { get; }
		public abstract bool wraparound { get; }
		public abstract int? number { get; }
		public abstract double func_size (int n);

		public event EventHandler<int> changed;
		public void notify_changed(int n)
		{
			if (changed != null)
			{
				changed(this, n);
			}
		}
	}

	public class Dimension_FromDelegate : Dimension_Base
	{
		private readonly Func<int?> _f_number;
		private readonly bool _wrap;
		private readonly Func<int,double> _f_size;

		public Dimension_FromDelegate(int? num, Func<int,double> f_size, bool wrap)
		{
			_f_number = () => num;
			_wrap = wrap;
			_f_size = f_size;
			// TODO something to listen to
		}

		public Dimension_FromDelegate(Func<int?> f_num, Func<int,double> f_size, bool wrap)
		{
			_f_number = f_num;
			_wrap = wrap;
			_f_size = f_size;
			// TODO something to listen to
		}

		public override bool variable_sizes {
			get {
				return true;
			}
		}

		public override bool wraparound {
			get {
				return _wrap;
			}
		}

		public override int? number
		{
			get {
				return _f_number();
			}
		}

		public override double func_size(int n) 
		{
			return _f_size(n);
		}
	}

	public class Dimension_Steady : Dimension_Base
	{
		private readonly int _number;
		private readonly double _size;
		private readonly bool _wrap;

		public Dimension_Steady(int num, double size, bool wrap) // TODO no bool arg
		{
			_number = num;
			_size = size;
			_wrap = wrap;
		}

		public override bool variable_sizes {
			get {
				return false;
			}
		}

		public override bool wraparound {
			get {
				return _wrap;
			}
		}

		public override int? number
		{
			get {
				return _number;
			}
		}

		public override double func_size(int n) 
		{
			return _size;
		}
	}

}

