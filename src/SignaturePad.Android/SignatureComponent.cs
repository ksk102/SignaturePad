using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Views;

namespace Xamarin.Controls
{
	/** This class receive all touches wiht their respective x, y coordinates and eventually the pressure
	 * it stores the values and normalizes them
	 */
	class SignatureComponent
	{
		public long? startTime = null;
		public int? lastOrientation = null;
		public int? lastAcceleration = null;
		//private string canvas: ElementRef;
		private List<Dictionary<string, double>> currentTouch = new List<Dictionary<string, double>> ();
		//private string touchesOverTime = [];
		private List<int?> orientationOverTime = new List<int?> () { null };
		private List<int?> accelerationOverTime = new List<int?> () { null };
		public int numStrokes = 0;
		private long? lastBegin = DateTimeOffset.Now.ToUnixTimeMilliseconds ();
		//private string lastEnd = null;
		//private string secondLastEnd = null;
		public List<Dictionary<string, double?>> normalizedTouches = new List<Dictionary<string, double?>> {
			{ new Dictionary<string, double?>{ { "0", null } } }
		};
		//public string normalizedOrientation = [];
		//public string normalizedAcceleration = [];

		// convert the touches data for the use of recognition
		public void TouchStart (MotionEvent e)
		{
			lastBegin = DateTimeOffset.Now.ToUnixTimeMilliseconds ();

			if (startTime == null)
			{
				startTime = lastBegin;
				var index = GetIndexForTimestamp (lastBegin);
				AddEntryToArrayAtIndex (lastOrientation, orientationOverTime, index);
				AddEntryToArrayAtIndex (lastAcceleration, accelerationOverTime, index);
			}

			numStrokes++;

			// obtain the location and pressure of the touch
			var x = e.GetX ();
			var y = e.GetY ();
			var force = e.GetPressure (0);

			var touchPointDict = new Dictionary<string, double?>
			{
				{ "timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds () },
				{ "x", x },
				{ "y", y },
				{ "pressure", force }
			};

			AddTouchPoint (touchPointDict, DateTimeOffset.Now.ToUnixTimeMilliseconds ());

			currentTouch.Add (new Dictionary<string, double>
			{
				{ "id", e.GetPointerId(0) },
				{ "x", x },
				{ "y", y }
			});
		}

		private int GetIndexForTimestamp (long? timestamp)
		{
			if (startTime == 0)
			{
				return -1;
			}

			int diff = (int) (timestamp - startTime);
			int index = (int) (Math.Floor ((double)(diff / 10)));

			return index;
		}

		private void AddEntryToArrayAtIndex (int? entry, List<int?> array, int index, int? filler = null)
		{
			var arrayLength = (array.Count == 1) ? 0 : array.Count;
			if (arrayLength >= index)
			{
				if (array[index] != null)
				{
					return;
				}
			}

			for (var i = arrayLength; i < index; i++)
			{
				array.Add (filler);
			}

			try
			{
				array[index] = entry;
			}
			catch (ArgumentOutOfRangeException)
			{
				array.Add (entry);
			}
		}

		private void AddEntryToArrayAtIndex (Dictionary<string, double?> entry, List<Dictionary<string, double?>> array, int index, int? filler = null)
		{
			var arrayLength = (array.Count == 1) ? 0 : array.Count;
			if (arrayLength >= index)
			{
				if ((array[index])["" + index] != null)
				{
					return;
				}
			}

			for (var i = arrayLength; i < index; i++)
			{
				array.Add (new Dictionary<string, double?>
				{
					{ "" + i, filler }
				});
			}

			try
			{
				array[index] = entry;
			}
			catch (ArgumentOutOfRangeException)
			{
				array.Add (entry);
			}
			
		}

		private void AddTouchPoint(Dictionary<string, double?> touchpoint, long timestamp)
		{
			var index = GetIndexForTimestamp (timestamp);
			AddEntryToArrayAtIndex (touchpoint, normalizedTouches, index);
		}
	}
}
