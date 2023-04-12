using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if HAS_UNO_WINUI || WINDOWS_WINUI
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#endif

namespace Uno.UI.RuntimeTests.Engine
{
	/// <summary>
	/// Contains sanity/smoke tests used to assert basic scenarios.
	/// </summary>
	[TestClass]
	public class SanityTests
	{
		[TestMethod]
		public void Is_Sane()
		{
		}

		[TestMethod]
		public async Task Is_Still_Sane()
		{
			await Task.Delay(2000);
		}

#if DEBUG
		[TestMethod]
		public async Task No_Longer_Sane() // expected to fail
		{
			await Task.Delay(2000);

			throw new Exception("Great works require a touch of insanity.");
		}

		[TestMethod, Ignore]
		public void Is_An_Illusion() // expected to be ignored
		{
		}
#endif
	}
}
