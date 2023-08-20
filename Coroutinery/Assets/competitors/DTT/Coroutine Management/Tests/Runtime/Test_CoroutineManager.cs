#if TEST_FRAMEWORK

using DTT.Utils.CoroutineManagement.CustomYieldInstructions;
using DTT.Utils.CoroutineManagement.Exceptions;
using NUnit.Framework;
using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.TestTools;

using Object = UnityEngine.Object;

namespace DTT.Utils.CoroutineManagement.Tests
{
	/// <summary>
	/// Contains all the test methods for testing the functionality of 
	/// the different features of the <see cref="CoroutineManager"/>.
	/// </summary>
	public class Test_CoroutineManager
	{
		/// <summary>
		/// The precision of the WaitForSeconds callback.
		/// </summary>
		private const double PRECISION = 0.005f;

		/// <summary>
		/// Tests the <see cref="CoroutineManager.KickOffCoroutine(IEnumerator)">
		/// Expects the KickOffCoroutine to throw an ArgumentNullException when the passed IEnumerator is null.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_CoroutineManager_StartCoroutine_WithNullReference()
		{
			// Assert.
			Assert.That(() => CoroutineManager.KickOffCoroutine(null), Throws.TypeOf<ArgumentNullException>());
			yield return null;
		}

		/// <summary>
		/// Tests the <see cref="CoroutineManager.StopCoroutine(Coroutine)">
		/// Expects the StopCoroutine to throw an ArgumentNullException when the passed Coroutine is null.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_CoroutineManager_StopCoroutine_WithNullReference()
		{
			// Assert.
			Assert.That(() => CoroutineManager.StopCoroutine(null), Throws.TypeOf<ArgumentNullException>());
			yield return null;
		}

		// Arrange.
		// IEnumerator to start a 'user' made coroutine for tracking by CoroutineManager.
		private IEnumerator WaitForTime(float time)
		{
			yield return new WaitForSeconds(time);
		}

		/// <summary>
		/// Tests the <see cref="CoroutineManager.StartCoroutine(IEnumerator)">
		/// Expects the Users Coroutine to properly be executed and be tracked.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_CoroutineManager_StartUserCoroutine()
		{
			// Arrange.
			float seconds = 0.4f;

			// Act.
			CoroutineManager.StartCoroutine(WaitForTime(seconds));

			// Assert.
			Assert.IsTrue(CoroutineManager.GetActiveCoroutines().Count == 1);
			yield return new WaitForSeconds(seconds);
			Assert.IsTrue(CoroutineManager.GetActiveCoroutines().Count == 0);
		}

		/// <summary>
		/// Tests the <see cref="CoroutineManager.StartCoroutine(IEnumerator)">
		/// Expects the KickOffCoroutine to throw an ArgumentNullException when the passed IEnumerator is null.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_CoroutineManager_StartUserCoroutine_WithNullReference()
		{
			// Assert.
			Assert.That(() => CoroutineManager.StartCoroutine(null), Throws.TypeOf<ArgumentNullException>());
			yield return 0;
		}

		/// <summary>
		/// Tests the <see cref="CoroutineManager.WaitSeconds(float, UnityEngine.Events.UnityAction)"/> 
		/// to see whether it waits the correct amount of time before the callback.
		/// Expects the callback to happen within the defined 
		/// upper- and lowerbound second by timing it with the <see cref="Stopwatch"/>.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_CoroutineManager_WaitSeconds_GetsCalledWithInTheDuration()
		{
			// Arrange.
			// How long this will wait using the WaitSeconds.
			const double WAIT_DURATION_SECONDS = 1;
			// Define a stopwatch and record the elapsed time.
			var stopwatch = new Stopwatch();
			stopwatch.Start();

			// Act.
			// Wait for the set amount of time.
			CoroutineManager.WaitForSeconds((float) WAIT_DURATION_SECONDS, () =>
			{
				// Stop recording time.
				stopwatch.Stop();

				// Define a upper and lower bound for where we expect the elapsed time to fall in.
				double lowerBound = WAIT_DURATION_SECONDS - PRECISION;
				double upperBound = WAIT_DURATION_SECONDS + PRECISION;

				// Assert.
				// Assert whether the elapsed time falls within the predefined range.
				Assert.GreaterOrEqual(stopwatch.Elapsed.TotalSeconds, lowerBound,
					$"Coroutine manager Wait method took longer to finish " +
					$"than expected. Elapsed: {stopwatch.Elapsed.TotalSeconds}");

				Assert.LessOrEqual(stopwatch.Elapsed.TotalSeconds, upperBound,
					$"Coroutine manager Wait method took longer to finish " +
					$"than expected. Elapsed: {stopwatch.Elapsed.TotalSeconds}");
			});

			// Define a delay that tests will wait until since the callback 
			// from the CoroutineManager is not expected to happen at that exact time.
			float delay = 0.2f;

			// Wait the set amount of time plus some extra.
			yield return new WaitForSeconds((float) WAIT_DURATION_SECONDS + delay);
		}

