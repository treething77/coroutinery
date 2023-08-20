#if TEST_FRAMEWORK

using UnityEngine;

namespace DTT.Utils.CoroutineManagement.Tests
{
	/// <summary>
	/// Monobehaviour that's meant to be destroyed in <see cref="Test_CoroutineManager.Test_CoroutineManager_CleanseFinishedCallbackTargets"/>.
	/// Here it destroys this method to invoke Unity's garabge collection.
	/// </summary>
	public class TestMono : MonoBehaviour
	{
		public void Foo() => Debug.Log(name);
	}
}

#endif
