using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DTT.CoroutineManagement.Tests")]
namespace DTT.Utils.CoroutineManagement
{
    // Used as a template without functionality to be able to expose internal members/classes to
    // other 'friend' assemblies using the 'InternalsVisibleToAttribute'.

    // DTT.CoroutineManagement.Tests using the internal CoroutineManager.KickOffCoroutine() method.
    // DTT.CoroutineManagement.Tests using the internal CoroutineManager.StopCoroutine() method.
}