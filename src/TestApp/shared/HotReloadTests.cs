using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.Logging;

#if HAS_UNO_WINUI || WINDOWS_WINUI
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
#else
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#endif

namespace Uno.UI.RuntimeTests.Engine;

[TestClass]
[RunsInSecondaryApp]
public class HotReloadTests
{
	[TestMethod]
	public async Task Is_SourcesEditable(CancellationToken ct)
	{
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
	public async Task Is_HotReload_Enabled(CancellationToken ct)
	{
		await UIHelper.Load(new HotReloadTests_Subject(), ct);

		Assert.AreEqual("Original text", UIHelper.FindChildren<TextBlock>().Single().Text);

		await using var _ = await HotReloadHelper.UpdateServerFile<HotReloadTests_Subject>("Original text", "Updated text", ct);

		await TestHelper.WaitFor(() => UIHelper.FindChildren<TextBlock>().Single().Text, "Updated text", ct);
	}
}