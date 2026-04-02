#if !IS_UNO_RUNTIMETEST_PROJECT
#pragma warning disable
#endif
#nullable enable

#if !UNO_RUNTIMETESTS_DISABLE_LIBRARY
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
		private Point _trackedPosition;

		public MouseHelper(InputInjector input)
		{
		}

		private Point CurrentPosition()
			=> _trackedPosition;

		/// <summary>
		/// Resets the internally tracked position without injecting mouse movement.
		/// On WinUI 3 (real Windows), injecting MoveTo(0,0) would actually move the OS cursor
		/// to the upper-left corner of the screen, potentially triggering hot corners.
		/// </summary>
		internal void ResetTrackedPosition()
			=> _trackedPosition = default;

		/// <summary>
		/// Sets the tracked position to the given window-client-relative coordinates.
		/// Used after directly positioning the OS cursor via SetCursorPos so that
		/// subsequent MoveTo calls compute correct relative deltas.
		/// </summary>
		internal void SetTrackedPosition(double x, double y)
			=> _trackedPosition = new Point(x, y);
#endif

		/// <summary>
		/// Create an injected pointer info which presses the left button
		/// </summary>
		public InjectedInputMouseInfo Press()
			=> new()
			{
				MouseOptions = InjectedInputMouseOptions.LeftDown,
			};

		/// <summary>
		/// Create an injected pointer info which release the left button
		/// </summary>
		public InjectedInputMouseInfo Release()
			=> new()
			{
				MouseOptions = InjectedInputMouseOptions.LeftUp,
			};

		/// <summary>
		/// Create an injected pointer info which releases any pressed button
		/// </summary>
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
			// XUp is intentionally excluded: the Windows InputInjector API requires MouseData
			// to specify which X button to release, and we don't track X button state.
			options = InjectedInputMouseOptions.LeftUp
				| InjectedInputMouseOptions.MiddleUp
				| InjectedInputMouseOptions.RightUp;
#endif

			return options is default(InjectedInputMouseOptions)
				? null
				: new()
				{
					MouseOptions = options
				};
		}

		/// <summary>
		/// Create an injected pointer info which moves the mouse by the given offests
		/// </summary>
		public InjectedInputMouseInfo MoveBy(int deltaX, int deltaY)
		{
#if !HAS_UNO
			_trackedPosition = new Point(_trackedPosition.X + deltaX, _trackedPosition.Y + deltaY);
#endif
			return new()
			{
				DeltaX = deltaX,
				DeltaY = deltaY,
				MouseOptions = InjectedInputMouseOptions.Move,
			};
		}

		/// <summary>
		/// Create some injected pointer infos which moves the mouse to the given coordinates
		/// </summary>
		/// <param name="x">The target x position</param>
		/// <param name="y">The traget y position</param>
		/// <param name="steps">Number injected pointer infos to generate to simutale a smooth manipulation.</param>
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