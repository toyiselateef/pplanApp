using MfgDocs.Api.Models;
using Microsoft.Extensions.Options;

namespace MfgDocs.Api.Services;

public class SizeCalculator
{
    private static readonly int[] FormSizes = new[] { 18, 20, 22, 24, 26, 28, 30 };

    public Dimension ComputePouredSize(WorkOrderLine line)
    {
        var w = line.FinishedSize.WidthInches;
        var l = line.FinishedSize.LengthInches;

        if (line.FinishType == FinishType.SmoothFace)
        {
            return new Dimension(w + 2, l + 2, line.FinishedSize.ThicknessInches);
        }

        // RockFace logic
        // Short side selects next-up form size from the available set; if > 29 then add 2 and round to next even
        var shortSide = Math.Min(w, l);
        var longSide = Math.Max(w, l);

        decimal pouredShort;
        var nextForm = FormSizes.FirstOrDefault(s => s >= shortSide + 2); // since examples: 16 -> use 18 form
        if (nextForm == 0)
        {
            pouredShort = ToNextEven(shortSide + 2);
        }
        else
        {
            pouredShort = nextForm;
        }

        // Long side addition depends on sides rocked
        decimal addLong = 1.5m;
        if (line.RockFaceSides is RockFaceSides.TwoLong or RockFaceSides.TwoLongOneShort)
        {
            addLong = 1.0m;
        }
        var pouredLong = longSide + addLong;

        // Reassign to keep orientation (Width x Length) roughly as provided
        if (w <= l)
            return new Dimension(pouredShort, pouredLong, line.FinishedSize.ThicknessInches);
        else
            return new Dimension(pouredLong, pouredShort, line.FinishedSize.ThicknessInches);
    }

    private static decimal ToNextEven(decimal value)
    {
        var ceil = Math.Ceiling(value);
        return ceil % 2 == 0 ? ceil : ceil + 1;
    }
}

public class WeightCalculator
{
    private readonly PricingOptions _opts;
    public WeightCalculator(IOptions<PricingOptions> opts) => _opts = opts.Value;

    public decimal ComputeUnitWeight(Dimension poured) =>
        Math.Round(poured.WidthInches * poured.LengthInches * _opts.WeightFactor, 2);
}

public class PricingCalculator
{
    private readonly PricingOptions _opts;
    public PricingCalculator(Microsoft.Extensions.Options.IOptions<PricingOptions> opts) => _opts = opts.Value;

    public PouredResult Compute(WorkOrderLine line, Dimension poured, decimal unitWeight)
    {
        var shortSide = Math.Min(poured.WidthInches, poured.LengthInches);
        var requiresRebar = poured.WidthInches > _opts.RebarLengthThresholdInches || poured.LengthInches > _opts.RebarLengthThresholdInches;

        decimal baseRate;
        if (line.FinishType == FinishType.SmoothFace)
        {
            // use large rate for smooth as approximation, then add smooth surcharge
            baseRate = _opts.RockfaceRateLarge;
        }
        else
        {
            baseRate = shortSide > _opts.LargeThresholdShortSideInches ? _opts.RockfaceRateLarge : _opts.RockfaceRateSmall;
        }

        var unitPrice = poured.WidthInches * poured.LengthInches * baseRate;

        if (requiresRebar)
        {
            unitPrice += _opts.RebarCost;
        }
        if (line.FinishType == FinishType.SmoothFace)
        {
            unitPrice += _opts.SmoothFaceSurcharge;
        }

        unitPrice = RoundTo(unitPrice, _opts.RoundTo);
        var lineTotal = RoundTo(unitPrice * line.Quantity, _opts.RoundTo);

        return new PouredResult(poured, requiresRebar, unitWeight, unitPrice, lineTotal);
    }

    private static decimal RoundTo(decimal value, decimal increment)
    {
        var factor = 1 / increment;
        return Math.Round(value * factor, MidpointRounding.AwayFromZero) / factor;
    }
}