		/// <summary>
		/// Tests whether coroutines display their correct status values.
		/// Expects that the correct <c>HasStarted, InProgress, HasStopped, HasFinished</c> flags are set.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_CoroutineManager_StatusChecks()
		{
			var coroutine = CoroutineManager.WaitForEndOfFrame(() => { });
			Assert.IsTrue(coroutine.HasStarted);
			Assert.IsTrue(coroutine.InProgress);
			Assert.IsFalse(coroutine.HasStopped);
			Assert.IsFalse(coroutine.HasFinished);
			yield return new WaitForEndOfFrame();
			Assert.IsTrue(coroutine.HasStarted);
			Assert.IsTrue(coroutine.HasFinished);
			Assert.IsFalse(coroutine.InProgress);
			Assert.IsFalse(coroutine.HasStopped);

			coroutine = CoroutineManager.WaitForSeconds(0.1f, () => { });
			yield return new WaitForEndOfFrame();
			coroutine.Stop();
			Assert.IsTrue(coroutine.HasStarted);
			Assert.IsTrue(coroutine.HasStopped);
			Assert.IsTrue(coroutine.InProgress);
			Assert.IsFalse(coroutine.HasFinished);
		}

		/// <summary>
		/// Tests whether the WaitUntil method fires when predicate is true.
		/// It expects the WaitUntil callback to happen after waiting 0.1f and updating the predicate.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_CoroutineManager_WaitUntil_ChangesAfterPredicateSwitches()
		{
			// Arrange.
			bool finished = false;
			void OnFinish() => finished = true;
			bool condition = false;

			// Act.
			var coroutine = CoroutineManager.WaitUntil(() => condition, OnFinish);
			yield return null;

			// Assert.
			Assert.IsFalse(finished, "Finish can't be be true after a frame.");
			yield return new WaitForSeconds(0.1f);
			condition = true;
			yield return new WaitForSeconds(0.1f);
			Assert.IsTrue(finished,
				"After switching predicate to true the callback still isn't finished.");
		}

		/// <summary>
		/// Tests whether the WaitUntil method callback fires with the correct time out time.
		/// Expects the timeout to be called when a predicate won't be set to true within the set time frame.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_CoroutineManager_WaitUntilWithTimeout_UsingTimeout()
		{
			// Arrange.
			bool finished = false;
			void OnFinish() => finished = true;
			bool condition = false;

			// Act.
			var coroutine = CoroutineManager.WaitUntil(() => condition, 1f, OnFinish);

			// Assert.
			yield return new WaitForSeconds(.9f);
			Assert.IsFalse(finished, "Coroutine shouldn't be finished, but the callback has already happened.");
			yield return new WaitForSeconds(.2f);
			Assert.IsTrue(finished, "After waiting for the timeout of the coroutine it still didn't get the callback.");
		}

		/// <summary>
		/// Tests whether the WaitUntil method callback fires before the time out according to the predicate.
		/// Expects the callback to happen when the predicate switches to true.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_CoroutineManager_WaitUntilWithTimeout_UsingPredicate()
		{
			// Arrange
			bool finished = false;
			void OnFinish() => finished = true;
			bool condition = false;

			// Act.
			var coroutine = CoroutineManager.WaitUntil(() => condition, OnFinish);
			yield return new WaitForSeconds(0.1f);
			condition = true;
			yield return null;

			// Assert.
			Assert.IsTrue(finished,
				"After switching predicate to true the callback still isn't finished.");
		}

		/// <summary>
		/// Tests whether the WaitForCustomeYield method fires when predicate is true.
		/// It expects the WaitForCustomeYield callback to happen after waiting 0.1f and updating the predicate.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_CoroutineManager_WaitForCustomeYield_ChangesAfterPredicateSwitches()
		{
			// Arrange.
			bool finished = false;
			void OnFinish() => finished = true;
			bool condition = false;
			WaitUntilTimeout customeYield = new WaitUntilTimeout(() => condition);

			// Act.
			var coroutine = CoroutineManager.WaitForCustomYield(customeYield, OnFinish);

			// Assert.
			yield return null;
			Assert.IsFalse(finished, "Finish can't be be true after a frame.");
			yield return new WaitForSeconds(0.1f);
			condition = true;
			yield return new WaitForSeconds(0.1f);
			Assert.IsTrue(finished,
				"After switching predicate to true the callback still isn't finished.");
		}

