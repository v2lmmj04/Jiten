using Microsoft.ML.Data;

namespace Jiten.Cli.ML;

public class PredictorInput
{
    [ColumnName("float_input")]
    [VectorType(39)]
    public float[] Features { get; set; }
}