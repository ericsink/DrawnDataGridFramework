using System;
using System.Collections.Generic;
using System.Linq;

using MonoTouch.Foundation;
using MonoTouch.UIKit;

using Zumero.DataGrid.iOS.UIKit;

namespace Zumero.DataGrid.Demo.XF.iOS
{
	[Register ("AppDelegate")]
	public partial class AppDelegate : UIApplicationDelegate
	{
		UIWindow window;

		public override bool FinishedLaunching (UIApplication app, NSDictionary options)
		{
			window = new UIWindow (UIScreen.MainScreen.Bounds);
			
            var vc = new UIViewController();
			var grid = new OvalGrid();
			var v = new DataGridView (grid);
			vc.View = v;

			window.RootViewController = vc;
			window.MakeKeyAndVisible ();
			
			return true;
		}
	}
}

