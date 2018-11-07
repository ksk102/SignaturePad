using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.Views;

using Org.Json;

namespace Xamarin.Controls
{
	/** This class receive all touches wiht their respective x, y coordinates and eventually the pressure
	 * it stores the values and normalizes them
	 */
	class SignatureComponent
	{
		public int numStrokes = 0;
		public long? startTime = null;
		private long? lastBegin = DateTimeOffset.Now.ToUnixTimeMilliseconds ();
		public int? lastOrientation = null;
		public int? lastAcceleration = null;
		private List<int?> orientationOverTime = new List<int?> () { null };
		private List<int?> accelerationOverTime = new List<int?> () { null };
		public JSONArray normalizedTouches = new JSONArray ();
		//public string normalizedOrientation = [];
		//public string normalizedAcceleration = [];
		//private string touchesOverTime = [];
		//private string lastEnd = null;
		//private string secondLastEnd = null;

		// convert the touches data for the use of recognition
		public void TouchStart (MotionEvent e)
		{
			lastBegin = DateTimeOffset.Now.ToUnixTimeMilliseconds ();

			if (startTime == null)
			{
				startTime = lastBegin;
				var index = GetIndexForTimestamp (lastBegin);
				AddEntryToArrayAtIndex (lastOrientation, ref orientationOverTime, index);
				AddEntryToArrayAtIndex (lastAcceleration, ref accelerationOverTime, index);
			}

			numStrokes++;

			// obtain the location and pressure of the touch
			var x = e.GetX ();
			var y = e.GetY ();
			var force = e.GetPressure (0);

			var touchPoint = new JSONObject ();
			touchPoint.Put ("timestamp", DateTimeOffset.Now.ToUnixTimeMilliseconds ());
			touchPoint.Put ("x", x);
			touchPoint.Put ("y", y);
			touchPoint.Put ("pressure", force);

			AddTouchPoint (touchPoint, DateTimeOffset.Now.ToUnixTimeMilliseconds ());
		}

		private int GetIndexForTimestamp (long? timestamp)
		{
			if (startTime == null)
			{
				throw new ArgumentNullException ("startTime cannot be null");
			}

			var diff = (int) (timestamp - startTime);
			var index = (int) (Math.Floor (diff / 10.0));

			return index;
		}

		private void AddEntryToArrayAtIndex (int? entry, ref List<int?> array, int index, int? filler = null)
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

		private void AddEntryToArrayAtIndex (JSONObject entry, ref JSONArray array, int index, int? filler = null)
		{
			var arrayLength = array.Length();
			if (!array.IsNull (index))
			{
				return;
			}

			for (var i = arrayLength; i < index; i++)
			{
				array.Put (filler);
			}

			array.Put (entry);
		}

		private void AddTouchPoint(JSONObject touchpoint, long timestamp)
		{
			var index = GetIndexForTimestamp (timestamp);
			AddEntryToArrayAtIndex (touchpoint, ref normalizedTouches, index);
		}
	}
}
