using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using FubiNET;

namespace Fubi_WPF_GUI.FubiXMLGenerator
{
	class CombinationXMLGenerator : RecognizerXMLGenerator
	{
		protected XMLGenerator.CombinationOptions CombOptions;

		public CombinationXMLGenerator(string name, MainWindow window, XMLGenerator.CombinationOptions options)
			: base(name, window, XMLGenerator.RecognizerType.Combination)
		{
			CombOptions = options;
		}

		private FubiUtils.RecognitionResult recognizeGesture(string recognizerName, uint targetID)
		{
			return UseHand
				? Fubi.recognizeGestureOnHand(recognizerName, targetID)
				: Fubi.recognizeGestureOn(recognizerName, targetID);
		}

		// Check what gestures the user is performing during the given time and take the one kept for the longest duration
		List<string> recordLongestGestures(Stopwatch timer, TimeSpan duration, CancellationToken ct)
		{
			var longestGestures = new List<string>();
			var recordedGestureDurations = new Dictionary<uint, double>();
            var id = getTargetID();
			
			double lastTime = 0;
			do
			{
				if (ct.IsCancellationRequested)
				{
					setDescription("Cancelled!");
					return longestGestures;
				}

				var currentTime = timer.ElapsedMilliseconds / 1000.0;
				var timeStep = currentTime - lastTime;
				lastTime = currentTime;
				setTimer(Math.Round(currentTime, 1));

				for (uint p = 0; p < Fubi.getNumUserDefinedRecognizers(); p++)
				{
					FubiUtils.RecognitionResult res;
					lock (MainWindow.LockFubiUpdate)
					{
						res = recognizeGesture(Fubi.getUserDefinedRecognizerName(p), id);
					}
					if (res == FubiUtils.RecognitionResult.RECOGNIZED)
					{
						if (recordedGestureDurations.ContainsKey(p))
							recordedGestureDurations[p] += timeStep;
						else
							recordedGestureDurations.Add(p, timeStep);
						setGestureLabel(Fubi.getUserDefinedRecognizerName(p));
					}
				}
				Thread.Sleep(10);
			} while (timer.Elapsed < duration);

			if (recordedGestureDurations.Count != 0)
			{
				// Find gestures that were recognized for at least half the duration
                var halfDuration = duration.TotalSeconds / 2.0;
				longestGestures.AddRange(from gesture in recordedGestureDurations where gesture.Value > halfDuration select Fubi.getUserDefinedRecognizerName(gesture.Key));
				if (longestGestures.Count == 0)
                {
                    // Add at least the gesture id with the longest duration in the dictionary
				    var gestureId = recordedGestureDurations.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
                    longestGestures.Add(Fubi.getUserDefinedRecognizerName(gestureId));
                }
			}

			return longestGestures;
		}

		public void trainGestures(CancellationToken ct)
		{
			setDescription("Perform gesture for state 0");
			for (var i = 0; i < CombOptions.States.Count; ++i)
			{
				var state = CombOptions.States[i];
				var lastGestures = (i > 0) ? CombOptions.States[i - 1].Recognizers : null;

				// TODO: better way to calculate the recording duration?
				var recordingDuration = TimeSpan.FromSeconds((state.MaxDuration >= 0) ? state.MaxDuration : state.MinDuration);
				var stopwatch = Stopwatch.StartNew();
				var gestures = recordLongestGestures(stopwatch, recordingDuration, ct);
				if (ct.IsCancellationRequested)
					return;
                if (gestures.Count == 0 || String.IsNullOrEmpty(gestures.First()))
				{
					--i;
					setDescription("No gestures detected! Retry " + (i + 1));
				}
                else if (gestures == lastGestures)
				{
					--i;
					setDescription("Last state" + i + " continued! Repeat " + (i + 1));
				}
				else
				{
					state.Recognizers = gestures;
					// Wait for the transition time
					if ((i + 1) < CombOptions.States.Count)
					{
						setDescription("Waiting for transition...");
						stopwatch.Restart();
						while (stopwatch.Elapsed < TimeSpan.FromSeconds(state.TimeForTransition))
						{
							setTimer(Math.Round((((double) stopwatch.ElapsedMilliseconds)/1000), 1));
							Thread.Sleep(10);
						}
						setDescription("Perform gesture for state " + (i + 1));
					}
				}
			}
		}

