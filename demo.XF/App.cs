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

namespace Zumero.DataGrid.Demo.XF
{
	public class App
	{
		// TODO this would need to support INotify* or IObservable?
		public class WordPair
		{
			public string en {
				get;
				set;
			}
			public string sp {
				get;
				set;
			}
		}

		public static Page GetMainPage ()
		{
			Dictionary<string, ContentPage> pages = new Dictionary<string, ContentPage> ();

			#if true
			pages ["SQLite"] = new ContentPage {
				Content = new SQLiteGrid {
					BackgroundColor = Color.Black,
					VerticalOptions = LayoutOptions.FillAndExpand,
				}
			};
			#endif

			pages["A1 2x2 wraparound rows"] = new ContentPage {
				Content = new A1Wraparound(2, 2, false, true) {
					BackgroundColor = Color.Black,
					VerticalOptions = LayoutOptions.FillAndExpand,
				}
			};

			pages["A1 2x2 wraparound columns"] = new ContentPage {
				Content = new A1Wraparound(2, 2, true, false) {
					BackgroundColor = Color.Black,
					VerticalOptions = LayoutOptions.FillAndExpand,
				}
			};

			pages["A1 2x2 wraparound both"] = new ContentPage {
				Content = new A1Wraparound(2, 2, true, true) {
					BackgroundColor = Color.Black,
					VerticalOptions = LayoutOptions.FillAndExpand,
				}
			};

			pages["A1 50x50 wraparound rows"] = new ContentPage {
				Content = new A1Wraparound(50, 50, false, true) {
					BackgroundColor = Color.Black,
					VerticalOptions = LayoutOptions.FillAndExpand,
				}
			};

			pages["A1 50x50 wraparound columns"] = new ContentPage {
				Content = new A1Wraparound(50, 50, true, false) {
					BackgroundColor = Color.Black,
					VerticalOptions = LayoutOptions.FillAndExpand,
				}
			};

			pages["A1 50x50 wraparound both"] = new ContentPage {
				Content = new A1Wraparound(50, 50, true, true) {
					BackgroundColor = Color.Black,
					VerticalOptions = LayoutOptions.FillAndExpand,
				}
			};

			pages["Cells get bigger, infinite"] = new ContentPage {
				Content = new Demo_CellsGetBigger {
					BackgroundColor = Color.Black,
					VerticalOptions = LayoutOptions.FillAndExpand,
				}
			};

			pages["Gradient"] = new ContentPage {
				Content = new Demo_GradientCells {
					BackgroundColor = Color.Black,
					VerticalOptions = LayoutOptions.FillAndExpand,
				}
			};

			pages["Shapes"] = new ContentPage {
				Content = new DrawGrid {
					BackgroundColor = Color.Black,
					VerticalOptions = LayoutOptions.FillAndExpand,
				}
			};

			pages["2D Array of Strings"] = new ContentPage {
				Content = new TrivialGrid {
					BackgroundColor = Color.Black,
					VerticalOptions = LayoutOptions.FillAndExpand,

					ColumnWidth = 100,
					RowHeight = 50,
					Font = Font.SystemFontOfSize(18),

					Cells = new string[,] {
						{ "a", "b", "c" },
						{ "d", "e", "f" },
						{ "g", "h", "i" },
						{ "j", "k", "l" },
						{ "m", "n", "o" },
						{ "p", "q", "r" },
						{ "s", "t", "u" },
						{ "v", "w", "x" },
						{ "y", "z", "." },
						{ "A", "B", "C" },
						{ "D", "E", "F" },
						{ "G", "H", "I" },
						{ "J", "K", "L" },
						{ "M", "N", "O" },
						{ "P", "Q", "R" },
						{ "S", "T", "U" },
						{ "V", "W", "X" },
						{ "Y", "Z", "." },
					},

				}
			};

			pages ["Words"] = new ContentPage {
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
					Rows = new WordPair[] {
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

			pages ["A1, infinite"] = new ContentPage {
				Content = new A1Grid {
					BackgroundColor = Color.Black,
					VerticalOptions = LayoutOptions.FillAndExpand,

					ColumnWidth = 100,
					RowHeight = 50,
					Font = Font.SystemFontOfSize(18),
				}
			};

			pages ["A1, 2x2"] = new ContentPage {
				Content = new A1Grid {
					BackgroundColor = Color.Black,
					VerticalOptions = LayoutOptions.FillAndExpand,

					ColumnWidth = 100,
					RowHeight = 50,
					Font = Font.SystemFontOfSize(18),

					Rows = 2,
					Columns = 2,

				}
			};

			pages ["A1, 100x100 tiny"] = new ContentPage {
				Content = new A1Grid {
					BackgroundColor = Color.Black,
					VerticalOptions = LayoutOptions.FillAndExpand,

					ColumnWidth = 20,
					RowHeight = 20,
					Font = Font.SystemFontOfSize(4),

					Rows = 100,
					Columns = 100,

				}
			};

			var lst = new ListView ();
			lst.ItemsSource = pages.Keys;

			var mainPage = new ContentPage {
				Content = lst
			};

			var nav = new NavigationPage (mainPage);

			lst.ItemSelected += (object sender, SelectedItemChangedEventArgs e) => {
				nav.PushAsync(pages[e.SelectedItem.ToString()]);
			};

			return nav;
		}
	}
}

