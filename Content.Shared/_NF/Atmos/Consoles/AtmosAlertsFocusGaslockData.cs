using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Components;

[Serializable, NetSerializable]
public struct AtmosAlertsFocusGaslockData
{
    /// <summary>
    /// Focus entity
    /// </summary>
    public NetEntity NetEntity;

    /// <summary>
    /// Requested pump pressure in kPa
    /// </summary>
    public float Pressure;

    /// <summary>
    /// Direction of the pump: true if pumping inwards
    /// </summary>
    public bool PumpingInwards;

    /// <summary>
    /// Whether or not the pump is running
    /// </summary>
    public bool Enabled;

    /// <summary>
    /// The entity the gaslock is docked with
    /// </summary>
    public NetEntity DockedEntity;

    /// <summary>
    /// Moles, percentage, and related alert state, for all detected gases
    /// </summary>
    public Dictionary<Gas, (float, float)> GasData;

    /// <summary>
    /// Populates the atmos monitoring console focus entry with atmospheric data
    /// </summary>
    public AtmosAlertsFocusGaslockData
        (NetEntity netEntity,
        float pressure,
        bool pumpingInwards,
        bool enabled,
        NetEntity dockedEntity,
        Dictionary<Gas, (float, float)> gasData)
    {
        NetEntity = netEntity;
        Pressure = pressure;
        PumpingInwards = pumpingInwards;
        Enabled = enabled;
        DockedEntity = dockedEntity;
        GasData = gasData;
    }
}