		public void trainTimes(CancellationToken ct)
		{
			for (var i = 0; i < CombOptions.States.Count; i++)
			{
				var nextState = (i+1 < CombOptions.States.Count) ? CombOptions.States[i+1] : null;
				var nextRecognizers = (nextState != null) ? nextState.Recognizers : null;
				var currentState = CombOptions.States[i];
				var currentRecognizers = currentState.Recognizers;
                var id = getTargetID();

				// Wait until the gesture is first performed
				setDescription(currentRecognizers.Aggregate("Please perform gesture(s):", (current, rec) => current + (" " + rec)));

                var recognized = true;
                do
                {
					if (ct.IsCancellationRequested)
					{
						setDescription("Cancelled!");
						return;
					}
					lock (MainWindow.LockFubiUpdate)
					{
						if (currentRecognizers.Any(rec => recognizeGesture(rec, id) != FubiUtils.RecognitionResult.RECOGNIZED))
							recognized = false;
	                }
	                Thread.Sleep(10);
                } while (recognized);

				var stateDuration = new Stopwatch();
				var stayInCurrentState = true;
				while (stayInCurrentState)
				{
					if (ct.IsCancellationRequested)
					{
						setDescription("Cancelled!");
						return;
					}
					setDescription(currentRecognizers.Aggregate("State: " + i + " - perform gesture(s): ", (current, rec) => current + (" " + rec)));

					// Wait until the current recognizer isn't fulfilled anymore
					stateDuration.Start();
                    recognized = true;
                    do
                    {
						if (ct.IsCancellationRequested)
						{
							setDescription("Cancelled!");
							return;
						}
						lock (MainWindow.LockFubiUpdate)
						{
							if (currentRecognizers.Any(rec => recognizeGesture(rec, id) != FubiUtils.RecognitionResult.RECOGNIZED))
								recognized = false;
                        }
						setTimer(Math.Round((((double)stateDuration.ElapsedMilliseconds) / 1000), 1));
						Thread.Sleep(10);
                    } while (recognized);
					stateDuration.Stop();

					// Interruption of the current gesture, so check for transition or end
					var transitionDuration = new Stopwatch();
					transitionDuration.Start();
					if (nextState != null) // Check for the next state
					{
						setDescription(nextRecognizers.Aggregate("Transition to " + (i + 1) + " - perform gesture(s):", (current, rec) => current + (" " + rec)));
						bool nextRecognizerTriggered;
                        recognized = true;
                        do
                        {
							if (ct.IsCancellationRequested)
							{
								setDescription("Cancelled!");
								return;
							}
							lock (MainWindow.LockFubiUpdate)
							{
								if (currentRecognizers.Any(rec => recognizeGesture(rec, id) != FubiUtils.RecognitionResult.RECOGNIZED))
									recognized = false;
                            }
                            setTimer(Math.Round((((double)transitionDuration.ElapsedMilliseconds) / 1000), 1));
	                        lock (MainWindow.LockFubiUpdate)
	                        {
		                        nextRecognizerTriggered =
			                        nextRecognizers.All(
										rec => recognizeGesture(rec, id) == FubiUtils.RecognitionResult.RECOGNIZED);
	                        }
	                        Thread.Sleep(5);
                        } while (!recognized && !nextRecognizerTriggered);

						if (nextRecognizerTriggered)
						{
							stayInCurrentState = false;
							transitionDuration.Stop();
							// Tolerance values will be added/substracted when generating the XML
							currentState.MinDuration = currentState.MaxDuration = ((double)stateDuration.ElapsedMilliseconds / 1000);
							currentState.TimeForTransition = ((double)transitionDuration.ElapsedMilliseconds / 1000);
						}
					}
					else // Already in last state, so only wait a bit whether the user continues with the gesture
					{
						setDescription("Finishing - continue gesture or stop if ready");
						var watch = Stopwatch.StartNew();
						// TODO: good value for the max interruption?
						if (recordLongestGestures(watch, TimeSpan.FromSeconds(0.05), ct) != currentRecognizers)
						{
							// Tolerance values will be added/substracted when generating the XML
							currentState.MinDuration = currentState.MaxDuration = ((double)watch.ElapsedMilliseconds / 1000);
							stayInCurrentState = false;
						}
					}
				}
			}
		}