		/// <summary>
		/// Tests whether the WaitForCustomeYield method callback fires with the correct time out time.
		/// Expects the timeout to be called when a predicate won't be set to true within the set time frame.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_CoroutineManager_WaitForCustomeYield_UsingTimeout()
		{
			// Arrange.
			bool finished = false;
			void OnFinish() => finished = true;
			bool condition = false;
			WaitUntilTimeout customeYield = new WaitUntilTimeout(() => condition, 1f);

			// Act.
			var coroutine = CoroutineManager.WaitForCustomYield(customeYield, OnFinish);

			// Assert.
			yield return new WaitForSeconds(.9f);
			Assert.IsFalse(finished, "Coroutine shouldn't be finished, but the callback has already happened.");
			yield return new WaitForSeconds(.2f);
			Assert.IsTrue(finished, "After waiting for the timeout of the coroutine it still didn't get the callback.");
		}

		/// <summary>
		/// Tests whether the WaitForCustomeYield method callback fires before the time out according to the predicate.
		/// Expects the callback to happen when the predicate switches to true.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_CoroutineManager_WaitForCustomeYield_UsingPredicate()
		{
			// Arrange.
			bool finished = false;
			void OnFinish() => finished = true;
			bool condition = false;
			WaitUntilTimeout customeYield = new WaitUntilTimeout(() => condition);

			// Act.
			var coroutine = CoroutineManager.WaitForCustomYield(customeYield, OnFinish);
			yield return new WaitForSeconds(0.1f);
			condition = true;

			// Assert.
			yield return null;
			Assert.IsTrue(finished,
				"After switching predicate to true the callback still isn't finished.");
		}

		/// <summary>
		/// Tests whether the callback actually happens on the same frame.
		/// Expects <see cref="Time.frameCount"/> to be the same as when it got called.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_CoroutineManager_WaitForEndOfFrame_GetsCalledInTheSameFrame()
		{
			// Arrange.
			int startFrameCount = Time.frameCount;
			int? finishFrameCount = null;

			// Act.
			CoroutineManager.WaitForEndOfFrame(() => finishFrameCount = Time.frameCount);
			yield return new WaitUntil(() => finishFrameCount.HasValue);

			// Assert.
			Assert.AreEqual(startFrameCount, finishFrameCount,
				"Expected the starting frame count to be equal to that " +
				"of the finish frame count, since it should happen on the same frame.");
		}

		/// <summary>
		/// Tests whether stopping a coroutine actually doesn't make it return a callback.
		/// Expects no callback to be returned.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_CoroutineManager_Stop_ActuallyDoesStop()
		{
			// Arrange.
			bool finished = false;
			var coroutine = CoroutineManager.WaitForEndOfFrame(() => finished = true);

			// Act.
			coroutine.Stop();

			// Assert.
			yield return new WaitForSeconds(0.1f);
			Assert.IsFalse(finished, "Expected to be false since no callback should happen");
		}

		/// <summary>
		/// Tests whether an exception is thrown when stopping an already stopping coroutine.
		/// It expects <see cref="StoppingStoppedCoroutineException"/> to be thrown when trying.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_CoroutineManager_Stop_StoppedCoroutine()
		{
			// Arrange.
			var coroutine = CoroutineManager.WaitForSeconds(0.1f, () => { });

			// Act.
			coroutine.Stop();

			// Assert.
			Assert.Catch<StoppingStoppedCoroutineException>(() => coroutine.Stop(),
				"Stopping stopped coroutine doesn't throw StoppingStoppedCoroutineException.");
			yield return null;
		}

		/// <summary>
		/// Tests whether an exception is thrown when stopping an already finished coroutine.
		/// It expects <see cref="StoppingFinishedCoroutineException"/> to be thrown when trying.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_CoroutineManager_Stop_FinishedCoroutine()
		{
			// Arrange.
			var coroutine = CoroutineManager.WaitForEndOfFrame(() => { });

			// Assert.
			yield return new WaitForSeconds(0.1f);
			Assert.Catch<StoppingFinishedCoroutineException>(() => coroutine.Stop(),
				"Stopping finished coroutine doesn't throw StoppingFinishedCoroutineException.");
			yield return null;
		}

