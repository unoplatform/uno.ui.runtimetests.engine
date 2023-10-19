using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;

#if HAS_UNO_WINUI || WINDOWS_WINUI
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#endif

namespace Uno.UI.RuntimeTests.Engine;

[TestClass]
public class HotReloadSanity
{
	[TestMethod]
	public void Is_HotReload_Supported()
	{
#if __SKIA__ // DevServer should be referenced also in release
		Assert.IsTrue(HotReloadHelper.IsSupported);
#else
		Assert.IsFalse(HotReloadHelper.IsSupported);
#endif
	}
}

[TestClass]
[RunsInSecondaryApp]
public class HotReloadTests
{
	[TestMethod]
#if !__SKIA__
	[Ignore("This tests assume directs access to the file system which not possible on this platform.")]
#endif
	public async Task Is_SourcesEditable(CancellationToken ct)
	{
		var sutPath = "../../shared/HotReloadTests_Subject.xaml";
		var dir = Path.GetDirectoryName(typeof(HotReloadHelper).Assembly.GetCustomAttribute<RuntimeTestsSourceProjectAttribute>()!.ProjectFullPath)!;
		var file = Path.Combine(dir, sutPath);

		Assert.IsTrue(File.Exists(file));
		Assert.IsTrue(File.ReadAllText(file).Contains("Original text"));

		await using var _ = await HotReloadHelper.UpdateSourceFile(sutPath, "Original text", "Updated text from Can_Edit_File", waitForMetadataUpdate: false, ct);

		await TestHelper.WaitFor(() => File.ReadAllText(file).Contains("Updated text from Can_Edit_File"), ct);

		Assert.IsTrue(File.ReadAllText(file).Contains("Updated text from Can_Edit_File"));
	}

	[TestMethod]
	public async Task Is_CodeHotReload_Enabled(CancellationToken ct)
	{
		if (!HotReloadHelper.IsSupported)
		{
			Assert.Inconclusive("Hot reload testing is not supported on this platform.");
		}

		var sut = new HotReloadTest_SimpleSubject();

		Debug.Assert(sut.Value == "42");

		await using var _ = await HotReloadHelper.UpdateSourceFile("../../shared/HotReloadTest_SimpleSubject.cs", "42", "43", ct);

		Debug.Assert(sut.Value == "43");
	}

	[TestMethod]
	public async Task Is_UIHotReload_Enabled(CancellationToken ct)
	{
		if (!HotReloadHelper.IsSupported)
		{
			Assert.Inconclusive("Hot reload testing is not supported on this platform.");
		}

		await UIHelper.Load(new HotReloadTests_Subject(), ct);

		Assert.AreEqual("Original text", UIHelper.GetChild<TextBlock>().Text);

		await using var _ = await HotReloadHelper.UpdateSourceFile<HotReloadTests_Subject>("Original text", "Updated text", ct);

		await AsyncAssert.AreEqual("Updated text", () => UIHelper.GetChild<TextBlock>().Text, ct);
	}
}