		public void trainGesturesAndTimes(CancellationToken ct)
		{
			var i = 0;
			var durationWatch = new Stopwatch();
			while (i < CombOptions.States.Count)
			{
				if (ct.IsCancellationRequested)
				{
					setDescription("Cancelled!");
					return;
				}
				var currentState = CombOptions.States[i];
                var id = getTargetID();

				setDescription("State: " + i + " - perform gestures");				
				
				var currentGestures = new List<string>();
				while (currentGestures.Count == 0 || String.IsNullOrEmpty(currentGestures.First()))
				{
					if (ct.IsCancellationRequested)
					{
						setDescription("Cancelled!");
						return;
					}
					durationWatch.Restart();
					// TODO what is a good time for finding the initial gesture?
					currentGestures = recordLongestGestures(durationWatch, TimeSpan.FromSeconds(0.2), ct);
				}

				var stayInCurrentState = true;
				while (stayInCurrentState)
				{
					if (ct.IsCancellationRequested)
					{
						setDescription("Cancelled!");
						return;
					}
					setDescription(currentGestures.Aggregate("State: " + i + " - perform gesture(s): ", (current, rec) => current + (" " + rec)));
					durationWatch.Start();
                    var recognized = true;
                    do
                    {
						if (ct.IsCancellationRequested)
						{
							setDescription("Cancelled!");
							return;
						}
						lock (MainWindow.LockFubiUpdate)
						{
							if (currentGestures.Any(rec => recognizeGesture(rec, id) != FubiUtils.RecognitionResult.RECOGNIZED))
								recognized = false;
                        }
                        setTimer(Math.Round((((double)durationWatch.ElapsedMilliseconds) / 1000), 1));
                        Thread.Sleep(10);
                    } while (recognized);
					durationWatch.Stop();

					var transitionWatch = new Stopwatch();
					if (i + 1 < CombOptions.States.Count)
					{
						setDescription("Transitioning" + " - perform next gesture(s)");
						var nextGestures = new List<string>();
						while (nextGestures.Count == 0 || String.IsNullOrEmpty(nextGestures.First()))
						{
							if (ct.IsCancellationRequested)
							{
								setDescription("Cancelled!");
								return;
							}
							transitionWatch.Restart();
							// TODO what is a good time for finding the next gesture?
							nextGestures = recordLongestGestures(transitionWatch, TimeSpan.FromSeconds(0.1), ct);
						}
						if (nextGestures != currentGestures)
						{
							transitionWatch.Stop();
							currentState.Recognizers = currentGestures;
							currentState.MinDuration = currentState.MaxDuration = ((double)durationWatch.ElapsedMilliseconds) / 1000;
							currentState.TimeForTransition = ((double)transitionWatch.ElapsedMilliseconds) / 1000;
							stayInCurrentState = false;
							i++;
						}
					}
					else
					{
						setDescription("Finishing - continue gesture(s) or stop if ready");
						transitionWatch.Start();
						// TODO: good value for the max interruption?
						if (recordLongestGestures(transitionWatch, TimeSpan.FromSeconds(0.1), ct) != currentGestures)
						{
							currentState.Recognizers = currentGestures;
							// Tolerance values will be added/substracted when generating the XML
							currentState.MinDuration = currentState.MaxDuration = ((double)durationWatch.ElapsedMilliseconds / 1000);
							currentState.TimeForTransition = -1;
							i++;
							break;
						}
					}
				}
			}
		}

		public override void trainValues(double start, double duration, CancellationToken ct)
		{
			// None means we don't train anything, but the user has already provided all information
			if (CombOptions.TrainType != XMLGenerator.CombinationTrainingType.None)
			{
				waitForStart(start, ct);

				switch (CombOptions.TrainType)
				{
					case XMLGenerator.CombinationTrainingType.Gestures:
						trainGestures(ct);
						break;
					case XMLGenerator.CombinationTrainingType.Times:
						trainTimes(ct);
						break;
					case XMLGenerator.CombinationTrainingType.GesturesAndTimes:
						trainGesturesAndTimes(ct);
						break;
				}
				// Reset gesture label
				setGestureLabel("");
			}
		}

		protected override void generateXML()
		{
			for (var i = 0; i < CombOptions.States.Count; i++)
			{
				if (CombOptions.States[i].Recognizers.Count > 0) // Skip emtpy states
				{
					var stateNode = Doc.CreateElement("State", NamespaceUri);

					foreach (var recognizer in CombOptions.States[i].Recognizers)
					{
						var recNode = Doc.CreateElement("Recognizer", NamespaceUri);
						appendStringAttribute(recNode, "name", recognizer);
						stateNode.AppendChild(recNode);
					}

					var minDuration = CombOptions.States[i].MinDuration - CombOptions.TimeTolerance;
                    if (minDuration > 0 && CombOptions.TimeToleranceType != XMLGenerator.ToleranceTypeString[(int)XMLGenerator.ToleranceType.Lesser])
					{
						appendNumericAttribute(stateNode, "minDuration", minDuration, 3);
					}
					if (CombOptions.States[i].MaxDuration >= 0 && CombOptions.TimeToleranceType != XMLGenerator.ToleranceTypeString[(int)XMLGenerator.ToleranceType.Greater])
					{
						appendNumericAttribute(stateNode, "maxDuration", CombOptions.States[i].MaxDuration + CombOptions.TimeTolerance, 3);
					}
					if (i != CombOptions.States.Count - 1 && CombOptions.States[i].TimeForTransition >= 0)
					{
						appendNumericAttribute(stateNode, "timeForTransition", CombOptions.States[i].TimeForTransition + CombOptions.TransitionTolerance, 3);
					}
					RecognizerNode.AppendChild(stateNode);
				}
			}
		}
	}
}
