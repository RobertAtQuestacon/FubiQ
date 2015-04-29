using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Specialized;

using FubiNET;

namespace Fubi_WPF_GUI
{
	public class FubiMouseKeyboard
	{
		internal class NativeMethods
		{
			[DllImport("user32.dll", SetLastError = true)]
			public static extern Int32 GetSystemMetrics(Int32 metric);

			public delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);

			//Declare the mouse hook constant.
			//For other hook types, you can obtain these values from Winuser.h in the Microsoft SDK.
			public const int WhMouse = 7;

			//Declare the wrapper managed POINT class.
			[StructLayout(LayoutKind.Sequential)]
			public class Point
			{
				public int x;
				public int y;
			}

			//Declare the wrapper managed MouseHookStruct class.
			[StructLayout(LayoutKind.Sequential)]
			public class MouseHookStruct
			{
				public Point pt;
				public int hwnd;
				public int wHitTestCode;
				public int dwExtraInfo;
			}

			//This is the Import for the SetWindowsHookEx function.
			//Use this function to install a thread-specific hook.
			[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
			public static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hInstance, int threadId);

			//This is the Import for the UnhookWindowsHookEx function.
			//Call this function to uninstall the hook.
			[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
			public static extern bool UnhookWindowsHookEx(int idHook);

			//This is the Import for the CallNextHookEx function.
			//Use this function to pass the hook information to the next hook procedure in chain.
			[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
			public static extern int CallNextHookEx(int idHook, int nCode, IntPtr wParam, IntPtr lParam);
		};

		public class KeyMouseBinding
		{
			public KeyMouseBinding()
			{
			}
			public KeyMouseBinding(string gesture, string keyMouseEvent)
			{
				if (MainWindow.GestureList.ContainsKey(gesture))
					Gesture = new KeyValuePair<string, MainWindow.GestureType>(gesture, MainWindow.GestureList[gesture]);
				KeyMouseEvent = keyMouseEvent;
			}
			public KeyValuePair<string, MainWindow.GestureType> Gesture { get; set; }
			public string KeyMouseEvent { get; set; }
		};
		public class SerializableKeyMouseBinding
		{
			public string Gesture { get; set; }
			public string KeyMouseEvent { get; set; }
			public SerializableKeyMouseBinding() { }
			public SerializableKeyMouseBinding(string gesture, string keyMouseEvent)
			{
				Gesture = gesture;
				KeyMouseEvent = keyMouseEvent;
			}
		};
		public class KeyMouseBindingCollection : ObservableCollection<KeyMouseBinding>
		{
			public void saveToXML(string fileName)
			{
				var serializableList = Items.Select(binding => new SerializableKeyMouseBinding(binding.Gesture.Key, binding.KeyMouseEvent)).ToList();

				try
				{
					var xs = new XmlSerializer(typeof(List<SerializableKeyMouseBinding>));
					using (var wr = new StreamWriter(fileName))
					{
						xs.Serialize(wr, serializableList);
					}
				}
				catch (Exception e)
				{
					throw new Exception("Error while saving bindings file " + fileName + ": " + e.Message);
				}
			}
			public void loadFromXML(string fileName)
			{
				try
				{
					var xs = new XmlSerializer(typeof(List<SerializableKeyMouseBinding>));
					List<SerializableKeyMouseBinding> deserializedList;
					using (var rd = new StreamReader(fileName))
					{
						deserializedList = xs.Deserialize(rd) as List<SerializableKeyMouseBinding>;
					}
					if (deserializedList != null)
					{
						foreach (var binding in deserializedList)
						{
							Items.Add(new KeyMouseBinding(binding.Gesture, binding.KeyMouseEvent));
						}
					}
					OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
				}
				catch (Exception e)
				{
					throw new Exception("Error while loading bindings file " +  fileName + ": " + e.Message);
				}
			}
		};
		public KeyMouseBindingCollection Bindings = new KeyMouseBindingCollection();
		public KeyMouseBinding getBinding(string gesture)
		{
			return Bindings.FirstOrDefault(i => Equals(i.Gesture.Key, gesture));
		}

		public FubiMouseKeyboard()
		{
			m_timeStamp = 0;
			m_lastMouseClick = 0;

			// Start mouse pos in the middle
			m_x = 0.5f;
			m_y = 0.5f;

			// Default mapping values
			m_mapX = -100.0f;
			m_mapY = 250.0f;
			m_mapH = 550.0f;

			// Get screen aspect
			var screenWidth = NativeMethods.GetSystemMetrics(0) - 1;
			var screenHeight = NativeMethods.GetSystemMetrics(1) - 1;
			m_aspect = screenWidth / (float)screenHeight;

			// Calculated Map width with aspect
			m_mapW = m_mapH / m_aspect;

			m_smoothingFactor = 0.5f;
		}

		public void applyHandPositionToMouse(uint userID, bool leftHand = false)
		{
			float x, y;
			applyHandPositionToMouse(userID, out x, out y, leftHand);
		}

		public void applyHandPositionToMouse(uint userID, out float x, out float y, bool leftHand = false)
		{
			x = float.NaN;
			y = float.NaN;

			float handX, handY, handZ, confidence;

			var joint = FubiUtils.SkeletonJoint.RIGHT_HAND;
			var relJoint = FubiUtils.SkeletonJoint.RIGHT_SHOULDER;
			if (leftHand)
			{
				joint = FubiUtils.SkeletonJoint.LEFT_HAND;
				relJoint = FubiUtils.SkeletonJoint.LEFT_SHOULDER;
			}

			double timeStamp;
			Fubi.getCurrentSkeletonJointPosition(userID, joint, out handX, out handY, out handZ, out confidence, out timeStamp);

			if (confidence > 0.5f)
			{
				float relX, relY, relZ;
				Fubi.getCurrentSkeletonJointPosition(userID, relJoint, out relX, out relY, out relZ, out confidence, out timeStamp);

				if (confidence > 0.5f)
				{
					// Take relative coordinates
					var rawX = handX - relX;
					var rawY = handY - relY;

					// Convert to screen coordinates
					var mapX = m_mapX;
					if (leftHand)
						// Mirror x  area for left hand
						mapX = -m_mapX - m_mapW;
					float newX = (rawX - mapX) / m_mapW;
					float newY = (m_mapY - rawY) / m_mapH;

					// Filtering
					// New coordinate is weighted more if it represents a longer distance change
					// This should reduce the lagging of the cursor on higher distances, but still filter out small jittering
					var changeX = newX - m_x;
					var changeY = newY - m_y;

					if (Math.Abs(changeX) > float.Epsilon || Math.Abs(changeY) > float.Epsilon && Math.Abs(timeStamp - m_timeStamp) > float.Epsilon)
					{
						var changeLength = Math.Sqrt(changeX * changeX + changeY * changeY);
						var filterFactor = (float)Math.Sqrt(changeLength) * m_smoothingFactor;
						if (filterFactor > 1.0f)
							filterFactor = 1.0f;

						// Apply the tracking to the current position with the given filter factor
						m_x = (1.0f - filterFactor) * m_x + filterFactor * newX;
						m_y = (1.0f - filterFactor) * m_y + filterFactor * newY;
						m_timeStamp = timeStamp;

						// Send it
						MouseKeyboardSimulator.sendMousePos(m_x, m_y);
						x = m_x;
						y = m_x;
					}
				}
			}
		}

		public void applyBinding(string gesture, bool down)
		{
			var b = getBinding(gesture);

			if (b != null && b.KeyMouseEvent != null && b.KeyMouseEvent != "")
			{
				var key = b.KeyMouseEvent;
				var mod = "";
				if (b.KeyMouseEvent.Contains("+"))
				{
					var tokens = key.Split('+');
					if (tokens.Length > 1)
					{
						key = tokens[1];
						mod = tokens[0];
					}
					else if (tokens.Length > 0)
						key = tokens[0];
				}

				// First handle modifiers
				if (mod.Length > 0)
				{
					MouseKeyboardSimulator.sendKeyEvent("Left" + mod, down);
				}

				// Handle mouse buttons
				if (key == "LMB")
				{
					if (down || Fubi.getCurrentTime() - m_lastMouseClick > 1)
					{
						MouseKeyboardSimulator.sendMouseButton(MouseKeyboardSimulator.MouseButtonType.LEFT, down);
						if (!down)
							m_lastMouseClick = Fubi.getCurrentTime();
					}
				}
				else if (key == "RMB")
				{
					MouseKeyboardSimulator.sendMouseButton(MouseKeyboardSimulator.MouseButtonType.RIGHT, down);
				}
				else if (key == "MMB")
				{
					MouseKeyboardSimulator.sendMouseButton(MouseKeyboardSimulator.MouseButtonType.MIDDLE, down);
				}
				else if (key ==  "X1MB" || key == "X2MB")
				{
					MouseKeyboardSimulator.sendMouseButton(MouseKeyboardSimulator.MouseButtonType.X, down);
				}
				// Handle keys
				else if (key.Length > 0)
				{
					MouseKeyboardSimulator.sendKeyEvent(key, down);
				}
			}
		}

		public void autoCalibrateMapping(bool leftHand)
		{
			var id = Fubi.getClosestUserID();
			if (id > 0)
			{
				var elbow = FubiUtils.SkeletonJoint.RIGHT_ELBOW;
				var shoulder = FubiUtils.SkeletonJoint.RIGHT_SHOULDER;
				var hand = FubiUtils.SkeletonJoint.RIGHT_HAND;

				if (leftHand)
				{
					elbow = FubiUtils.SkeletonJoint.LEFT_ELBOW;
					shoulder = FubiUtils.SkeletonJoint.LEFT_SHOULDER;
					hand = FubiUtils.SkeletonJoint.LEFT_HAND;
				}

				float confidence;
				double timeStamp;
				float elbowX, elbowY, elbowZ;
				Fubi.getCurrentSkeletonJointPosition(id, elbow, out elbowX, out elbowY, out elbowZ, out confidence, out timeStamp);
				if (confidence > 0.5f)
				{
					float shoulderX, shoulderY, shoulderZ;
					Fubi.getCurrentSkeletonJointPosition(id, shoulder, out shoulderX, out shoulderY, out shoulderZ, out confidence, out timeStamp);
					if (confidence > 0.5f)
					{
						var dist1 = Math.Sqrt(Math.Pow(elbowX - shoulderX, 2) + Math.Pow(elbowY - shoulderY, 2) + Math.Pow(elbowZ - shoulderZ, 2));
						float handX, handY, handZ;
						Fubi.getCurrentSkeletonJointPosition(id, hand, out handX, out handY, out handZ, out confidence, out timeStamp);
						if (confidence > 0.5f)
						{
							var dist2 = Math.Sqrt(Math.Pow(elbowX - handX, 2) + Math.Pow(elbowY - handY, 2) + Math.Pow(elbowZ - handZ, 2));
							MapH = (float)(dist1 + dist2);
							// Calculate all others in depence of maph
							MapY = 250.0f / 550.0f * MapH;
							MapW = MapH / m_aspect;
							MapX = -100.0f / (550.0f / m_aspect) * MapW;
						}
					}
				}
			}
		}

		public float SmoothingFactor
		{
			get { return m_smoothingFactor; }
			set
			{
				m_smoothingFactor = value;
				if (m_smoothingFactor > 1.0f)
					m_smoothingFactor = 1.0f;
				if (m_smoothingFactor < 0.00001f)
					m_smoothingFactor = 0.00001f;
			}
		}
		public float MapH
		{
			get { return m_mapH; }
			set { m_mapH = value; }
		}

		public float MapW
		{
			get { return m_mapW; }
			set { m_mapW = value; }
		}

		public float MapY
		{
			get { return m_mapY; }
			set { m_mapY = value; }
		}

		public float MapX
		{
			get { return m_mapX; }
			set { m_mapX = value; }
		}

		float m_mapX, m_mapY, m_mapW, m_mapH;
		float m_x, m_y;
		readonly float m_aspect;
		float m_smoothingFactor;
		double m_timeStamp;
		double m_lastMouseClick;
	}
}

