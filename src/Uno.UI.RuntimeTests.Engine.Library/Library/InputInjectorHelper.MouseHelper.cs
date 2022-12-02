#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Windows.Foundation;
using Windows.UI.Input;
using Windows.UI.Input.Preview.Injection;

namespace Uno.UI.RuntimeTests;

public partial class InputInjectorHelper
{
#pragma warning disable CA1822 // Mark members as static
	public class MouseHelper
	{
#if HAS_UNO
		public MouseHelper(InputInjector input)
		{
			var getCurrent = typeof(InputInjector).GetProperty("Mouse", BindingFlags.Instance | BindingFlags.NonPublic)?.GetMethod
				?? throw new NotSupportedException("This version of uno is not supported for pointer injection.");
			var currentType = getCurrent.Invoke(input, null)!.GetType();
			var getCurrentPosition = currentType.GetProperty("Position", BindingFlags.Instance | BindingFlags.Public)?.GetMethod
				?? throw new NotSupportedException("This version of uno is not supported for pointer injection.");
			var getCurrentProperties = currentType.GetProperty("Properties", BindingFlags.Instance | BindingFlags.Public)?.GetMethod
				?? throw new NotSupportedException("This version of uno is not supported for pointer injection.");

			CurrentPosition = () => (Point)getCurrentPosition.Invoke(getCurrent.Invoke(input, null)!, null)!;
			CurrentProperties = () => (PointerPointProperties)getCurrentProperties.Invoke(getCurrent.Invoke(input, null)!, null)!;
		}

		private Func<PointerPointProperties> CurrentProperties;

		private Func<Point> CurrentPosition;
#else
		public MouseHelper(InputInjector input)
		{
		}

		private Point CurrentPosition()
			=> CoreWindow.GetForCurrentThread().PointerPosition;
#endif

		public InjectedInputMouseInfo Press()
			=> new()
			{
				TimeOffsetInMilliseconds = 1,
				MouseOptions = InjectedInputMouseOptions.LeftDown,
			};

		public InjectedInputMouseInfo Release()
			=> new()
			{
				TimeOffsetInMilliseconds = 1,
				MouseOptions = InjectedInputMouseOptions.LeftUp,
			};

		public InjectedInputMouseInfo? ReleaseAny()
		{
			var options = default(InjectedInputMouseOptions);

#if HAS_UNO
			var currentProps = CurrentProperties();
			if (currentProps.IsLeftButtonPressed)
			{
				options |= InjectedInputMouseOptions.LeftUp;
			}

			if (currentProps.IsMiddleButtonPressed)
			{
				options |= InjectedInputMouseOptions.MiddleUp;
			}

			if (currentProps.IsRightButtonPressed)
			{
				options |= InjectedInputMouseOptions.RightUp;
			}

			if (currentProps.IsXButton1Pressed)
			{
				options |= InjectedInputMouseOptions.XUp;
			}
#else
			options = InjectedInputMouseOptions.LeftUp
				| InjectedInputMouseOptions.MiddleUp
				| InjectedInputMouseOptions.RightUp
				| InjectedInputMouseOptions.XUp;
#endif

			return options is default(InjectedInputMouseOptions)
				? null
				: new()
				{
					TimeOffsetInMilliseconds = 1,
					MouseOptions = options
				};
		}

		public InjectedInputMouseInfo MoveBy(int deltaX, int deltaY)
			=> new()
			{
				DeltaX = deltaX,
				DeltaY = deltaY,
				TimeOffsetInMilliseconds = 1,
				MouseOptions = InjectedInputMouseOptions.MoveNoCoalesce,
			};

		public IEnumerable<InjectedInputMouseInfo> MoveTo(double x, double y, int? steps = null)
		{
			var deltaX = x - CurrentPosition().X;
			var deltaY = y - CurrentPosition().Y;

			steps ??= (int)Math.Min(Math.Max(Math.Abs(deltaX), Math.Abs(deltaY)), 512);
			if (steps is 0)
			{
				yield break;
			}

			var stepX = deltaX / steps.Value;
			var stepY = deltaY / steps.Value;

			stepX = stepX is > 0 ? Math.Ceiling(stepX) : Math.Floor(stepX);
			stepY = stepY is > 0 ? Math.Ceiling(stepY) : Math.Floor(stepY);

			for (var step = 0; step <= steps && (stepX is not 0 || stepY is not 0); step++)
			{
				yield return MoveBy((int)stepX, (int)stepY);

				if (Math.Abs(CurrentPosition().X - x) < stepX)
				{
					stepX = 0;
				}

				if (Math.Abs(CurrentPosition().Y - y) < stepY)
				{
					stepY = 0;
				}
			}
		}
	}
}

#endif