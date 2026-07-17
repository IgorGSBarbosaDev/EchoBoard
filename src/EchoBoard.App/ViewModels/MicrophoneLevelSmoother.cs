namespace EchoBoard.App.ViewModels;

public sealed class MicrophoneLevelSmoother
{
    private static readonly TimeSpan AttackTime = TimeSpan.FromMilliseconds(20);
    private static readonly TimeSpan ReleaseTime = TimeSpan.FromMilliseconds(120);
    private const double SilenceThreshold = 0.005;

    public double Value { get; private set; }

    public double Update(double target, TimeSpan elapsed)
    {
        target = double.IsFinite(target) ? Math.Clamp(target, 0.0, 1.0) : 0.0;
        if (elapsed <= TimeSpan.Zero)
        {
            return Value;
        }

        var responseTime = target > Value ? AttackTime : ReleaseTime;
        var blend = 1.0 - Math.Exp(-elapsed.TotalSeconds / responseTime.TotalSeconds);
        Value += (target - Value) * blend;

        if (target == 0.0 && Value < SilenceThreshold)
        {
            Value = 0.0;
        }

        return Value;
    }

    public void Reset(double value = 0.0)
    {
        Value = double.IsFinite(value) ? Math.Clamp(value, 0.0, 1.0) : 0.0;
    }
}
