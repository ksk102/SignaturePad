﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.Views;
using Android.Hardware;
using Android.App;
using Android.Content;

using Org.Json;

namespace Xamarin.Controls
{
	/** This class receive all touches wiht their respective x, y coordinates and eventually the pressure
	 * it stores the values and normalizes them
	 */
	class SignatureComponent : Activity, ISensorEventListener
	{
		private SensorManager sensorManager;
		public int numStrokes = 0;
		public long? startTime = null;
		private long? lastBegin = DateTimeOffset.Now.ToUnixTimeMilliseconds ();
		private long? lastEnd = null;
		public double? lastOrientation = null;
		public double? lastAcceleration = null;
		private List<double?> orientationOverTime = new List<double?> () { null };
		private List<double?> accelerationOverTime = new List<double?> () { null };
		public JSONArray normalizedTouches = new JSONArray ();
		public JSONArray normalizedOrientation = new JSONArray ();
		public JSONArray normalizedAcceleration = new JSONArray ();

		public SignatureComponent (Context context)
		{
			sensorManager = (SensorManager) context.GetSystemService (Context.SensorService);

			// Orientation
			sensorManager.RegisterListener (this, sensorManager.GetDefaultSensor (SensorType.Orientation), SensorDelay.Ui);

			// Acceleration
			sensorManager.RegisterListener (this, sensorManager.GetDefaultSensor (SensorType.LinearAcceleration), SensorDelay.Ui);
		}

		~SignatureComponent ()
		{
			sensorManager.UnregisterListener (this);
			sensorManager.Dispose ();
			sensorManager = null;
		}

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

		public void TouchMove (MotionEvent e)
		{
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

		public void TouchEnd (MotionEvent e)
		{
			lastEnd = DateTimeOffset.Now.ToUnixTimeMilliseconds ();
		}

		public void Clear ()
		{
			startTime = null;
			normalizedTouches = new JSONArray();
			normalizedOrientation = new JSONArray ();
			normalizedAcceleration = new JSONArray ();
			numStrokes = 0;
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

		private void AddEntryToArrayAtIndex (double? entry, ref List<double?> array, int index, double? filler = null)
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

		private void AddEntryToArrayAtIndex (JSONObject entry, ref JSONArray array, int index, double? filler = null)
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

		private void AddEntryToArrayAtIndex (double? entry, ref JSONArray array, int index, double? filler = null)
		{
			var arrayLength = array.Length ();
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

		public void OnAccuracyChanged (Sensor sensor, SensorStatus accuracy)
		{

		}

		public void OnSensorChanged (SensorEvent e)
		{
			var sensor = e.Sensor;

			if (sensor.Type == SensorType.Orientation)
			{
				var entry = e.Values[0];
				lastOrientation = entry;

				try
				{
					var index = GetIndexForTimestamp (DateTimeOffset.Now.ToUnixTimeMilliseconds ());
					AddEntryToArrayAtIndex (entry, ref normalizedOrientation, index, entry);
				}
				catch (ArgumentNullException)
				{

				}
			}
			else if (sensor.Type == SensorType.LinearAcceleration)
			{
				var entry = Math.Sqrt (e.Values[0] * e.Values[0] + e.Values[1] * e.Values[1] + e.Values[2] * e.Values[2]);
				lastAcceleration = entry;

				try
				{
					var index = GetIndexForTimestamp (DateTimeOffset.Now.ToUnixTimeMilliseconds ());
					AddEntryToArrayAtIndex (entry, ref normalizedAcceleration, index);
				}
				catch (ArgumentNullException)
				{

				}
			}
		}

		public JSONArray GetTouches ()
		{
			return normalizedTouches;
		}

		public JSONArray GetOrientation ()
		{
			var cutOrientation = JsonArrayRemoveFrom (normalizedOrientation, GetTouches ().Length ());

			while (cutOrientation.Length () < GetTouches ().Length ())
			{
				cutOrientation.Put (lastOrientation);
			}

			return cutOrientation;
		}

		public JSONArray GetAcceleration ()
		{
			var cutAcceleration = JsonArrayRemoveFrom (normalizedAcceleration, GetTouches ().Length ());

			while (cutAcceleration.Length() < GetTouches ().Length ())
			{
				cutAcceleration.Put (null);
			}

			return cutAcceleration;
		}

		public double GetWidth ()
		{
			GetMinMaxFor ("x", out var min, out var max);
			return Math.Round(max - min);
		}

		public double GetHeight ()
		{
			GetMinMaxFor ("y", out var min, out var max);
			return Math.Round (max - min);
		}

		public void GetMinMaxFor (string variable, out double min, out double max)
		{
			max = 0.0;
			min = 99999.99;
			var len = GetTouches ().Length ();

			for (var i = 0; i < len; i++)
			{
				if (!GetTouches ().IsNull (i))
				{
					var jsonObject = GetTouches ().OptJSONObject (i);
					if (jsonObject.OptDouble (variable) < min)
					{
						min = jsonObject.OptDouble (variable);
					}

					if (jsonObject.OptDouble (variable) > max)
					{
						max = jsonObject.OptDouble (variable);
					}
				}
			}
		}

		public int GetNumStrokes ()
		{
			return numStrokes;
		}

		public JSONArray JsonArrayRemoveFrom (JSONArray array, int index)
		{
			if (array.Length() <= index)
			{
				return array;
			}

			var newArray = new JSONArray ();

			for (var i = 0; i < index; i++)
			{
				newArray.Put (array.Opt(i));
			}

			return newArray;
		}
	}
}
