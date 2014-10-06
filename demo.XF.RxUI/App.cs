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

using Zumero.DataGrid.XF;

using Xamarin.Forms;

using ReactiveUI;

namespace Zumero.DataGrid.Demo.XF.RxUI
{
	public class App
	{
		public class WordPair : ReactiveObject
		{
			private string _en;
			private string _sp;

			public string en {
				get { return _en; }
				set { this.RaiseAndSetIfChanged (ref _en, value); }
			}
			public string sp {
				get { return _sp; }
				set { this.RaiseAndSetIfChanged (ref _sp, value); }
			}
		}

		public static Page GetMainPage ()
		{
			var mainPage = new ContentPage {
				Content = new ColumnishGrid<WordPair> {
					BackgroundColor = Color.Black,
					VerticalOptions = LayoutOptions.FillAndExpand,

					RowHeight = 50,

					Columns = new ColumnishGrid<WordPair>.ColumnInfo[] {
						new ColumnishGrid<WordPair>.ColumnInfo {
							Title = "English",
							PropertyName = "en",
							Width = 100,
							FillColor = Color.Aqua,
							HorizontalAlignment = TextAlignment.Start,
						},
						new ColumnishGrid<WordPair>.ColumnInfo {
							Title = "Spanish",
							PropertyName = "sp",
							Width = 150,
							FillColor = Color.Yellow,
							HorizontalAlignment = TextAlignment.End,
						}
					},
					Rows = new ReactiveList<WordPair> {
						new WordPair { en = "drive", sp = "conducir" },
						new WordPair { en = "speak", sp = "hablar" },
						new WordPair { en = "give", sp = "dar" },
						new WordPair { en = "be", sp = "ser" },
						new WordPair { en = "go", sp = "ir" },
						new WordPair { en = "wait", sp = "esperar" },
						new WordPair { en = "live", sp = "vivir" },
						new WordPair { en = "walk", sp = "andar" },
						new WordPair { en = "run", sp = "correr" },
						new WordPair { en = "sleep", sp = "dormir" },
						new WordPair { en = "want", sp = "querer" },
					}
				}
			};

			var nav = new NavigationPage (mainPage);

			return nav;
		}
	}
}