		/// <summary>
		/// Tests whether an exception is thrown when starting an already started coroutine.
		/// It expects <see cref="StartingStartedCoroutineException"/> to be thrown when trying.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_CoroutineManager_Start_StartedCoroutine()
		{
			// Arrange.
			var coroutine = CoroutineManager.WaitForEndOfFrame(() => { });

			// Assert.
			Assert.Catch<StartingStartedCoroutineException>(() => coroutine.Start(),
				"Starting started coroutine doesn't throw StartingStartedCoroutineException.");
			yield return null;
		}

		/// <summary>
		/// Tests whether a negative timeout will immediately callback.
		/// Expects that with a negative timeout the callback is within a one frame minimum.
		/// </summary>
		[Test]
		public void Test_CoroutineManager_WaitUntilTimeout_Negative()
		{
			// Arrange.
			TestDelegate action = () => CoroutineManager.WaitUntil(() => true, -1, () => { });

			// Assert.
			Assert.Catch<ArgumentException>(action,
				"Expected the negative timeout to cause an exception but it didn't.");
		}

		/// <summary>
		/// Tests subscribing to the Finished event and destroying the component that was added.
		/// Expects no errors to be thrown, if an error is thrown the test fails.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_CoroutineManager_CleanseFinishedCallbackTargets()
		{
			// Arrange.
			TestMono mono = new GameObject("To be deleted").AddComponent<TestMono>();
			CoroutineWrapper wait = CoroutineManager.WaitForSeconds(1, mono.Foo);

			// Act.
			Object.Destroy(mono);
			yield return new WaitForSeconds(1f);
		}

		/// <summary>
		/// Tests whether the coroutines are getting cleaned up after they're finished.
		/// It expects the active coroutines count to be even after starting and stopping them.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_CoroutineManager_ActiveCoroutines_CheckCoroutinesGetCleanedUpAfterFinishing()
		{
			// Arrange.
			int activeCount = CoroutineManager.GetActiveCoroutines().Count;

			// Act.
			CoroutineManager.WaitForEndOfFrame(() => { });
			CoroutineManager.WaitForEndOfFrame(() => { });

			// Assert.
			yield return new WaitForSeconds(0.1f);
			Assert.AreEqual(activeCount, CoroutineManager.GetActiveCoroutines().Count,
				"Coroutines didn't get properly clean up since the amount of " +
				"active coroutines differ even though they should be the same.");
		}

		/// <summary>
		/// Tests  whether the coroutines get cleaned up by the coroutine manager after they're stopped.
		/// It expects the active coroutine count to be equal to the initial count after 
		/// starting, stopping, and removing new coroutines.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_CoroutineManager_ActiveCoroutines_CheckCoroutinesGetCleanedUpAfterStopping()
		{
			// Arrange.
			int activeCount = CoroutineManager.GetActiveCoroutines().Count;
			var c1 = CoroutineManager.WaitForSeconds(0.2f, () => { });
			var c2 = CoroutineManager.WaitForSeconds(0.2f, () => { });

			// Act.
			yield return new WaitForSeconds(0.1f);
			c1.Stop();
			c2.Stop();

			// Assert.
			yield return new WaitForSeconds(0.1f);
			Assert.AreEqual(activeCount, CoroutineManager.GetActiveCoroutines().Count,
				"Coroutines didn't get properly clean up since the amount " +
				"of active coroutines differ even though they should be the same.");
		}

		/// <summary>
		/// Tests whether the Finished event doesn't get called after manually stopping a coroutine.
		/// It expects no callback to happen.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_CoroutineManager_FinishedEvent_FinishedDoesntGetCalledAfterStopping()
		{
			// Arrange.
			bool finishCalled = false;
			void OnFinish() => finishCalled = true;
			var c = CoroutineManager.WaitForSeconds(0.1f, () => { });
			c.Finished += OnFinish;

			// Act.
			yield return null;
			c.Stop();

			// Assert.
			yield return new WaitForSeconds(0.1f);
			Assert.IsFalse(finishCalled, "Finish got called even though the coroutine was stopped.");
		}

