using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace TileEditor
{
    public interface ILocationObjectsFactory
    {
        Task<LocationObject> Create(LocationObjectModel objectModel, Transform root, CancellationTokenSource cancellationTokenSource = null);
    }
}