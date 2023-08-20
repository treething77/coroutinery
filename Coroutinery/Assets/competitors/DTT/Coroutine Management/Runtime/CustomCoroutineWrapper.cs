using System.Collections;
using UnityEngine;

namespace DTT.Utils.CoroutineManagement
{
	/// <summary>
	/// Wrapper class for <see cref="Coroutine"/>.
	/// This is also the main object that will be used by <see cref="CoroutineManager"/>.
	/// </summary>
	public class CustomCoroutineWrapper : CoroutineWrapperBase
	{
		/// <summary>
		/// The yield instruction used for waiting in the coroutine.
		/// </summary>
		public readonly IEnumerator customYieldInstruction;
		
		/// <summary>
		/// Will create a new CoroutineWrapper object.
		/// This should generally only happen by <see cref="CoroutineManager"/>.
		/// </summary>
		/// <param name="customYieldInstruction">The instruction used for this coroutine.</param>
		public CustomCoroutineWrapper(IEnumerator customYieldInstruction) 
			=> this.customYieldInstruction = customYieldInstruction;
		
		/// <summary>
		/// Waits on the yield instruction.
		/// </summary>
		/// <returns>The yield instruction.</returns>
		protected override IEnumerator WaitOnYield()
		{
			yield return customYieldInstruction;
		}
	}
}