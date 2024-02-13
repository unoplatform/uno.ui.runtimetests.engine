#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif

#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY && WINDOWS_UWP
using System;
using System.Collections.Generic;
using System.Text;

namespace System.Runtime.CompilerServices
{
	/// <summary>Allows capturing of the expressions passed to a method.</summary>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
	public sealed class CallerArgumentExpressionAttribute : Attribute
	{
		/// <summary>Initializes a new instance of the <see cref="T:System.Runtime.CompilerServices.CallerArgumentExpressionAttribute" /> class.</summary>
		/// <param name="parameterName">The name of the targeted parameter.</param>
		public CallerArgumentExpressionAttribute(string parameterName) => this.ParameterName = parameterName;

		/// <summary>Gets the target parameter name of the CallerArgumentExpression.</summary>
		/// <returns>The name of the targeted parameter of the CallerArgumentExpression.</returns>
		public string ParameterName { get; }
	}
}
#endif
