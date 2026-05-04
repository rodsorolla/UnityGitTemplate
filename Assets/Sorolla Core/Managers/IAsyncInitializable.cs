using System.Threading;
using Cysharp.Threading.Tasks;

namespace Sorolla
{
    /// <summary>
    /// Marker for managers in <see cref="GameManager"/>'s <c>_gameManagers</c> array
    /// that require async initialization (network fetch, file I/O, etc.). GameManager
    /// awaits <see cref="InitializeAsync"/> in order before proceeding to the next manager.
    ///
    /// A manager that implements this interface does NOT also get its sync
    /// <see cref="SorollaManager.Init"/> called — the async path is exclusive.
    /// </summary>
    public interface IAsyncInitializable
    {
        UniTask InitializeAsync(CancellationToken ct);
    }
}
