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

