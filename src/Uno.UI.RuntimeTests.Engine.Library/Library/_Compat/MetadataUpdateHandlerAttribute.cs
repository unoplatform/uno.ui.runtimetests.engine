#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY && WINDOWS_UWP
#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif


using System.Diagnostics.CodeAnalysis;

#nullable enable
namespace System.Reflection.Metadata
{
	/// <summary>Indicates that a type that should receive notifications of metadata updates.</summary>
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	public sealed class MetadataUpdateHandlerAttribute : Attribute
	{
		/// <summary>Initializes the attribute.</summary>
		/// <param name="handlerType">A type that handles metadata updates and that should be notified when any occur.</param>
		public MetadataUpdateHandlerAttribute(/*[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]*/ Type handlerType) => this.HandlerType = handlerType;

		/// <summary>Gets the type that handles metadata updates and that should be notified when any occur.</summary>
		/*[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]*/
		public Type HandlerType { get; }
	}
}
#endif