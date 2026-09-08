using DTBK.Domain;

namespace DTBK.Engine;

/// <summary>Application composition boundary for transport. GUI supplies requests; only this coordinator resolves/calculates transport before entering CalculationStateEngine.</summary>
public sealed class TransportCalculationCoordinator
{
    private readonly TransportEngine _transport;
    private readonly CalculationStateEngine _calculation;

    public TransportCalculationCoordinator(TransportEngine transport, CalculationStateEngine calculation)
    {
        _transport = transport ?? throw new ArgumentNullException(nameof(transport));
        _calculation = calculation ?? throw new ArgumentNullException(nameof(calculation));
    }

    public CalculationState Calculate(CalculationRequest request, IReadOnlyDictionary<long, TransportRequest> transportRequests)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(transportRequests);
        var transportByItem = new Dictionary<long, decimal>();
        foreach (var pair in transportRequests)
            transportByItem[pair.Key] = _transport.Calculate(pair.Value).TotalCost;
        return _calculation.Calculate(request with { TransportByItem = transportByItem });
    }
}
