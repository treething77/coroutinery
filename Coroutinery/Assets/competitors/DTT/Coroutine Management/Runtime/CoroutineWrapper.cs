using System.Collections;
using UnityEngine;

namespace DTT.Utils.CoroutineManagement
{
	/// <summary>
	/// Wrapper class for <see cref="Coroutine"/>.
	/// This class will contain extra data for coroutines.
	/// This is also the main object that will be used by <see cref="CoroutineManager"/>.
	/// </summary>
	public class CoroutineWrapper : CoroutineWrapperBase
	{
		/// <summary>
		/// The yield instruction used for waiting in the coroutine.
		/// <!--This instruction is used just for instructions from Unity API, 
		/// these can't be inherited from. 
		/// That's why these have a specific class to be handled in.-->
		/// </summary>
		public readonly YieldInstruction yieldInstruction;

		/// <summary>
		/// Will create a new CoroutineWrapper object.
		/// This should generally only happen by <see cref="CoroutineManager"/>.
		/// </summary>
		/// <param name="yieldInstruction">The instruction used for this coroutine.</param>
		public CoroutineWrapper(YieldInstruction yieldInstruction) => this.yieldInstruction = yieldInstruction;

		/// <summary>
		/// Waits on the yield instruction.
		/// </summary>
		/// <returns>The yield instruction.</returns>
		protected override IEnumerator WaitOnYield()
		{
			yield return yieldInstruction;
		}		
	}
}