using System.Threading.Tasks;
using CsvLoader.Editor;
using UnityEngine;

namespace core
{
    [CreateAssetMenu(fileName = "SimplePipeline", menuName = "CsvLoader/Pipeline/SimplePipeline")]
    public class SimplePipeline : CsvLoadingPipeline
    {
        protected override Task ParsingPostProcess() => Task.CompletedTask;
    }
}