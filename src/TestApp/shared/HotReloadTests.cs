using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#if HAS_UNO_WINUI || WINDOWS_WINUI
using Microsoft.UI.Xaml.Controls;
#else
using Windows.UI.Xaml.Controls;
#endif

namespace Uno.UI.RuntimeTests.Engine;

[TestClass]
[RunsInSecondaryApp]
public class HotReloadTests
{
	[TestMethod]
	public async Task Is_SourcesEditable()
	{
		var ct = CancellationToken.None;

		var sutPath = "../../shared/HotReloadTests_Subject.xaml";
		var dir = Path.GetDirectoryName(typeof(HotReloadHelper).Assembly.GetCustomAttribute<RuntimeTestsSourceProjectAttribute>()!.ProjectFullPath)!;
		var file = Path.Combine(dir, sutPath);


		Assert.IsTrue(File.Exists(file));
		Assert.IsTrue(File.ReadAllText(file).Contains("Original text"));

		await using var _ = await HotReloadHelper.UpdateServerFile(sutPath, "Original text", "Updated text from Can_Edit_File", waitForMetadataUpdate: false, ct);

		await TestHelper.WaitFor(() => File.ReadAllText(file).Contains("Updated text from Can_Edit_File"), ct);

		Assert.IsTrue(File.ReadAllText(file).Contains("Updated text from Can_Edit_File"));
	}

	[TestMethod]
	[Ignore("Hot reload not working yet")]
	public async Task Is_HotReload_Enabled()
	{
		var ct = CancellationToken.None;

		var sut = new HotReloadTests_Subject();
		await UIHelper.Load(sut, ct);

		Console.WriteLine("Loaded");

		var text = UIHelper.FindChildren<TextBlock>(sut).Single();

		Console.WriteLine("Found text: " + text.Text);

		Assert.AreEqual("Original text", text.Text);

		await using var _ = await HotReloadHelper.UpdateServerFile<HotReloadTests_Subject>("Original text", "Updated text", ct);

		await Task.Delay(1500, ct);

		sut = new HotReloadTests_Subject();
		await UIHelper.Load(sut, ct);
		text = UIHelper.FindChildren<TextBlock>(sut).Single();

		await TestHelper.WaitFor(() => text.Text == "Updated text", ct);
		Assert.AreEqual("Updated text", text.Text);
	}
}