		/// <summary>
		/// Tests whether the coroutine manager can wait for any of a collection of conditions.
		/// It expects the first condition to trigger the callback.
		[UnityTest]
		public IEnumerator Test_WaitForAny_Conditions()
		{
			// Arrange.
			float waitTimeOne = 15f;
			float waitTimeTwo = 1f;
			float waitTimeThree = 10f;
			float currentTime = Time.time;

			Func<bool>[] conditions = new Func<bool>[]
			{
				() => Time.time >= (currentTime + waitTimeOne),
				() => Time.time >= (currentTime + waitTimeTwo),
				() => Time.time >= (currentTime + waitTimeThree),
			};

			// Act.
			bool callbacked = false;
			float delay = 0.2f;
			CoroutineManager.WaitForAny(conditions, () => callbacked = true);

			float smallest = Mathf.Min(waitTimeOne, waitTimeTwo, waitTimeThree);
			yield return new WaitForSeconds(smallest + delay);

			// Assert.
			Assert.IsTrue(callbacked,
				"Expected the callback to occur after the first condition was met but it wasn't.");
		}

		/// <summary>
		/// Tests whether the coroutine manager can wait for any of a collection of routines.
		/// It expects the first enumerator that triggers to trigger the callback.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_WaitForAny_IEnumerators()
		{
			// Arrange.
			float waitTimeOne = 15f;
			float waitTimeTwo = 3f;
			float waitTimeThree = 10f;

			IEnumerator[] routines = new IEnumerator[]
			{
				WaitForTime(waitTimeOne),
				WaitForTime(waitTimeTwo),
				WaitForTime(waitTimeThree)
			};

			// Act.
			bool callbacked = false;
			float delay = 0.2f;
			CoroutineManager.WaitForAny(routines, () => callbacked = true);

			float smallest = Mathf.Min(waitTimeOne, waitTimeTwo, waitTimeThree);
			yield return new WaitForSeconds(smallest + delay);

			// Assert.
			Assert.IsTrue(callbacked,
				"Expected the callback to occur after the first routine was finished but it wasn't.");
		}
		
		/// <summary>
		/// Tests whether the coroutine manager can wait for a collection of routines.
		/// It expects all routines to finish before calling back.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_WaitForAll_IEnumerators()
		{
			// Arrange.
			float waitTimeOne = 2f;
			float waitTimeTwo = 1f;
			float waitTimeThree = 4f;
			float currentTime = Time.time;

			IEnumerator[] routines = new IEnumerator[]
			{
				WaitForTime(waitTimeOne),
				WaitForTime(waitTimeTwo),
				WaitForTime(waitTimeThree)
			};

			// Act.
			bool callbacked = false;
			float delay = 0.2f;
			float largest = Mathf.Max(waitTimeOne, waitTimeTwo, waitTimeThree);
			CoroutineManager.WaitForAll(routines, () => callbacked = Time.time >= (currentTime + largest - delay));

			yield return new WaitForSeconds(largest + delay);


			// Assert.
			Assert.IsTrue(callbacked,
				"Expected the callback to occur after the last routine was finished but it wasn't.");
		}

		/// <summary>
		/// Tests whether the coroutine manager can wait for a collection of conditions.
		/// It expects all conditions to trigger before calling back.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_WaitForAll_Conditions()
		{
			// Arrange.
			float waitTimeOne = 2f;
			float waitTimeTwo = 1f;
			float waitTimeThree = 4f;
			float currentTime = Time.time;

			Func<bool>[] conditions = new Func<bool>[]
			{
				() => Time.time >= (currentTime + waitTimeOne),
				() => Time.time >= (currentTime + waitTimeTwo),
				() => Time.time >= (currentTime + waitTimeThree),
			};
			
			// Act.
			bool callbacked = false;
			float delay = 0.2f;
			float largest = Mathf.Max(waitTimeOne, waitTimeTwo, waitTimeThree);
			CoroutineManager.WaitForAll(conditions, () =>  callbacked = Time.time >= (currentTime + largest - delay));
			
			yield return new WaitForSeconds(largest + delay);

			// Assert.
			Assert.IsTrue(callbacked, "Expected the callback to occur after the last condition was met but it wasn't.");
		}

		/// <summary>
		/// Tests whether the coroutine manager can wait for a given ammount of frames.
		/// It expects the given amount of frames to pass before calling back.
		/// </summary>
		[UnityTest]
		public IEnumerator Test_WaitForFrames()
		{
			// Arrange.
			int frameCount = 5;
			
			// Act.
			bool callbacked = false;
			CoroutineManager.WaitForFrames(frameCount, () => callbacked = true);

			// Wait for frames to pass,
			for (int i = 0; i < frameCount; i++)
				yield return null;

			// Assert.
			Assert.IsTrue(callbacked, $"Expected the callback to occur after {frameCount} frames but it didn't.");
		}
	}
}

#endif