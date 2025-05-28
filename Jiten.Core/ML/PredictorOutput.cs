using Microsoft.ML.Data;

namespace Jiten.Cli.ML;

public class PredictorOutput
{
    [ColumnName("variable")]
    public float[] PredictedDifficultyArray { get; set; }

    public float PredictedDifficulty => PredictedDifficultyArray is { Length: > 0 }
        ? PredictedDifficultyArray[0]
        : float.NaN;
}