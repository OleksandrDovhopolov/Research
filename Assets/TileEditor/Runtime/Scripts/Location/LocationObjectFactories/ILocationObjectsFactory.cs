using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Fabros.TileEditor
{
    public interface ILocationObjectsFactory
    {
        Task<LocationObject> Create(LocationObjectModel objectModel, Transform root, CancellationTokenSource cancellationTokenSource = null);
    }
}